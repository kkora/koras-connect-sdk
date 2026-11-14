using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>The single Salesforce REST API client foundation every other Salesforce feature builds on (SFD-003).
/// Composes the base URL as <c>{instance_url}/services/data/{version}/</c>.</summary>
public sealed partial class SalesforceClient : IConnector
{
    private readonly IntegrationTransport _transport;
    private readonly SalesforceOptions _options;
    private readonly Uri _root;
    private readonly TimeProvider _clock;

    public SalesforceClient(IntegrationTransport transport, SalesforceOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _transport = transport;
        _options = options;
        _root = new Uri(EndpointSafety.ValidateRoot(options.InstanceRoot), $"services/data/{options.ApiVersion}/");
        _clock = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Validates a <see cref="SalesforceOptions"/> instance using the same rule the constructor enforces,
    /// without requiring an <see cref="IntegrationTransport"/>. Intended for fail-fast DI registration.</summary>
    public static void ValidateOptions(SalesforceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
    }

    private static Koras.Connect.Exceptions.IntegrationException Classify(IntegrationRequestException ex)
        => SalesforceApiException.FromTransportFailure(ex);

    /// <summary>Fetches the "Resources by Version" map (SFD-003): a single authenticated, non-mutating GET on the
    /// version root itself, returning resource name to relative-URI pairs (<c>sobjects</c>, <c>query</c>, etc.).
    /// Also used as <see cref="DiagnoseAsync"/>'s one permitted probe call.</summary>
    public async Task<IReadOnlyDictionary<string, string>> GetResourceMapAsync(CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(_root, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ContractException("Resource map response was not a JSON object.", new ErrorDetails("resource_map_contract"));
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
                if (property.Value.ValueKind == JsonValueKind.String) map[property.Name] = property.Value.GetString()!;
            return map;
        }
    }

    public ConnectorDescriptor Descriptor => new("Salesforce", "Salesforce", _options.ApiVersion,
        SupportedOperations: ["GetResourceMap", "CreateRecord", "GetRecord", "UpdateRecord", "DeleteRecord", "Query", "EnumerateQuery",
            "DescribeObject", "DescribeGlobal", "UpsertByExternalId", "ExecuteComposite", "BulkIngestJob", "BulkQueryJob"],
        Entitlements: new Dictionary<string, EntitlementEvidence>());

    /// <summary>Bounded, read-only, opt-in connectivity probe. Never issues a mutating request - the only call
    /// made is <see cref="GetResourceMapAsync"/>.</summary>
    public async ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectionDiagnostic> { new(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, _clock.GetUtcNow()) };
        try
        {
            await GetResourceMapAsync(cancellationToken);
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Passed, "connected", null, _clock.GetUtcNow()));
        }
        catch (SalesforceApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured client credentials and connected app/external client app authorization."));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Skipped, "skipped_after_authentication_failure", null, _clock.GetUtcNow()));
        }
        catch (Koras.Connect.Exceptions.IntegrationException ex)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured instance URL and network connectivity."));
        }
        return results;
    }
}

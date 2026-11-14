using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>Result of the <c>WhoAmI</c> unbound function - documented as a connectivity check, not a discovery
/// mechanism (DVT-002) [D5].</summary>
public sealed record WhoAmIResult(Guid UserId, Guid BusinessUnitId, Guid OrganizationId);

/// <summary>The single Dataverse Web API client foundation every other Dataverse feature builds on (DVT-003).
/// Composes the base URL as <c>{environment-url}/api/data/v9.2/</c> [D3], requires the documented
/// <c>OData-MaxVersion</c>/<c>OData-Version</c> headers on every request, and enforces the documented 32 KB max URL
/// length and 260-character max path-segment length as local pre-send validation.</summary>
public sealed partial class DataverseClient : IConnector
{
    /// <summary>Fixed Web API version this connector targets [D3]. Isolated to one constant for a future bump;
    /// code written against this version keeps working on later minor versions per Microsoft's own compatibility
    /// policy, but new capabilities are not backported to earlier minor versions.</summary>
    public const string ApiVersion = "v9.2";

    private const int MaxUrlLength = 32 * 1024;
    private const int MaxBatchUrlLength = 64 * 1024;
    private const int MaxSegmentLength = 260;

    private static readonly IReadOnlyDictionary<string, string> RequiredHeaders = new Dictionary<string, string>
    {
        ["OData-MaxVersion"] = "4.0",
        ["OData-Version"] = "4.0",
    };

    private readonly IntegrationTransport _transport;
    private readonly DataverseOptions _options;
    private readonly Uri _root;
    private readonly TimeProvider _clock;

    public DataverseClient(IntegrationTransport transport, DataverseOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _transport = transport;
        _options = options;
        _root = new Uri(EndpointSafety.ValidateRoot(options.EnvironmentRoot), $"api/data/{ApiVersion}/");
        _clock = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Validates a <see cref="DataverseOptions"/> instance using the same rule the constructor enforces,
    /// without requiring an <see cref="IntegrationTransport"/>. Intended for fail-fast DI registration.</summary>
    public static void ValidateOptions(DataverseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
    }

    /// <summary>Confines a request address to the documented URL-length bound (widened inside <c>$batch</c>) and
    /// rejects any individual path segment over the documented 260-character bound, before any HTTP attempt.</summary>
    private static void ValidateRequestUri(Uri address, bool insideBatch = false)
    {
        var limit = insideBatch ? MaxBatchUrlLength : MaxUrlLength;
        if (address.AbsoluteUri.Length > limit)
            throw new ArgumentException($"Request URL exceeds the documented {limit} byte bound.", nameof(address));
        foreach (var segment in address.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
            if (segment.Length > MaxSegmentLength)
                throw new ArgumentException($"Path segment '{segment}' exceeds the documented {MaxSegmentLength}-character bound.", nameof(address));
    }

    private static Koras.Connect.Exceptions.IntegrationException Classify(IntegrationRequestException ex)
        => DataverseApiException.FromTransportFailure(ex);

    /// <summary>Confirms connectivity to the configured environment and returns the caller's identity (DVT-002).</summary>
    public async Task<WhoAmIResult> WhoAmIAsync(CancellationToken cancellationToken = default)
    {
        var address = new Uri(_root, "WhoAmI");
        ValidateRequestUri(address);
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, RequiredHeaders, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            return new WhoAmIResult(RequireGuid(root, "UserId"), RequireGuid(root, "BusinessUnitId"), RequireGuid(root, "OrganizationId"));
        }
    }

    private static Guid RequireGuid(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || !Guid.TryParse(value.GetString(), out var guid))
            throw new ContractException($"WhoAmI response missing required field '{property}'.", new ErrorDetails("whoami_contract"));
        return guid;
    }

    public ConnectorDescriptor Descriptor => new("Microsoft", "Dataverse", ApiVersion,
        SupportedOperations: ["WhoAmI", "CreateRow", "GetRow", "UpdateRow", "DeleteRow", "EnumerateRows", "ExecuteBatch", "UpsertByAlternateKey",
            "GetMetadata", "EnumerateEntityDefinitions", "EnumerateGlobalOptionSets", "BindLookup", "ClearLookup", "Associate", "Disassociate", "GetChanges"],
        Entitlements: new Dictionary<string, EntitlementEvidence>());

    /// <summary>Bounded, read-only, opt-in connectivity probe (DVT-002). Never issues a mutating request - the only
    /// call made is <see cref="WhoAmIAsync"/>, which Dataverse documents purely as an identity/connectivity check.</summary>
    public async ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectionDiagnostic> { new(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, _clock.GetUtcNow()) };
        try
        {
            await WhoAmIAsync(cancellationToken);
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Passed, "connected", null, _clock.GetUtcNow()));
        }
        catch (DataverseApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured client credentials and the Dataverse Application User's security role."));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Skipped, "skipped_after_authentication_failure", null, _clock.GetUtcNow()));
        }
        catch (Koras.Connect.Exceptions.IntegrationException ex)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured environment URL and network connectivity."));
        }
        return results;
    }
}

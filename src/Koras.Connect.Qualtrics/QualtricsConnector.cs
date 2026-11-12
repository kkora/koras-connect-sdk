using Koras.Connect.Abstractions;
using Koras.Connect.Transport;

namespace Koras.Connect.Qualtrics;

/// <summary>Read-only, opt-in diagnostic wrapper around <see cref="QualtricsClient"/>. The Configuration stage only
/// validates <see cref="QualtricsOptions"/> locally (no I/O); the Authentication stage issues exactly one minimal
/// authenticated GET (a single page of up to one survey) and never performs a mutating request. Connectivity is not
/// separately probed because the Authentication-stage GET already exercises the same network path.</summary>
public sealed class QualtricsConnector : IConnector
{
    private readonly QualtricsClient _client;
    private readonly QualtricsOptions _options;

    public QualtricsConnector(QualtricsClient client, QualtricsOptions options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Identifies this connector. Every entitlement starts <see cref="EntitlementState.Unknown"/>; this type
    /// never infers a grant from local configuration, only from a confirmed remote outcome.</summary>
    public ConnectorDescriptor Descriptor => new(
        Provider: "Qualtrics",
        Product: "XM Platform API",
        ApiVersion: "v3",
        SupportedOperations: ["surveys.list", "surveys.get", "responses.export", "directory.contacts", "directory.mailinglists"],
        Entitlements: new Dictionary<string, EntitlementEvidence>
        {
            ["surveys.list"] = new(EntitlementState.Unknown),
            ["responses.export"] = new(EntitlementState.Unknown),
            ["directory.contacts"] = new(EntitlementState.Unknown),
            ["directory.mailinglists"] = new(EntitlementState.Unknown)
        });

    public async ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectionDiagnostic>();
        static DateTimeOffset Now() => DateTimeOffset.UtcNow;
        try
        {
            _options.Validate();
        }
        catch (OptionsValidationException failure)
        {
            results.Add(new ConnectionDiagnostic(DiagnosticStage.Configuration, DiagnosticOutcome.Failed, "configuration_invalid",
                null, Now(), "Verify QualtricsOptions.ApiRoot and MaxPages: " + string.Join("; ", failure.Failures)));
            results.Add(new ConnectionDiagnostic(DiagnosticStage.Authentication, DiagnosticOutcome.Skipped, "configuration_invalid", null, Now()));
            return results;
        }
        results.Add(new ConnectionDiagnostic(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, Now()));

        try
        {
            // A single bounded page is enough to prove the token/root pair authenticates; it never iterates further
            // pages and never issues anything but a GET.
            await _client.ListSurveysPageAsync(cancellationToken: cancellationToken);
            results.Add(new ConnectionDiagnostic(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authentication_valid", null, Now()));
        }
        catch (IntegrationRequestException failure)
        {
            var classified = QualtricsApiException.FromTransportFailure(failure);
            results.Add(new ConnectionDiagnostic(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, classified.Details.Code,
                classified.Details.CorrelationId, Now(), "Verify the configured API token and tenant data-center root."));
        }
        return results;
    }
}

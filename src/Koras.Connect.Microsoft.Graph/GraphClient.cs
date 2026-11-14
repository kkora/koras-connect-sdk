using Koras.Connect.Abstractions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>The single Graph HTTP client foundation every other Graph feature builds on (MGR-003). Targets
/// either the GA <c>v1.0</c> root (default) or the explicit-opt-in <c>beta</c> root, per <see cref="GraphOptions.UseBetaEndpoint"/>.</summary>
public sealed partial class GraphClient : IConnector
{
    private const string V1Root = "https://graph.microsoft.com/v1.0/";
    private const string BetaRoot = "https://graph.microsoft.com/beta/";

    private readonly IntegrationTransport _transport;
    private readonly GraphOptions _options;
    private readonly Uri _root;
    private readonly TimeProvider _clock;

    public GraphClient(IntegrationTransport transport, GraphOptions? options = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        options ??= new GraphOptions();
        options.Validate();
        _transport = transport;
        _options = options;
        _root = EndpointSafety.ValidateRoot(new Uri(options.UseBetaEndpoint ? BetaRoot : V1Root));
        _clock = timeProvider ?? TimeProvider.System;
    }

    private static Koras.Connect.Exceptions.IntegrationException Classify(IntegrationRequestException ex)
        => GraphApiException.FromTransportFailure(ex);

    public ConnectorDescriptor Descriptor => new("MicrosoftGraph", "Microsoft Graph", _options.UseBetaEndpoint ? "beta" : "v1.0",
        SupportedOperations: ["EnumerateUsers", "GetUser", "GetMe", "CreateUser", "UpdateUser", "DeleteUser",
            "EnumerateGroups", "EnumerateGroupMembers", "AddGroupMember", "RemoveGroupMember"],
        Entitlements: new Dictionary<string, EntitlementEvidence>());

    /// <summary>Bounded, read-only, opt-in connectivity probe (MGR-026). Only the Configuration stage is
    /// implemented so far - Authentication/Connectivity wire to MGR-008's <c>GET /organization</c> once that
    /// feature exists, and a Permissions stage wires to MGR-024's grant-listing once that feature exists. Never
    /// issues a mutating request.</summary>
    public ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<ConnectionDiagnostic>>(
            [new(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, _clock.GetUtcNow())]);
}

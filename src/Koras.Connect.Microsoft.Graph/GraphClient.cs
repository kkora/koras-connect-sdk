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
            "EnumerateGroups", "EnumerateGroupMembers", "AddGroupMember", "RemoveGroupMember",
            "EnumerateDirectoryRoles", "EnumerateAdministrativeUnits",
            "EnumerateApplications", "GetApplication", "CreateApplication",
            "EnumerateServicePrincipals", "GetServicePrincipal", "CreateServicePrincipal", "GetOrganization",
            "GetMyDrive", "GetUserDrive", "EnumerateDrives", "GetDrive", "EnumerateDriveItems",
            "UploadSmallFile", "DownloadFile",
            "GetRootSite", "GetSiteByPath", "EnumerateSiteDrives",
            "EnumerateLists", "EnumerateListItems", "CreateListItem", "UpdateListItemFields",
            "EnumerateMyMessages", "EnumerateUserMessages", "SendMail",
            "EnumerateEvents", "CreateEvent", "UpdateEvent", "DeleteEvent",
            "EnumerateTeams", "GetTeam", "EnumerateChannels",
            "EnumerateChannelMessages", "SendChannelMessage", "EnumerateChatMessages", "SendChatMessage",
            "EnumerateMyPlans", "EnumeratePlanTasks",
            "EnumerateTodoLists", "EnumerateTodoTasks", "CreateTodoTask", "UpdateTodoTask",
            "GetDelta",
            "CreateSubscription", "RenewSubscription", "DeleteSubscription",
            "ExecuteBatch",
            "CreateUploadSession", "UploadSessionFragment", "GetUploadSessionStatus", "CancelUploadSession",
            "ListGrantsForCurrentApp", "ListMyGrants", "BuildAdminConsentUrl"],
        Entitlements: new Dictionary<string, EntitlementEvidence>());

    /// <summary>Bounded, read-only, opt-in connectivity probe (MGR-026). The Authentication/Connectivity stages
    /// use MGR-008's <c>GET /organization</c> as the one permitted minimal, non-mutating probe call. The
    /// Permissions stage (MGR-024) lists the current app's grants via <c>GET /oauth2PermissionGrants</c>, a
    /// Graph-specific diagnostic beyond the base three stages - it only runs after Connectivity passes, and a
    /// failure here (for example missing <c>Directory.Read.All</c>) is reported, never thrown. Never issues a
    /// mutating request.</summary>
    public async ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectionDiagnostic> { new(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "configuration_valid", null, _clock.GetUtcNow()) };
        try
        {
            await GetOrganizationAsync(cancellationToken);
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Passed, "connected", null, _clock.GetUtcNow()));
        }
        catch (GraphApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured app registration credentials and admin-consented permissions."));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Skipped, "skipped_after_authentication_failure", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Permissions, DiagnosticOutcome.Skipped, "skipped_after_authentication_failure", null, _clock.GetUtcNow()));
            return results;
        }
        catch (Koras.Connect.Exceptions.IntegrationException ex)
        {
            results.Add(new(DiagnosticStage.Authentication, DiagnosticOutcome.Passed, "authenticated", null, _clock.GetUtcNow()));
            results.Add(new(DiagnosticStage.Connectivity, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify network connectivity to graph.microsoft.com and the configured tenant."));
            results.Add(new(DiagnosticStage.Permissions, DiagnosticOutcome.Skipped, "skipped_after_connectivity_failure", null, _clock.GetUtcNow()));
            return results;
        }
        try
        {
            await foreach (var _ in ListGrantsForCurrentAppAsync(cancellationToken)) break;
            results.Add(new(DiagnosticStage.Permissions, DiagnosticOutcome.Passed, "permissions_readable", null, _clock.GetUtcNow()));
        }
        catch (Koras.Connect.Exceptions.IntegrationException ex)
        {
            results.Add(new(DiagnosticStage.Permissions, DiagnosticOutcome.Failed, ex.Details.Code, ex.Details.CorrelationId, _clock.GetUtcNow(),
                "Verify the configured app registration holds Directory.Read.All to list permission grants."));
        }
        return results;
    }
}

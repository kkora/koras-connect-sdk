using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
namespace Koras.Connect.Adp;

/// <summary>Explicit allowlist of documented WFN operations. Values are not inferred tenant permissions.</summary>
public enum AdpOperation
{
    Hire, Rehire, TerminateAssignment, ChangeManager, ChangePersonalEmail,
    ChangePayDataInput, PayrollInstructions, PayDistributions, TimeOffBalances, TimeOffRequests,
    OrganizationDepartments, EventNotifications, ModifyWorkAssignment, ModifyDepartmentAllocations, SpendingAccountEnrollments
}
public sealed record AdpOperationContract(AdpOperation Operation, string Method, string Path, bool WorkerSpecific, bool HasMetadata);
public static class AdpOperationCatalog
{
    public static IReadOnlyList<AdpOperationContract> Contracts { get; } = Array.AsReadOnly(new AdpOperationContract[]
    {
        new(AdpOperation.ModifyWorkAssignment, "POST", "/events/hr/v1/worker.work-assignment.modify", false, true),
        new(AdpOperation.ModifyDepartmentAllocations, "POST", "/events/hr/v1/worker.work-assignment.assigned-organizational-units.modify", false, false),
        new(AdpOperation.SpendingAccountEnrollments, "GET", "/benefits/v1/associates/{worker}/spending-account-enrollments", true, false),
        new(AdpOperation.Hire, "POST", "/events/hr/v1/worker.hire", false, true),
        new(AdpOperation.Rehire, "POST", "/events/hr/v1/worker.rehire", false, true),
        new(AdpOperation.TerminateAssignment, "POST", "/events/hr/v1/worker.work-assignment.terminate", false, false),
        new(AdpOperation.ChangeManager, "POST", "/events/hr/v1/worker.reports-to.modify", false, false),
        new(AdpOperation.ChangePersonalEmail, "POST", "/events/hr/v1/worker.personal-communication.email.change", false, true),
        new(AdpOperation.ChangePayDataInput, "POST", "/events/payroll/v1/pay-data-input.modify", false, true),
        new(AdpOperation.PayrollInstructions, "GET", "/payroll/v1/workers/{worker}/payroll-instructions", true, false),
        new(AdpOperation.PayDistributions, "GET", "/payroll/v2/workers/{worker}/pay-distributions", true, false),
        new(AdpOperation.TimeOffBalances, "GET", "/time/v2/workers/{worker}/time-off-details/time-off-balances", true, false),
        new(AdpOperation.TimeOffRequests, "GET", "/time/v2/workers/{worker}/time-off-details/time-off-requests", true, false),
        new(AdpOperation.OrganizationDepartments, "GET", "/hr/v1/organization-departments", false, false),
        new(AdpOperation.EventNotifications, "GET", "/core/v1/event-notification-messages", false, false)
    });

    /// <summary>Documented candidate CAR scope string for the small subset of operations where the official WFN
    /// guides cited in docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md section 13 (refs A1/A2) name the
    /// canonical grant path explicitly: "The readable A2 guide identifies .../worker.workAssignment.modify ...
    /// A1 identifies .../worker.reportsTo.modify ... and .../worker.workAssignment.assignedOrganizationalUnits.modify".
    /// This is a <b>documented candidate scope, not a confirmed tenant grant</b> &#8212; the same caution the master
    /// spec repeats ("Treat these as historical documented canonical candidates; actual current CAR approval,
    /// actor/population and product applicability still require authorized confirmation"). Enabling an
    /// <see cref="AdpOperation"/> or calling it never depends on this value. Every operation without an explicitly
    /// documented candidate in that section maps to <c>null</c> here rather than a guessed string &#8212; the master
    /// spec explicitly warns "Never synthesize remaining scope strings by concatenating event names."</summary>
    public static IReadOnlyDictionary<AdpOperation, string?> CandidateScopes { get; } = new Dictionary<AdpOperation, string?>
    {
        [AdpOperation.ModifyWorkAssignment] = "/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.modify",
        [AdpOperation.ChangeManager] = "/hr/workerInformationManagement/workerManagement/workRelationshipManagement/worker.reportsTo.modify",
        [AdpOperation.ModifyDepartmentAllocations] = "/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.assignedOrganizationalUnits.modify",
        [AdpOperation.Hire] = null,
        [AdpOperation.Rehire] = null,
        [AdpOperation.TerminateAssignment] = null,
        [AdpOperation.ChangePersonalEmail] = null,
        [AdpOperation.ChangePayDataInput] = null,
        [AdpOperation.PayrollInstructions] = null,
        [AdpOperation.PayDistributions] = null,
        [AdpOperation.TimeOffBalances] = null,
        [AdpOperation.TimeOffRequests] = null,
        [AdpOperation.OrganizationDepartments] = null,
        [AdpOperation.EventNotifications] = null,
        [AdpOperation.SpendingAccountEnrollments] = null,
    };
}
/// <summary>Documented endpoint dispatch; vendor-specific payloads must be validated against the customer's current metadata.
/// No automatic write retries or claims of tenant authorization. JsonDocument results belong to the caller.</summary>
public sealed class AdpOperationsClient
{
    private readonly IntegrationTransport _transport;
    private readonly Uri _root;
    private readonly HashSet<AdpOperation> _enabled;
    private readonly AdpEntitlementTracker? _entitlements;
    public AdpOperationsClient(IntegrationTransport transport, Uri apiRoot, IEnumerable<AdpOperation> enabledOperations, AdpEntitlementTracker? entitlements = null)
    {
        new Workers.AdpWorkersOptions(apiRoot).Validate();
        _transport = transport; _root = apiRoot; _enabled = new(enabledOperations); _entitlements = entitlements;
    }
    public Task<JsonDocument> ReadAsync(AdpOperation operation, string? associateOid = null, CancellationToken cancellationToken = default)
    {
        if (operation == AdpOperation.SpendingAccountEnrollments) throw new ArgumentException("Use ReadSpendingAccountEnrollmentsAsync with the mandatory effective date.");
        var c = Contract(operation); if (c.Method != "GET") throw new ArgumentException("Operation is not a read.", nameof(operation));
        return GuardAsync(operation, () => _transport.ReadJsonAsync(Address(c, associateOid), cancellationToken: cancellationToken));
    }
    public Task<JsonDocument> GetMetadataAsync(AdpOperation operation, CancellationToken cancellationToken = default)
    {
        var c = Contract(operation); if (!c.HasMetadata) throw new NotSupportedException("Metadata endpoint has not been verified for this operation.");
        return GuardAsync(operation, () => _transport.ReadJsonAsync(new Uri(_root.AbsoluteUri.TrimEnd('/') + c.Path + "/meta"), cancellationToken: cancellationToken));
    }
    public Task<JsonDocument> SubmitEventAsync(AdpOperation operation, JsonElement envelope, CancellationToken cancellationToken = default)
    {
        var c = Contract(operation); if (c.Method != "POST") throw new ArgumentException("Operation is not an event submission.", nameof(operation));
        if (operation == AdpOperation.ModifyWorkAssignment)
        {
            if (envelope.ValueKind != JsonValueKind.Object || !envelope.TryGetProperty("associateOID", out _)
                || !envelope.TryGetProperty("workAssignmentID", out _) || !envelope.TryGetProperty("workAssignment", out _))
                throw new ArgumentException("Work assignment payload requires associateOID, workAssignmentID and workAssignment; validate remaining fields against metadata.", nameof(envelope));
        }
        else if (envelope.ValueKind != JsonValueKind.Object || !envelope.TryGetProperty("events", out var events)
            || events.ValueKind != JsonValueKind.Array || events.GetArrayLength() == 0
            || events.EnumerateArray().Any(e => e.ValueKind != JsonValueKind.Object))
            throw new ArgumentException("A nonempty ADP events array of objects is required. Validate event contents against authorized ADP metadata.", nameof(envelope));
        return _transport.SendJsonAsync(HttpMethod.Post, Address(c, null), envelope, cancellationToken: cancellationToken);
    }

    /// <summary>Convenience submission for <see cref="AdpOperation.ChangeManager"/>: builds the documented events
    /// envelope via <see cref="ChangeManagerCommand.Build"/> (which rejects a missing subject/target identity
    /// locally, before any HTTP call) and submits it through <see cref="SubmitEventAsync"/>.</summary>
    public Task<JsonDocument> SubmitChangeManagerAsync(string associateOid, ManagerRelationship manager, CancellationToken cancellationToken = default)
        => SubmitEventAsync(AdpOperation.ChangeManager, ChangeManagerCommand.Build(associateOid, manager), cancellationToken);

    /// <summary>Convenience submission for <see cref="AdpOperation.ModifyDepartmentAllocations"/>: builds the
    /// documented events envelope via <see cref="ModifyDepartmentAllocationsCommand.Build"/> (which rejects a
    /// missing subject identity or allocation itemID locally, before any HTTP call) and submits it through
    /// <see cref="SubmitEventAsync"/>.</summary>
    public Task<JsonDocument> SubmitModifyDepartmentAllocationsAsync(string associateOid, IReadOnlyList<OrganizationDepartment> departments, CancellationToken cancellationToken = default)
        => SubmitEventAsync(AdpOperation.ModifyDepartmentAllocations, ModifyDepartmentAllocationsCommand.Build(associateOid, departments), cancellationToken);

    public Task<JsonDocument> ReadSpendingAccountEnrollmentsAsync(string associateOid, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        var c = Contract(AdpOperation.SpendingAccountEnrollments);
        var date = effectiveDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var filter = $"accountBasedCoverage/effectivePeriod/validFrom ge '{date}' and accountBasedCoverage/effectivePeriod/validTo le '{date}'";
        return GuardAsync(AdpOperation.SpendingAccountEnrollments, () =>
            _transport.ReadJsonAsync(new Uri(Address(c, associateOid).AbsoluteUri + "?$filter=" + Uri.EscapeDataString(filter)), cancellationToken: cancellationToken));
    }
    public Task<IntegrationJsonResponse> ReadNotificationAsync(CancellationToken cancellationToken = default)
        => GuardAsync(AdpOperation.EventNotifications, () => _transport.SendResponseAsync(HttpMethod.Get, Address(Contract(AdpOperation.EventNotifications), null), cancellationToken: cancellationToken));

    /// <summary>Acknowledge only after durable downstream processing; no automatic acknowledgement or persistence.</summary>
    public Task<JsonDocument> AcknowledgeNotificationAsync(string messageId, CancellationToken cancellationToken = default)
    {
        Contract(AdpOperation.EventNotifications);
        var id = Segment(messageId);
        return GuardAsync(AdpOperation.EventNotifications, () =>
            _transport.SendJsonAsync(HttpMethod.Delete, new Uri(_root.AbsoluteUri.TrimEnd('/') + "/core/v1/event-notification-messages/" + id), cancellationToken: cancellationToken));
    }
    private AdpOperationContract Contract(AdpOperation operation)
    {
        if (!_enabled.Contains(operation)) throw new InvalidOperationException("Operation must be explicitly enabled after entitlement verification.");
        if (_entitlements is not null && _entitlements.Get(operation).State == EntitlementState.Denied)
            throw new AuthorizationException($"Operation '{operation}' is recorded as denied by prior tenant entitlement evidence; no request was sent.", new ErrorDetails("entitlement_denied"));
        return AdpOperationCatalog.Contracts.SingleOrDefault(c => c.Operation == operation) ?? throw new ArgumentOutOfRangeException(nameof(operation));
    }
    /// <summary>Classifies a failed call into the structured <see cref="AdpApiException"/> hierarchy. Deliberately not
    /// applied to <see cref="SubmitEventAsync"/>, whose documented contract is to surface the raw
    /// <see cref="IntegrationRequestException"/> unmodified so write callers can inspect <c>ResponseStatus</c> directly.</summary>
    private static async Task<T> GuardAsync<T>(AdpOperation operation, Func<Task<T>> call)
    {
        try { return await call(); }
        catch (IntegrationRequestException failure) { throw AdpApiException.Classify(operation, failure); }
    }
    private Uri Address(AdpOperationContract contract, string? worker)
        => new(_root.AbsoluteUri.TrimEnd('/') + (contract.WorkerSpecific ? contract.Path.Replace("{worker}", Segment(worker)) : contract.Path));
    private static string Segment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Contains('/') || value.Contains('\\')) throw new ArgumentException("A single nonempty identifier segment is required.");
        return Uri.EscapeDataString(value);
    }
}

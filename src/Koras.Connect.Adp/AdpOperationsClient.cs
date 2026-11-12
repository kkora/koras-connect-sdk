using System.Text.Json;
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
}
/// <summary>Documented endpoint dispatch; vendor-specific payloads must be validated against the customer's current metadata.
/// No automatic write retries or claims of tenant authorization. JsonDocument results belong to the caller.</summary>
public sealed class AdpOperationsClient
{
    private readonly IntegrationTransport _transport;
    private readonly Uri _root;
    private readonly HashSet<AdpOperation> _enabled;
    public AdpOperationsClient(IntegrationTransport transport, Uri apiRoot, IEnumerable<AdpOperation> enabledOperations)
    {
        new Workers.AdpWorkersOptions(apiRoot).Validate();
        _transport = transport; _root = apiRoot; _enabled = new(enabledOperations);
    }
    public Task<JsonDocument> ReadAsync(AdpOperation operation, string? associateOid = null, CancellationToken cancellationToken = default)
    {
        if (operation == AdpOperation.SpendingAccountEnrollments) throw new ArgumentException("Use ReadSpendingAccountEnrollmentsAsync with the mandatory effective date.");
        var c = Contract(operation); if (c.Method != "GET") throw new ArgumentException("Operation is not a read.", nameof(operation));
        return _transport.ReadJsonAsync(Address(c, associateOid), cancellationToken: cancellationToken);
    }
    public Task<JsonDocument> GetMetadataAsync(AdpOperation operation, CancellationToken cancellationToken = default)
    {
        var c = Contract(operation); if (!c.HasMetadata) throw new NotSupportedException("Metadata endpoint has not been verified for this operation.");
        return _transport.ReadJsonAsync(new Uri(_root.AbsoluteUri.TrimEnd('/') + c.Path + "/meta"), cancellationToken: cancellationToken);
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
    public Task<JsonDocument> ReadSpendingAccountEnrollmentsAsync(string associateOid, DateOnly effectiveDate, CancellationToken cancellationToken = default)
    {
        var c = Contract(AdpOperation.SpendingAccountEnrollments);
        var date = effectiveDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var filter = $"accountBasedCoverage/effectivePeriod/validFrom ge '{date}' and accountBasedCoverage/effectivePeriod/validTo le '{date}'";
        return _transport.ReadJsonAsync(new Uri(Address(c, associateOid).AbsoluteUri + "?$filter=" + Uri.EscapeDataString(filter)), cancellationToken: cancellationToken);
    }
    public Task<IntegrationJsonResponse> ReadNotificationAsync(CancellationToken cancellationToken = default)
        => _transport.SendResponseAsync(HttpMethod.Get, Address(Contract(AdpOperation.EventNotifications), null), cancellationToken: cancellationToken);

    /// <summary>Acknowledge only after durable downstream processing; no automatic acknowledgement or persistence.</summary>
    public Task<JsonDocument> AcknowledgeNotificationAsync(string messageId, CancellationToken cancellationToken = default)
    {
        Contract(AdpOperation.EventNotifications);
        var id = Segment(messageId);
        return _transport.SendJsonAsync(HttpMethod.Delete, new Uri(_root.AbsoluteUri.TrimEnd('/') + "/core/v1/event-notification-messages/" + id), cancellationToken: cancellationToken);
    }
    private AdpOperationContract Contract(AdpOperation operation)
    {
        if (!_enabled.Contains(operation)) throw new InvalidOperationException("Operation must be explicitly enabled after entitlement verification.");
        return AdpOperationCatalog.Contracts.SingleOrDefault(c => c.Operation == operation) ?? throw new ArgumentOutOfRangeException(nameof(operation));
    }
    private Uri Address(AdpOperationContract contract, string? worker)
        => new(_root.AbsoluteUri.TrimEnd('/') + (contract.WorkerSpecific ? contract.Path.Replace("{worker}", Segment(worker)) : contract.Path));
    private static string Segment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Contains('/') || value.Contains('\\')) throw new ArgumentException("A single nonempty identifier segment is required.");
        return Uri.EscapeDataString(value);
    }
}

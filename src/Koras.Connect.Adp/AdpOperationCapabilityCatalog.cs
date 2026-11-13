namespace Koras.Connect.Adp;

/// <summary>One operation's richer matrix fields (ADP-021), modeled separately from <see cref="AdpOperationContract"/>
/// and <see cref="AdpEntitlementTracker"/> rather than widening either &#8212; the same "sibling lookup" approach
/// already used for <see cref="AdpOperationCatalog.CandidateScopes"/>, so existing contract/entitlement call sites
/// are untouched. Every field is transcribed directly from the reviewed <c>docs/features/ADP-FEATURE-MATRIX.md</c>
/// row for this operation; nothing here is inferred or synthesized, and a field the matrix does not document for
/// this operation is <c>null</c> rather than guessed (ADP-021's "unknown stays unknown").</summary>
/// <param name="Operation">The operation this row describes.</param>
/// <param name="Product">The ADP product/version scope the matrix was reviewed against for every row (U.S.
/// Workforce Now only &#8212; the matrix is explicit that this is "not a promise of compatibility with WFN Next
/// Gen, Lyric, RUN, Vantage or international products").</param>
/// <param name="Family">An informal grouping of related operations (Lifecycle, Work assignment, Organization,
/// Personal communication, Payroll, Time, Benefits, Events) for readability; not a vendor-documented taxonomy.</param>
/// <param name="GrantOrAccessNote">The matrix's "Source / access" column text for this operation verbatim (canonical
/// grant-path fragment and any access caveat), the clearest per-operation entitlement evidence currently
/// available. Mirrors <see cref="AdpOperationCatalog.CandidateScopes"/>'s caution: documented candidate evidence,
/// not a confirmed tenant grant.</param>
/// <param name="PagingOrQuota">Paging or quota behavior the matrix documents for this specific operation, or
/// <c>null</c> when the matrix documents none (true for every operation in this enum: the matrix's only paging
/// bound, worker list/search's <c>$top</c> 1-100, applies to <see cref="Workers.AdpWorkersClient"/>'s read
/// surfaces, which are not dispatched through this <see cref="AdpOperation"/> catalog).</param>
/// <param name="EvidenceSource">The matrix's source alias(es) (S1-S8) for this row.</param>
/// <param name="EvidenceDate">The date the cited matrix sources were reviewed, from the matrix's own "Sources
/// reviewed" header &#8212; not a live-tenant verification date.</param>
public sealed record AdpOperationCapability(
    AdpOperation Operation,
    string Product,
    string Family,
    string GrantOrAccessNote,
    string? PagingOrQuota,
    string EvidenceSource,
    DateOnly EvidenceDate);

public static class AdpOperationCapabilityCatalog
{
    private const string Product = "ADP Workforce Now (WFN), U.S. only; not a promise of compatibility with WFN Next Gen, Lyric, RUN, Vantage or international products";
    private static readonly DateOnly ReviewedAt = new(2026, 11, 11);

    /// <summary>Exactly one entry per <see cref="AdpOperation"/>, transcribed from
    /// <c>docs/features/ADP-FEATURE-MATRIX.md</c>'s "Implemented operation contracts" table.</summary>
    public static IReadOnlyDictionary<AdpOperation, AdpOperationCapability> Capabilities { get; } = new Dictionary<AdpOperation, AdpOperationCapability>
    {
        [AdpOperation.Hire] = new(AdpOperation.Hire, Product, "Lifecycle",
            "lifecycleManagement/worker.hire; product/version restrictions apply", null, "S1/S2", ReviewedAt),
        [AdpOperation.Rehire] = new(AdpOperation.Rehire, Product, "Lifecycle",
            "lifecycleManagement/worker.rehire", null, "S1/S2", ReviewedAt),
        [AdpOperation.TerminateAssignment] = new(AdpOperation.TerminateAssignment, Product, "Lifecycle",
            "workAssignmentManagement/worker.workAssignment.terminate; Practitioner", null, "S3", ReviewedAt),
        [AdpOperation.ChangeManager] = new(AdpOperation.ChangeManager, Product, "Organization",
            "workRelationshipManagement/worker.reportsTo.modify; not universally supported in Next Gen", null, "S1", ReviewedAt),
        [AdpOperation.ModifyWorkAssignment] = new(AdpOperation.ModifyWorkAssignment, Product, "Work assignment",
            "workAssignmentManagement/worker.workAssignment.modify; Practitioner", null, "S4", ReviewedAt),
        [AdpOperation.ModifyDepartmentAllocations] = new(AdpOperation.ModifyDepartmentAllocations, Product, "Organization",
            "workAssignmentManagement/worker.workAssignment.assignedOrganizationalUnits.modify; not equivalent to arbitrary home department replacement", null, "S1", ReviewedAt),
        [AdpOperation.ChangePersonalEmail] = new(AdpOperation.ChangePersonalEmail, Product, "Personal communication",
            "communication change grant, tenant metadata", null, "S2", ReviewedAt),
        [AdpOperation.ChangePayDataInput] = new(AdpOperation.ChangePayDataInput, Product, "Payroll",
            "licensed payroll input grant; valid open payroll batch", null, "S2/S8", ReviewedAt),
        [AdpOperation.PayrollInstructions] = new(AdpOperation.PayrollInstructions, Product, "Payroll",
            "payroll instruction read grant", null, "S2", ReviewedAt),
        [AdpOperation.PayDistributions] = new(AdpOperation.PayDistributions, Product, "Payroll",
            "pay distribution read grant", null, "S2", ReviewedAt),
        [AdpOperation.TimeOffBalances] = new(AdpOperation.TimeOffBalances, Product, "Time",
            "time-off read grant", null, "S2", ReviewedAt),
        [AdpOperation.TimeOffRequests] = new(AdpOperation.TimeOffRequests, Product, "Time",
            "time-off request read grant", null, "S2", ReviewedAt),
        [AdpOperation.SpendingAccountEnrollments] = new(AdpOperation.SpendingAccountEnrollments, Product, "Benefits",
            "external benefit provider and spending-account enrollment grant, limited plan types", null, "S5", ReviewedAt),
        [AdpOperation.OrganizationDepartments] = new(AdpOperation.OrganizationDepartments, Product, "Organization",
            "department read grant", null, "S2", ReviewedAt),
        [AdpOperation.EventNotifications] = new(AdpOperation.EventNotifications, Product, "Events",
            "separate notification subscriptions per event; queue subscription and processing rights required to acknowledge", null, "S2", ReviewedAt),
    };
}

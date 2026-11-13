namespace Koras.Connect.Adp.Workers;

public enum SortDirection
{
    Ascending,
    Descending,
}

/// <summary>A single, typed worker sort key that renders to a safe OData <c>$orderby</c> fragment, reusing
/// <see cref="WorkerFilterField"/>'s whitelist so the same small set of documented-safe fields is sortable and
/// filterable. <b>Best-effort, unconfirmed:</b> the "Worker list / search" row in
/// <c>docs/features/ADP-FEATURE-MATRIX.md</c> documents only <c>$top</c>/<c>$skip</c>/<c>$filter</c> for this
/// endpoint &#8212; it does not cite an <c>$orderby</c> parameter at all. This SDK builds a syntactically correct
/// OData <c>$orderby</c> expression on the caller's behalf, but ADP's actual support for sorting a worker read is
/// unverified: a tenant may ignore the parameter silently, reject the request, or document a different
/// convention. Confirm via <see cref="AdpWorkersClient.GetMetadataAsync"/>/tenant documentation before relying on
/// sort order in production, the same caution this package already applies to <c>PayrollInstruction</c>/
/// <c>PayDistribution</c> in <c>PayrollModels.cs</c>.</summary>
public sealed class WorkerSort
{
    // Mirrors WorkerFilter.Paths; duplicated intentionally so WorkerFilter and WorkerSort can evolve their
    // whitelists independently even though they start out identical.
    private static readonly IReadOnlyDictionary<WorkerFilterField, string> Paths = new Dictionary<WorkerFilterField, string>
    {
        [WorkerFilterField.AssociateOid] = "associateOID",
        [WorkerFilterField.LegalGivenName] = "person/legalName/givenName",
        [WorkerFilterField.LegalFamilyName] = "person/legalName/familyName1",
        [WorkerFilterField.WorkAssignmentJobTitle] = "workAssignments/jobTitle",
    };

    public WorkerFilterField Field { get; }
    public SortDirection Direction { get; }

    public WorkerSort(WorkerFilterField field, SortDirection direction = SortDirection.Ascending)
    {
        if (!Paths.ContainsKey(field)) throw new ArgumentOutOfRangeException(nameof(field));
        Field = field;
        Direction = direction;
    }

    /// <summary>Renders the OData <c>$orderby</c> fragment for this sort key, for example
    /// <c>"workAssignments/jobTitle desc"</c>.</summary>
    public string ToODataOrderBy() => $"{Paths[Field]} {(Direction == SortDirection.Descending ? "desc" : "asc")}";
}

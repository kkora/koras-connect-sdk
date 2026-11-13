namespace Koras.Connect.Adp.Workers;

/// <summary>The worker fields this SDK whitelists for a typed <see cref="WorkerFilter"/>, each mapped to the
/// documented WFN path used by <see cref="AdpWorkersClient.SearchWorkersAsync(WorkerFilter,int,WorkerSort,CancellationToken)"/>.
/// Schema source: "Worker list / search" row (S1/S2) in <c>docs/features/ADP-FEATURE-MATRIX.md</c>, which notes
/// authorized worker population and tenant metadata determine which filters actually work; this whitelist only
/// bounds which <em>syntax</em> the SDK will build on the caller's behalf, not which filters a given tenant grants.</summary>
public enum WorkerFilterField
{
    AssociateOid,
    LegalGivenName,
    LegalFamilyName,
    WorkAssignmentJobTitle,
}

/// <summary>The OData comparison operators this SDK's typed filter builder supports.</summary>
public enum WorkerFilterOperator
{
    Equals,
    Contains,
}

/// <summary>A single, typed worker search predicate that renders to a safe OData <c>$filter</c> fragment without
/// requiring the caller to hand-escape ADP's filter syntax. Complements, rather than replaces, the existing raw
/// <see cref="AdpWorkersClient.SearchWorkersAsync(string,int,WorkerSort,CancellationToken)"/> overload, which remains the
/// escape hatch for metadata-approved filters this whitelist does not cover.</summary>
public sealed class WorkerFilter
{
    private static readonly IReadOnlyDictionary<WorkerFilterField, string> Paths = new Dictionary<WorkerFilterField, string>
    {
        [WorkerFilterField.AssociateOid] = "associateOID",
        [WorkerFilterField.LegalGivenName] = "person/legalName/givenName",
        [WorkerFilterField.LegalFamilyName] = "person/legalName/familyName1",
        [WorkerFilterField.WorkAssignmentJobTitle] = "workAssignments/jobTitle",
    };

    private static readonly IReadOnlyDictionary<WorkerFilterOperator, string> Operators = new Dictionary<WorkerFilterOperator, string>
    {
        [WorkerFilterOperator.Equals] = "eq",
        [WorkerFilterOperator.Contains] = "co",
    };

    public WorkerFilterField Field { get; }
    public WorkerFilterOperator Operator { get; }
    public string Value { get; }

    public WorkerFilter(WorkerFilterField field, WorkerFilterOperator @operator, string value)
    {
        if (!Paths.ContainsKey(field)) throw new ArgumentOutOfRangeException(nameof(field));
        if (!Operators.ContainsKey(@operator)) throw new ArgumentOutOfRangeException(nameof(@operator));
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Filter value is required.", nameof(value));
        Field = field;
        Operator = @operator;
        Value = value;
    }

    /// <summary>Renders the OData <c>$filter</c> fragment for this predicate. The value is always emitted as a
    /// single quoted string literal with embedded single quotes doubled per OData escaping, so no value content can
    /// alter the filter's structure regardless of what the caller passes.</summary>
    public string ToODataFilter() => $"{Paths[Field]} {Operators[Operator]} '{Value.Replace("'", "''")}'";
}

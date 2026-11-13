using Koras.Connect.Adp.Workers;
using Koras.Connect.Export;
namespace Koras.Connect.Adp.Export;

/// <summary>The documented, fixed export column set for <see cref="Worker"/>: associate OID, legal given/family
/// name and the currently effective work assignment's job title, hire date and termination date. Deliberately
/// excludes SSN, other government identifiers and compensation fields by default, and never exports the raw
/// <c>AdditionalData</c> vendor extension-data bag; callers who need additional authorized fields must build and
/// pass their own <see cref="ExportSchema{T}"/> after confirming tenant authorization for those fields.</summary>
public static class WorkerExportProjection
{
    public static readonly IReadOnlyList<string> ColumnNames = ["AssociateOid", "GivenName", "FamilyName", "JobTitle", "HireDate", "TerminationDate"];

    public static ExportSchema<Worker> CreateSchema(DateOnly? effectiveAssignmentAsOf = null) => CreateSchema(ColumnNames, effectiveAssignmentAsOf);

    /// <summary>Opt-in field-selection overload (ADP-008): builds a schema containing only the caller-selected
    /// subset of <see cref="ColumnNames"/>, in that fixed canonical order regardless of the order columns are
    /// requested in - for a tenant authorized for fewer fields than this package's full documented column set.
    /// Rejects any name outside <see cref="ColumnNames"/> rather than silently ignoring it, so a typo or an attempt
    /// to select an unapproved field fails loudly instead of producing a smaller-than-expected export.</summary>
    public static ExportSchema<Worker> CreateSchema(IReadOnlyCollection<string> includeColumns, DateOnly? effectiveAssignmentAsOf = null)
    {
        ArgumentNullException.ThrowIfNull(includeColumns);
        if (includeColumns.Count == 0) throw new ArgumentException("At least one column must be selected.", nameof(includeColumns));
        var unknown = includeColumns.Where(c => !ColumnNames.Contains(c)).ToArray();
        if (unknown.Length > 0)
            throw new ArgumentException($"Unknown or unapproved column(s): {string.Join(", ", unknown)}. Only {nameof(WorkerExportProjection)}.{nameof(ColumnNames)} may be selected.", nameof(includeColumns));
        var selected = new HashSet<string>(includeColumns, StringComparer.Ordinal);
        var all = new Dictionary<string, ExportColumn<Worker>>
        {
            ["AssociateOid"] = new("AssociateOid", w => w.AssociateOid),
            ["GivenName"] = new("GivenName", w => w.Person?.LegalName?.GivenName),
            ["FamilyName"] = new("FamilyName", w => w.Person?.LegalName?.FamilyName),
            ["JobTitle"] = new("JobTitle", w => Effective(w, effectiveAssignmentAsOf)?.JobTitle),
            ["HireDate"] = new("HireDate", w => Effective(w, effectiveAssignmentAsOf)?.HireDate),
            ["TerminationDate"] = new("TerminationDate", w => Effective(w, effectiveAssignmentAsOf)?.TerminationDate),
        };
        return new ExportSchema<Worker>(ColumnNames.Where(selected.Contains).Select(name => all[name]).ToArray());
    }

    /// <summary>Streams workers from an <see cref="AdpWorkersClient.EnumerateTypedWorkersAsync"/>-shaped sequence
    /// straight through the given <see cref="IExportProvider"/> without buffering the whole population in memory.
    /// The CSV/XML/XLSX export providers all honor the supplied <see cref="ExportSchema{T}"/> and therefore only
    /// ever emit the documented columns. The generic JSON export provider instead serializes the raw record type,
    /// which would also surface <see cref="Worker.AdditionalData"/>; callers needing a column-restricted JSON
    /// export should project through <see cref="CreateSchema(DateOnly?)"/> themselves in the consuming application,
    /// which already references Koras.Connect.Export directly (this package does not, by design: see
    /// KORAS-CONNECT-SDK-MASTER-FEATURES.md section 7 &#8212; "ADP does not require Export transitively").</summary>
    public static Task ExportAsync(IExportProvider provider, Stream destination, IAsyncEnumerable<Worker> workers,
        DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.WriteAsync(destination, workers, CreateSchema(effectiveAssignmentAsOf), cancellationToken);
    }

    /// <summary>Opt-in field-selection overload of <see cref="ExportAsync(IExportProvider,Stream,IAsyncEnumerable{Worker},DateOnly?,CancellationToken)"/>
    /// (ADP-008), exporting only <paramref name="includeColumns"/> instead of the full documented column set.</summary>
    public static Task ExportAsync(IExportProvider provider, Stream destination, IAsyncEnumerable<Worker> workers,
        IReadOnlyCollection<string> includeColumns, DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.WriteAsync(destination, workers, CreateSchema(includeColumns, effectiveAssignmentAsOf), cancellationToken);
    }

    private static WorkAssignment? Effective(Worker worker, DateOnly? asOf)
        => EffectiveAssignmentSelector.SelectEffective(worker.WorkAssignments, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow))
            ?? worker.WorkAssignments.FirstOrDefault();
}

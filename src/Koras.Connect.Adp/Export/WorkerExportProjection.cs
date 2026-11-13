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

    public static ExportSchema<Worker> CreateSchema(DateOnly? effectiveAssignmentAsOf = null) => new(
    [
        new("AssociateOid", w => w.AssociateOid),
        new("GivenName", w => w.Person?.LegalName?.GivenName),
        new("FamilyName", w => w.Person?.LegalName?.FamilyName),
        new("JobTitle", w => Effective(w, effectiveAssignmentAsOf)?.JobTitle),
        new("HireDate", w => Effective(w, effectiveAssignmentAsOf)?.HireDate),
        new("TerminationDate", w => Effective(w, effectiveAssignmentAsOf)?.TerminationDate)
    ]);

    /// <summary>Streams workers from an <see cref="AdpWorkersClient.EnumerateTypedWorkersAsync"/>-shaped sequence
    /// straight through the given <see cref="IExportProvider"/> without buffering the whole population in memory.
    /// The CSV/XML/XLSX export providers all honor the supplied <see cref="ExportSchema{T}"/> and therefore only
    /// ever emit the documented columns. The generic JSON export provider instead serializes the raw record type,
    /// which would also surface <see cref="Worker.AdditionalData"/>; callers needing a column-restricted JSON
    /// export should project through <see cref="CreateSchema"/> themselves in the consuming application, which
    /// already references Koras.Connect.Export directly (this package does not, by design: see
    /// KORAS-CONNECT-SDK-MASTER-FEATURES.md section 7 &#8212; "ADP does not require Export transitively").</summary>
    public static Task ExportAsync(IExportProvider provider, Stream destination, IAsyncEnumerable<Worker> workers,
        DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.WriteAsync(destination, workers, CreateSchema(effectiveAssignmentAsOf), cancellationToken);
    }

    private static WorkAssignment? Effective(Worker worker, DateOnly? asOf)
        => EffectiveAssignmentSelector.SelectEffective(worker.WorkAssignments, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow))
            ?? worker.WorkAssignments.FirstOrDefault();
}

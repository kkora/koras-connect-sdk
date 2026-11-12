using System.Runtime.CompilerServices;
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
    /// <see cref="CsvExportProvider"/>, <see cref="XmlExportProvider"/> and <see cref="XlsxExportProvider"/> all
    /// honor the supplied <see cref="ExportSchema{T}"/> and therefore only ever emit the documented columns. The
    /// generic <see cref="JsonExportProvider"/> instead serializes the raw record type, which would also surface
    /// <see cref="Worker.AdditionalData"/>; use <see cref="ExportJsonAsync"/> instead when a column-restricted JSON
    /// export is required.</summary>
    public static Task ExportAsync(IExportProvider provider, Stream destination, IAsyncEnumerable<Worker> workers,
        DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.WriteAsync(destination, workers, CreateSchema(effectiveAssignmentAsOf), cancellationToken);
    }

    /// <summary>Streams a column-restricted JSON export: each worker becomes a JSON object with exactly
    /// <see cref="ColumnNames"/> as keys. Unlike the generic <see cref="JsonExportProvider"/>, this never surfaces
    /// <see cref="Worker.AdditionalData"/> vendor extension fields, matching the CSV/XML/XLSX behavior.</summary>
    public static Task ExportJsonAsync(Stream destination, IAsyncEnumerable<Worker> workers,
        DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        var schema = CreateSchema(effectiveAssignmentAsOf);
        return TabularExport.WriteJsonAsync(destination, ProjectRows(workers, schema, cancellationToken), cancellationToken);
    }

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ProjectRows(IAsyncEnumerable<Worker> workers, ExportSchema<Worker> schema,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var worker in workers.WithCancellation(cancellationToken))
        {
            var values = schema.Project(worker);
            var row = new Dictionary<string, string?>(schema.ColumnNames.Count, StringComparer.Ordinal);
            for (var i = 0; i < schema.ColumnNames.Count; i++) row[schema.ColumnNames[i]] = values[i];
            yield return row;
        }
    }

    private static WorkAssignment? Effective(Worker worker, DateOnly? asOf)
        => EffectiveAssignmentSelector.SelectEffective(worker.WorkAssignments, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow))
            ?? worker.WorkAssignments.FirstOrDefault();
}

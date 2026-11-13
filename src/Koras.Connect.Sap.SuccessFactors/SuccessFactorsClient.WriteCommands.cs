using System.Text.Json;
namespace Koras.Connect.Sap.SuccessFactors;

public sealed partial class SuccessFactorsClient
{
    /// <summary>Typed convenience wrapper over <see cref="UpsertAsync"/> for the EmpEmployment entity. Validates
    /// every row's documented composite key (userId + startDate) locally, before any HTTP call, rather than
    /// letting a missing key or an unparseable date surface only as an opaque row failure. Does not invent a
    /// universal PATCH/MERGE API - this is still the same opt-in upsert primitive, scoped to one entity.</summary>
    public Task<IReadOnlyList<UpsertResult>> UpsertEmploymentAsync(IEnumerable<JsonElement> records, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying entity upsert support and RBP permissions.");
        var rows = AsRows(records);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty("userId", out var userId) || userId.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(userId.GetString()))
                throw new ArgumentException($"Row {i}: userId is required to form the EmpEmployment composite key.", nameof(records));
            if (!row.TryGetProperty("startDate", out var startDate))
                throw new ArgumentException($"Row {i}: startDate is required to form the EmpEmployment composite key.", nameof(records));
            try { SapDates.ParseUtc(startDate); }
            catch (FormatException ex) { throw new ArgumentException($"Row {i}: startDate is not a valid SAP date value ({ex.Message}).", nameof(records)); }
        }
        return UpsertAsync("EmpEmployment", rows, writesEnabled, cancellationToken);
    }

    /// <summary>Typed convenience wrapper over <see cref="UpsertAsync"/> for the EmpJob entity. Validates every
    /// row's documented composite key (userId + seqNumber) locally, before any HTTP call. Unlike EmpEmployment's
    /// startDate, EmpJob's startDate is not part of the key and is not validated here - an invalid value there is
    /// left for SAP's own row-level validation to report.</summary>
    public Task<IReadOnlyList<UpsertResult>> UpsertJobAsync(IEnumerable<JsonElement> records, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying entity upsert support and RBP permissions.");
        var rows = AsRows(records);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty("userId", out var userId) || userId.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(userId.GetString()))
                throw new ArgumentException($"Row {i}: userId is required to form the EmpJob composite key.", nameof(records));
            if (!row.TryGetProperty("seqNumber", out var seqNumber) ||
                !(seqNumber.ValueKind == JsonValueKind.Number || (seqNumber.ValueKind == JsonValueKind.String && long.TryParse(seqNumber.GetString(), out _))))
                throw new ArgumentException($"Row {i}: seqNumber is required to form the EmpJob composite key.", nameof(records));
        }
        return UpsertAsync("EmpJob", rows, writesEnabled, cancellationToken);
    }

    private static IReadOnlyList<JsonElement> AsRows(IEnumerable<JsonElement> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return records is IReadOnlyList<JsonElement> list ? list : records.ToList();
    }
}

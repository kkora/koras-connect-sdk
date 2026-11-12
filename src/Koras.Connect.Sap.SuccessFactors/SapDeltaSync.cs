using Koras.Connect.Synchronization;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>A delta-extraction checkpoint for one SAP entity's modification-time field. SAP documents a distinct
/// modification-time field per entity (for example EmpJob vs. EmpEmployment); this SDK never assumes a universal
/// field name such as a generic "lastModifiedOn" across every entity.</summary>
public sealed record SapDeltaCheckpoint(string EntityName, string ModificationField, DateTimeOffset LastValue);

/// <summary>Builds the OData $filter expression for a checkpointed delta read.</summary>
public static class SapDeltaFilters
{
    /// <summary>Combines the checkpoint's modification-time lower bound with a trailing overlap window, so records
    /// committed at or just before the last checkpoint are re-read rather than possibly missed due to clock skew
    /// or a same-timestamp write that lands after a prior scan already completed. The overlap can and will
    /// re-deliver already-seen, unchanged records - callers must dedupe (see
    /// <see cref="Reconciliation.DetectChangesAsync{T}"/>) rather than treat the filter alone as exact.</summary>
    public static string BuildFilter(SapDeltaCheckpoint checkpoint, TimeSpan overlap)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (overlap < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap window cannot be negative.");
        if (string.IsNullOrWhiteSpace(checkpoint.ModificationField)) throw new ArgumentException("ModificationField is required.", nameof(checkpoint));
        var lowerBound = (checkpoint.LastValue - overlap).UtcDateTime;
        return $"{checkpoint.ModificationField} ge datetime'{lowerBound:yyyy-MM-ddTHH:mm:ss.fff}'";
    }
}

/// <summary>Runs a reconciliation scan to completion and only then returns an advanced checkpoint. If the scan
/// throws partway through (transport failure, cancellation, malformed record, etc.) this method also throws and
/// the caller never receives a new checkpoint value - it must retry the whole scan from the last checkpoint it
/// already committed. This is the enforcement point for "a failed scan does not advance the checkpoint".</summary>
public static class SapDeltaSync
{
    public static async Task<(IReadOnlyList<RecordChange<T>> Changes, SapDeltaCheckpoint Checkpoint)> RunAsync<T>(
        IAsyncEnumerable<T> records, Func<T, string> keySelector, Func<T, DateTimeOffset> modifiedSelector,
        IReadOnlyDictionary<string, string> previousFingerprints, SapDeltaCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        var changes = new List<RecordChange<T>>();
        var max = checkpoint.LastValue;
        await foreach (var change in Reconciliation.DetectChangesAsync(records, keySelector, previousFingerprints, cancellationToken))
        {
            changes.Add(change);
            var modified = modifiedSelector(change.Value);
            if (modified > max) max = modified;
        }
        // Reached only when the full enumeration above has completed without throwing.
        return (changes, checkpoint with { LastValue = max });
    }
}

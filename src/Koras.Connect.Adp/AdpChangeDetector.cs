using Koras.Connect.Adp.Workers;
using Koras.Connect.Synchronization;
namespace Koras.Connect.Adp;

/// <summary>Caller-owned durable store for the fingerprints <see cref="AdpChangeDetector"/> compares a scan
/// against. The host is responsible for persistence and must implement <see cref="IWorkerFingerprintStore.CommitAsync"/>
/// atomically: a snapshot must never be partially written, so that a process crash mid-commit still leaves either
/// the old or the new snapshot intact, never a mix of both.</summary>
public interface IWorkerFingerprintStore
{
    Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(IReadOnlyDictionary<string, string> fingerprints, CancellationToken cancellationToken = default);
}

/// <summary>Composes <see cref="AdpWorkersClient.EnumerateTypedWorkersAsync"/> with the provider-neutral
/// <see cref="Reconciliation.DetectChangesAsync{T}"/> to support two complementary change-detection strategies:
/// an event-notification-triggered re-read of a single worker (<see cref="HandleNotificationAsync"/>), and a
/// periodic full-snapshot reconciliation backstop (<see cref="ScanAsync(IReadOnlyDictionary{string,string},CancellationToken)"/>).
/// Deliberately does not own any
/// persistence: the caller supplies the previous fingerprint snapshot and the durable, idempotent notification-id
/// set, and must only persist a new snapshot after fully and successfully draining a scan and durably processing
/// every yielded change. ADP never invents a modifiedSince filter or infers deletion from invisibility.</summary>
public sealed class AdpChangeDetector
{
    private readonly AdpWorkersClient _workers;

    public AdpChangeDetector(AdpWorkersClient workers) => _workers = workers ?? throw new ArgumentNullException(nameof(workers));

    /// <summary>Runs one full-snapshot reconciliation scan against <paramref name="previous"/> fingerprints and
    /// yields only the workers that changed (added or modified) since that snapshot. The caller must fully and
    /// successfully drain this sequence, durably process every yielded change, and only then persist a new
    /// snapshot built from the fingerprints it observed. If the scan throws or is canceled partway through, the
    /// caller must discard whatever partial snapshot it was accumulating and retain the prior one; this method
    /// never commits anything itself, so an interrupted scan cannot corrupt or partially overwrite caller state.</summary>
    public IAsyncEnumerable<RecordChange<Worker>> ScanAsync(IReadOnlyDictionary<string, string> previous, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return Reconciliation.DetectChangesAsync(_workers.EnumerateTypedWorkersAsync(cancellationToken), KeySelector, previous, cancellationToken);
    }

    /// <summary>Loads the previous snapshot from a caller-supplied <see cref="IWorkerFingerprintStore"/> and scans
    /// against it. The returned sequence must still be fully drained and every change durably processed before
    /// calling <see cref="CommitAsync(IWorkerFingerprintStore,IReadOnlyDictionary{string,string},IEnumerable{RecordChange{Worker}},CancellationToken)"/>;
    /// loading alone never commits anything.</summary>
    public async IAsyncEnumerable<RecordChange<Worker>> ScanAsync(IWorkerFingerprintStore store, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        var previous = await store.LoadAsync(cancellationToken);
        await foreach (var change in ScanAsync(previous, cancellationToken)) yield return change;
    }

    /// <summary>Persists the snapshot for a scan the caller has already fully drained and durably processed.
    /// Intentionally a separate, explicit step from <see cref="ScanAsync(IWorkerFingerprintStore,CancellationToken)"/>
    /// so a scan interrupted by an exception or cancellation never reaches this call and therefore never commits a
    /// partial snapshot; the store retains whatever it last successfully committed.</summary>
    public static Task CommitAsync(IWorkerFingerprintStore store, IReadOnlyDictionary<string, string> previous,
        IEnumerable<RecordChange<Worker>> observedChanges, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.CommitAsync(BuildSnapshot(previous, observedChanges), cancellationToken);
    }

    /// <summary>Handles one ADP event-notification delivery as a trigger to re-read a single worker from ADP,
    /// never as a source of truth for the change itself: the notification payload is not trusted, only the
    /// associate OID it names. Duplicate deliveries of the same message id (including out-of-order redelivery) are
    /// suppressed by the caller-owned <paramref name="seenMessageIds"/> durable inbox, so a repeated or
    /// late-arriving notification triggers at most one re-read per message id and never forces duplicate downstream
    /// processing. Returns <c>null</c> for a duplicate id without issuing any request.</summary>
    public async Task<Worker?> HandleNotificationAsync(string messageId, string associateOid, ISet<string> seenMessageIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentException("A message id is required.", nameof(messageId));
        ArgumentNullException.ThrowIfNull(seenMessageIds);
        if (!seenMessageIds.Add(messageId)) return null;
        return await _workers.GetWorkerAsync(associateOid, cancellationToken);
    }

    /// <summary>Builds the fingerprint snapshot to persist from a fully drained, successfully processed scan:
    /// carries forward every unchanged key from <paramref name="previous"/> and overlays the fingerprints observed
    /// for every changed record. Intentionally a pure function with no I/O; the caller decides when (and whether)
    /// to persist the result, and only after durable downstream delivery.</summary>
    public static IReadOnlyDictionary<string, string> BuildSnapshot(IReadOnlyDictionary<string, string> previous, IEnumerable<RecordChange<Worker>> observedChanges)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(observedChanges);
        var next = new Dictionary<string, string>(previous, StringComparer.Ordinal);
        foreach (var change in observedChanges) next[change.Key] = change.Fingerprint;
        return next;
    }

    private static string KeySelector(Worker worker)
        => worker.AssociateOid ?? throw new InvalidDataException("Worker is missing an associate OID; snapshot cannot be committed.");
}

using System.Runtime.CompilerServices;
namespace Koras.Connect.Adp.Workers;

/// <summary>Cycle-safe traversal of a worker's reporting-relationship chain (ADP-013), for host consumers who need
/// "who does this worker ultimately report up to" without risking an infinite loop from a non-hierarchical ADP
/// reporting graph (for example a data-entry error where A reports to B and B reports to A). This SDK has no
/// server-side "manager of" endpoint, so every hop is resolved by a caller-supplied delegate &#8212; typically backed
/// by a cached worker population or repeated <see cref="AdpWorkersClient.GetWorkerAsync"/> calls &#8212; and this
/// helper only sequences those lookups safely.</summary>
public static class ManagerChainTraversal
{
    /// <summary>Walks upward from <paramref name="startingAssociateOid"/>, yielding each worker on the chain
    /// exactly once (the starting worker included). Stops when the effective work assignment has no reporting
    /// relationship, when <paramref name="resolveWorker"/> returns <c>null</c> (manager not found or not
    /// accessible to the caller), or &#8212; safely, without throwing &#8212; the instant a previously visited
    /// identifier would be revisited, which signals cyclical/non-hierarchical source data rather than a traversal
    /// bug. <paramref name="effectiveAssignmentAsOf"/> selects which work assignment's reporting relationship is
    /// followed at each hop, via the same <see cref="EffectiveAssignmentSelector"/> logic ADP-007 already uses;
    /// defaults to today.</summary>
    public static async IAsyncEnumerable<Worker> WalkUpAsync(string startingAssociateOid,
        Func<string, CancellationToken, Task<Worker?>> resolveWorker, DateOnly? effectiveAssignmentAsOf = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolveWorker);
        if (string.IsNullOrWhiteSpace(startingAssociateOid)) throw new ArgumentException("A starting associate OID is required.", nameof(startingAssociateOid));

        var visited = new HashSet<string>(StringComparer.Ordinal);
        string? currentId = startingAssociateOid;
        while (currentId is not null && visited.Add(currentId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var worker = await resolveWorker(currentId, cancellationToken);
            if (worker is null) yield break;
            yield return worker;
            currentId = NextManagerIdentifier(worker, effectiveAssignmentAsOf);
        }
    }

    /// <summary>The identifier carried by the first reporting-relationship entry on the worker's effective (or,
    /// absent one, first) work assignment, or <c>null</c> if the assignment has none. "First" is this SDK's
    /// documented tie-break when multiple <c>reportsTo</c> entries exist (for example dotted-line relationships);
    /// ADP does not document a universal "primary manager" flag. The identifier's exact meaning (associate OID vs.
    /// employee workerID) is tenant-metadata-dependent and interpreted entirely by <c>resolveWorker</c>, not by
    /// this helper.</summary>
    private static string? NextManagerIdentifier(Worker worker, DateOnly? asOf)
    {
        var assignment = EffectiveAssignmentSelector.SelectEffective(worker.WorkAssignments, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow))
            ?? worker.WorkAssignments.FirstOrDefault();
        var relationship = assignment?.GetManagerRelationships().FirstOrDefault();
        return relationship?.WorkerId?.Value;
    }
}

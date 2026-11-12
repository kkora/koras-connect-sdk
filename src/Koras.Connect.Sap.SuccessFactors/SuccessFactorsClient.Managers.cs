using System.Text.Json;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>A single resolved reporting-line edge derived from an <see cref="EmpJob"/> record. <see cref="ManagerId"/>
/// is null when the job record carries no manager (an explicit "no manager", not an error).</summary>
public sealed record ManagerAssignment(string UserId, string? ManagerId, DateTimeOffset? EffectiveDate);

public sealed partial class SuccessFactorsClient
{
    /// <summary>Projects an <see cref="EmpJob"/> record into a <see cref="ManagerAssignment"/>. A startDate that
    /// does not match the <see cref="SapDates"/> policy is treated as unknown (null EffectiveDate) here rather than
    /// throwing, since job-selection ordering (see <see cref="FindCurrentAssignmentAsync"/>) must remain total.</summary>
    public static ManagerAssignment ToManagerAssignment(EmpJob job)
    {
        ArgumentNullException.ThrowIfNull(job);
        if (string.IsNullOrWhiteSpace(job.UserId)) throw new ArgumentException("EmpJob requires a UserId.", nameof(job));
        DateTimeOffset? effective;
        try { effective = SapDates.ParseUtcOrNull(job.StartDate); } catch (FormatException) { effective = null; }
        return new ManagerAssignment(job.UserId, job.ManagerId, effective);
    }

    /// <summary>Walks the EmpJob.ManagerId chain for a user, up to <paramref name="maxDepth"/> hops.
    /// <list type="bullet">
    /// <item>A revisited userId - including an immediate self-reference (UserId == ManagerId) - throws
    /// <see cref="InvalidDataException"/>, using the same visited-set cycle guard already used for OData
    /// pagination in <see cref="EnumerateAsync{T}"/>.</item>
    /// <item>A missing/null manager ends the chain normally; the last entry's <see cref="ManagerAssignment.ManagerId"/>
    /// is null and no exception is thrown.</item>
    /// <item>Reaching <paramref name="maxDepth"/> without a cycle simply truncates the returned chain - a
    /// legitimately deep org chart is not itself an error.</item>
    /// <item>A user with multiple concurrent EmpJob records is resolved to a single "current" assignment per hop:
    /// the record with the latest parseable startDate, ties broken by the higher SequenceNumber (see
    /// <see cref="FindCurrentAssignmentAsync"/>).</item>
    /// </list>
    /// Each hop issues one filtered <see cref="EnumerateJobsAsync"/> traversal for a single user; this never
    /// performs a whole-tenant scan.</summary>
    public async Task<IReadOnlyList<ManagerAssignment>> ResolveManagerChainAsync(string userId, int maxDepth = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A userId is required.", nameof(userId));
        if (maxDepth is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        var chain = new List<ManagerAssignment>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = userId;
        for (var depth = 0; depth < maxDepth; depth++)
        {
            if (!visited.Add(current)) throw new InvalidDataException("Manager chain cycle (including self-reference) detected.");
            var assignment = await FindCurrentAssignmentAsync(current, cancellationToken);
            if (assignment is null) break;
            chain.Add(assignment);
            if (string.IsNullOrWhiteSpace(assignment.ManagerId)) break;
            current = assignment.ManagerId;
        }
        return chain;
    }

    private async Task<ManagerAssignment?> FindCurrentAssignmentAsync(string userId, CancellationToken cancellationToken)
    {
        EmpJob? best = null;
        var filter = $"userId eq '{userId.Replace("'", "''", StringComparison.Ordinal)}'";
        await foreach (var job in EnumerateJobsAsync(new ODataQuery(Filter: filter), cancellationToken))
        {
            if (!string.Equals(job.UserId, userId, StringComparison.Ordinal)) continue;
            if (best is null || IsMoreCurrent(job, best)) best = job;
        }
        return best is null ? null : ToManagerAssignment(best);
    }

    private static bool IsMoreCurrent(EmpJob candidate, EmpJob current)
    {
        var candidateDate = SafeDate(candidate.StartDate);
        var currentDate = SafeDate(current.StartDate);
        if (candidateDate != currentDate) return candidateDate > currentDate;
        return (candidate.SequenceNumber ?? long.MinValue) > (current.SequenceNumber ?? long.MinValue);
    }

    private static DateTimeOffset SafeDate(JsonElement? value)
    {
        try { return SapDates.ParseUtcOrNull(value) ?? DateTimeOffset.MinValue; }
        catch (FormatException) { return DateTimeOffset.MinValue; }
    }
}

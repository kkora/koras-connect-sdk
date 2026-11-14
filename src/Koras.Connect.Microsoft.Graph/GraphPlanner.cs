using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Planner task (MGR-017).</summary>
public sealed class GraphPlannerTask
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Microsoft Planner has no application-permission path at all - confirmed explicitly for both
    /// read and write in the official permissions table. Every Planner method on this client throws this
    /// exception locally, before any HTTP call, unless <see cref="GraphOptions.IsDelegatedContext"/> was
    /// explicitly set - this is the single clearest "do not even attempt app-only" case in the whole Graph
    /// catalog and must surface as a clear, specific failure, not a confusing runtime 403.</summary>
    private void RequireDelegatedContextForPlanner()
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("Planner has no application-permission path at all; it is delegated-only. Construct GraphClient with GraphOptions.IsDelegatedContext set to true and a delegated token source.");
    }

    /// <summary>Enumerates the signed-in user's plans via <c>GET /me/planner/plans</c> (MGR-017/019).
    /// Delegated-only.</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateMyPlansAsync(CancellationToken cancellationToken = default)
    {
        RequireDelegatedContextForPlanner();
        return EnumerateAsync<JsonDocument>("me/planner/plans", cancellationToken: cancellationToken);
    }

    /// <summary>Enumerates a plan's tasks via <c>GET /planner/plans/{planId}/tasks</c> (MGR-017/019).
    /// Delegated-only.</summary>
    public IAsyncEnumerable<GraphPlannerTask> EnumeratePlanTasksAsync(string planId, CancellationToken cancellationToken = default)
    {
        RequireDelegatedContextForPlanner();
        if (string.IsNullOrWhiteSpace(planId)) throw new ArgumentException("A plan id is required.", nameof(planId));
        return EnumerateAsync<GraphPlannerTask>($"planner/plans/{Uri.EscapeDataString(planId)}/tasks", cancellationToken: cancellationToken);
    }
}

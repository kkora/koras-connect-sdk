using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Microsoft To Do task (MGR-018).</summary>
public sealed class GraphTodoTask
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates <c>GET /me/todo/lists</c> (MGR-018/019). Unlike most other <c>/me</c> aliases, this
    /// path is documented to support both delegated and application permissions - no
    /// <see cref="GraphOptions.IsDelegatedContext"/> guard is imposed here, correcting the common misconception
    /// that To Do is delegated-only like Planner.</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateTodoListsAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<JsonDocument>("me/todo/lists", cancellationToken: cancellationToken);

    /// <summary>Enumerates a list's tasks via <c>GET /me/todo/lists/{listId}/tasks</c> (MGR-018/019). Works
    /// under both delegated and application tokens for reads, per the official overview.</summary>
    public IAsyncEnumerable<GraphTodoTask> EnumerateTodoTasksAsync(string listId, CancellationToken cancellationToken = default)
    {
        ValidateTodoListId(listId);
        return EnumerateAsync<GraphTodoTask>($"me/todo/lists/{Uri.EscapeDataString(listId)}/tasks", cancellationToken: cancellationToken);
    }

    /// <summary>Creates a task via <c>POST /me/todo/lists/{listId}/tasks</c> (MGR-018). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Write-side application-permission symmetry
    /// with the read path was not independently confirmed - only the read-side <c>Tasks.Read.All</c> scope was
    /// checked, so an app-only caller should independently verify write access works before relying on it.</summary>
    public async Task<string> CreateTodoTaskAsync(string listId, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the To Do-write privilege and business impact.");
        ValidateTodoListId(listId);
        var address = new Uri(_root, $"me/todo/lists/{Uri.EscapeDataString(listId)}/tasks");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Updates a task via <c>PATCH /me/todo/lists/{listId}/tasks/{taskId}</c> (MGR-018). Disabled
    /// unless <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdateTodoTaskAsync(string listId, string taskId, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the To Do-write privilege and business impact.");
        ValidateTodoListId(listId);
        if (string.IsNullOrWhiteSpace(taskId)) throw new ArgumentException("A task id is required.", nameof(taskId));
        var address = new Uri(_root, $"me/todo/lists/{Uri.EscapeDataString(listId)}/tasks/{Uri.EscapeDataString(taskId)}");
        try { await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static void ValidateTodoListId(string listId)
    {
        if (string.IsNullOrWhiteSpace(listId)) throw new ArgumentException("A To Do list id is required.", nameof(listId));
    }
}

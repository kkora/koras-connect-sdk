using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;

// Offline demonstration of the Microsoft Graph connector's collaboration surface: Teams channels/chats, Planner,
// and Microsoft To Do - none of which is touched by samples/MicrosoftGraph.OfflineWalkthrough (that sample covers
// auth/diagnostics/users/groups/OneDrive/delta instead). Every HTTP response below is synthetic; this sample
// never contacts a live Entra tenant. Planner has no application-permission path at all (confirmed vendor
// constraint, not an SDK limitation), so this client is constructed with IsDelegatedContext: true throughout -
// Teams and To Do both work identically under either permission tier, so this choice doesn't narrow what they
// demonstrate.
Console.WriteLine("Koras.Connect.Microsoft.Graph offline sample: Teams channels/chats -> Planner -> Microsoft To Do.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const string teamId = "team-1";
const string channelId = "channel-1";
const string chatId = "chat-1";
const string planId = "plan-1";
const string todoListId = "todo-list-1";

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path == "/v1.0/teams")
        return Reply("""{"value":[{"id":"team-1","displayName":"Project Phoenix","description":"Cross-team delivery","visibility":"private"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/teams/{teamId}/channels")
        return Reply("""{"value":[{"id":"channel-1","displayName":"General"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/teams/{teamId}/channels/{channelId}/messages")
        return Reply("""{"value":[{"id":"msg-1","body":{"content":"Kickoff at 10am"}}]}""");
    if (request.Method == HttpMethod.Post && path == $"/v1.0/teams/{teamId}/channels/{channelId}/messages")
        return Reply("""{"id":"msg-2"}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/chats/{chatId}/messages")
        return Reply("""{"value":[{"id":"chatmsg-1","body":{"content":"Can you review the doc?"}}]}""");
    if (request.Method == HttpMethod.Post && path == $"/v1.0/chats/{chatId}/messages")
        return Reply("""{"id":"chatmsg-2"}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/me/planner/plans")
        return Reply("""{"value":[{"id":"plan-1","title":"Phoenix Launch Plan"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/planner/plans/{planId}/tasks")
        return Reply("""{"value":[{"id":"task-1","title":"Draft launch checklist"}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/me/todo/lists")
        return Reply("""{"value":[{"id":"todo-list-1","displayName":"My Day"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/me/todo/lists/{todoListId}/tasks")
        return Reply("""{"value":[{"id":"todo-1","title":"Follow up with vendor"}]}""");
    if (request.Method == HttpMethod.Post && path == $"/v1.0/me/todo/lists/{todoListId}/tasks")
        return Reply("""{"id":"todo-2"}""", HttpStatusCode.Created);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var tokens = new StaticTokenSource("sample-access-token");
var client = new GraphClient(new IntegrationTransport(http, tokens), new GraphOptions(IsDelegatedContext: true));

Console.WriteLine("Enumerating teams:");
await foreach (var team in client.EnumerateTeamsAsync())
    Console.WriteLine($"  Team: {team.DisplayName} (visibility={team.Visibility})");

Console.WriteLine("Enumerating channels and messages:");
await foreach (var channel in client.EnumerateChannelsAsync(teamId))
    Console.WriteLine($"  Channel: {channel.RootElement.GetProperty("displayName").GetString()}");
await foreach (var message in client.EnumerateChannelMessagesAsync(teamId, channelId))
    Console.WriteLine($"  Channel message: {message.Body}");
var sentChannelMessageId = await client.SendChannelMessageAsync(teamId, channelId, "Thanks, see you there!", writesEnabled: true);
Console.WriteLine($"Sent channel message {sentChannelMessageId}.");

Console.WriteLine("Enumerating 1:1 chat messages:");
await foreach (var message in client.EnumerateChatMessagesAsync(chatId))
    Console.WriteLine($"  Chat message: {message.Body}");
var sentChatMessageId = await client.SendChatMessageAsync(chatId, "Reviewing now.", writesEnabled: true);
Console.WriteLine($"Sent chat message {sentChatMessageId}.");

Console.WriteLine("Enumerating Planner plans and tasks (delegated-only - no app-only path exists):");
await foreach (var plan in client.EnumerateMyPlansAsync())
    Console.WriteLine($"  Plan: {plan.RootElement.GetProperty("title").GetString()}");
await foreach (var task in client.EnumeratePlanTasksAsync(planId))
    Console.WriteLine($"  Plan task: {task.Title}");

Console.WriteLine("Enumerating Microsoft To Do lists and tasks:");
await foreach (var list in client.EnumerateTodoListsAsync())
    Console.WriteLine($"  To Do list: {list.RootElement.GetProperty("displayName").GetString()}");
await foreach (var task in client.EnumerateTodoTasksAsync(todoListId))
    Console.WriteLine($"  To Do task: {task.Title}");
using var newTaskFields = System.Text.Json.JsonDocument.Parse("""{"title":"Send the recap email"}""");
var newTaskId = await client.CreateTodoTaskAsync(todoListId, newTaskFields.RootElement, writesEnabled: true);
Console.WriteLine($"Created To Do task {newTaskId}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

sealed class StaticTokenSource(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

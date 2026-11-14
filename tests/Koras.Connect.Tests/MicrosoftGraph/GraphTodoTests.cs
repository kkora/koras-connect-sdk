using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-018: Microsoft To Do - read works without a delegated context, correcting the common
/// misconception that To Do is delegated-only like Planner.</summary>
public sealed class GraphTodoTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateTodoListsWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"l1"}]}""")); });
        await foreach (var _ in client.EnumerateTodoListsAsync()) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/me/todo/lists", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateTodoTasksWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"t1","title":"Buy milk"}]}""")); });
        var tasks = new List<GraphTodoTask>();
        await foreach (var task in client.EnumerateTodoTasksAsync("l1")) tasks.Add(task);
        Assert.Single(tasks);
        Assert.Equal("Buy milk", tasks[0].Title);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/todo/lists/l1/tasks", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateTodoTaskRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateTodoTaskAsync("l1", JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateTodoTaskPostsAndParsesTheId()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"t1"}""")); });
        var id = await client.CreateTodoTaskAsync("l1", JsonDocument.Parse("""{"title":"Buy milk"}""").RootElement, writesEnabled: true);
        Assert.Equal("t1", id);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/todo/lists/l1/tasks", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UpdateTodoTaskRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateTodoTaskAsync("l1", "t1", JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpdateTodoTaskPatchesTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }); });
        await client.UpdateTodoTaskAsync("l1", "t1", JsonDocument.Parse("""{"status":"completed"}""").RootElement, writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/todo/lists/l1/tasks/t1", request.RequestUri!.AbsoluteUri);
    }
}

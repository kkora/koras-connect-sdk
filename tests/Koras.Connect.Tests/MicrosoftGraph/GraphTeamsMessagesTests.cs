using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-016: Teams messages and chats - channel and chat message read/send composition.</summary>
public sealed class GraphTeamsMessagesTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateChannelMessagesParsesTheFlattenedBody()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"m1","body":{"contentType":"html","content":"Hello team"}}]}""")); });
        var messages = new List<GraphChatMessage>();
        await foreach (var message in client.EnumerateChannelMessagesAsync("t1", "c1")) messages.Add(message);
        Assert.Single(messages);
        Assert.Equal("Hello team", messages[0].Body);
        Assert.Equal("https://graph.microsoft.com/v1.0/teams/t1/channels/c1/messages", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task SendChannelMessageRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendChannelMessageAsync("t1", "c1", "Hello"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SendChannelMessagePostsTheDocumentedBodyEnvelope()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"id":"m1"}"""); });
        var id = await client.SendChannelMessageAsync("t1", "c1", "Hello team", writesEnabled: true);
        Assert.Equal("m1", id);
        Assert.Equal("https://graph.microsoft.com/v1.0/teams/t1/channels/c1/messages", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("Hello team", parsed.RootElement.GetProperty("body").GetProperty("content").GetString());
    }

    [Fact]
    public async Task EnumerateChatMessagesHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateChatMessagesAsync("chat1")) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/chats/chat1/messages", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task SendChatMessageRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendChatMessageAsync("chat1", "Hello"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SendChatMessagePostsToTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"m1"}""")); });
        var id = await client.SendChatMessageAsync("chat1", "Hello", writesEnabled: true);
        Assert.Equal("m1", id);
        Assert.Equal("https://graph.microsoft.com/v1.0/chats/chat1/messages", request!.RequestUri!.AbsoluteUri);
    }
}

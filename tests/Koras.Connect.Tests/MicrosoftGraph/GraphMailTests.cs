using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-013: Outlook mail - the /me delegated-only guard, $top clamping, and sendMail composition.</summary>
public sealed class GraphMailTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, GraphOptions? options = null)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")), options);

    [Fact]
    public async Task EnumerateMyMessagesThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.EnumerateMyMessagesAsync()) { }
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task EnumerateMyMessagesParsesTheHtmlBody()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"m1","subject":"Hi","body":{"contentType":"html","content":"<p>Hi</p>"}}]}""")); },
            new GraphOptions(IsDelegatedContext: true));
        var messages = new List<GraphMessage>();
        await foreach (var message in client.EnumerateMyMessagesAsync()) messages.Add(message);
        Assert.Single(messages);
        Assert.Equal("<p>Hi</p>", messages[0].BodyHtml);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/messages", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateUserMessagesWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateUserMessagesAsync("u1")) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/users/u1/messages", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task TopAboveTheDocumentedMaxIsClampedNotRejected()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateUserMessagesAsync("u1", new GraphQuery(Top: 5000))) { }
        Assert.Contains($"$top={GraphClient.MaxMessagesTop}", requested!.Query);
    }

    [Fact]
    public async Task SendMailRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); }, new GraphOptions(IsDelegatedContext: true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendMailAsync("Hi", "<p>Hi</p>", ["a@contoso.com"]));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SendMailRequiresADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendMailAsync("Hi", "<p>Hi</p>", ["a@contoso.com"], writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SendMailTreats202NoBodyAsSuccess()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) =>
        {
            request = r;
            body = await r.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }, new GraphOptions(IsDelegatedContext: true));
        await client.SendMailAsync("Hi there", "<p>Hi</p>", ["a@contoso.com", "b@contoso.com"], writesEnabled: true);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/sendMail", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        var message = parsed.RootElement.GetProperty("message");
        Assert.Equal("Hi there", message.GetProperty("subject").GetString());
        Assert.Equal(2, message.GetProperty("toRecipients").GetArrayLength());
    }
}

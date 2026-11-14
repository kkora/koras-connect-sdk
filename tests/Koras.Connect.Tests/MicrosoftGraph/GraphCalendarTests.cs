using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-014: Outlook calendar - event CRUD, with no artificial /me-only restriction since Graph
/// documents app-only/delegated symmetry for calendars.</summary>
public sealed class GraphCalendarTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateEventsWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) =>
        {
            requested = r.RequestUri;
            return Task.FromResult(Json("""{"value":[{"id":"e1","subject":"Standup","start":{"dateTime":"2026-11-14T09:00:00","timeZone":"Pacific Standard Time"},"end":{"dateTime":"2026-11-14T09:30:00","timeZone":"Pacific Standard Time"}}]}"""));
        });
        var events = new List<GraphEvent>();
        await foreach (var ev in client.EnumerateEventsAsync("u1")) events.Add(ev);
        Assert.Single(events);
        Assert.Equal("Standup", events[0].Subject);
        Assert.Equal("Pacific Standard Time", events[0].Start!.TimeZone);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/u1/events", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateEventRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateEventAsync("u1", new GraphEvent { Subject = "Standup" }));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateEventPostsToTheUserScopedResourceAndParsesTheId()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"id":"e1"}""", HttpStatusCode.Created); });
        var id = await client.CreateEventAsync("u1", new GraphEvent { Subject = "Standup" }, writesEnabled: true);
        Assert.Equal("e1", id);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/u1/events", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("Standup", parsed.RootElement.GetProperty("subject").GetString());
    }

    [Fact]
    public async Task UpdateAndDeleteRequireWritesEnabled()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateEventAsync("u1", "e1", JsonDocument.Parse("{}").RootElement));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteEventAsync("u1", "e1"));
    }

    [Fact]
    public async Task DeleteSendsToTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteEventAsync("u1", "e1", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/u1/events/e1", request.RequestUri!.AbsoluteUri);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-020: change notifications and subscriptions - creation/renewal/deletion composition.</summary>
public sealed class GraphSubscriptionsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task CreateSubscriptionRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CreateSubscriptionAsync("users", "updated", new Uri("https://example.com/notify"), DateTimeOffset.UtcNow.AddHours(1)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateSubscriptionPostsTheDocumentedBodyAndParsesTheResponse()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var expiration = DateTimeOffset.Parse("2026-11-15T00:00:00Z");
        var client = Client(async (r, ct) =>
        {
            request = r;
            body = await r.Content!.ReadAsStringAsync(ct);
            return Json($$"""{"id":"s1","resource":"users","changeType":"updated","expirationDateTime":"{{expiration:O}}","notificationUrl":"https://example.com/notify"}""", HttpStatusCode.Created);
        });
        var subscription = await client.CreateSubscriptionAsync("users", "updated", new Uri("https://example.com/notify"), expiration, writesEnabled: true);
        Assert.Equal("s1", subscription.Id);
        Assert.Equal(expiration, subscription.ExpirationDateTime);
        Assert.Equal("https://graph.microsoft.com/v1.0/subscriptions", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("users", parsed.RootElement.GetProperty("resource").GetString());
        Assert.Equal("updated", parsed.RootElement.GetProperty("changeType").GetString());
        Assert.Equal("https://example.com/notify", parsed.RootElement.GetProperty("notificationUrl").GetString());
        Assert.False(parsed.RootElement.GetProperty("includeResourceData").GetBoolean());
    }

    [Fact]
    public async Task IncludeResourceDataIsSurfacedAsItsOwnExplicitOptIn()
    {
        string? body = null;
        var client = Client(async (r, ct) =>
        {
            body = await r.Content!.ReadAsStringAsync(ct);
            return Json($$"""{"id":"s1","resource":"users","changeType":"updated","expirationDateTime":"{{DateTimeOffset.UtcNow:O}}","notificationUrl":"https://example.com/notify"}""");
        });
        await client.CreateSubscriptionAsync("users", "updated", new Uri("https://example.com/notify"), DateTimeOffset.UtcNow.AddHours(1), includeResourceData: true, writesEnabled: true);
        using var parsed = JsonDocument.Parse(body!);
        Assert.True(parsed.RootElement.GetProperty("includeResourceData").GetBoolean());
    }

    [Fact]
    public async Task RenewSubscriptionRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.RenewSubscriptionAsync("s1", DateTimeOffset.UtcNow.AddHours(1)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task RenewSubscriptionPatchesTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }); });
        await client.RenewSubscriptionAsync("s1", DateTimeOffset.UtcNow.AddHours(1), writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/subscriptions/s1", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task DeleteSubscriptionRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteSubscriptionAsync("s1"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DeleteSubscriptionSendsToTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteSubscriptionAsync("s1", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/subscriptions/s1", request.RequestUri!.AbsoluteUri);
    }
}

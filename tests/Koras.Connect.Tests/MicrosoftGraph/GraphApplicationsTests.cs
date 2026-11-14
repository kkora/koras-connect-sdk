using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-007: app registrations and service principals - read/create composition for both resources.</summary>
public sealed class GraphApplicationsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateApplicationsHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"a1","appId":"app-1","displayName":"My App"}]}""")); });
        var apps = new List<GraphApplication>();
        await foreach (var app in client.EnumerateApplicationsAsync()) apps.Add(app);
        Assert.Single(apps);
        Assert.Equal("My App", apps[0].DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/applications", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetApplicationReadsById()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"a1","displayName":"My App"}""")); });
        var app = await client.GetApplicationAsync("a1");
        Assert.Equal("My App", app.DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/applications/a1", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateApplicationRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateApplicationAsync(JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateApplicationPostsAndParsesTheId()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"a1"}""")); });
        var id = await client.CreateApplicationAsync(JsonDocument.Parse("""{"displayName":"My App"}""").RootElement, writesEnabled: true);
        Assert.Equal("a1", id);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/applications", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateServicePrincipalsHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"sp1","appId":"app-1"}]}""")); });
        var sps = new List<GraphServicePrincipal>();
        await foreach (var sp in client.EnumerateServicePrincipalsAsync()) sps.Add(sp);
        Assert.Single(sps);
        Assert.Equal("https://graph.microsoft.com/v1.0/servicePrincipals", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetServicePrincipalReadsById()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"sp1","appId":"app-1"}""")); });
        var sp = await client.GetServicePrincipalAsync("sp1");
        Assert.Equal("app-1", sp.AppId);
        Assert.Equal("https://graph.microsoft.com/v1.0/servicePrincipals/sp1", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateServicePrincipalRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateServicePrincipalAsync(JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateServicePrincipalPostsAndParsesTheId()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"sp1"}""")); });
        var id = await client.CreateServicePrincipalAsync(JsonDocument.Parse("""{"appId":"app-1"}""").RootElement, writesEnabled: true);
        Assert.Equal("sp1", id);
        Assert.Equal("https://graph.microsoft.com/v1.0/servicePrincipals", request!.RequestUri!.AbsoluteUri);
    }
}

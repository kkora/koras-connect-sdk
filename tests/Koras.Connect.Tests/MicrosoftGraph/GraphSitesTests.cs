using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-011: SharePoint sites and document libraries - the three documented resolution-path forms.</summary>
public sealed class GraphSitesTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task GetRootSiteHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"contoso.sharepoint.com,abc,def","displayName":"Root","webUrl":"https://contoso.sharepoint.com"}""")); });
        var site = await client.GetRootSiteAsync();
        Assert.Equal("Root", site.DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/root", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetSiteByHostnameWithNoPathHitsTheHostnameOnlyResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"s1"}""")); });
        await client.GetSiteByPathAsync("contoso.sharepoint.com");
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/contoso.sharepoint.com", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetSiteByPathComposesTheColonPathForm()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"s1"}""")); });
        await client.GetSiteByPathAsync("contoso.sharepoint.com", "/sites/marketing");
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/contoso.sharepoint.com:/sites/marketing", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateSiteDrivesHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"d1"}]}""")); });
        var drives = new List<System.Text.Json.JsonDocument>();
        await foreach (var drive in client.EnumerateSiteDrivesAsync("s1")) drives.Add(drive);
        Assert.Single(drives);
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/s1/drives", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EmptyHostnameOrSiteIdIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetSiteByPathAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(async () => { await foreach (var _ in client.EnumerateSiteDrivesAsync("")) { } });
        Assert.Equal(0, calls);
    }
}

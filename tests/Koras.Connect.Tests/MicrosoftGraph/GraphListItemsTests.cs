using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-012: SharePoint lists and items - the site-scoped-only path shape and item CRUD.</summary>
public sealed class GraphListItemsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateListsHitsTheSiteScopedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"l1"}]}""")); });
        var lists = new List<JsonDocument>();
        await foreach (var list in client.EnumerateListsAsync("s1")) lists.Add(list);
        Assert.Single(lists);
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/s1/lists", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateListItemsParsesTheFieldsSubResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"i1","fields":{"Title":"Row 1"}}]}""")); });
        var items = new List<GraphListItem>();
        await foreach (var item in client.EnumerateListItemsAsync("s1", "l1")) items.Add(item);
        Assert.Single(items);
        Assert.Equal("i1", items[0].Id);
        Assert.Equal("Row 1", items[0].Fields["Title"].GetString());
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/s1/lists/l1/items", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateListItemRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateListItemAsync("s1", "l1", JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateListItemPostsTheDocumentedFieldsEnvelope()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"id":"i1"}"""); });
        var id = await client.CreateListItemAsync("s1", "l1", JsonDocument.Parse("""{"Title":"New Row"}""").RootElement, writesEnabled: true);
        Assert.Equal("i1", id);
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/s1/lists/l1/items", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("New Row", parsed.RootElement.GetProperty("fields").GetProperty("Title").GetString());
    }

    [Fact]
    public async Task UpdateListItemFieldsRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateListItemFieldsAsync("s1", "l1", "i1", JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpdateListItemFieldsTargetsTheFieldsSubResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateListItemFieldsAsync("s1", "l1", "i1", JsonDocument.Parse("""{"Title":"Updated"}""").RootElement, writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/sites/s1/lists/l1/items/i1/fields", request.RequestUri!.AbsoluteUri);
    }
}

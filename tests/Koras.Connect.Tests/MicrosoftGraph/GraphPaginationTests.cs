using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-019 (pulled forward as MGR-004's structural dependency): <c>@odata.nextLink</c> pagination and
/// <c>GraphQuery</c> composition, including the <c>ConsistencyLevel: eventual</c> header for <c>$count</c>/<c>$search</c>.</summary>
public sealed class GraphPaginationTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    private sealed record Item([property: JsonPropertyName("name")] string Name);

    [Fact]
    public async Task EnumerateFollowsNextLinkAcrossPages()
    {
        var calls = 0;
        var requested = new List<Uri>();
        var client = Client((r, _) =>
        {
            calls++;
            requested.Add(r.RequestUri!);
            return Task.FromResult(calls switch
            {
                1 => Json("""{"value":[{"name":"Adventure Works"}],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users?$skiptoken=abc"}"""),
                2 => Json("""{"value":[{"name":"Fabrikam Inc"}]}"""),
                _ => throw new InvalidOperationException("unexpected extra call"),
            });
        });
        var items = new List<Item>();
        await foreach (var item in client.EnumerateAsync<Item>("users")) items.Add(item);
        Assert.Equal(["Adventure Works", "Fabrikam Inc"], items.Select(i => i.Name));
        Assert.Equal(2, calls);
        Assert.Equal("https://graph.microsoft.com/v1.0/users", requested[0].AbsoluteUri);
        Assert.Equal("https://graph.microsoft.com/v1.0/users?$skiptoken=abc", requested[1].AbsoluteUri);
    }

    [Fact]
    public async Task ARepeatedNextLinkIsTreatedAsACycle()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"name":"X"}],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users?$skiptoken=same"}""")));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in client.EnumerateAsync<Item>("users")) { }
        });
    }

    [Fact]
    public async Task MissingValueArraySurfacesAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"notValue":[]}""")));
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.EnumerateAsync<Item>("users")) { }
        });
    }

    [Fact]
    public async Task QueryOptionsComposeIntoTheDocumentedODataQueryString()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        var query = new GraphQuery(Select: "id,displayName", Filter: "startswith(displayName,'A')", OrderBy: "displayName", Top: 10);
        await foreach (var _ in client.EnumerateAsync<Item>("users", query)) { }
        var decoded = Uri.UnescapeDataString(requested!.Query);
        Assert.Contains("$select=id,displayName", decoded);
        Assert.Contains("$filter=startswith(displayName,'A')", decoded);
        Assert.Contains("$orderby=displayName", decoded);
        Assert.Contains("$top=10", decoded);
    }

    [Theory]
    [InlineData(true, null)]
    [InlineData(null, "Contoso")]
    public async Task CountOrSearchSendsConsistencyLevelEventual(bool? count, string? search)
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateAsync<Item>("users", new GraphQuery(Count: count, Search: search))) { }
        Assert.Equal("eventual", request!.Headers.GetValues("ConsistencyLevel").Single());
    }

    [Fact]
    public async Task NoConsistencyLevelHeaderWhenNeitherCountNorSearchIsUsed()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateAsync<Item>("users", new GraphQuery(Select: "id"))) { }
        Assert.False(request!.Headers.Contains("ConsistencyLevel"));
    }

    [Fact]
    public async Task ResourcePathsMustBeRelativeAndNonEmpty()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<ArgumentException>(async () => { await foreach (var _ in client.EnumerateAsync<Item>("")) { } });
        await Assert.ThrowsAsync<ArgumentException>(async () => { await foreach (var _ in client.EnumerateAsync<Item>("/users")) { } });
        await Assert.ThrowsAsync<ArgumentException>(async () => { await foreach (var _ in client.EnumerateAsync<Item>("https://evil.example/users")) { } });
    }
}

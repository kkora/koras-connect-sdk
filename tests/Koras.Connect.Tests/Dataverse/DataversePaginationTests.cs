using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-006: pagination and continuation links - <c>Prefer: odata.maxpagesize</c>, opaque
/// <c>@odata.nextLink</c> forwarding, <c>$top</c>/<c>maxpagesize</c> mutual exclusivity, and cycle/off-origin guards.</summary>
public sealed class DataversePaginationTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task MaxPageSizeIsSentAsAPreferHeader()
    {
        string? prefer = null;
        var client = Client((r, _) => { prefer = r.Headers.TryGetValues("Prefer", out var v) ? v.First() : null; return Task.FromResult(Json("""{"value":[]}""")); });
        await client.EnumeratePageAsync("accounts", maxPageSize: 250);
        Assert.Equal("odata.maxpagesize=250", prefer);
    }

    [Fact]
    public async Task MaxPageSizeAbove5000IsNotClientRejected()
    {
        // Documented as server-ignored/capped, not a client error - the connector must not second-guess the server.
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[]}""")));
        var page = await client.EnumeratePageAsync("accounts", maxPageSize: 50_000);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task TopAndMaxPageSizeTogetherAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("""{"value":[]}""")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.EnumeratePageAsync("accounts", new DataverseQuery(Top: 10), maxPageSize: 100));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task NextLinkIsFollowedTransparently()
    {
        var calls = 0;
        var client = Client((r, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(Json("""{"value":[{"accountid":"1"}],"@odata.nextLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken=opaque-cookie"}"""));
            Assert.EndsWith("$skiptoken=opaque-cookie", r.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            return Task.FromResult(Json("""{"value":[{"accountid":"2"}]}"""));
        });
        var items = new List<System.Text.Json.JsonElement>();
        await foreach (var item in client.EnumerateAsync("accounts")) items.Add(item);
        Assert.Equal(2, items.Count);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task EmptyPageWithANonNullNextLinkIsStillFollowed()
    {
        var calls = 0;
        var client = Client((r, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(Json("""{"value":[],"@odata.nextLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken=cookie"}"""));
            return Task.FromResult(Json("""{"value":[{"accountid":"only"}]}"""));
        });
        var items = new List<System.Text.Json.JsonElement>();
        await foreach (var item in client.EnumerateAsync("accounts")) items.Add(item);
        Assert.Equal(2, calls);
        Assert.Single(items);
    }

    [Fact]
    public async Task OffOriginNextLinkIsRejected()
    {
        var calls = 0;
        var client = Client((r, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(Json("""{"value":[],"@odata.nextLink":"https://evil.example.com/api/data/v9.2/accounts?$skiptoken=cookie"}"""));
            throw new InvalidOperationException("Should never reach a second request.");
        });
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in client.EnumerateAsync("accounts")) { }
        });
    }

    [Fact]
    public async Task RepeatedIdenticalNextLinkIsDetectedAsACycle()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"accountid":"x"}],"@odata.nextLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken=same"}""")));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in client.EnumerateAsync("accounts")) { }
        });
    }

    [Fact]
    public async Task MaxPagesGuardStopsARunawayEnumeration()
    {
        var http = new HttpClient(new Handler((r, _) =>
        {
            var token = Uri.EscapeDataString(Guid.NewGuid().ToString());
            return Task.FromResult(Json($$"""{"value":[{"accountid":"x"}],"@odata.nextLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken={{token}}"}"""));
        }));
        var client = new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot, MaxPages: 2));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in client.EnumerateAsync("accounts")) { }
        });
    }
}

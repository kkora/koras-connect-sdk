using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-019 (delta half): <c>@odata.nextLink</c>-vs-<c>@odata.deltaLink</c> distinction, cycle guard,
/// and the distinct <c>410</c>/expired-token classification.</summary>
public sealed class GraphDeltaTests
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
    public async Task FirstCallTargetsTheDeltaResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/users/delta?$deltatoken=abc"}""")); });
        await client.GetDeltaAsync<Item>("users");
        Assert.Equal("https://graph.microsoft.com/v1.0/users/delta", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task FollowsNextLinkUntilDeltaLinkTerminatesThePass()
    {
        var calls = 0;
        var client = Client((_, _) =>
        {
            calls++;
            return Task.FromResult(calls switch
            {
                1 => Json("""{"value":[{"name":"Adventure Works"}],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users/delta?$skiptoken=p2"}"""),
                2 => Json("""{"value":[{"name":"Fabrikam Inc"}],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/users/delta?$deltatoken=final"}"""),
                _ => throw new InvalidOperationException("unexpected extra call"),
            });
        });
        var page = await client.GetDeltaAsync<Item>("users");
        Assert.Equal(["Adventure Works", "Fabrikam Inc"], page.Items.Select(i => i.Name));
        Assert.Equal(2, calls);
        Assert.NotNull(page.DeltaLink);
        Assert.Contains("$deltatoken=final", page.DeltaLink!.OpaqueValue);
    }

    [Fact]
    public async Task ResumingFromADeltaLinkSkipsTheInitialDeltaPath()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/users/delta?$deltatoken=next"}""")); });
        var resumeFrom = new ContinuationToken("MicrosoftGraph", "users", "https://graph.microsoft.com/v1.0/users/delta?$deltatoken=abc");
        await client.GetDeltaAsync<Item>("users", resumeFrom);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/delta?$deltatoken=abc", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task A410ResponseClassifiesAsExpiredTokenNotAGenericContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"error":{"code":"syncStateNotFound"}}""", (HttpStatusCode)410)));
        var ex = await Assert.ThrowsAsync<GraphDeltaTokenExpiredException>(() => client.GetDeltaAsync<Item>("users"));
        Assert.Equal("graph_delta_token_expired", ex.Details.Code);
    }

    [Fact]
    public async Task ARepeatedNextLinkIsTreatedAsACycle()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users/delta?$skiptoken=same"}""")));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetDeltaAsync<Item>("users"));
    }

    [Fact]
    public async Task MissingBothLinksSurfacesAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[]}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.GetDeltaAsync<Item>("users"));
    }
}

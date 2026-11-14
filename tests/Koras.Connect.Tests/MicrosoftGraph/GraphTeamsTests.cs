using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-015: Teams and channels - the restricted list-call field shape and channel enumeration.</summary>
public sealed class GraphTeamsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateTeamsParsesTheRestrictedListShape()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"t1","displayName":"Engineering","description":null,"visibility":"private"}]}""")); });
        var teams = new List<GraphTeamSummary>();
        await foreach (var team in client.EnumerateTeamsAsync()) teams.Add(team);
        Assert.Single(teams);
        Assert.Equal("Engineering", teams[0].DisplayName);
        Assert.Null(teams[0].Description);
        Assert.Equal("https://graph.microsoft.com/v1.0/teams", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetTeamReadsFullDetailById()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"t1","displayName":"Engineering","webUrl":"https://teams.microsoft.com/..."}""")); });
        await client.GetTeamAsync("t1");
        Assert.Equal("https://graph.microsoft.com/v1.0/teams/t1", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateChannelsHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"c1","displayName":"General"}]}""")); });
        var channels = new List<System.Text.Json.JsonDocument>();
        await foreach (var channel in client.EnumerateChannelsAsync("t1")) channels.Add(channel);
        Assert.Single(channels);
        Assert.Equal("https://graph.microsoft.com/v1.0/teams/t1/channels", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EmptyTeamIdIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetTeamAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(async () => { await foreach (var _ in client.EnumerateChannelsAsync("")) { } });
        Assert.Equal(0, calls);
    }
}

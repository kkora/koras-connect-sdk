using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-005: groups and memberships - enumeration and <c>$ref</c> membership add/remove composition.</summary>
public sealed class GraphGroupsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateGroupsHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"g1","displayName":"Marketing"}]}""")); });
        var groups = new List<GraphGroup>();
        await foreach (var group in client.EnumerateGroupsAsync()) groups.Add(group);
        Assert.Single(groups);
        Assert.Equal("Marketing", groups[0].DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/groups", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateGroupMembersHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"u1","displayName":"Ada"}]}""")); });
        var members = new List<GraphUser>();
        await foreach (var member in client.EnumerateGroupMembersAsync("g1")) members.Add(member);
        Assert.Single(members);
        Assert.Equal("https://graph.microsoft.com/v1.0/groups/g1/members", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task AddGroupMemberRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.AddGroupMemberAsync("g1", "u1"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AddGroupMemberPostsTheDocumentedRefBody()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return new HttpResponseMessage(HttpStatusCode.NoContent); });
        await client.AddGroupMemberAsync("g1", "u1", writesEnabled: true);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/groups/g1/members/$ref", request.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("https://graph.microsoft.com/v1.0/directoryObjects/u1", parsed.RootElement.GetProperty("@odata.id").GetString());
    }

    [Fact]
    public async Task RemoveGroupMemberRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.RemoveGroupMemberAsync("g1", "u1"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task RemoveGroupMemberSendsToTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.RemoveGroupMemberAsync("g1", "u1", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/groups/g1/members/u1/$ref", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task EmptyGroupOrMemberIdIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.AddGroupMemberAsync("", "u1", writesEnabled: true));
        await Assert.ThrowsAsync<ArgumentException>(() => client.AddGroupMemberAsync("g1", "", writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

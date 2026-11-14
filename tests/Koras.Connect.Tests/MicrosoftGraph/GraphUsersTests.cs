using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-004: users and profiles - CRUD composition and the <c>/me</c> delegated-only local guard.</summary>
public sealed class GraphUsersTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, GraphOptions? options = null)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")), options);

    [Fact]
    public async Task EnumerateUsersHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"u1","displayName":"Ada","userPrincipalName":"ada@contoso.com"}]}""")); });
        var users = new List<GraphUser>();
        await foreach (var user in client.EnumerateUsersAsync()) users.Add(user);
        Assert.Single(users);
        Assert.Equal("Ada", users[0].DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/users", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetUserReadsByIdOrUpn()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"u1","displayName":"Ada"}""")); });
        var user = await client.GetUserAsync("ada@contoso.com");
        Assert.Equal("Ada", user.DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/ada%40contoso.com", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetMeThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetMeAsync());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetMeSucceedsWithADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"u1","displayName":"Ada"}""")); }, new GraphOptions(IsDelegatedContext: true));
        var me = await client.GetMeAsync();
        Assert.Equal("Ada", me.DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/me", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateUserRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateUserAsync(JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateUserPostsToUsersAndParsesTheId()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"u1"}""")); });
        var id = await client.CreateUserAsync(JsonDocument.Parse("""{"displayName":"Ada"}""").RootElement, writesEnabled: true);
        Assert.Equal("u1", id);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/users", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UpdateAndDeleteRequireWritesEnabled()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateUserAsync("u1", JsonDocument.Parse("{}").RootElement));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteUserAsync("u1"));
    }

    [Fact]
    public async Task DeleteSendsToTheDocumentedResource()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteUserAsync("u1", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/users/u1", request.RequestUri!.AbsoluteUri);
    }
}

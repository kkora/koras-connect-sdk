using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-021: batch requests - the 20-request cap, the documented request/response envelope, and
/// id-based (not array-position-based) result mapping.</summary>
public sealed class GraphBatchTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task MoreThan20RequestsAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = Enumerable.Range(0, 21).Select(i => new GraphBatchRequest($"{i}", HttpMethod.Get, "/me")).ToArray();
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteBatchAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DuplicateIdsAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = new[] { new GraphBatchRequest("1", HttpMethod.Get, "/me"), new GraphBatchRequest("1", HttpMethod.Get, "/me/drive") };
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteBatchAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AWriteSubrequestWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = new[] { new GraphBatchRequest("1", HttpMethod.Post, "/users", JsonDocument.Parse("{}").RootElement) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteBatchAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ReadOnlyBatchNeedsNoWriteOptIn()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"responses":[{"id":"1","status":200,"body":{"id":"u1"}}]}""")));
        var requests = new[] { new GraphBatchRequest("1", HttpMethod.Get, "/me") };
        var results = await client.ExecuteBatchAsync(requests);
        Assert.Single(results);
        Assert.True(results[0].Succeeded);
    }

    [Fact]
    public async Task PayloadIncludesTheDocumentedPerRequestFields()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"responses":[]}"""); });
        var requests = new[] { new GraphBatchRequest("1", HttpMethod.Get, "/me", DependsOn: ["0"]) };
        await client.ExecuteBatchAsync(requests);
        Assert.Equal("https://graph.microsoft.com/v1.0/$batch", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        var sub = parsed.RootElement.GetProperty("requests")[0];
        Assert.Equal("1", sub.GetProperty("id").GetString());
        Assert.Equal("GET", sub.GetProperty("method").GetString());
        Assert.Equal("/me", sub.GetProperty("url").GetString());
        Assert.Equal("0", sub.GetProperty("dependsOn")[0].GetString());
    }

    [Fact]
    public async Task ResultsAreMappedByIdNotByResponseArrayPosition()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"responses":[
              {"id":"2","status":200,"body":{"name":"second"}},
              {"id":"1","status":424},
              {"id":"0","status":200,"body":{"name":"first"}}
            ]}
            """)));
        var requests = new[]
        {
            new GraphBatchRequest("0", HttpMethod.Get, "/sites/root"),
            new GraphBatchRequest("1", HttpMethod.Get, "/sites/root/lists/l1", DependsOn: ["0"]),
            new GraphBatchRequest("2", HttpMethod.Get, "/sites/root/drives"),
        };
        var results = await client.ExecuteBatchAsync(requests);
        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].Index);
        Assert.True(results[0].Succeeded);
        Assert.Equal(1, results[1].Index);
        Assert.False(results[1].Succeeded);
        Assert.Equal("http_424", results[1].ErrorCode);
        Assert.Equal(2, results[2].Index);
        Assert.True(results[2].Succeeded);
    }

    [Fact]
    public async Task AnUnrecognizedResponseIdSurfacesAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"responses":[{"id":"unknown","status":200}]}""")));
        var requests = new[] { new GraphBatchRequest("1", HttpMethod.Get, "/me") };
        await Assert.ThrowsAsync<ContractException>(() => client.ExecuteBatchAsync(requests));
    }
}

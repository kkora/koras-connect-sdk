using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-014: service protection limits and throttling. No new public contract is proposed for this
/// feature - it is fully covered by the existing shared <see cref="ThrottleException"/>/<see cref="TransportOptions"/>
/// (Section 8.1). These tests confirm that coverage actually reaches Dataverse's own wrapper paths
/// (<see cref="DataverseApiException"/>, batch/delta), not just the shared transport in isolation.</summary>
public sealed class DataverseThrottlingTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");
    private static readonly Guid RowId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, TransportOptions? transport = null)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token"), transport), new DataverseOptions(EnvironmentRoot));
    }

    private static HttpResponseMessage Throttled(TimeSpan? retryAfter = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        if (retryAfter is not null) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter.Value);
        return response;
    }

    [Fact]
    public async Task ThrottledGetRetriesThenSucceeds()
    {
        var calls = 0;
        var client = Client((_, _) =>
        {
            calls++;
            if (calls < 2) return Task.FromResult(Throttled(TimeSpan.Zero));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($$"""{"accountid":"{{RowId:D}}"}""", Encoding.UTF8, "application/json") });
        }, new TransportOptions { BaseDelay = TimeSpan.Zero });
        using var row = await client.GetRowAsync("accounts", RowId);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ThrottledGetExhaustingRetriesSurfacesThrottleExceptionWithRetryAfter()
    {
        var client = Client((_, _) => Task.FromResult(Throttled(TimeSpan.FromMilliseconds(5))), new TransportOptions { BaseDelay = TimeSpan.Zero, MaxRetries = 1 });
        var ex = await Assert.ThrowsAsync<DataverseApiException>(() => client.GetRowAsync("accounts", RowId));
        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
        Assert.IsType<ThrottleException>(ex.InnerException);
    }

    [Fact]
    public async Task RetryAfterBeyondMaxDelayFailsFastRatherThanWaiting()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Throttled(TimeSpan.FromHours(1))); }, new TransportOptions { MaxDelay = TimeSpan.FromSeconds(1) });
        await Assert.ThrowsAsync<DataverseApiException>(() => client.GetRowAsync("accounts", RowId));
        Assert.Equal(1, calls); // never actually waited an hour
    }

    [Fact]
    public async Task WritesAreNeverRetriedOnThrottle()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Throttled()); });
        await Assert.ThrowsAsync<DataverseApiException>(() => client.UpdateRowAsync("accounts", RowId, JsonDocument().RootElement, writesEnabled: true));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task BatchIsNeverRetriedOnThrottle()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Throttled()); });
        await Assert.ThrowsAsync<DataverseApiException>(() => client.ExecuteBatchAsync([new DataverseBatchRequest(HttpMethod.Get, "accounts")]));
        Assert.Equal(1, calls);
    }

    private static System.Text.Json.JsonDocument JsonDocument() => System.Text.Json.JsonDocument.Parse("""{"name":"Contoso"}""");
}

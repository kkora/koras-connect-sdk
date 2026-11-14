using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-022: throttling and Retry-After. No new public API contract - fully covered by the existing
/// shared <see cref="ThrottleException"/>/<see cref="TransportOptions"/> reused unchanged. These tests confirm
/// that reuse actually holds for the Graph connector specifically: a 429 classifies correctly, a GET retries
/// within budget honoring the server's delay, and a write is never auto-retried.</summary>
public sealed class GraphThrottlingTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task A429WithRetryAfterClassifiesAsThrottleException()
    {
        var client = new GraphClient(new IntegrationTransport(
            new HttpClient(new Handler((_, _) =>
            {
                var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("""{"error":{"code":"TooManyRequests"}}""", Encoding.UTF8, "application/json") };
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(10));
                return Task.FromResult(response);
            })),
            new StaticToken("token")));
        var ex = await Assert.ThrowsAsync<GraphApiException>(() => client.GetOrganizationAsync());
        Assert.Equal((HttpStatusCode)429, ex.StatusCode);
        Assert.IsType<ThrottleException>(ex.InnerException);
    }

    [Fact]
    public async Task AGetRetriesOnATransient429WithinBudgetAndHonorsRetryAfter()
    {
        var calls = 0;
        var client = new GraphClient(new IntegrationTransport(
            new HttpClient(new Handler((_, _) =>
            {
                calls++;
                if (calls == 1)
                {
                    var response = new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
                    response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1));
                    return Task.FromResult(response);
                }
                return Task.FromResult(Json("""{"value":[{"id":"org1"}]}"""));
            })),
            new StaticToken("token")));
        var organization = await client.GetOrganizationAsync();
        Assert.Equal("org1", organization.Id);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task AWriteIsNeverAutoRetriedOn429()
    {
        var calls = 0;
        var client = new GraphClient(new IntegrationTransport(
            new HttpClient(new Handler((_, _) =>
            {
                calls++;
                return Task.FromResult(new HttpResponseMessage((HttpStatusCode)429) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            })),
            new StaticToken("token")));
        await Assert.ThrowsAsync<GraphApiException>(() => client.CreateUserAsync(System.Text.Json.JsonDocument.Parse("{}").RootElement, writesEnabled: true));
        Assert.Equal(1, calls);
    }
}

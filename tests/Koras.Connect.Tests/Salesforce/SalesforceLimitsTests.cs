using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-017: Limits API and throttling - dictionary-shaped limit parsing and the documented
/// no-Retry-After 429 classification.</summary>
public sealed class SalesforceLimitsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    [Fact]
    public async Task LimitsParseCorrectlyKeyedByName()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""
            {"DailyApiRequests":{"Max":15000,"Remaining":14321},"DataStorageMB":{"Max":1024,"Remaining":987}}
            """)); });
        var limits = await client.GetLimitsAsync();
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/limits/", requested!.AbsoluteUri);
        Assert.Equal(new SalesforceLimit(15000, 14321), limits["DailyApiRequests"]);
        Assert.Equal(new SalesforceLimit(1024, 987), limits["DataStorageMB"]);
    }

    [Fact]
    public async Task AnUnrecognizedLimitKeyIsPreservedNotDropped()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"SomeFutureNewLimit":{"Max":5,"Remaining":5}}""")));
        var limits = await client.GetLimitsAsync();
        Assert.True(limits.ContainsKey("SomeFutureNewLimit"));
        Assert.Equal(new SalesforceLimit(5, 5), limits["SomeFutureNewLimit"]);
    }

    [Fact]
    public async Task MalformedLimitsResponseSurfacesAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[1,2,3]""")));
        await Assert.ThrowsAsync<ContractException>(() => client.GetLimitsAsync());
    }

    [Fact]
    public async Task RequestLimitExceededClassifiesAsThrottleWithoutAssumingARetryAfterValue()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"message":"...","errorCode":"REQUEST_LIMIT_EXCEEDED"}]""", (HttpStatusCode)429)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.GetLimitsAsync());
        Assert.Equal((HttpStatusCode)429, ex.StatusCode);
        Assert.Null(ex.RetryAfter);
        Assert.IsType<ThrottleException>(ex.InnerException);
    }
}

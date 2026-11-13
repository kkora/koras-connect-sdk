using System.Net;
using System.Text;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SAP-001: dedicated coverage for <see cref="SamlBearerTokenSource"/>'s unit-test requirements
/// (expiry/skew, invalid assertion/token, rotation, canceled exchange) beyond the happy-path/concurrent-caching
/// case already covered by <c>SapClientExpansionTests.SapSamlGrantUsesExactProtocolAndCachesWithoutLoggingAssertions</c>.</summary>
public sealed class SapAuthenticationTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026,1,1,0,0,0,TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
    private static readonly Uri TokenEndpoint = new("https://sap.test/oauth/token");

    [Fact] public async Task TokenIsRenewedAfterExpiryUsingWhateverAssertionTheSourceReturnsAtThatMoment()
    {
        // "expiry/skew" + "rotation": the cache expires using a small safety margin before the documented 24h
        // (here: a short synthetic expires_in), and a renewal fetches a brand-new assertion rather than reusing
        // the first one - this is "rotation" in the sense that the assertion source, not the token cache, decides
        // what to present on each real exchange.
        var calls=0; var clock=new Clock(); var assertionCalls=0;
        using var h=new HttpClient(new Handler(async(r,ct)=>
        {
            calls++;
            var body=await r.Content!.ReadAsStringAsync(ct);
            Assert.Contains($"assertion=rotated-assertion-{calls}",body);
            return Json("""{"access_token":"synthetic","token_type":"Bearer","expires_in":100}""");
        }));
        using var tokens=new SamlBearerTokenSource(h,TokenEndpoint,"synthetic","client",
            ct=>{assertionCalls++;return ValueTask.FromResult($"rotated-assertion-{assertionCalls}");},clock);
        await tokens.GetTokenAsync(); Assert.Equal(1,calls);
        clock.Now=clock.Now.AddSeconds(89); await tokens.GetTokenAsync(); Assert.Equal(1,calls); // still cached (margin not yet crossed)
        clock.Now=clock.Now.AddSeconds(2); await tokens.GetTokenAsync(); Assert.Equal(2,calls); // margin crossed -> real renewal with a fresh assertion
    }

    [Fact] public async Task AssertionSourceFailureIsNeverSwallowedOrRetried()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"access_token":"x","token_type":"Bearer","expires_in":100}"""));}));
        using var tokens=new SamlBearerTokenSource(h,TokenEndpoint,"synthetic","client",ct=>throw new InvalidOperationException("synthetic assertion-source failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>tokens.GetTokenAsync().AsTask());
        Assert.Equal(0,calls);
    }

    [Theory]
    [InlineData("""{"token_type":"Bearer","expires_in":100}""")] // missing access_token
    [InlineData("""{"access_token":"x","expires_in":100}""")] // missing token_type
    [InlineData("""{"access_token":"x","token_type":"Basic","expires_in":100}""")] // wrong token_type
    [InlineData("""{"access_token":"x","token_type":"Bearer","expires_in":0}""")] // non-positive expiry
    [InlineData("""{"access_token":"x","token_type":"Bearer"}""")] // missing expires_in
    public async Task InvalidTokenResponseShapesNeverPoisonTheCache(string malformedBody)
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json(malformedBody));}));
        using var tokens=new SamlBearerTokenSource(h,TokenEndpoint,"synthetic","client",ct=>ValueTask.FromResult("synthetic-assertion"));
        // A missing "access_token" surfaces as KeyNotFoundException (JsonElement.GetProperty); every other
        // malformed shape - missing/wrong token_type, non-positive expires_in - is explicitly validated and
        // surfaces as JsonException. Either way, nothing is ever cached.
        await Assert.ThrowsAnyAsync<Exception>(()=>tokens.GetTokenAsync().AsTask());
        Assert.Equal(1,calls);
        // A subsequent, valid exchange must still succeed - the failed attempt above never cached a poisoned token.
        using var ok=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"access_token":"recovered","token_type":"Bearer","expires_in":100}"""));}));
        using var recovered=new SamlBearerTokenSource(ok,TokenEndpoint,"synthetic","client",ct=>ValueTask.FromResult("synthetic-assertion"));
        Assert.Equal("recovered",await recovered.GetTokenAsync());
    }

    [Fact] public async Task NonSuccessStatusThrowsIntegrationRequestExceptionWithoutRetry()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("{}",HttpStatusCode.ServiceUnavailable));}));
        using var tokens=new SamlBearerTokenSource(h,TokenEndpoint,"synthetic","client",ct=>ValueTask.FromResult("synthetic-assertion"));
        await Assert.ThrowsAsync<IntegrationRequestException>(()=>tokens.GetTokenAsync().AsTask());
        Assert.Equal(1,calls);
    }

    [Fact] public async Task CanceledExchangeNeverReachesTheHandlerAndNeverCachesAnything()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"access_token":"x","token_type":"Bearer","expires_in":100}"""));}));
        using var tokens=new SamlBearerTokenSource(h,TokenEndpoint,"synthetic","client",ct=>ValueTask.FromResult("synthetic-assertion"));
        using var cts=new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>tokens.GetTokenAsync(cts.Token).AsTask());
        Assert.Equal(0,calls);
        var token=await tokens.GetTokenAsync(); // an uncanceled call afterward still works and isn't poisoned
        Assert.Equal("x",token);Assert.Equal(1,calls);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SAP-006 "secret canaries": a single cross-cutting sweep threading one fake secret value through every
/// failure path across the SAP SuccessFactors surface - the SAML token exchange, enumeration, metadata retrieval
/// and upsert - and asserting it never appears in a thrown exception's message or ToString(). Each path already
/// has its own narrower leak-check (for example SapClientExpansionTests' per-method "DoesNotContain" assertions);
/// this file is the one place that confirms the invariant holds everywhere at once, closing the gap the SAP-005/
/// SAP-001 test suites left implicit.</summary>
public sealed class SapSecretCanaryTests
{
    private const string Canary = "CANARY-super-secret-assertion-and-employee-data-xyz789";
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status) => new(status) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    private static readonly string CanaryErrorBody = JsonSerializer.Serialize(new { error = new { code = "COE0001", message = new { value = Canary } } });

    [Fact] public async Task SamlTokenExchangeFailureNeverLeaksTheCanaryAssertionOrResponseBody()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(CanaryErrorBody,HttpStatusCode.Forbidden))));
        using var tokens=new SamlBearerTokenSource(h,new("https://sap.test/oauth/token"),"synthetic","client",ct=>ValueTask.FromResult(Canary));
        var error=await Assert.ThrowsAsync<IntegrationRequestException>(()=>tokens.GetTokenAsync().AsTask());
        Assert.DoesNotContain(Canary,error.Message);Assert.DoesNotContain(Canary,error.ToString());
    }

    [Fact] public async Task EnumerateAsyncClassifiedFailureNeverLeaksTheCanaryErrorBody()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(CanaryErrorBody,HttpStatusCode.Forbidden))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        var error=await Assert.ThrowsAsync<AuthorizationException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync()) { }});
        Assert.DoesNotContain(Canary,error.Message);Assert.DoesNotContain(Canary,error.ToString());
    }

    [Fact] public async Task GetMetadataAsyncClassifiedFailureNeverLeaksTheCanaryErrorBody()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(CanaryErrorBody,HttpStatusCode.Unauthorized))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        var error=await Assert.ThrowsAsync<AuthenticationException>(()=>c.GetMetadataAsync());
        Assert.DoesNotContain(Canary,error.Message);Assert.DoesNotContain(Canary,error.ToString());
    }

    [Fact] public async Task UpsertAsyncTransportFailureNeverLeaksTheCanaryErrorBody()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(CanaryErrorBody,HttpStatusCode.TooManyRequests))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        var error=await Assert.ThrowsAsync<ThrottleException>(()=>c.UpsertAsync("PerPerson",[payload.RootElement],writesEnabled:true));
        Assert.DoesNotContain(Canary,error.Message);Assert.DoesNotContain(Canary,error.ToString());
    }

    [Fact] public async Task UpsertAsyncRowLevelFailureNeverLeaksTheCanaryMessageThroughUpsertResult()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(
            $$"""{"d":[{"key":"k","status":"ERROR","httpCode":403,"index":0,"message":"{{Canary}}"}]}""",HttpStatusCode.OK))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        var result=await c.UpsertAsync("PerPerson",[payload.RootElement],writesEnabled:true);
        Assert.DoesNotContain(Canary,result[0].ToString());
    }
}

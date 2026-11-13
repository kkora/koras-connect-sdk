using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>Split out of the former ExpansionTests.cs (which bundled SAP and Qualtrics cases in one file) during
/// the test-suite reorganization into per-integration folders.</summary>
public sealed class SapClientExpansionTests
{
    private sealed class Token : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    [Fact] public async Task SapPagesPreserveUnknownFieldsAndEffectiveDatesWithoutChangingNextLink()
    {
        var calls=0;
        using var http=new HttpClient(new Handler((r,ct)=>
        {
            Assert.Equal("Bearer",r.Headers.Authorization?.Scheme);calls++;
            if(calls==1)
            {
                Assert.Contains("asOfDate=2026-01-01",r.RequestUri!.Query);
                Assert.Contains("%26",r.RequestUri.Query);
                return Task.FromResult(Json("""{"d":{"results":[{"personIdExternal":"synthetic-one","custom":true}],"__next":"https://sap.test/odata/v2/PerPerson?$skiptoken=abc%3D&$format=json"}}"""));
            }
            Assert.Equal("?$skiptoken=abc%3D&$format=json",r.RequestUri!.Query);
            return Task.FromResult(Json("""{"d":{"results":[{"personIdExternal":"synthetic-two"}]}}"""));
        }));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));var people=new List<PerPerson>();
        await foreach(var person in c.EnumeratePersonsAsync(new(Filter:"personIdExternal eq 'x&y'",AsOfDate:new(2026,1,1)))) people.Add(person);
        Assert.Equal(2,people.Count);Assert.True(people[0].AdditionalData!["custom"].GetBoolean());Assert.Equal(2,calls);
    }
    [Theory]
    [InlineData("https://attacker.test/odata/v2/PerPerson")]
    [InlineData("http://sap.test/odata/v2/PerPerson")]
    [InlineData("https://sap.test/other/PerPerson")]
    [InlineData("https://user:secret@sap.test/odata/v2/PerPerson")]
    public async Task SapContinuationCannotExfiltrateCredentials(string next)
    {
        var calls=0;
        using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json(JsonSerializer.Serialize(new{d=new{results=Array.Empty<object>(),__next=next}})));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<InvalidDataException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync()) { }});Assert.Equal(1,calls);
    }
    [Fact] public async Task SapPaginationCyclesAndBoundsAreEnforced()
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""{"d":{"results":[],"__next":"https://sap.test/odata/v2/PerPerson?$format=json&customPageSize=100"}}"""))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<InvalidDataException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync()) { }});
    }
    [Fact] public async Task SapUpsertAddsMetadataAndReturnsRowFailuresAtHttp200WithoutLeakingMessages()
    {
        using var h=new HttpClient(new Handler(async(r,ct)=>
        {
            Assert.Equal(HttpMethod.Post,r.Method);Assert.Equal("/odata/v2/upsert",r.RequestUri!.AbsolutePath);
            using var sent=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal("PerPerson",sent.RootElement[0].GetProperty("__metadata").GetProperty("uri").GetString());
            return Json("""{"d":[{"status":"ERROR","httpCode":403,"index":0,"key":"private-key","message":"private employee data"}]}""");
        }));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        var result=await c.UpsertAsync("PerPerson",[payload.RootElement],writesEnabled:true);
        Assert.False(result[0].Succeeded);Assert.Equal(403,result[0].HttpCode);Assert.DoesNotContain("private",result[0].ToString());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>c.UpsertAsync("PerPerson",[payload.RootElement]));
    }
    [Fact] public async Task SapUpsertDoesNotRetryOrAcceptArbitraryMetadata()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("{}",HttpStatusCode.ServiceUnavailable));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        await Assert.ThrowsAsync<IntegrationRequestException>(()=>c.UpsertAsync("PerPerson",[payload.RootElement],true));Assert.Equal(1,calls);
        using var bad=JsonDocument.Parse("""{"__metadata":{"uri":"User"}}""");
        await Assert.ThrowsAsync<ArgumentException>(()=>c.UpsertAsync("PerPerson",[bad.RootElement],true));Assert.Equal(1,calls);
    }
    [Fact] public async Task SapMetadataUsesXmlAcceptAndRejectsExternalEntities()
    {
        using var h=new HttpClient(new Handler((r,ct)=>
        {
            Assert.Equal("/odata/v2/$metadata",r.RequestUri!.AbsolutePath);Assert.Contains(r.Headers.Accept,a=>a.MediaType=="application/xml");
            return Task.FromResult(Json("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><x>&e;</x>"));
        }));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));await Assert.ThrowsAsync<XmlException>(()=>c.GetMetadataAsync());
    }
    [Fact] public async Task SapSamlGrantUsesExactProtocolAndCachesWithoutLoggingAssertions()
    {
        var calls=0;using var h=new HttpClient(new Handler(async(r,ct)=>
        {
            calls++;Assert.Null(r.Headers.Authorization);Assert.Equal("/oauth/token",r.RequestUri!.AbsolutePath);
            var body=await r.Content!.ReadAsStringAsync(ct);Assert.Contains("company_id=synthetic",body);Assert.Contains("saml2-bearer",body);Assert.Contains("assertion=",body);
            return Json("""{"access_token":"synthetic","token_type":"Bearer","expires_in":3600}""");
        }));
        using var tokens=new SamlBearerTokenSource(h,new("https://sap.test/oauth/token"),"synthetic","client",ct=>ValueTask.FromResult("synthetic-assertion"));
        await Task.WhenAll(Enumerable.Range(0,10).Select(_=>tokens.GetTokenAsync().AsTask()));Assert.Equal(1,calls);
    }
}

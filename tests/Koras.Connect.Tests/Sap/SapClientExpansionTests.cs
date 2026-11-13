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
    [Fact] public async Task EmptyPageWithANonNullNextLinkStillFollowsItRatherThanTreatingItAsTerminal()
    {
        // SAP-004 "empty page with continuation": a page with zero results but a non-null __next must not be
        // mistaken for the end of the data set - the OData standard's __next link is the sole terminal signal.
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>
        {
            calls++;
            return Task.FromResult(calls==1
                ? Json("""{"d":{"results":[],"__next":"https://sap.test/odata/v2/PerPerson?$format=json&$skiptoken=next"}}""")
                : Json("""{"d":{"results":[{"personIdExternal":"after-empty-page"}]}}"""));
        }));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        var people=new List<PerPerson>();
        await foreach(var p in c.EnumeratePersonsAsync()) people.Add(p);
        Assert.Equal("after-empty-page",Assert.Single(people).PersonIdExternal);Assert.Equal(2,calls);
    }
    [Theory]
    [InlineData("snapshot")]
    [InlineData("cursor")]
    public async Task PagingModeIsForwardedVerbatimInTheQueryString(string mode)
    {
        using var h=new HttpClient(new Handler((r,ct)=>{Assert.Contains($"paging={mode}",r.RequestUri!.Query);return Task.FromResult(Json("""{"d":{"results":[]}}"""));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await foreach(var p in c.EnumeratePersonsAsync(new(Paging:mode))) { }
    }
    [Fact] public async Task ConflictingAsOfAndEffectiveDateRangeIsRejectedLocallyBeforeAnyHttpCall()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"d":{"results":[]}}"""));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<ArgumentException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync(new(AsOfDate:new(2026,1,1),FromDate:new(2026,1,1)))) { }});
        Assert.Equal(0,calls);
    }
    [Fact] public async Task OrderByIsForwardedVerbatimInTheQueryString()
    {
        using var h=new HttpClient(new Handler((r,ct)=>{Assert.Contains("$orderby=username%20desc",r.RequestUri!.Query);return Task.FromResult(Json("""{"d":{"results":[]}}"""));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await foreach(var p in c.EnumeratePersonsAsync(new(OrderBy:"username desc"))) { }
    }
    [Fact] public async Task OrderByIsRejectedWithCursorBasedPaginationBeforeAnyHttpCall()
    {
        // Per the official SAP SuccessFactors API Reference Guide (2H 2026), section 6.3.5.3.2.1
        // "Cursor-based Pagination": "$orderby cannot be used. An error will occur if it is attempted."
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"d":{"results":[]}}"""));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<ArgumentException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync(new(Paging:"cursor",OrderBy:"username"))) { }});
        Assert.Equal(0,calls);
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
    [Theory]
    [InlineData(0,typeof(ArgumentException))]
    [InlineData(1000,null)]
    [InlineData(1001,typeof(ArgumentException))]
    public async Task UpsertAsyncEnforcesTheOneToOneThousandRecordBoundaryExactly(int count,Type? expectedError)
    {
        var calls=0;using var h=new HttpClient(new Handler(async(r,ct)=>
        {
            calls++;
            using var sent=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            Assert.Equal(count,sent.RootElement.GetArrayLength());
            return Json(JsonSerializer.Serialize(new{d=Enumerable.Range(0,count).Select(i=>new{key=$"k{i}",status="OK",httpCode=200,index=i,message=(string?)null}).ToArray()}));
        }));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        var rows=Enumerable.Range(0,count).Select(i=>JsonDocument.Parse($$"""{"personIdExternal":"p{{i}}"}""").RootElement).ToArray();
        if(expectedError is null)
        {
            var result=await c.UpsertAsync("PerPerson",rows,writesEnabled:true);
            Assert.Equal(count,result.Count);Assert.Equal(1,calls);
        }
        else
        {
            await Assert.ThrowsAsync(expectedError,()=>c.UpsertAsync("PerPerson",rows,writesEnabled:true));
            Assert.Equal(0,calls);
        }
    }
    [Theory]
    [InlineData("""{"notD":[]}""")]
    [InlineData("""{"d":"not-an-array-or-object"}""")]
    public async Task UpsertAsyncThrowsRatherThanSilentlyReturningNoResultsOnAMalformedBody(string malformedBody)
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(malformedBody))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        await Assert.ThrowsAnyAsync<Exception>(()=>c.UpsertAsync("PerPerson",[payload.RootElement],writesEnabled:true));
    }
    [Fact] public async Task UpsertResultPreservesEditStatusDistinguishingInsertFromUpdate()
    {
        // Confirmed against the official SAP SuccessFactors API Reference Guide (2H 2026), section 6.3.6.4.1
        // "HTTP Response Status Codes for Upsert": editStatus is INSERTED/UPDATED/UPSERTED, independent of httpCode.
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("""{"d":[{"key":"newuser","status":"OK","editStatus":"INSERTED","httpCode":201,"index":0,"message":null},{"key":"existuser","status":"OK","editStatus":"UPDATED","httpCode":204,"index":1,"message":null}]}"""))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var a=JsonDocument.Parse("""{"personIdExternal":"a"}"""); using var b=JsonDocument.Parse("""{"personIdExternal":"b"}""");
        var result=await c.UpsertAsync("PerPerson",[a.RootElement,b.RootElement],writesEnabled:true);
        Assert.Equal("INSERTED",result[0].EditStatus);Assert.True(result[0].Succeeded);
        Assert.Equal("UPDATED",result[1].EditStatus);Assert.True(result[1].Succeeded);
    }
    [Fact] public async Task SapUpsertDoesNotRetryOrAcceptArbitraryMetadata()
    {
        // SAP-005: a transport-level failure (non-2xx for the whole request) is now classified like every other
        // read path, distinct from a per-row failure (HTTP 200 with row-level httpCode/status), which stays
        // exactly as returned - see SapUpsertAddsMetadataAndReturnsRowFailuresAtHttp200WithoutLeakingMessages.
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("{}",HttpStatusCode.ServiceUnavailable));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        using var payload=JsonDocument.Parse("""{"personIdExternal":"synthetic"}""");
        await Assert.ThrowsAsync<Koras.Connect.Exceptions.TransportException>(()=>c.UpsertAsync("PerPerson",[payload.RootElement],true));Assert.Equal(1,calls);
        using var bad=JsonDocument.Parse("""{"__metadata":{"uri":"User"}}""");
        await Assert.ThrowsAsync<ArgumentException>(()=>c.UpsertAsync("PerPerson",[bad.RootElement],true));Assert.Equal(1,calls);
    }
    [Fact] public async Task EnumerateAsyncRejectsUnsafeEntityNameBeforeAnyHttpCall()
    {
        // SAP-002 "unsafe entity name": ValidateEntity must reject anything that isn't a plain metadata entity-set
        // name before any request is sent - never attempt to percent-encode or otherwise "sanitize" a hostile
        // entity string into the URI.
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"d":{"results":[]}}"""));}));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<ArgumentException>(async()=>{await foreach(var p in c.EnumerateAsync<PerPerson>("Per Person; DROP TABLE")) { }});
        Assert.Equal(0,calls);
    }

    [Theory]
    [InlineData("""{"notD":{"results":[]}}""")]
    [InlineData("""{"d":{"notResults":[]}}""")]
    [InlineData("""{"d":"not-an-object"}""")]
    public async Task EnumerateAsyncThrowsRatherThanSilentlyYieldingNothingOnAMalformedBody(string malformedBody)
    {
        // SAP-002 "malformed d.results": a response missing the documented "d"/"d.results" envelope shape must
        // surface as a thrown exception, never as a silently-empty enumeration that looks identical to "no records".
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(malformedBody))));
        var c=new SuccessFactorsClient(new(h,new Token()),new(SapRoot));
        await Assert.ThrowsAnyAsync<Exception>(async()=>{await foreach(var p in c.EnumeratePersonsAsync()) { }});
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

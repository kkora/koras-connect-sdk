using System.Net;
using System.Text;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SAP-010 scaffolding is explicitly best-effort and not verified against a live tenant; these tests
/// only exercise protocol shape (unknown/retired/duplicate-version/missing-reference preservation), never a
/// business rule about what a "retired" or "duplicate" foundation-object record should mean.</summary>
public sealed class SapFoundationObjectTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");

    [Fact] public async Task EnumerateFoundationObjectPreservesRetiredCodesDuplicateVersionsAndMissingParent()
    {
        using var http=new HttpClient(new Handler((r,ct)=>
        {
            Assert.EndsWith("/FODepartment", r.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""
            {"d":{"results":[
                {"externalCode":"DEPT1","effectiveStartDate":"/Date(1600000000000)/","status":"T","parentExternalCode":null},
                {"externalCode":"DEPT1","effectiveStartDate":"/Date(1700000000000)/","status":"A","parentExternalCode":"DEPT-ROOT"}
            ]}}
            """,Encoding.UTF8,"application/json")});
        }));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        var departments=new List<FODepartment>();
        await foreach(var d in c.EnumerateFoundationObjectAsync<FODepartment>("FODepartment")) departments.Add(d);
        Assert.Equal(2,departments.Count); // duplicate effective-dated versions both preserved, not collapsed
        Assert.Equal("T",departments[0].Status); // retired code preserved, not filtered out
        Assert.Null(departments[0].ParentExternalCode); // missing parent reference preserved as an explicit null
        Assert.Equal("DEPT-ROOT",departments[1].ParentExternalCode);
        Assert.Equal(departments[0].ExternalCode,departments[1].ExternalCode);
        Assert.NotEqual(departments[0].EffectiveStartDate!.Value.GetString(),departments[1].EffectiveStartDate!.Value.GetString());
    }

    [Fact] public async Task EnumerateFoundationObjectPropagatesAuthorizationExceptionOnADeniedEntity()
    {
        // SAP-010 "denied entity": a 403 on a foundation-object entity set must classify like every other read
        // path (SAP-005), reusing EnumerateAsync's existing classification - no new wiring needed, just coverage.
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden))));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<AuthorizationException>(async()=>{await foreach(var d in c.EnumerateFoundationObjectAsync<FODepartment>("FODepartment")) { }});
    }

    [Fact] public async Task ResolveDepartmentParentChainAsyncWalksToARootDepartmentWithNoParent()
    {
        using var http=new HttpClient(new Handler((r,ct)=>
        {
            var filter=r.RequestUri!.Query;
            var body = filter.Contains("DEPT-ROOT")
                ? """{"d":{"results":[{"externalCode":"DEPT-ROOT","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":null}]}}"""
                : """{"d":{"results":[{"externalCode":"DEPT-CHILD","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":"DEPT-ROOT"}]}}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,Encoding.UTF8,"application/json")});
        }));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        var chain=await c.ResolveDepartmentParentChainAsync("DEPT-CHILD");
        Assert.Equal(2,chain.Count);
        Assert.Equal("DEPT-CHILD",chain[0].ExternalCode);Assert.Equal("DEPT-ROOT",chain[0].ParentExternalCode);
        Assert.Equal("DEPT-ROOT",chain[1].ExternalCode);Assert.Null(chain[1].ParentExternalCode);
    }

    [Fact] public async Task ResolveDepartmentParentChainAsyncDetectsASelfReferenceCycle()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(
            """{"d":{"results":[{"externalCode":"DEPT-SELF","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":"DEPT-SELF"}]}}""",
            Encoding.UTF8,"application/json")})));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<InvalidDataException>(()=>c.ResolveDepartmentParentChainAsync("DEPT-SELF"));
    }
}

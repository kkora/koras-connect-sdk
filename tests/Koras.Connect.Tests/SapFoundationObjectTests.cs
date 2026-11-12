using System.Net;
using System.Text;
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
}

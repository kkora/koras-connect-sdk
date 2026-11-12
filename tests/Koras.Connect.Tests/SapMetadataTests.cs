using System.Net;
using System.Text;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapMetadataTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    private const string FixtureA = """
        <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm">
        <EntityType Name="PerPerson"><Key><PropertyRef Name="personIdExternal"/></Key><Property Name="personIdExternal" Type="Edm.String"/><Property Name="personId" Type="Edm.String"/></EntityType>
        </Schema></edmx:DataServices></edmx:Edmx>
        """;
    private const string FixtureB = """
        <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm">
        <EntityType Name="EmpJob"><Key><PropertyRef Name="userId"/><PropertyRef Name="seqNumber"/></Key><Property Name="userId" Type="Edm.String"/><Property Name="seqNumber" Type="Edm.Int64"/></EntityType>
        </Schema></edmx:DataServices></edmx:Edmx>
        """;

    [Fact] public async Task ParsesEntityKeysAndPropertiesIndependentOfNamespaceVersion()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(FixtureB,Encoding.UTF8,"application/xml")})));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        var metadata=await c.GetMetadataAsync();
        var descriptors=SuccessFactorsClient.ParseEntityDescriptors(metadata);
        var job=Assert.Single(descriptors);
        Assert.Equal("EmpJob",job.Name);
        Assert.Equal(new[]{"userId","seqNumber"},job.Keys);
        Assert.Contains("seqNumber",job.Properties);
    }

    [Fact] public async Task OversizedMetadataIsRejectedRatherThanParsed()
    {
        var oversized = "<x>" + new string('a', SuccessFactorsClient.MaxMetadataCharacters + 1) + "</x>";
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(oversized,Encoding.UTF8,"application/xml")})));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<InvalidDataException>(()=>c.GetMetadataAsync());
    }

    [Fact] public async Task ChangedFixtureInvalidatesTheCachedHashAndRefreshAlwaysReparses()
    {
        var current = FixtureA; var calls=0;
        using var http=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(current,Encoding.UTF8,"application/xml")});}));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        var first=await c.GetMetadataAsync();
        Assert.Equal("PerPerson",Assert.Single(SuccessFactorsClient.ParseEntityDescriptors(first)).Name);
        var second=await c.GetMetadataAsync(); // unchanged content -> same hash -> cached document instance reused
        Assert.Same(first,second);
        current = FixtureB; // changed content -> hash differs -> reparsed automatically, even via GetMetadataAsync
        var third=await c.GetMetadataAsync();
        Assert.NotSame(first,third);
        Assert.Equal("EmpJob",Assert.Single(SuccessFactorsClient.ParseEntityDescriptors(third)).Name);
        var refreshed=await c.RefreshMetadataAsync(); // RefreshMetadataAsync always reparses, even with an unchanged hash
        Assert.NotSame(third,refreshed);
        Assert.Equal(4,calls);
    }

    [Fact] public async Task CanceledMetadataRetrievalPropagatesOperationCanceledException()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(FixtureA,Encoding.UTF8,"application/xml")})));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        using var cts=new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>c.GetMetadataAsync(cts.Token));
    }
}

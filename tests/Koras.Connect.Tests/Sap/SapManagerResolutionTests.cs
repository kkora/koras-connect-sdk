using System.Net;
using System.Text;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapManagerResolutionTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    private static SuccessFactorsClient ClientReturning(string body)
    {
        var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,Encoding.UTF8,"application/json")})));
        return new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
    }

    [Fact] public async Task MissingManagerEndsChainWithNullManagerIdWithoutError()
    {
        var c=ClientReturning("""{"d":{"results":[{"userId":"synthetic-alice","managerId":null,"startDate":"/Date(1700000000000)/"}]}}""");
        var chain=await c.ResolveManagerChainAsync("synthetic-alice");
        Assert.Single(chain);
        Assert.Null(chain[0].ManagerId);
    }

    [Fact] public async Task SelfReferenceIsDetectedAsACycle()
    {
        var c=ClientReturning("""{"d":{"results":[{"userId":"synthetic-bob","managerId":"synthetic-bob","startDate":"/Date(1700000000000)/"}]}}""");
        await Assert.ThrowsAsync<InvalidDataException>(()=>c.ResolveManagerChainAsync("synthetic-bob"));
    }

    [Fact] public async Task ThreeNodeCycleIsDetected()
    {
        var c=ClientReturning("""
        {"d":{"results":[
            {"userId":"synthetic-a","managerId":"synthetic-b","startDate":"/Date(1700000000000)/"},
            {"userId":"synthetic-b","managerId":"synthetic-c","startDate":"/Date(1700000000000)/"},
            {"userId":"synthetic-c","managerId":"synthetic-a","startDate":"/Date(1700000000000)/"}
        ]}}
        """);
        await Assert.ThrowsAsync<InvalidDataException>(()=>c.ResolveManagerChainAsync("synthetic-a"));
    }

    [Fact] public async Task MultipleConcurrentEmploymentsResolveToTheLatestStartDatesManager()
    {
        var c=ClientReturning("""
        {"d":{"results":[
            {"userId":"synthetic-carol","managerId":"synthetic-old-mgr","startDate":"/Date(1600000000000)/","seqNumber":1},
            {"userId":"synthetic-carol","managerId":"synthetic-new-mgr","startDate":"/Date(1700000000000)/","seqNumber":1}
        ]}}
        """);
        var chain=await c.ResolveManagerChainAsync("synthetic-carol");
        Assert.Single(chain);
        Assert.Equal("synthetic-new-mgr",chain[0].ManagerId);
    }

    [Fact] public async Task MaxDepthBoundsTraversalWithoutThrowingWhenThereIsNoCycle()
    {
        var rows = string.Join(",", Enumerable.Range(0,6).Select(i =>
            "{\"userId\":\"synthetic-" + i + "\",\"managerId\":\"synthetic-" + (i+1) + "\",\"startDate\":\"/Date(1700000000000)/\"}"));
        var c=ClientReturning("{\"d\":{\"results\":[" + rows + "]}}");
        var chain=await c.ResolveManagerChainAsync("synthetic-0",maxDepth:3);
        Assert.Equal(3,chain.Count);
    }

    [Fact] public async Task ResolveManagerChainAsyncPropagatesAuthorizationExceptionOnADeniedTarget()
    {
        // SAP-011 "denied target": an RBP-denied job lookup must propagate as the classified AuthorizationException
        // (SAP-005), not be swallowed or misreported as "user has no manager".
        var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden))));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        await Assert.ThrowsAsync<AuthorizationException>(()=>c.ResolveManagerChainAsync("synthetic-denied"));
    }

    [Fact] public async Task SameStartDateBoundaryBreaksTheTieOnTheHigherSequenceNumber()
    {
        // SAP-011 "date boundary": two EmpJob records effective on the exact same date (the boundary itself, not
        // merely "different dates") must still resolve deterministically via the seqNumber tie-break.
        var c=ClientReturning("""
        {"d":{"results":[
            {"userId":"synthetic-dave","managerId":"synthetic-mgr-a","startDate":"/Date(1700000000000)/","seqNumber":1},
            {"userId":"synthetic-dave","managerId":"synthetic-mgr-b","startDate":"/Date(1700000000000)/","seqNumber":2}
        ]}}
        """);
        var chain=await c.ResolveManagerChainAsync("synthetic-dave");
        Assert.Single(chain);
        Assert.Equal("synthetic-mgr-b",chain[0].ManagerId);
    }

    [Fact] public async Task AnUnparseableStartDateNeverOutranksARecordWithAValidDate()
    {
        var c=ClientReturning("""
        {"d":{"results":[
            {"userId":"synthetic-erin","managerId":"synthetic-garbage-date-mgr","startDate":"not-a-date","seqNumber":1},
            {"userId":"synthetic-erin","managerId":"synthetic-valid-date-mgr","startDate":"/Date(1700000000000)/","seqNumber":1}
        ]}}
        """);
        var chain=await c.ResolveManagerChainAsync("synthetic-erin");
        Assert.Single(chain);
        Assert.Equal("synthetic-valid-date-mgr",chain[0].ManagerId);
    }
}

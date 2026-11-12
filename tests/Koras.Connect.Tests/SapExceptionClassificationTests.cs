using System.Net;
using System.Text;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapExceptionClassificationTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static readonly Uri SapRoot = new("https://sap.test/odata/v2/");
    private static SuccessFactorsClient ClientForStatus(HttpStatusCode status)
    {
        var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(status))));
        return new SuccessFactorsClient(new IntegrationTransport(http,new Token(),new TransportOptions{MaxRetries=0}),new(SapRoot));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AuthenticationException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(AuthorizationException))]
    [InlineData(HttpStatusCode.TooManyRequests, typeof(ThrottleException))]
    [InlineData(HttpStatusCode.ServiceUnavailable, typeof(TransportException))]
    public async Task EnumerateAsyncClassifiesFailuresByStatusCode(HttpStatusCode status, Type expected)
    {
        var c=ClientForStatus(status);
        var error=await Assert.ThrowsAnyAsync<IntegrationException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync()) { }});
        Assert.IsType(expected,error);
    }

    [Fact] public async Task MetadataClassifiesAuthenticationFailureDistinctlyFromRbpDenial()
    {
        await Assert.ThrowsAsync<AuthenticationException>(()=>ClientForStatus(HttpStatusCode.Unauthorized).GetMetadataAsync());
        await Assert.ThrowsAsync<AuthorizationException>(()=>ClientForStatus(HttpStatusCode.Forbidden).GetMetadataAsync());
    }

    [Fact] public async Task CanceledEnumerationProbePropagatesWithoutBeingReclassified()
    {
        using var http=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("""{"d":{"results":[]}}""",Encoding.UTF8,"application/json")})));
        var c=new SuccessFactorsClient(new(http,new Token()),new(SapRoot));
        using var cts=new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>{await foreach(var p in c.EnumeratePersonsAsync(cancellationToken:cts.Token)) { }});
    }

    [Theory]
    [InlineData("""{"error":{"code":"COE_INVALID_FIELD","message":{"value":"private employee-specific text"}}}""","COE_INVALID_FIELD")]
    [InlineData("not json at all",null)]
    [InlineData("""{"unexpected":"shape"}""",null)]
    [InlineData("",null)]
    public void SapODataErrorExtractsCodeWithoutLoggingMessageAndNeverThrowsOnMalformedBody(string raw,string? expected)
    {
        var code=SapODataError.TryParseSanitizedCode(raw);
        Assert.Equal(expected,code);
        Assert.DoesNotContain("private",code ?? "");
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Transport;
using Koras.Connect.Qualtrics;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>Split out of the former ExpansionTests.cs (which bundled SAP and Qualtrics cases in one file) during
/// the test-suite reorganization into per-integration folders.</summary>
public sealed class QualtricsClientExpansionTests
{
    private sealed class Token : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-token"); }
    private sealed class Handler(Func<HttpRequestMessage,CancellationToken,Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => reply(r,ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body,Encoding.UTF8,"application/json") };
    private static readonly Uri QualtricsRoot = new("https://qualtrics.test/API/v3/");
    [Fact] public async Task QualtricsUsesOnlyApiTokenAndPagesSurveys()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>
        {
            Assert.Null(r.Headers.Authorization);Assert.Equal("synthetic-token",r.Headers.GetValues("X-API-TOKEN").Single());calls++;
            return Task.FromResult(calls==1?Json("""{"result":{"elements":[{"id":"SV_synthetic","name":"Example","future":true}],"nextPage":"https://qualtrics.test/API/v3/surveys?offset=1"}}"""):Json("""{"result":{"elements":[],"nextPage":null}}"""));
        }));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));var surveys=new List<SurveySummary>();await foreach(var survey in c.EnumerateSurveysAsync()) surveys.Add(survey);
        Assert.Equal("SV_synthetic",Assert.Single(surveys).Id);Assert.True(surveys[0].AdditionalData!["future"].GetBoolean());Assert.Equal(2,calls);
    }
    [Fact] public async Task TwoClientInstancesWithDifferentTokenSourcesNeverLeakEachOthersToken()
    {
        // QLT-001 "cross-account isolation": each QualtricsClient captures its own IAccessTokenSource instance
        // (HeaderTokenAuthenticator holds no static/shared state), so a host juggling two Qualtrics accounts in the
        // same process can never have one account's request accidentally carry the other account's token.
        var seenA=new List<string>();var seenB=new List<string>();
        using var hA=new HttpClient(new Handler((r,ct)=>{seenA.Add(r.Headers.GetValues("X-API-TOKEN").Single());return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}"""));}));
        using var hB=new HttpClient(new Handler((r,ct)=>{seenB.Add(r.Headers.GetValues("X-API-TOKEN").Single());return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}"""));}));
        var clientA=QualtricsClient.Create(hA,new FixedToken("account-a-token"),new(QualtricsRoot));
        var clientB=QualtricsClient.Create(hB,new FixedToken("account-b-token"),new(QualtricsRoot));
        await clientA.ListSurveysPageAsync();await clientB.ListSurveysPageAsync();await clientA.ListSurveysPageAsync();
        Assert.All(seenA,t=>Assert.Equal("account-a-token",t));Assert.All(seenB,t=>Assert.Equal("account-b-token",t));
        Assert.Equal(2,seenA.Count);Assert.Single(seenB);
    }
    private sealed class FixedToken(string value) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(value); }
    [Theory] [InlineData("")] [InlineData("   ")] [InlineData("\t")]
    public async Task BlankOrWhitespaceOnlyTokenIsRejectedBeforeAnyHttpSend(string blankToken)
    {
        // QLT-008 "offline/missing-token behavior": a caller-supplied token source that resolves to an empty or
        // whitespace-only string must fail locally before the request ever reaches the wire - never sent as a
        // vacuous X-API-TOKEN header. The handler below throws if ever invoked, catching a regression that
        // started sending the request anyway.
        var called = false;
        using var h=new HttpClient(new Handler((r,ct)=>{called=true;return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":null}}"""));}));
        var c=QualtricsClient.Create(h,new FixedToken(blankToken),new(QualtricsRoot));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>c.ListSurveysPageAsync());
        Assert.False(called);
    }
    [Fact] public async Task QualtricsExportUsesProgressThenFileIdAndStreamsWithoutExtraction()
    {
        var calls=0;using var h=new HttpClient(new Handler(async(r,ct)=>
        {
            calls++;var path=r.RequestUri!.AbsolutePath;
            if(r.Method==HttpMethod.Post)
            { using var body=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));Assert.Equal("json",body.RootElement.GetProperty("format").GetString());return Json("""{"result":{"progressId":"progress-synthetic"}}"""); }
            if(path.EndsWith("/file"))
            { Assert.EndsWith("/file-synthetic/file",path);return new(HttpStatusCode.OK){Content=new ByteArrayContent([0,1,2,3,255])}; }
            Assert.EndsWith("/progress-synthetic",path);
            return calls==2?Json("""{"result":{"status":"inProgress","percentComplete":50}}"""):Json("""{"result":{"status":"complete","percentComplete":100,"fileId":"file-synthetic"}}""");
        }));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));var progress=await c.StartResponseExportAsync("SV_synthetic",new("json"));
        var completed=await c.WaitForExportAsync("SV_synthetic",progress,TimeSpan.Zero,3);using var output=new MemoryStream();await c.DownloadExportAsync("SV_synthetic",completed.FileId!,output);
        Assert.Equal(new byte[]{0,1,2,3,255},output.ToArray());Assert.True(output.CanWrite);Assert.Equal(4,calls);
    }
    [Theory] [InlineData("failed",typeof(InvalidOperationException))] [InlineData("unknown",typeof(JsonException))] [InlineData("complete",typeof(JsonException))]
    public async Task QualtricsDoesNotDownloadFailedOrIncompleteExports(string status,Type error)
    {
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json(JsonSerializer.Serialize(new{result=new{status}})))));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));await Assert.ThrowsAsync(error,()=>c.WaitForExportAsync("SV_synthetic","progress",TimeSpan.Zero,1));
    }
    [Fact] public async Task QualtricsPollBoundsAndCancellationDoNotRestartJob()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;Assert.Equal(HttpMethod.Get,r.Method);return Task.FromResult(Json("""{"result":{"status":"inProgress"}}"""));}));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));await Assert.ThrowsAsync<TimeoutException>(()=>c.WaitForExportAsync("SV_synthetic","p",TimeSpan.Zero,2));Assert.Equal(2,calls);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>c.WaitForExportAsync("SV_synthetic","p",cancellationToken:new(true)));Assert.Equal(2,calls);
    }
    [Fact] public async Task QualtricsRejectsOffOriginContinuationAndInvalidFormats()
    {
        var calls=0;using var h=new HttpClient(new Handler((r,ct)=>{calls++;return Task.FromResult(Json("""{"result":{"elements":[],"nextPage":"https://evil.test/API/v3/surveys"}}"""));}));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));await Assert.ThrowsAsync<InvalidDataException>(async()=>{await foreach(var s in c.EnumerateSurveysAsync()) { }});Assert.Equal(1,calls);
        await Assert.ThrowsAsync<ArgumentException>(()=>c.StartResponseExportAsync("SV_synthetic",new("invalid")));Assert.Equal(1,calls);
    }
    [Fact] public async Task DownloadInterruptedMidStreamPreservesPartialBytesAndLeavesTheOutputStreamOpen()
    {
        // QLT-003: a connection drop partway through a successful (200 OK) download is a different failure mode
        // than an HTTP error status - it must never be miscategorized as a QualtricsApiException (that catch only
        // ever fires for a failing response status, which this never has), and the bytes already streamed to the
        // caller's output before the drop must survive untouched so a caller can inspect/discard/resume, not have
        // this SDK silently truncate or wipe them.
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new PartialThenFailingContent()})));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));using var output=new MemoryStream();
        // HttpClient wraps the stream failure as HttpRequestException with the original IOException as InnerException.
        var error=await Assert.ThrowsAsync<HttpRequestException>(()=>c.DownloadExportAsync("SV_synthetic","file",output));
        Assert.IsType<IOException>(error.InnerException);
        Assert.Equal(new byte[]{1,2,3},output.ToArray());Assert.True(output.CanWrite);
    }
    [Fact] public async Task DownloadFailuresDoNotWriteProviderErrorsToOutput()
    {
        // QLT-005: DownloadExportAsync is now classified like the other single-resource reads (GetSurveyAsync,
        // GetExportProgressAsync) rather than surfacing the raw transport exception; only EnumerateSurveysAsync's
        // pagination and the StartResponseExportAsync write stay pinned to the raw IntegrationRequestException
        // contract, matching the read-vs-write/pagination split already established for the ADP connector.
        using var h=new HttpClient(new Handler((r,ct)=>Task.FromResult(Json("private-response",HttpStatusCode.Forbidden))));
        var c=QualtricsClient.Create(h,new Token(),new(QualtricsRoot));using var output=new MemoryStream();
        var error=await Assert.ThrowsAsync<QualtricsApiException>(()=>c.DownloadExportAsync("SV_synthetic","file",output));
        Assert.Equal(HttpStatusCode.Forbidden,error.StatusCode);Assert.Equal(0,output.Length);Assert.DoesNotContain("private-response",error.ToString());
    }

    /// <summary>Simulates a connection that writes a few real bytes, then drops - for proving a mid-stream
    /// download failure preserves whatever was already written rather than silently truncating it.</summary>
    private sealed class PartialThenFailingContent : HttpContent
    {
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await stream.WriteAsync(new byte[] { 1, 2, 3 });
            throw new IOException("Simulated connection drop mid-stream.");
        }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
    }
}

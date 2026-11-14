using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-010: Bulk API 2.0 ingest - job lifecycle (create/upload/close/abort/state), results download, and
/// the write opt-in gate every mutating bulk operation shares.</summary>
public sealed class SalesforceBulkIngestTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");
    private const string JobId = "001gL000003OpvBQAS";

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    [Fact]
    public async Task CreateIngestJobRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateIngestJobAsync("Account", SalesforceBulkIngestOperation.Insert));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpsertOperationWithoutExternalIdFieldNameThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateIngestJobAsync("Account", SalesforceBulkIngestOperation.Upsert, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateIngestJobPostsTheDocumentedBodyAndParsesTheJobId()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json($$"""{"id":"{{JobId}}","state":"Open"}"""); });
        var job = await client.CreateIngestJobAsync("Account", SalesforceBulkIngestOperation.Upsert, "External_Id__c", writesEnabled: true);
        Assert.Equal(JobId, job.JobId);
        Assert.Equal(SalesforceBulkIngestOperation.Upsert, job.Operation);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/jobs/ingest", request!.RequestUri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, request.Method);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("Account", parsed.RootElement.GetProperty("object").GetString());
        Assert.Equal("upsert", parsed.RootElement.GetProperty("operation").GetString());
        Assert.Equal("CSV", parsed.RootElement.GetProperty("contentType").GetString());
        Assert.Equal("External_Id__c", parsed.RootElement.GetProperty("externalIdFieldName").GetString());
    }

    [Fact]
    public async Task InsertOperationOmitsExternalIdFieldNameFromThePayload()
    {
        string? body = null;
        var client = Client(async (r, ct) => { body = await r.Content!.ReadAsStringAsync(ct); return Json($$"""{"id":"{{JobId}}"}"""); });
        await client.CreateIngestJobAsync("Account", SalesforceBulkIngestOperation.Insert, writesEnabled: true);
        using var parsed = JsonDocument.Parse(body!);
        Assert.False(parsed.RootElement.TryGetProperty("externalIdFieldName", out _));
    }

    [Fact]
    public async Task UploadIngestBatchRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        using var csv = new MemoryStream(Encoding.UTF8.GetBytes("Name\nAcme\n"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadIngestBatchAsync(new SalesforceBulkJobHandle(JobId), csv));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UploadIngestBatchPutsTheCsvBodyToTheBatchesEndpoint()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") }; });
        using var csv = new MemoryStream(Encoding.UTF8.GetBytes("Name\nAcme\n"));
        await client.UploadIngestBatchAsync(new SalesforceBulkJobHandle(JobId), csv, writesEnabled: true);
        Assert.Equal(HttpMethod.Put, request!.Method);
        Assert.Equal($"https://contoso.my.salesforce.com/services/data/v68.0/jobs/ingest/{JobId}/batches", request.RequestUri!.AbsoluteUri);
        Assert.Equal("text/csv", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("Name\nAcme\n", body);
    }

    [Fact]
    public async Task CloseIngestJobPatchesUploadCompleteState()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("{}"); });
        await client.CloseIngestJobAsync(new SalesforceBulkJobHandle(JobId), writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, request!.Method);
        Assert.Equal($"https://contoso.my.salesforce.com/services/data/v68.0/jobs/ingest/{JobId}", request.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("UploadComplete", parsed.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public async Task AbortIngestJobPatchesAbortedState()
    {
        string? body = null;
        var client = Client(async (r, ct) => { body = await r.Content!.ReadAsStringAsync(ct); return Json("{}"); });
        await client.AbortIngestJobAsync(new SalesforceBulkJobHandle(JobId), writesEnabled: true);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("Aborted", parsed.RootElement.GetProperty("state").GetString());
    }

    [Fact]
    public async Task CloseAndAbortRequireWritesEnabled()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CloseIngestJobAsync(new SalesforceBulkJobHandle(JobId)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.AbortIngestJobAsync(new SalesforceBulkJobHandle(JobId)));
    }

    [Fact]
    public async Task GetIngestJobStateReadsTheDocumentedField()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"id":"{{JobId}}","state":"JobComplete"}""")));
        var state = await client.GetIngestJobStateAsync(new SalesforceBulkJobHandle(JobId));
        Assert.Equal("JobComplete", state);
    }

    [Fact]
    public async Task DownloadResultsStreamsTheCsvBodyFromEachDocumentedPath()
    {
        foreach (var (download, path) in new (Func<SalesforceClient, SalesforceBulkJobHandle, Stream, Task> Download, string Path)[]
        {
            ((c, j, s) => c.DownloadSuccessfulResultsAsync(j, s), "successfulResults"),
            ((c, j, s) => c.DownloadFailedResultsAsync(j, s), "failedResults"),
            ((c, j, s) => c.DownloadUnprocessedRecordsAsync(j, s), "unprocessedrecords"),
        })
        {
            Uri? requested = null;
            var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("sf__Id,sf__Created\n001xx,true\n") }); });
            using var destination = new MemoryStream();
            await download(client, new SalesforceBulkJobHandle(JobId), destination);
            Assert.Equal($"https://contoso.my.salesforce.com/services/data/v68.0/jobs/ingest/{JobId}/{path}/", requested!.AbsoluteUri);
            Assert.Equal("sf__Id,sf__Created\n001xx,true\n", Encoding.UTF8.GetString(destination.ToArray()));
        }
    }

    [Theory]
    [InlineData("bad id")]
    [InlineData("tooshort")]
    public async Task InvalidJobIdsAreRejectedLocally(string badId)
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetIngestJobStateAsync(new SalesforceBulkJobHandle(badId)));
    }
}

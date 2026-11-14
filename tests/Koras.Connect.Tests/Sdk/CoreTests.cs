using System.Net;
using System.Text.Json;
using System.IO.Compression;
using System.Xml.Linq;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Koras.Connect.Export;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Synchronization;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class CoreTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(reply(r)); }
    }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body) };
    [Fact] public async Task ConcurrentTokenAcquisitionIsSingleFlightAndRedacted()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(r => { Interlocked.Increment(ref calls); Assert.Equal("Basic", r.Headers.Authorization?.Scheme); return Reply("""{"access_token":"synthetic","expires_in":3600,"token_type":"Bearer"}"""); }));
        var options = new ClientCredentialsOptions(new("https://auth.test"), "client", "never-print-this");
        Assert.DoesNotContain("never-print-this", options.ToString());
        using var source = new ClientCredentialsTokenSource(http, options);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => source.GetTokenAsync().AsTask()));
        Assert.Equal(1, calls);
    }
    [Theory]
    [InlineData("""{"access_token":"x","expires_in":0}""")]
    [InlineData("""{"access_token":"","expires_in":10}""")]
    [InlineData("""{"access_token":"x","expires_in":10,"token_type":"Other"}""")]
    public async Task InvalidTokensAreRejected(string body)
    {
        using var h = new HttpClient(new Handler(_ => Reply(body)));
        using var source = new ClientCredentialsTokenSource(h, new(new("https://auth.test"), "id", "secret"));
        await Assert.ThrowsAsync<JsonException>(() => source.GetTokenAsync().AsTask());
    }
    [Theory] [InlineData(429)] [InlineData(500)] [InlineData(503)]
    public async Task TransientReadsRetry(int status)
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => ++calls < 3 ? Reply("private", (HttpStatusCode)status) : Reply("{}")));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.Zero });
        using var result = await transport.ReadJsonAsync(new("https://api.test")); Assert.Equal(3, calls);
    }
    [Fact] public async Task WritesAreNeverRetried()
    {
        var calls = 0; using var h = new HttpClient(new Handler(_ => { calls++; return Reply("private", HttpStatusCode.ServiceUnavailable); }));
        var t = new IntegrationTransport(h, new Token());
        var e = await Assert.ThrowsAsync<IntegrationRequestException>(() => t.SendJsonAsync(HttpMethod.Post, new("https://api.test")));
        Assert.Equal(1, calls); Assert.DoesNotContain("private", e.ToString());
    }
    [Fact] public async Task RetryAfterOutsideBudgetFailsWithoutRetry()
    {
        var calls = 0; using var h = new HttpClient(new Handler(_ => { calls++; var r = Reply("", HttpStatusCode.TooManyRequests); r.Headers.RetryAfter = new(TimeSpan.FromHours(1)); return r; }));
        await Assert.ThrowsAsync<IntegrationRequestException>(() => new IntegrationTransport(h, new Token()).ReadJsonAsync(new("https://api.test")));
        Assert.Equal(1, calls);
    }
    [Fact] public async Task CancellationAndUnsafeEndpointsAreRejected()
    {
        using var h = new HttpClient(new Handler(_ => throw new Exception("Should not call")));
        var t = new IntegrationTransport(h, new Token());
        await Assert.ThrowsAsync<ArgumentException>(() => t.ReadJsonAsync(new("http://api.test")));
        await Assert.ThrowsAsync<ArgumentException>(() => t.ReadJsonAsync(new("https://api.test"), new Dictionary<string,string> { ["Authorization"] = "x" }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t.ReadJsonAsync(new("https://api.test"), cancellationToken: new(true)));
    }
    [Fact] public async Task SendRawAsyncRoundTripsAnArbitraryContentType()
    {
        var calls = 0; string? sentContentType = null; string? sentBody = null;
        using var h = new HttpClient(new Handler(r =>
        {
            calls++; sentContentType = r.Content?.Headers.ContentType?.ToString(); sentBody = r.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            var reply = Reply("--b\r\nContent-Type: application/http\r\n\r\nHTTP/1.1 204 No Content\r\n\r\n\r\n--b--");
            reply.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("multipart/mixed; boundary=b");
            return reply;
        }));
        var t = new IntegrationTransport(h, new Token());
        var response = await t.SendRawAsync(HttpMethod.Post, new("https://api.test"), "raw-body", "multipart/mixed; boundary=in");
        Assert.Equal(1, calls);
        Assert.Equal("multipart/mixed; boundary=in", sentContentType);
        Assert.Equal("raw-body", sentBody);
        Assert.Contains("multipart/mixed", response.ContentType);
        Assert.Contains("204 No Content", response.Body);
    }
    [Fact] public async Task SendRawAsyncThrowsOnNonSuccessWithoutRetryAndRejectsReservedHeaders()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => { calls++; return Reply("private", HttpStatusCode.BadRequest); }));
        var t = new IntegrationTransport(h, new Token());
        var ex = await Assert.ThrowsAsync<IntegrationRequestException>(() => t.SendRawAsync(HttpMethod.Post, new("https://api.test"), "body", "text/plain"));
        Assert.Equal(1, calls); Assert.DoesNotContain("private", ex.ToString());
        await Assert.ThrowsAsync<ArgumentException>(() => t.SendRawAsync(HttpMethod.Post, new("https://api.test"), "body", "text/plain", new Dictionary<string, string> { ["Host"] = "x" }));
    }
    [Fact] public async Task IndividualWorkerPreservesUnknownFieldsAndEscapesFilters()
    {
        using var h = new HttpClient(new Handler(r => { Assert.Contains("/hr/v2/workers/", r.RequestUri!.AbsolutePath); return Reply("""{"workers":[{"associateOID":"synthetic","person":{"legalName":{"givenName":"Example"}},"future":{"a":1},"workAssignments":[{"jobTitle":"Tester"}]}]}"""); }));
        var c = new AdpWorkersClient(new(h, new Token()), new(new("https://api.test")));
        var w = await c.GetWorkerAsync("synthetic"); Assert.Equal("Example", w!.Person!.LegalName!.GivenName); Assert.Equal(1, w.AdditionalData!["future"].GetProperty("a").GetInt32()); Assert.Equal("Tester", w.WorkAssignments[0].JobTitle);
    }
    [Fact] public async Task SearchCannotInjectQueryParameters()
    {
        using var h = new HttpClient(new Handler(r => { Assert.Contains("%26", r.RequestUri!.Query); return Reply("""{"workers":[]}"""); }));
        var c = new AdpWorkersClient(new(h, new Token()), new(new("https://api.test")));
        using var doc = await c.SearchWorkersAsync("name eq 'x'&$top=9999");
    }
    [Fact] public async Task ExportsEscapeDataAndLeaveStreamOpen()
    {
        string[] columns = ["Name", "Value"]; IReadOnlyList<string?>[] rows = [["A<&", "=1+1"], ["line\nquote\"", " value "]];
        using var csv = new MemoryStream(); await TabularExport.WriteCsvAsync(csv, columns, rows);
        Assert.Contains("'=1+1", System.Text.Encoding.UTF8.GetString(csv.ToArray())); Assert.True(csv.CanWrite);
        using var xml = new MemoryStream(); TabularExport.WriteXml(xml, columns, rows); xml.Position = 0;
        Assert.Equal("A<&", XDocument.Load(xml).Descendants("field").First().Value);
        using var excel = new MemoryStream(); TabularExport.WriteExcel(excel, columns, rows); excel.Position = 0;
        using var zip = new ZipArchive(excel, ZipArchiveMode.Read); Assert.Equal(5, zip.Entries.Count);
        using var sheet = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open(); var doc = XDocument.Load(sheet);
        Assert.DoesNotContain(doc.Descendants(), e => e.Name.LocalName == "f");
        Assert.Contains(doc.Descendants(), e => e.Name.LocalName == "t" && e.Value == "=1+1");
    }
    private static async IAsyncEnumerable<string> Records() { yield return "one"; await Task.Yield(); yield return "two"; }
    [Fact] public async Task ReconciliationAndBatchesAreCallerOwned()
    {
        var first = new Dictionary<string,string>();
        await foreach (var c in Reconciliation.DetectChangesAsync(Records(), x => x, first)) first.Add(c.Key,c.Fingerprint);
        var changes = 0; await foreach (var c in Reconciliation.DetectChangesAsync(Records(), x => x, first)) changes++;
        Assert.Equal(0, changes);
        var batches = new List<IReadOnlyList<string>>(); await foreach (var b in Reconciliation.BatchAsync(Records(), 1)) batches.Add(b);
        Assert.Equal(2, batches.Count); Assert.Equal("one", batches[0][0]);
    }
}

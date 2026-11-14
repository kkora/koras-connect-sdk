using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-011: Bulk API 2.0 query - job creation, state, and <c>Sforce-Locator</c>-driven CSV results paging,
/// including the documented literal-string-<c>"null"</c> termination and cycle-guard hazards.</summary>
public sealed class SalesforceBulkQueryTests
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

    private static HttpResponseMessage Csv(string body, string? locator)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/csv") };
        if (locator is not null) response.Headers.TryAddWithoutValidation("Sforce-Locator", locator);
        return response;
    }

    [Fact]
    public async Task CreateQueryJobPostsTheDocumentedOperationAndQuery()
    {
        string? body = null;
        var client = Client(async (r, ct) => { body = await r.Content!.ReadAsStringAsync(ct); return Json($$"""{"id":"{{JobId}}"}"""); });
        var job = await client.CreateQueryJobAsync("SELECT Id FROM Account");
        Assert.Equal(JobId, job.JobId);
        Assert.Null(job.Operation);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("query", parsed.RootElement.GetProperty("operation").GetString());
        Assert.Equal("SELECT Id FROM Account", parsed.RootElement.GetProperty("query").GetString());
    }

    [Fact]
    public async Task IncludeDeletedSelectsQueryAll()
    {
        string? body = null;
        var client = Client(async (r, ct) => { body = await r.Content!.ReadAsStringAsync(ct); return Json($$"""{"id":"{{JobId}}"}"""); });
        await client.CreateQueryJobAsync("SELECT Id FROM Account", includeDeleted: true);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("queryAll", parsed.RootElement.GetProperty("operation").GetString());
    }

    [Fact]
    public async Task GetQueryJobStateReadsTheDocumentedField()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"id":"{{JobId}}","state":"JobComplete"}""")));
        var state = await client.GetQueryJobStateAsync(new SalesforceBulkJobHandle(JobId));
        Assert.Equal("JobComplete", state);
    }

    [Fact]
    public async Task EnumerateQueryResultsStopsOnTheLiteralNullLocatorString()
    {
        var client = Client((_, _) => Task.FromResult(Csv("Id,Name\n001,Acme\n", "null")));
        var pages = new List<string>();
        await foreach (var page in client.EnumerateQueryResultsAsync(new SalesforceBulkJobHandle(JobId)))
        {
            using var reader = new StreamReader(page);
            pages.Add(await reader.ReadToEndAsync());
        }
        Assert.Equal(["Id,Name\n001,Acme\n"], pages);
    }

    [Fact]
    public async Task EnumerateQueryResultsFollowsTheLocatorAcrossMultiplePages()
    {
        var calls = 0;
        var requested = new List<Uri>();
        var client = Client((r, _) =>
        {
            calls++;
            requested.Add(r.RequestUri!);
            return Task.FromResult(calls switch
            {
                1 => Csv("Id\n001\n", "page2token"),
                2 => Csv("Id\n002\n", "null"),
                _ => throw new InvalidOperationException("unexpected extra call"),
            });
        });
        var pages = new List<string>();
        await foreach (var page in client.EnumerateQueryResultsAsync(new SalesforceBulkJobHandle(JobId)))
        {
            using var reader = new StreamReader(page);
            pages.Add(await reader.ReadToEndAsync());
        }
        Assert.Equal(["Id\n001\n", "Id\n002\n"], pages);
        Assert.Equal(2, calls);
        Assert.DoesNotContain("locator=", requested[0].Query);
        Assert.Contains("locator=page2token", requested[1].Query);
    }

    [Fact]
    public async Task MaxRecordsIsPassedThroughVerbatimWhenSupplied()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Csv("Id\n001\n", "null")); });
        await foreach (var _ in client.EnumerateQueryResultsAsync(new SalesforceBulkJobHandle(JobId), maxRecords: 500)) { }
        Assert.Contains("maxRecords=500", requested!.Query);
    }

    [Fact]
    public async Task AnAbsentLocatorHeaderIsATerminalContractViolationNotEndOfResults()
    {
        var client = Client((_, _) => Task.FromResult(Csv("Id\n001\n", locator: null)));
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.EnumerateQueryResultsAsync(new SalesforceBulkJobHandle(JobId))) { }
        });
    }

    [Fact]
    public async Task ARepeatedLocatorIsTreatedAsACycleNotAnInfiniteLoop()
    {
        var client = Client((_, _) => Task.FromResult(Csv("Id\n001\n", "sametoken")));
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.EnumerateQueryResultsAsync(new SalesforceBulkJobHandle(JobId))) { }
        });
    }
}

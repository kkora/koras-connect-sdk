using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-005/006: SOQL query execution and pagination - query/queryAll resource selection, batch-size
/// bounds, and <c>nextRecordsUrl</c> traversal.</summary>
public sealed class SalesforceQueryTests
{
    private sealed record Account(string Id, string Name);

    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    [Fact]
    public async Task QueryUsesTheQueryResourceByDefault()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        await client.QueryAsync<Account>("SELECT Id, Name FROM Account");
        Assert.Contains("/query?q=", requested!.AbsoluteUri);
        Assert.Contains("SELECT", Uri.UnescapeDataString(requested.Query));
    }

    [Fact]
    public async Task IncludeDeletedUsesTheQueryAllResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        await client.QueryAsync<Account>("SELECT Id FROM Account", includeDeleted: true);
        Assert.Contains("/queryAll?q=", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task BatchSizeOutsideBoundsIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.QueryAsync<Account>("SELECT Id FROM Account", batchSize: 199));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.QueryAsync<Account>("SELECT Id FROM Account", batchSize: 2001));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task BatchSizeSendsTheDocumentedHeader()
    {
        string? header = null;
        var client = Client((r, _) => { header = r.Headers.TryGetValues("Sforce-Query-Options", out var v) ? v.First() : null; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        await client.QueryAsync<Account>("SELECT Id FROM Account", batchSize: 500);
        Assert.Equal("batchSize=500", header);
    }

    [Fact]
    public async Task EnumerateQueryFollowsNextRecordsUrl()
    {
        var calls = 0;
        var client = Client((r, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(Json("""{"totalSize":2,"done":false,"records":[{"Id":"001","Name":"A"}],"nextRecordsUrl":"/services/data/v68.0/query/01gD0000002-2000"}"""));
            Assert.EndsWith("01gD0000002-2000", r.RequestUri!.AbsolutePath, StringComparison.Ordinal);
            return Task.FromResult(Json("""{"totalSize":2,"done":true,"records":[{"Id":"002","Name":"B"}]}"""));
        });
        var items = new List<Account>();
        await foreach (var item in client.EnumerateQueryAsync<Account>("SELECT Id, Name FROM Account")) items.Add(item);
        Assert.Equal(2, items.Count);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task EmptyPageWithDoneFalseIsStillFollowed()
    {
        var calls = 0;
        var client = Client((_, _) =>
        {
            calls++;
            if (calls == 1) return Task.FromResult(Json("""{"totalSize":1,"done":false,"records":[],"nextRecordsUrl":"/services/data/v68.0/query/cookie"}"""));
            return Task.FromResult(Json("""{"totalSize":1,"done":true,"records":[{"Id":"001","Name":"A"}]}"""));
        });
        var items = new List<Account>();
        await foreach (var item in client.EnumerateQueryAsync<Account>("SELECT Id FROM Account")) items.Add(item);
        Assert.Equal(2, calls);
        Assert.Single(items);
    }

    [Fact]
    public async Task RepeatedNextRecordsUrlIsDetectedAsACycle()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"totalSize":1,"done":false,"records":[{"Id":"001","Name":"A"}],"nextRecordsUrl":"/services/data/v68.0/query/same"}""")));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in client.EnumerateQueryAsync<Account>("SELECT Id FROM Account")) { }
        });
    }

    [Fact]
    public async Task MalformedQueryResponseSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"done":true}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.QueryAsync<Account>("SELECT Id FROM Account"));
    }

    [Fact]
    public async Task MalformedSoqlSurfacesSalesforceApiException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"message":"unexpected token","errorCode":"MALFORMED_QUERY"}]""", HttpStatusCode.BadRequest)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.QueryAsync<Account>("SELECT bad"));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }
}

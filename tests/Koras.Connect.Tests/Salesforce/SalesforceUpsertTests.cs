using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-008: external IDs and upsert - the three documented outcomes (201/204/300) and the write opt-in gate.</summary>
public sealed class SalesforceUpsertTests
{
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

    private static JsonElement Fields() => JsonDocument.Parse("""{"Name":"Acme"}""").RootElement;

    [Fact]
    public async Task UpsertWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task InsertedStatusReportsCreatedTrueWithId()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"001gL000003OpvBQAS","errors":[],"success":true}""", HttpStatusCode.Created)); });
        var result = await client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true);
        Assert.True(result.Created);
        Assert.Equal("001gL000003OpvBQAS", result.RecordId);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/sobjects/Account/External_Id__c/A-1", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task UpdatedStatusReportsCreatedFalseWithNoId()
    {
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        var result = await client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true);
        Assert.False(result.Created);
        Assert.Null(result.RecordId);
        Assert.Null(result.AmbiguousMatchIds);
    }

    [Fact]
    public async Task AmbiguousMatchReturnsEveryMatchingIdNotAGenericFailure()
    {
        var client = Client((_, _) => Task.FromResult(Json("""["001gL000003OpvBQAS","001gL000003OpvCQAS"]""", (HttpStatusCode)300)));
        var result = await client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true);
        Assert.False(result.Created);
        Assert.Equal(2, result.AmbiguousMatchIds!.Count);
    }

    [Fact]
    public async Task AmbiguousMatchWithObjectShapeAlsoExtractsIds()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"Id":"001gL000003OpvBQAS"},{"Id":"001gL000003OpvCQAS"}]""", (HttpStatusCode)300)));
        var result = await client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true);
        Assert.Equal(2, result.AmbiguousMatchIds!.Count);
    }

    [Fact]
    public async Task UpdateOnlySendsTheDocumentedQueryParameter()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), updateOnly: true, writesEnabled: true);
        Assert.Contains("updateOnly=true", requested!.Query);
    }

    [Fact]
    public async Task OtherFailuresStillClassifyThroughTheSharedPath()
    {
        var client = Client((_, _) => Task.FromResult(Json("[]", HttpStatusCode.Forbidden)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task AFailureWithAParsableErrorBodyExtractsTheRealSalesforceErrorCodeInsteadOfAGenericOne()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"errorCode":"DUPLICATE_VALUE","message":"duplicate value found"}]""", HttpStatusCode.BadRequest)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.UpsertByExternalIdAsync("Account", "External_Id__c", "A-1", Fields(), writesEnabled: true));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal("DUPLICATE_VALUE", ex.Details.Code);
    }
}

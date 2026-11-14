using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-004: sObject CRUD - the documented create-response shape, the write opt-in gate, and 404/300
/// classification.</summary>
public sealed class SalesforceRecordsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");
    private const string RecordId = "001gL000003OpvBQAS";

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    private static JsonElement Fields(string json = """{"Name":"Acme Corp"}""") => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task CreateWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateRecordAsync("Account", Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateParsesIdFromTheDocumentedResponseShape()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(Json($$"""{"id":"{{RecordId}}","errors":[],"success":true}""", HttpStatusCode.Created)); });
        var id = await client.CreateRecordAsync("Account", Fields(), writesEnabled: true);
        Assert.Equal(RecordId, id);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/sobjects/Account/", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateReportingSuccessFalseSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"id":"{{RecordId}}","errors":[],"success":false}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.CreateRecordAsync("Account", Fields(), writesEnabled: true));
    }

    [Fact]
    public async Task GetRecordAppliesTheFieldsQueryParameter()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json($$"""{"Id":"{{RecordId}}","Name":"Acme Corp"}""")); });
        using var record = await client.GetRecordAsync("Account", RecordId, ["Name", "Industry"]);
        Assert.Equal($"https://contoso.my.salesforce.com/services/data/v68.0/sobjects/Account/{RecordId}/?fields=Name,Industry", Uri.UnescapeDataString(requested!.AbsoluteUri));
    }

    [Fact]
    public async Task GetRecordOnMissingRecordSurfacesSalesforceApiException()
    {
        var client = Client((_, _) => Task.FromResult(Json("[]", HttpStatusCode.NotFound)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.GetRecordAsync("Account", RecordId));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task AmbiguousExternalIdMatchSurfacesAsADistinctStatusCode()
    {
        // 300 is documented for an ambiguous external-ID match (SFD-008); confirm it is distinguished by StatusCode
        // rather than collapsed into a generic failure.
        var client = Client((_, _) => Task.FromResult(Json("[]", (HttpStatusCode)300)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.GetRecordAsync("Account", RecordId));
        Assert.Equal((HttpStatusCode)300, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateRecordAsync("Account", RecordId, Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpdateSendsAPatchToTheRecordUrl()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRecordAsync("Account", RecordId, Fields(), writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, method);
        Assert.Equal($"https://contoso.my.salesforce.com/services/data/v68.0/sobjects/Account/{RecordId}/", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task DeleteWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteRecordAsync("Account", RecordId));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Account")]
    [InlineData("Acc/ount")]
    public async Task UnsafeSObjectNamesAreRejectedLocally(string sObject)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRecordAsync(sObject, RecordId));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("tooshort")]
    [InlineData("001gL000003OpvBQAS-extra")]
    public async Task InvalidRecordIdsAreRejectedLocally(string id)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRecordAsync("Account", id));
        Assert.Equal(0, calls);
    }
}

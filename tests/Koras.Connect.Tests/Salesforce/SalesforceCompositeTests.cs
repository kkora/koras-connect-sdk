using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-009: Composite API (<c>/composite</c>) - subrequest-count bound, cross-referencing payload shape,
/// mixed-outcome parsing, and the write opt-in gate.</summary>
public sealed class SalesforceCompositeTests
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

    [Fact]
    public async Task MoreThan25SubrequestsAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = Enumerable.Range(0, 26).Select(i => new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account", $"ref{i}")).ToArray();
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteCompositeAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task WriteSubrequestWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Post, "/sobjects/Account", "ref1", JsonDocument.Parse("{}").RootElement) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteCompositeAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ReadOnlyCompositeNeedsNoWriteOptIn()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"compositeResponse":[{"body":{"Id":"1"},"httpStatusCode":200,"referenceId":"ref1"}]}""")));
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account/001", "ref1") };
        var results = await client.ExecuteCompositeAsync(requests);
        Assert.Single(results);
        Assert.True(results[0].Succeeded);
    }

    [Fact]
    public async Task PayloadIncludesAllOrNoneAndSubrequestFields()
    {
        string? body = null;
        var client = Client(async (r, ct) => { body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"compositeResponse":[]}"""); });
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account/001?fields=Name", "refAccount") };
        await client.ExecuteCompositeAsync(requests, allOrNone: true);
        using var parsed = JsonDocument.Parse(body!);
        Assert.True(parsed.RootElement.GetProperty("allOrNone").GetBoolean());
        var sub = parsed.RootElement.GetProperty("compositeRequest")[0];
        Assert.Equal("GET", sub.GetProperty("method").GetString());
        Assert.Equal("refAccount", sub.GetProperty("referenceId").GetString());
    }

    [Fact]
    public async Task MixedOutcomesAreReportedPerSubrequestNotCollapsedToOneFailure()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"compositeResponse":[
              {"body":{"id":"001"},"httpStatusCode":201,"referenceId":"refCreate"},
              {"body":[{"errorCode":"NOT_FOUND"}],"httpStatusCode":404,"referenceId":"refGet"}
            ]}
            """)));
        var requests = new[]
        {
            new SalesforceCompositeSubrequest(HttpMethod.Post, "/sobjects/Account", "refCreate", JsonDocument.Parse("{}").RootElement),
            new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account/999", "refGet"),
        };
        var results = await client.ExecuteCompositeAsync(requests, writesEnabled: true);
        Assert.True(results[0].Succeeded);
        Assert.False(results[1].Succeeded);
        Assert.Equal("NOT_FOUND", results[1].ErrorCode);
    }

    [Fact]
    public async Task AFailedSubrequestWithoutAnExtractableErrorCodeFallsBackToTheHttpStatusCode()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"compositeResponse":[{"body":{"unexpected":"shape"},"httpStatusCode":500,"referenceId":"refGet"}]}
            """)));
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account/999", "refGet") };
        var results = await client.ExecuteCompositeAsync(requests);
        Assert.False(results[0].Succeeded);
        Assert.Equal("http_500", results[0].ErrorCode);
    }

    [Fact]
    public async Task UnsafeReferenceIdsAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account", "bad ref") };
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteCompositeAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task MalformedCompositeResponseSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"notCompositeResponse":[]}""")));
        var requests = new[] { new SalesforceCompositeSubrequest(HttpMethod.Get, "/sobjects/Account", "ref1") };
        await Assert.ThrowsAsync<ContractException>(() => client.ExecuteCompositeAsync(requests));
    }
}

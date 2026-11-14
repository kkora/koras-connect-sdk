using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-013: Platform Events - publish reuses the sObject-create mechanism against the event's
/// <c>__e</c>-suffixed API name; subscribe shares SFD-012's CometD loop against <c>/event/{name}</c>.</summary>
public sealed class SalesforcePlatformEventsTests
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

    private static async Task<string> BayeuxChannel(HttpRequestMessage request, CancellationToken ct)
    {
        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        return document.RootElement[0].GetProperty("channel").GetString()!;
    }

    [Theory]
    [InlineData("Order_Created")]
    [InlineData("Order_Created__c")]
    public async Task EventApiNamesWithoutTheDocumentedSuffixAreRejectedLocally(string badName)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.PublishPlatformEventAsync(badName, JsonDocument.Parse("{}").RootElement, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task PublishRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PublishPlatformEventAsync("Order_Created__e", JsonDocument.Parse("{}").RootElement));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task PublishPostsToTheEventSObjectEndpoint()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"e01xx0000001AAA","errors":[],"success":true}""")); });
        var id = await client.PublishPlatformEventAsync("Order_Created__e", JsonDocument.Parse("""{"Amount__c":100}""").RootElement, writesEnabled: true);
        Assert.Equal("e01xx0000001AAA", id);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/sobjects/Order_Created__e/", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task SubscribeHandshakesAndSubscribesToTheEventChannelAndParsesTheRawPayload()
    {
        var client = Client(async (r, ct) =>
        {
            var channel = await BayeuxChannel(r, ct);
            return channel switch
            {
                "/meta/handshake" => Json("""[{"channel":"/meta/handshake","clientId":"abc","successful":true}]"""),
                "/meta/subscribe" => Json("""[{"channel":"/meta/subscribe","subscription":"/event/Order_Created__e","successful":true}]"""),
                _ => Json("""[{"channel":"/event/Order_Created__e","data":{"payload":{"Amount__c":100}}}]"""),
            };
        });
        var payloads = new List<JsonDocument>();
        await foreach (var document in client.SubscribeToPlatformEventAsync("Order_Created__e"))
        {
            payloads.Add(document);
            break;
        }
        Assert.Single(payloads);
        Assert.Equal(100, payloads[0].RootElement.GetProperty("payload").GetProperty("Amount__c").GetInt32());
    }

    [Fact]
    public async Task SubscribeRejectsAnEventApiNameWithoutTheDocumentedSuffix()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("[]")); });
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in client.SubscribeToPlatformEventAsync("Order_Created")) { }
        });
        Assert.Equal(0, calls);
    }
}

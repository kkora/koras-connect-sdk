using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-012: Change Data Capture - the CometD/Bayeux handshake/subscribe/connect loop and
/// <c>ChangeEventHeader</c> parsing.</summary>
public sealed class SalesforceChangeDataCaptureTests
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
    [InlineData("not-a-channel")]
    [InlineData("/event/Foo__e")]
    public async Task InvalidChannelsAreRejectedLocally(string badChannel)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("[]")); });
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in client.SubscribeToChangeEventsAsync(badChannel)) { }
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SubscribeHandshakesSubscribesAndParsesTheDocumentedChangeEventHeaderFields()
    {
        var requestedUris = new List<Uri>();
        var client = Client(async (r, ct) =>
        {
            requestedUris.Add(r.RequestUri!);
            var channel = await BayeuxChannel(r, ct);
            return channel switch
            {
                "/meta/handshake" => Json("""[{"channel":"/meta/handshake","clientId":"abc123","successful":true}]"""),
                "/meta/subscribe" => Json("""[{"channel":"/meta/subscribe","clientId":"abc123","subscription":"/data/ChangeEvents","successful":true}]"""),
                "/meta/connect" => Json("""
                    [{"channel":"/meta/connect","successful":true},
                     {"channel":"/data/ChangeEvents","data":{"payload":{"Name":"Acme","ChangeEventHeader":{
                        "entityName":"Account","changeType":"UPDATE","changeOrigin":"com/salesforce/api/rest/68.0",
                        "transactionKey":"000a1b2c-3d4e","commitTimestamp":1700000000000,
                        "recordIds":["001xx000003DGXXX"],"changedFields":["Name"],"nulledFields":[]}}}}]
                    """),
                _ => throw new InvalidOperationException($"unexpected channel {channel}"),
            };
        });
        var events = new List<SalesforceChangeEvent>();
        await foreach (var changeEvent in client.SubscribeToChangeEventsAsync("/data/ChangeEvents"))
        {
            events.Add(changeEvent);
            break;
        }
        Assert.Single(events);
        var e = events[0];
        Assert.Equal("Account", e.EntityName);
        Assert.Equal("UPDATE", e.ChangeType);
        Assert.Equal("com/salesforce/api/rest/68.0", e.ChangeOrigin);
        Assert.Equal("000a1b2c-3d4e", e.TransactionKey);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1700000000000), e.CommitTimestamp);
        Assert.Equal(["001xx000003DGXXX"], e.RecordIds);
        Assert.Equal(["Name"], e.ChangedFields);
        Assert.Empty(e.NulledFields!);
        Assert.All(requestedUris, uri => Assert.Equal("https://contoso.my.salesforce.com/cometd/68.0/", uri.AbsoluteUri));
    }

    [Fact]
    public async Task AnUnsuccessfulHandshakeThrowsAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"channel":"/meta/handshake","clientId":"abc","successful":false}]""")));
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.SubscribeToChangeEventsAsync("/data/ChangeEvents")) { }
        });
    }

    [Fact]
    public async Task AnUnsuccessfulSubscribeThrowsAContractException()
    {
        var client = Client(async (r, ct) =>
        {
            var channel = await BayeuxChannel(r, ct);
            return channel == "/meta/handshake"
                ? Json("""[{"channel":"/meta/handshake","clientId":"abc","successful":true}]""")
                : Json("""[{"channel":"/meta/subscribe","successful":false}]""");
        });
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.SubscribeToChangeEventsAsync("/data/ChangeEvents")) { }
        });
    }

    [Fact]
    public async Task AnUnsuccessfulMetaConnectStopsTheLoopInsteadOfRetryingTightlyWithTheRejectedClientId()
    {
        var connectCalls = 0;
        var client = Client(async (r, ct) =>
        {
            var channel = await BayeuxChannel(r, ct);
            return channel switch
            {
                "/meta/handshake" => Json("""[{"channel":"/meta/handshake","clientId":"abc","successful":true}]"""),
                "/meta/subscribe" => Json("""[{"channel":"/meta/subscribe","successful":true}]"""),
                "/meta/connect" => Connect(),
                _ => throw new InvalidOperationException($"unexpected channel {channel}"),
            };

            HttpResponseMessage Connect()
            {
                connectCalls++;
                return Json("""[{"channel":"/meta/connect","successful":false,"error":"402::Unknown client"}]""");
            }
        });
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.SubscribeToChangeEventsAsync("/data/ChangeEvents")) { }
        });
        Assert.Equal(1, connectCalls); // never looped back and re-issued a second connect with the rejected clientId
    }

    [Fact]
    public async Task AChangeEventMissingChangeEventHeaderSurfacesAContractException()
    {
        var client = Client(async (r, ct) =>
        {
            var channel = await BayeuxChannel(r, ct);
            return channel switch
            {
                "/meta/handshake" => Json("""[{"channel":"/meta/handshake","clientId":"abc","successful":true}]"""),
                "/meta/subscribe" => Json("""[{"channel":"/meta/subscribe","successful":true}]"""),
                _ => Json("""[{"channel":"/data/ChangeEvents","data":{"payload":{"Name":"Acme"}}}]"""),
            };
        });
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.SubscribeToChangeEventsAsync("/data/ChangeEvents")) { }
        });
    }
}

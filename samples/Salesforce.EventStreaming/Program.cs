using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;

// Offline demonstration of the Salesforce connector's event-streaming surface: Change Data Capture and Platform
// Events, both delivered over the CometD/Bayeux long-polling protocol (handshake -> subscribe -> connect), plus
// publishing a Platform Event via plain REST - none of which is touched by samples/Salesforce.OfflineWalkthrough
// or .BulkDataJobs. Every Bayeux and REST response below is synthetic; this sample never contacts a live
// Salesforce org or CometD endpoint. Each subscription here is deliberately broken out of after its first
// delivered event (via a plain `break` inside `await foreach`), since the underlying long-poll loop never
// terminates on its own - a real consumer would keep iterating indefinitely instead.
Console.WriteLine("Koras.Connect.Salesforce offline sample: Change Data Capture -> publish a Platform Event -> subscribe to it.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var instanceRoot = new Uri("https://sample.my.salesforce.com/");
string? lastSubscribedChannel = null;

using var oauthHttp = new HttpClient(new SyntheticHandler((_, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":7200,"instance_url":"https://sample.my.salesforce.com","issued_at":"1700000000000"}""")));
var credentials = new SalesforceClientCredentialsOptions(
    new Uri("https://sample.my.salesforce.com/services/oauth2/token"), "sample-client-id", "sample-client-secret");
var tokens = SalesforceAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, cancellationToken) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Post && path == "/cometd/68.0/")
    {
        var bodyText = request.Content!.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        var channel = JsonDocument.Parse(bodyText).RootElement[0].GetProperty("channel").GetString();
        if (channel == "/meta/handshake")
            return Reply("""[{"channel":"/meta/handshake","clientId":"abc123","successful":true}]""");
        if (channel == "/meta/subscribe")
        {
            lastSubscribedChannel = JsonDocument.Parse(bodyText).RootElement[0].GetProperty("subscription").GetString();
            return Reply($$"""[{"channel":"/meta/subscribe","clientId":"abc123","subscription":"{{lastSubscribedChannel}}","successful":true}]""");
        }
        // "/meta/connect" - deliver the event belonging to whichever channel was most recently subscribed.
        if (lastSubscribedChannel == "/data/ChangeEvents")
            return Reply("""
                [{"channel":"/meta/connect","successful":true},
                 {"channel":"/data/ChangeEvents","data":{"payload":{"Name":"Acme","ChangeEventHeader":{
                    "entityName":"Account","changeType":"UPDATE","changeOrigin":"com/salesforce/api/rest/68.0",
                    "transactionKey":"000a1b2c-3d4e","commitTimestamp":1700000000000,
                    "recordIds":["001xx000003DGXXX"],"changedFields":["Name"],"nulledFields":[]}}}}]
                """);
        return Reply("""[{"channel":"/event/Order_Created__e","data":{"payload":{"Amount__c":100}}}]""");
    }
    if (request.Method == HttpMethod.Post && path == "/services/data/v68.0/sobjects/Order_Created__e/")
        return Reply("""{"id":"e01gL0000001AAAQ","errors":[],"success":true}""", HttpStatusCode.Created);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SalesforceClient(new IntegrationTransport(apiHttp, tokens), new SalesforceOptions(instanceRoot));

Console.WriteLine("Subscribing to Change Data Capture on /data/ChangeEvents:");
await foreach (var changeEvent in client.SubscribeToChangeEventsAsync("/data/ChangeEvents"))
{
    Console.WriteLine($"  {changeEvent.ChangeType} on {changeEvent.EntityName}, record(s)={string.Join(",", changeEvent.RecordIds)}, changedFields={string.Join(",", changeEvent.ChangedFields ?? [])}");
    break;
}

Console.WriteLine();
Console.WriteLine("Publishing a Platform Event:");
using var eventFields = JsonDocument.Parse("""{"Amount__c":100}""");
var publishedEventId = await client.PublishPlatformEventAsync("Order_Created__e", eventFields.RootElement, writesEnabled: true);
Console.WriteLine($"  Published event, id={publishedEventId}.");

Console.WriteLine();
Console.WriteLine("Subscribing to the same Platform Event channel:");
await foreach (var payload in client.SubscribeToPlatformEventAsync("Order_Created__e"))
{
    Console.WriteLine($"  Received payload: Amount__c={payload.RootElement.GetProperty("payload").GetProperty("Amount__c").GetInt32()}");
    break;
}

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Salesforce org or CometD endpoint.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

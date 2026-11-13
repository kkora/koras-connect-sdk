using System.Net;
using System.Text;
using Koras.Connect.Qualtrics;
using Koras.Connect.Transport;

// Offline demonstration of the event-subscription management surface added under QLT-007: list subscriptions ->
// create one (opt-in write) -> delete it (opt-in write). This client only manages subscription *registrations*
// with Qualtrics - it is NOT a webhook receiver. Standing up the endpoint that actually accepts an inbound
// delivery, verifying that delivery's authenticity/freshness, and deduplicating retries are entirely host-owned
// responsibilities this SDK does not attempt, the same division of responsibility already documented for ADP-018's
// webhook support. Every HTTP response below is synthetic; this sample never contacts a live Qualtrics tenant and
// requires no API token or data center to run. Endpoint shapes are based on the publicly documented Qualtrics API
// v3 Event Subscriptions contract and are NOT verified against a live tenant.
Console.WriteLine("Koras.Connect.Qualtrics offline sample: list/create/delete event subscriptions.");
Console.WriteLine("This manages subscription registrations only; it is not a webhook receiver.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const string createdSubscriptionId = "ES_new";
using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path.EndsWith("/eventsubscriptions", StringComparison.Ordinal))
        return Reply("""{"result":{"elements":[{"id":"ES_1","topics":"surveyengine.completedResponse.SV_sample","publicationUrl":"https://host.sample.test/hook","encrypt":false}],"nextPage":null}}""");
    if (request.Method == HttpMethod.Post && path.EndsWith("/eventsubscriptions", StringComparison.Ordinal))
        return Reply("""{"result":{"id":"ES_new"}}""");
    if (request.Method == HttpMethod.Delete && path.EndsWith($"/eventsubscriptions/{createdSubscriptionId}", StringComparison.Ordinal))
        return Reply("null");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = QualtricsClient.Create(http, new StaticToken("synthetic-sample-token"), new QualtricsOptions(new Uri("https://sample.qualtrics.test/API/v3/")));

await foreach (var subscription in client.ListEventSubscriptionsAsync())
    Console.WriteLine($"Listed subscription: {subscription.Id} (topics={subscription.Topics}, url={subscription.PublicationUrl}).");

// Writes require explicit opt-in (writesEnabled: true) only after the host has verified Event Subscriptions
// create/delete support and permissions against its own tenant - this sample's "verification" is simply the
// synthetic handler above. Multiple topics are joined into one comma-separated value per the documented contract.
var newSubscriptionId = await client.CreateEventSubscriptionAsync(
    ["surveyengine.completedResponse.SV_sample", "contacts.import.complete"],
    new Uri("https://host.sample.test/hook"),
    writesEnabled: true);
Console.WriteLine($"Created subscription {newSubscriptionId}.");

await client.DeleteEventSubscriptionAsync(newSubscriptionId, writesEnabled: true);
Console.WriteLine($"Deleted subscription {newSubscriptionId}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Qualtrics tenant and without running a receiver.");

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class StaticToken(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

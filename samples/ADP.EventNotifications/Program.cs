using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Events;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;

// Offline demonstration of ADP-006/ADP-018: webhook publisher-header verification, safe parsing of a notification
// envelope's events[] array without trusting its payload, polling as the alternative delivery mechanism, treating
// any notification only as a trigger to re-read the named worker from source, explicit acknowledgement only after
// durable downstream processing, and duplicate-message-id suppression. Every HTTP response below is synthetic;
// this sample never contacts a live ADP tenant.
Console.WriteLine("Koras.Connect.Adp offline sample: webhook/polled event notifications - safe parsing, trigger-only re-read, ack, dedup.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

var seenMessageIds = new HashSet<string>(StringComparer.Ordinal);

Console.WriteLine("Scenario 1: verify a webhook delivery's publisher header, valid vs tampered (ADP-018, AdpWebhookAuthentication).");
RunVerifyPublisherHeader();

Console.WriteLine();
Console.WriteLine("Scenario 2: safely parse a webhook delivery envelope without trusting its payload (ADP-018, NotificationEnvelope.FromWebhookDelivery).");
RunParseWebhookDelivery();

Console.WriteLine();
Console.WriteLine("Scenario 3: poll for a notification instead of receiving a webhook (ADP-018, ReadNotificationAsync).");
var (polledMessageId, polledAssociateOid) = await RunPollAndParseNotificationAsync();

Console.WriteLine();
Console.WriteLine("Scenario 4: treat the notification only as a trigger - re-read the worker from source, never trust its payload (ADP-006).");
await RunReReadFromSourceAsync(polledMessageId, polledAssociateOid, seenMessageIds);

Console.WriteLine();
Console.WriteLine("Scenario 5: acknowledge only after durable downstream processing succeeded (ADP-018, AcknowledgeNotificationAsync).");
await RunAcknowledgeAsync(polledMessageId);

Console.WriteLine();
Console.WriteLine("Scenario 6: a redelivered/duplicate message id is suppressed, never forcing a second re-read (ADP-006).");
await RunDuplicateSuppressionAsync(polledMessageId, polledAssociateOid, seenMessageIds);

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static void RunVerifyPublisherHeader()
{
    const string clientId = "sample-client-id";
    const string clientSecret = "sample-client-secret";
    var validHeader = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(clientId)));
    var tamperedHeader = new string('0', 64);

    Console.WriteLine($"  Valid header verifies: {AdpWebhookAuthentication.VerifyPublisherHeader(validHeader, clientId, clientSecret)}.");
    Console.WriteLine($"  Tampered header verifies: {AdpWebhookAuthentication.VerifyPublisherHeader(tamperedHeader, clientId, clientSecret)}.");
    Console.WriteLine("  Note: this authenticates the publisher's credentials only, not payload integrity/freshness -");
    Console.WriteLine("  require independent endpoint authentication, TLS and durable deduplication at the host.");
}

static void RunParseWebhookDelivery()
{
    using var envelope = JsonDocument.Parse("""
        {"meta":{"orgID":"sample-org","messageId":"webhook-msg-1"},
         "events":[{"data":{"eventContext":{"worker":{"associateOID":"sample-worker-1"}}}}]}
        """);
    var typed = NotificationEnvelope.FromWebhookDelivery(envelope.RootElement);
    Console.WriteLine($"  Parsed messageId: {typed.MessageId}.");
    Console.WriteLine($"  Deduplication identity (org+messageId, for a host-owned durable inbox): {AdpWebhookAuthentication.GetDeliveryKey(envelope.RootElement)}.");
    Console.WriteLine($"  Worker associate OID named by the event: {typed.Events[0].WorkerAssociateOid} (a routing hint only - not yet trusted as the worker's actual data).");
}

static async Task<(string MessageId, string AssociateOid)> RunPollAndParseNotificationAsync()
{
    using var http = new HttpClient(new SyntheticHandler(_ =>
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("""{"events":[{"data":{"eventContext":{"worker":{"associateOID":"sample-worker-1"}}}}]}""", Encoding.UTF8, "application/json") };
        response.Headers.Add("adp-msg-msgid", "polled-msg-1");
        return response;
    }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.EventNotifications]);

    using var result = await client.ReadNotificationAsync();
    var messageId = result.Headers["adp-msg-msgid"][0];
    var envelope = NotificationEnvelope.FromPolledMessage(messageId, result.Body.RootElement);
    var associateOid = envelope.Events[0].WorkerAssociateOid!;
    Console.WriteLine($"  Polled message id (from the allowlisted adp-msg-msgid response header, never the payload): {messageId}.");
    Console.WriteLine($"  Worker associate OID named by the event: {associateOid}.");
    return (messageId, associateOid);
}

static async Task RunReReadFromSourceAsync(string messageId, string associateOid, ISet<string> seenMessageIds)
{
    // If the notification's own worker data were trusted directly instead of triggering a re-read, this handler
    // would still be called with these exact values - proving the re-read is a real, independent round trip
    // against the source of truth, not a reuse of whatever the notification claimed.
    using var http = new HttpClient(new SyntheticHandler(_ =>
        Reply("""{"workers":[{"associateOID":"sample-worker-1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}}}]}""")));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var detector = new AdpChangeDetector(workers);

    var worker = await detector.HandleNotificationAsync(messageId, associateOid, seenMessageIds);
    Console.WriteLine($"  Re-read from source: {worker!.AssociateOid}, name={worker.Person!.LegalName!.GivenName} {worker.Person.LegalName.FamilyName}.");
}

static async Task RunAcknowledgeAsync(string messageId)
{
    string? acknowledgedPath = null;
    using var http = new HttpClient(new SyntheticHandler(request => { acknowledgedPath = request.RequestUri!.AbsolutePath; return Reply("{}"); }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.EventNotifications]);

    using var result = await client.AcknowledgeNotificationAsync(messageId);
    Console.WriteLine($"  Acknowledged after durable downstream processing: DELETE {acknowledgedPath}.");
}

static async Task RunDuplicateSuppressionAsync(string messageId, string associateOid, ISet<string> seenMessageIds)
{
    using var http = new HttpClient(new SyntheticHandler(_ => throw new InvalidOperationException("Should not be called: this message id was already seen and processed.")));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var detector = new AdpChangeDetector(workers);

    var result = await detector.HandleNotificationAsync(messageId, associateOid, seenMessageIds);
    Console.WriteLine($"  Redelivered message suppressed: HandleNotificationAsync returned {(result is null ? "null" : "an unexpected non-null result")}; no second re-read was issued.");
}

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class FixedToken : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("sample-token");
}

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

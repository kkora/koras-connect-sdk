using System.Net;
using System.Text;
using Koras.Connect.Qualtrics;
using Koras.Connect.Transport;

// Offline demonstration of the XM Directory contact/mailing-list surface added under QLT-006/QLT-009/QLT-015:
// list contacts -> read one -> create one (opt-in write) -> apply a partial update via ContactChange (opt-in write)
// -> list mailing lists -> create one (opt-in write). Every HTTP response below is synthetic; this sample never
// contacts a live Qualtrics tenant and requires no API token, data center or directory ID to run. Endpoint shapes
// are based on the publicly documented Qualtrics API v3 contract and are NOT verified against a live tenant.
Console.WriteLine("Koras.Connect.Qualtrics offline sample: list/get/create/update contacts, list/create mailing lists.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const string createdContactId = "CID_new";
using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path.EndsWith("/contacts", StringComparison.Ordinal))
        return Reply("""{"result":{"elements":[{"id":"CID_1","firstName":"Ada","email":"ada@example.com"}],"nextPage":null}}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/contacts/CID_1", StringComparison.Ordinal))
        return Reply("""{"result":{"id":"CID_1","firstName":"Ada","email":"ada@example.com","unsubscribed":false}}""");
    if (request.Method == HttpMethod.Post && path.EndsWith("/contacts", StringComparison.Ordinal))
        return Reply("""{"result":{"id":"CID_new"}}""");
    if (request.Method == HttpMethod.Put && path.EndsWith($"/contacts/{createdContactId}", StringComparison.Ordinal))
        return Reply("null");
    if (request.Method == HttpMethod.Get && path.EndsWith("/mailinglists", StringComparison.Ordinal))
        return Reply("""{"result":{"elements":[{"id":"ML_1","name":"VIP","contactCount":42}],"nextPage":null}}""");
    if (request.Method == HttpMethod.Post && path.EndsWith("/mailinglists", StringComparison.Ordinal))
        return Reply("""{"result":{"id":"ML_new"}}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = QualtricsClient.Create(http, new StaticToken("synthetic-sample-token"), new QualtricsOptions(new Uri("https://sample.qualtrics.test/API/v3/")));
const string directoryId = "DIR_sample";

await foreach (var contact in client.ListContactsAsync(directoryId))
    Console.WriteLine($"Listed contact: {contact.Id} ({contact.FirstName}, {contact.Email}).");

var fetched = await client.GetContactAsync(directoryId, "CID_1");
Console.WriteLine($"Fetched contact {fetched.Id}: unsubscribed={fetched.Unsubscribed.GetBoolean()}.");

// Writes require explicit opt-in (writesEnabled: true) only after the host has verified directory write
// permissions against its own tenant - this sample's "verification" is simply the synthetic handler above.
var newContactId = await client.CreateContactAsync(directoryId, new QualtricsContact { Email = "new@example.com", FirstName = "Grace" }, writesEnabled: true);
Console.WriteLine($"Created contact {newContactId}.");

// ContactChange (QLT-015) only serializes the fields it was explicitly told to change, so updating just the last
// name here can never accidentally null out the email/firstName/etc. that a full QualtricsContact PUT would risk.
var change = new ContactChange().SetLastName("Hopper");
await client.UpdateContactAsync(directoryId, newContactId, change, writesEnabled: true);
Console.WriteLine($"Updated contact {newContactId}: lastName -> Hopper (only that field was sent).");

await foreach (var list in client.ListMailingListsAsync(directoryId))
    Console.WriteLine($"Listed mailing list: {list.Id} ({list.Name}, {list.ContactCount} contacts).");

var newListId = await client.CreateMailingListAsync(directoryId, new QualtricsMailingList { Name = "Sample list" }, writesEnabled: true);
Console.WriteLine($"Created mailing list {newListId}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Qualtrics tenant.");

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

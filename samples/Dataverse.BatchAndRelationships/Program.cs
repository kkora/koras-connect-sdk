using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;

// Offline demonstration of the Dataverse connector's multi-operation surface: $batch/changesets, upsert by
// alternate key, and lookup/relationship management - none of which is touched by samples/Dataverse.OfflineWalkthrough
// (that sample covers auth/WhoAmI/CRUD/pagination/$metadata instead). Every HTTP response below is synthetic;
// this sample never contacts a live Dataverse environment or Entra tenant.
Console.WriteLine("Koras.Connect.Dataverse offline sample: $batch/changesets -> upsert by alternate key -> relationships.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var environmentRoot = "https://sample.api.crm.dynamics.com/";
var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var contactId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var newContactId = Guid.Parse("33333333-3333-3333-3333-333333333333");
var upsertedContactId = Guid.Parse("44444444-4444-4444-4444-444444444444");

using var oauthHttp = new HttpClient(new SyntheticHandler((request, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3599}""")));
var credentials = new DataverseClientCredentialsOptions(
    new Uri("https://login.microsoftonline.com/sample-tenant/oauth2/v2.0/token"),
    "sample-client-id", environmentRoot, ClientSecret: "sample-client-secret");
var tokens = DataverseAuthentication.CreateTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Post && path.EndsWith("/$batch", StringComparison.Ordinal))
    {
        const string outerBoundary = "batchresponse_1";
        const string innerBoundary = "changesetresponse_1";
        var body = SinglePart(outerBoundary, 200, """{"value":[{"accountid":"11111111-1111-1111-1111-111111111111","name":"Contoso Ltd"}]}""")
            + $"--{outerBoundary}\r\nContent-Type: multipart/mixed; boundary={innerBoundary}\r\n\r\n"
            + SinglePart(innerBoundary, 201, $$"""{"contactid":"{{newContactId:D}}"}""")
            + SinglePart(innerBoundary, 204, null)
            + $"--{innerBoundary}--\r\n\r\n"
            + $"--{outerBoundary}--";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8) };
        response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse($"multipart/mixed; boundary={outerBoundary}");
        return response;
    }
    if (request.Method == HttpMethod.Patch && path.EndsWith($"contacts(emailaddress1='avery.chen@contoso.com')", StringComparison.Ordinal))
        return Reply($$"""{"@odata.id":"https://sample.api.crm.dynamics.com/api/data/v9.2/contacts({{upsertedContactId:D}})","fullname":"Avery Chen"}""", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Patch && path.EndsWith($"accounts({accountId:D})", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Delete && path.EndsWith($"accounts({accountId:D})/primarycontactid/$ref", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Post && path.EndsWith($"accounts({accountId:D})/contact_customer_accounts/$ref", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Delete && path.EndsWith($"accounts({accountId:D})/contact_customer_accounts({contactId:D})/$ref", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new DataverseClient(new IntegrationTransport(apiHttp, tokens), new DataverseOptions(new Uri(environmentRoot)));

Console.WriteLine("Executing a $batch call: 1 standalone read + a changeset with 2 writes:");
var batchRequests = new[] { new DataverseBatchRequest(HttpMethod.Get, "accounts") };
using var newContactFields = JsonDocument.Parse("""{"fullname":"Jordan Rivera"}""");
using var accountUpdateFields = JsonDocument.Parse("""{"name":"Contoso Ltd (Batch Updated)"}""");
var changeset = new DataverseChangeset(
[
    new DataverseBatchRequest(HttpMethod.Post, "contacts", newContactFields.RootElement, ContentId: "newcontact"),
    new DataverseBatchRequest(HttpMethod.Patch, $"accounts({accountId:D})", accountUpdateFields.RootElement),
]);
var batchResults = await client.ExecuteBatchAsync(requests: batchRequests, changesets: [changeset], writesEnabled: true);
foreach (var result in batchResults)
    Console.WriteLine($"  Result[{result.Index}]: succeeded={result.Succeeded}");

Console.WriteLine();
Console.WriteLine("Upserting a contact by alternate key (email address):");
using var upsertFields = JsonDocument.Parse("""{"fullname":"Avery Chen"}""");
var upsertResult = await client.UpsertByAlternateKeyAsync("contacts",
    new Dictionary<string, string> { ["emailaddress1"] = "avery.chen@contoso.com" }, upsertFields.RootElement, writesEnabled: true);
Console.WriteLine($"  Upserted contact {upsertResult.RowId} (created={upsertResult.Created}).");

Console.WriteLine();
Console.WriteLine("Managing relationships on an account:");
await client.BindLookupAsync("accounts", accountId, "primarycontactid", "contacts", contactId, writesEnabled: true);
Console.WriteLine("  Bound primary contact lookup.");
await client.ClearLookupAsync("accounts", accountId, "primarycontactid", writesEnabled: true);
Console.WriteLine("  Cleared primary contact lookup.");
await client.AssociateAsync("accounts", accountId, "contact_customer_accounts", "contacts", contactId, writesEnabled: true);
Console.WriteLine("  Associated a contact with the account's contact collection.");
await client.DisassociateAsync("accounts", accountId, "contact_customer_accounts", contactId, writesEnabled: true);
Console.WriteLine("  Disassociated the contact.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Dataverse environment or Entra tenant.");

static string SinglePart(string boundary, int status, string? jsonBody)
{
    var sb = new StringBuilder();
    sb.Append("--").Append(boundary).Append("\r\n");
    sb.Append("Content-Type: application/http\r\n\r\n");
    sb.Append("HTTP/1.1 ").Append(status).Append(" Status\r\n");
    if (jsonBody is not null) sb.Append("Content-Type: application/json\r\n\r\n").Append(jsonBody).Append("\r\n");
    else sb.Append("\r\n");
    sb.Append("\r\n");
    return sb.ToString();
}

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

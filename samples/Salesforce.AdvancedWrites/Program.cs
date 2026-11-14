using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;

// Offline demonstration of the Salesforce connector's advanced write patterns: upsert-by-External-ID (all three
// documented outcomes - insert/update/ambiguous match), the cross-referencing /composite API, and conditional
// (If-Unmodified-Since) updates surfacing a dedicated concurrency exception - none of which is touched by
// samples/Salesforce.OfflineWalkthrough, .BulkDataJobs, or .EventStreaming. Every HTTP response below is
// synthetic; this sample never contacts a live Salesforce org.
Console.WriteLine("Koras.Connect.Salesforce offline sample: upsert by External ID -> composite requests -> conditional updates.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var instanceRoot = new Uri("https://sample.my.salesforce.com/");
const string accountIdUpdatesCleanly = "001gL000003OpvBQA1";
const string accountIdHasAStaleCopy = "001gL000003OpvBQA2";
var newLeadEmail = Uri.EscapeDataString("new.lead@contoso.com");
var existingContactEmail = Uri.EscapeDataString("existing.contact@contoso.com");
var duplicateEmail = Uri.EscapeDataString("duplicate@contoso.com");

using var oauthHttp = new HttpClient(new SyntheticHandler((_, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":7200,"instance_url":"https://sample.my.salesforce.com","issued_at":"1700000000000"}""")));
var credentials = new SalesforceClientCredentialsOptions(
    new Uri("https://sample.my.salesforce.com/services/oauth2/token"), "sample-client-id", "sample-client-secret");
var tokens = SalesforceAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Contact/Email__c/{newLeadEmail}")
        return Reply("""{"id":"003gL000004NewLead","success":true,"errors":[]}""", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Contact/Email__c/{existingContactEmail}")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Contact/Email__c/{duplicateEmail}")
        return Reply("""["003gL000004DupA","003gL000004DupB"]""", HttpStatusCode.Ambiguous);
    if (request.Method == HttpMethod.Post && path == "/services/data/v68.0/composite")
        return Reply("""
            {"compositeResponse":[
              {"body":{"id":"001gL000004AccNew","success":true,"errors":[]},"httpHeaders":{"Location":"/services/data/v68.0/sobjects/Account/001gL000004AccNew"},"httpStatusCode":201,"referenceId":"refAccount"},
              {"body":{"id":"003gL000004ConNew","success":true,"errors":[]},"httpHeaders":{"Location":"/services/data/v68.0/sobjects/Contact/003gL000004ConNew"},"httpStatusCode":201,"referenceId":"refContact"}
            ]}
            """);
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Account/{accountIdUpdatesCleanly}/")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Account/{accountIdHasAStaleCopy}/")
        return Reply("""[{"message":"The record was modified since the supplied If-Unmodified-Since value.","errorCode":"CANNOT_UPDATE_CONVERTED_LEAD"}]""", HttpStatusCode.PreconditionFailed);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SalesforceClient(new IntegrationTransport(apiHttp, tokens), new SalesforceOptions(instanceRoot));

Console.WriteLine("Upserting by External ID - all three documented outcomes:");
using var upsertFields = JsonDocument.Parse("""{"LastName":"Chen"}""");
var inserted = await client.UpsertByExternalIdAsync("Contact", "Email__c", "new.lead@contoso.com", upsertFields.RootElement, writesEnabled: true);
Console.WriteLine($"  Insert: created={inserted.Created}, id={inserted.RecordId}");
var updated = await client.UpsertByExternalIdAsync("Contact", "Email__c", "existing.contact@contoso.com", upsertFields.RootElement, writesEnabled: true);
Console.WriteLine($"  Update: created={updated.Created}, id={updated.RecordId ?? "(unchanged - server already knew it)"}");
var ambiguous = await client.UpsertByExternalIdAsync("Contact", "Email__c", "duplicate@contoso.com", upsertFields.RootElement, writesEnabled: true);
Console.WriteLine($"  Ambiguous match: {ambiguous.AmbiguousMatchIds!.Count} candidate id(s): {string.Join(", ", ambiguous.AmbiguousMatchIds)}");

Console.WriteLine();
Console.WriteLine("Executing a /composite call that cross-references an earlier subrequest's not-yet-committed id:");
using var newAccountFields = JsonDocument.Parse("""{"Name":"Globex Corporation"}""");
using var newContactFields = JsonDocument.Parse("""{"LastName":"Reyes","AccountId":"@{refAccount.id}"}""");
var compositeRequests = new[]
{
    new SalesforceCompositeSubrequest(HttpMethod.Post, "/services/data/v68.0/sobjects/Account", "refAccount", newAccountFields.RootElement),
    new SalesforceCompositeSubrequest(HttpMethod.Post, "/services/data/v68.0/sobjects/Contact", "refContact", newContactFields.RootElement),
};
var compositeResults = await client.ExecuteCompositeAsync(compositeRequests, allOrNone: true, writesEnabled: true);
foreach (var result in compositeResults)
    Console.WriteLine($"  Subrequest[{result.Index}] ({compositeRequests[result.Index].ReferenceId}): succeeded={result.Succeeded}, id={result.Value.GetProperty("id").GetString()}");

Console.WriteLine();
Console.WriteLine("Conditional updates guarded by If-Unmodified-Since:");
using var accountUpdateFields = JsonDocument.Parse("""{"Name":"Globex Corporation (Updated)"}""");
await client.UpdateRecordAsync("Account", accountIdUpdatesCleanly, accountUpdateFields.RootElement,
    ifUnmodifiedSince: "Fri, 13 Nov 2026 12:00:00 GMT", writesEnabled: true);
Console.WriteLine("  Clean conditional update succeeded.");
try
{
    await client.UpdateRecordAsync("Account", accountIdHasAStaleCopy, accountUpdateFields.RootElement,
        ifUnmodifiedSince: "Fri, 13 Nov 2026 12:00:00 GMT", writesEnabled: true);
}
catch (SalesforceConcurrencyException ex)
{
    Console.WriteLine($"  Stale conditional update correctly rejected: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Salesforce org.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;

// Offline demonstration of the Salesforce connector: Connected App client-credentials token exchange ->
// DiagnoseAsync connectivity check -> create/read/update/delete an Account -> paginated SOQL query -> describe.
// Every HTTP response below is synthetic; this sample never contacts a live Salesforce org and requires no
// client secret, instance URL or org credentials to run. Numeric allocation/limit figures and the Composite/Bulk
// API wire shapes used elsewhere in this connector are not exercised here and are not asserted as confirmed -
// see each feature's own tracker doc under docs/features/salesforce/ for its current verification tier.
Console.WriteLine("Koras.Connect.Salesforce offline sample: auth -> diagnostics -> CRUD -> paginated SOQL -> describe.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const string recordId = "001gL000003OpvBQAS";
var instanceRoot = new Uri("https://sample.my.salesforce.com/");

using var oauthHttp = new HttpClient(new SyntheticHandler((_, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":7200,"instance_url":"https://sample.my.salesforce.com","issued_at":"1700000000000"}""")));
var credentials = new SalesforceClientCredentialsOptions(
    new Uri("https://sample.my.salesforce.com/services/oauth2/token"), "sample-client-id", "sample-client-secret");
var tokens = SalesforceAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path == "/services/data/v68.0/")
        return Reply("""{"v68.0":"/services/data/v68.0"}""");
    if (request.Method == HttpMethod.Post && path == "/services/data/v68.0/sobjects/Account/")
        return Reply($$"""{"id":"{{recordId}}","errors":[],"success":true}""", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Get && path == $"/services/data/v68.0/sobjects/Account/{recordId}/")
        return Reply($$"""{"Id":"{{recordId}}","Name":"Acme Corp"}""");
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/sobjects/Account/{recordId}/")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Delete && path == $"/services/data/v68.0/sobjects/Account/{recordId}/")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Get && path == "/services/data/v68.0/query")
        return Reply("""{"totalSize":2,"done":false,"records":[{"Name":"Adventure Works"}],"nextRecordsUrl":"/services/data/v68.0/query/01gL000000000001"}""");
    if (request.Method == HttpMethod.Get && path == "/services/data/v68.0/query/01gL000000000001")
        return Reply("""{"totalSize":2,"done":true,"records":[{"Name":"Fabrikam Inc"}]}""");
    if (request.Method == HttpMethod.Get && path == "/services/data/v68.0/sobjects/Account/describe/")
        return Reply("""
            {"name":"Account","label":"Account","createable":true,"updateable":true,"deletable":true,"queryable":true,"keyPrefix":"001",
             "fields":[{"name":"Name","type":"string","createable":true,"updateable":true,"nillable":false,"unique":false,"externalId":false}]}
            """);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SalesforceClient(new IntegrationTransport(apiHttp, tokens), new SalesforceOptions(instanceRoot));

var diagnostics = await client.DiagnoseAsync();
foreach (var diagnostic in diagnostics)
    Console.WriteLine($"Diagnostic [{diagnostic.Stage}]: {diagnostic.Outcome} ({diagnostic.SanitizedCode})");

using var fields = JsonDocument.Parse("""{"Name":"Acme Corp"}""");
var createdId = await client.CreateRecordAsync("Account", fields.RootElement, writesEnabled: true);
Console.WriteLine($"Created account {createdId}");

using var created = await client.GetRecordAsync("Account", createdId);
Console.WriteLine($"Read back: Name={created.RootElement.GetProperty("Name").GetString()}");

using var updatedFields = JsonDocument.Parse("""{"Name":"Acme Corp (Updated)"}""");
await client.UpdateRecordAsync("Account", createdId, updatedFields.RootElement, writesEnabled: true);
Console.WriteLine("Updated account name.");

Console.WriteLine("Enumerating accounts via SOQL across pages:");
await foreach (var account in client.EnumerateQueryAsync<JsonElement>("SELECT Name FROM Account"))
    Console.WriteLine($"  Account: {account.GetProperty("Name").GetString()}");

await client.DeleteRecordAsync("Account", createdId, writesEnabled: true);
Console.WriteLine("Deleted account.");

var descriptor = await client.DescribeObjectAsync("Account");
Console.WriteLine($"Describe Account: {descriptor!.Fields.Count} field(s) returned, keyPrefix={descriptor.KeyPrefix}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Salesforce org.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;

// Offline demonstration of the Dataverse connector's metadata/discovery surface: $metadata parsing, entity and
// global choice (option-set) metadata enumeration, formatted-value annotations, and change tracking (delta)
// pagination - none of which is touched by samples/Dataverse.OfflineWalkthrough (that sample covers auth/WhoAmI/
// CRUD/plain pagination/$metadata-parsing-only instead) or samples/Dataverse.BatchAndRelationships (batch/upsert/
// relationships). Every HTTP response below is synthetic; this sample never contacts a live Dataverse environment
// or Entra tenant.
Console.WriteLine("Koras.Connect.Dataverse offline sample: metadata -> choice option sets -> formatted values -> change tracking.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var environmentRoot = "https://sample.api.crm.dynamics.com/";
var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var deletedAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");

using var oauthHttp = new HttpClient(new SyntheticHandler((request, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3599}""")));
var credentials = new DataverseClientCredentialsOptions(
    new Uri("https://login.microsoftonline.com/sample-tenant/oauth2/v2.0/token"),
    "sample-client-id", environmentRoot, ClientSecret: "sample-client-secret");
var tokens = DataverseAuthentication.CreateTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    var query = request.RequestUri!.Query;
    if (path.EndsWith("/$metadata", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""
            <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm">
            <EntityType Name="account"><Key><PropertyRef Name="accountid"/></Key><Property Name="accountid" Type="Edm.Guid"/><Property Name="name" Type="Edm.String"/></EntityType>
            </Schema></edmx:DataServices></edmx:Edmx>
            """, Encoding.UTF8, "application/xml") };
    if (path.EndsWith("/EntityDefinitions", StringComparison.Ordinal))
        return Reply("""{"value":[{"LogicalName":"account","DisplayName":{"UserLocalizedLabel":{"Label":"Account"}}},{"LogicalName":"contact","DisplayName":{"UserLocalizedLabel":{"Label":"Contact"}}}]}""");
    if (path.EndsWith("/GlobalOptionSetDefinitions", StringComparison.Ordinal))
        return Reply("""{"value":[{"Name":"account_statuscode","Options":[{"Value":1,"Label":{"UserLocalizedLabel":{"Label":"Active"}}},{"Value":2,"Label":{"UserLocalizedLabel":{"Label":"Inactive"}}}]}]}""");
    if (request.Method == HttpMethod.Get && path.EndsWith($"accounts({accountId:D})", StringComparison.Ordinal))
        return Reply($$"""{"accountid":"{{accountId:D}}","name":"Contoso Ltd","statuscode":1,"statuscode@OData.Community.Display.V1.FormattedValue":"Active"}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/accounts", StringComparison.Ordinal) && query.Contains("deltatoken=page2"))
        return Reply($$"""{"value":[],"@odata.deltaLink":"https://sample.api.crm.dynamics.com/api/data/v9.2/accounts?$select=name&$deltatoken=final"}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/accounts", StringComparison.Ordinal))
        return Reply($$"""
            {"value":[{"accountid":"{{accountId:D}}","name":"Contoso Ltd"},{"id":"{{deletedAccountId:D}}","reason":"deleted"}],
             "@odata.nextLink":"https://sample.api.crm.dynamics.com/api/data/v9.2/accounts?$select=name&$deltatoken=page2"}
            """);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new DataverseClient(new IntegrationTransport(apiHttp, tokens), new DataverseOptions(new Uri(environmentRoot)));

var metadata = await client.RefreshMetadataAsync();
var descriptors = DataverseClient.ParseEntityDescriptors(metadata);
Console.WriteLine($"$metadata describes {descriptors.Count} entity type(s): {string.Join(", ", descriptors.Select(d => d.LogicalName))}.");

Console.WriteLine("Enumerating entity definitions:");
await foreach (var entity in client.EnumerateEntityDefinitionsAsync())
    Console.WriteLine($"  Entity: {entity.GetProperty("LogicalName").GetString()}");

Console.WriteLine("Enumerating global option sets:");
await foreach (var optionSet in client.EnumerateGlobalOptionSetsAsync())
{
    Console.WriteLine($"  Option set: {optionSet.GetProperty("Name").GetString()}");
    foreach (var option in optionSet.GetProperty("Options").EnumerateArray())
        Console.WriteLine($"    {option.GetProperty("Value").GetInt32()} = {option.GetProperty("Label").GetProperty("UserLocalizedLabel").GetProperty("Label").GetString()}");
}

using var account = await client.GetRowAsync("accounts", accountId, includeFormattedValues: true);
var formattedStatus = DataverseClient.TryGetFormattedValue(account.RootElement, "statuscode");
Console.WriteLine($"Account status (raw={account.RootElement.GetProperty("statuscode").GetInt32()}, formatted={formattedStatus}).");

Console.WriteLine();
Console.WriteLine("Walking change tracking (delta) pages for accounts:");
var page = await client.GetChangesAsync("accounts", select: "name");
foreach (var changed in page.Changed)
    Console.WriteLine($"  Changed: {changed.GetProperty("name").GetString()}");
foreach (var deleted in page.Deleted)
    Console.WriteLine($"  Deleted: {deleted.Id}");
page = await client.GetChangesAsync("accounts", select: "name", resumeFrom: page.Next);
Console.WriteLine($"  Second page: {page.Changed.Count} changed, {page.Deleted.Count} deleted (deltaLink reached - sync is caught up).");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Dataverse environment or Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

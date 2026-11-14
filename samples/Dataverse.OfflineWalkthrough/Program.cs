using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;

// Offline demonstration of the Dataverse connector: Entra client-credentials token exchange -> WhoAmI connectivity
// check -> create/read/update/delete a row -> paginated query -> $metadata parsing. Every HTTP response below is
// synthetic; this sample never contacts a live Dataverse environment or Entra tenant and requires no client
// secret, environment URL or tenant ID to run.
Console.WriteLine("Koras.Connect.Dataverse offline sample: auth -> WhoAmI -> CRUD -> paginated query -> metadata.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var rowId = Guid.Parse("11111111-1111-1111-1111-111111111111");

using var oauthHttp = new HttpClient(new SyntheticHandler((request, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3599}""")));
var credentials = new DataverseClientCredentialsOptions(
    new Uri("https://login.microsoftonline.com/sample-tenant/oauth2/v2.0/token"),
    "sample-client-id", "https://sample.api.crm.dynamics.com/", ClientSecret: "sample-client-secret");
var tokens = DataverseAuthentication.CreateTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (path.EndsWith("/WhoAmI", StringComparison.Ordinal))
        return Reply($$"""{"UserId":"{{rowId:D}}","BusinessUnitId":"22222222-2222-2222-2222-222222222222","OrganizationId":"33333333-3333-3333-3333-333333333333"}""");
    if (request.Method == HttpMethod.Post && path.EndsWith("/accounts", StringComparison.Ordinal))
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        response.Headers.Add("OData-EntityId", $"https://sample.api.crm.dynamics.com/api/data/v9.2/accounts({rowId:D})");
        return response;
    }
    if (request.Method == HttpMethod.Get && path.EndsWith($"accounts({rowId:D})", StringComparison.Ordinal))
        return Reply($$"""{"accountid":"{{rowId:D}}","name":"Contoso Ltd"}""");
    if (request.Method == HttpMethod.Patch && path.EndsWith($"accounts({rowId:D})", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Delete && path.EndsWith($"accounts({rowId:D})", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Get && path.EndsWith("/accounts", StringComparison.Ordinal))
    {
        if (request.RequestUri!.Query.Contains("skiptoken"))
            return Reply("""{"value":[{"accountid":"44444444-4444-4444-4444-444444444444","name":"Fabrikam Inc"}]}""");
        return Reply("""{"value":[{"accountid":"55555555-5555-5555-5555-555555555555","name":"Adventure Works"}],"@odata.nextLink":"https://sample.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken=opaque-cookie"}""");
    }
    if (path.EndsWith("/$metadata", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""
            <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm">
            <EntityType Name="account"><Key><PropertyRef Name="accountid"/></Key><Property Name="accountid" Type="Edm.Guid"/><Property Name="name" Type="Edm.String"/></EntityType>
            </Schema></edmx:DataServices></edmx:Edmx>
            """, Encoding.UTF8, "application/xml") };
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new DataverseClient(new IntegrationTransport(apiHttp, tokens), new DataverseOptions(new Uri("https://sample.api.crm.dynamics.com/")));

var who = await client.WhoAmIAsync();
Console.WriteLine($"WhoAmI: UserId={who.UserId}");

using var fields = JsonDocument.Parse("""{"name":"Contoso Ltd"}""");
var createdId = await client.CreateRowAsync("accounts", fields.RootElement, writesEnabled: true);
Console.WriteLine($"Created account {createdId}");

using var created = await client.GetRowAsync("accounts", createdId);
Console.WriteLine($"Read back: name={created.RootElement.GetProperty("name").GetString()}");

using var updatedFields = JsonDocument.Parse("""{"name":"Contoso Ltd (Updated)"}""");
await client.UpdateRowAsync("accounts", createdId, updatedFields.RootElement, writesEnabled: true);
Console.WriteLine("Updated account name.");

Console.WriteLine("Enumerating accounts across pages:");
await foreach (var account in client.EnumerateAsync("accounts"))
    Console.WriteLine($"  Account: {account.GetProperty("name").GetString()}");

await client.DeleteRowAsync("accounts", createdId, writesEnabled: true);
Console.WriteLine("Deleted account.");

var metadata = await client.GetMetadataAsync();
var descriptors = DataverseClient.ParseEntityDescriptors(metadata);
Console.WriteLine($"Metadata describes {descriptors.Count} entity type(s): {string.Join(", ", descriptors.Select(d => d.LogicalName))}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Dataverse environment or Entra tenant.");

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;

// Offline demonstration of the Microsoft Graph connector: app-only client-credentials token exchange ->
// DiagnoseAsync (Configuration/Authentication/Connectivity/Permissions) -> users/groups -> OneDrive ->
// paginated/delta enumeration -> v1.0-vs-beta endpoint selection. Every HTTP response below is synthetic;
// this sample never contacts a live Entra tenant and requires no tenant id, client secret, or certificate to
// run. Numeric allocation/limit figures and the wire shapes exercised elsewhere in this connector (Teams,
// Planner, upload sessions, etc.) are not exercised here and are not asserted as confirmed - see each
// feature's own tracker doc under docs/features/microsoft-graph/ for its current verification tier.
Console.WriteLine("Koras.Connect.Microsoft.Graph offline sample: auth -> diagnostics -> users/groups -> OneDrive -> pagination/delta -> v1.0 vs beta.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

using var oauthHttp = new HttpClient(new SyntheticHandler((_, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3600}""")));
var credentials = new GraphClientCredentialsOptions("11111111-1111-1111-1111-111111111111", "sample-client-id", "sample-client-secret");
var tokens = GraphAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);

const string driveId = "sample-drive-id";

using var v1Http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    var query = request.RequestUri!.Query;
    if (request.Method == HttpMethod.Get && path == "/v1.0/organization")
        return Reply("""{"value":[{"id":"22222222-2222-2222-2222-222222222222","displayName":"Contoso","verifiedDomains":[{"name":"contoso.com"}]}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/oauth2PermissionGrants")
        return Reply("""{"value":[{"clientId":"sample-client-id","consentType":"AllPrincipals","principalId":null,"resourceId":"33333333-3333-3333-3333-333333333333","scope":"User.Read.All"}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/users" && !query.Contains("page2"))
        return Reply("""{"value":[{"id":"user-1","displayName":"Ada Lovelace","userPrincipalName":"ada@contoso.com"}],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users?$skiptoken=page2"}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/users" && query.Contains("page2"))
        return Reply("""{"value":[{"id":"user-2","displayName":"Grace Hopper","userPrincipalName":"grace@contoso.com"}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/groups")
        return Reply("""{"value":[{"id":"group-1","displayName":"Engineering"}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/drives")
        return Reply($$"""{"value":[{"id":"{{driveId}}","name":"Sample Drive"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/drives/{driveId}/root/children")
        return Reply("""{"value":[{"id":"item-1","name":"Report.docx","size":10240}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/users/delta" && !query.Contains("skiptoken"))
        return Reply("""{"value":[{"id":"user-1","displayName":"Ada Lovelace"}],"@odata.nextLink":"https://graph.microsoft.com/v1.0/users/delta?$skiptoken=deltapage2"}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/users/delta" && query.Contains("deltapage2"))
        return Reply("""{"value":[{"id":"user-2","displayName":"Grace Hopper"}],"@odata.deltaLink":"https://graph.microsoft.com/v1.0/users/delta?$deltatoken=sample-delta-token"}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new GraphClient(new IntegrationTransport(v1Http, tokens));

var diagnostics = await client.DiagnoseAsync();
foreach (var diagnostic in diagnostics)
    Console.WriteLine($"Diagnostic [{diagnostic.Stage}]: {diagnostic.Outcome} ({diagnostic.SanitizedCode})");

Console.WriteLine();
Console.WriteLine("Enumerating users across pages:");
await foreach (var user in client.EnumerateUsersAsync())
    Console.WriteLine($"  User: {user.DisplayName} <{user.UserPrincipalName}>");

Console.WriteLine("Enumerating groups:");
await foreach (var group in client.EnumerateGroupsAsync())
    Console.WriteLine($"  Group: {group.DisplayName}");

Console.WriteLine("Enumerating OneDrive drives and the root folder's children:");
await foreach (var drive in client.EnumerateDrivesAsync())
{
    var id = drive.RootElement.GetProperty("id").GetString()!;
    await foreach (var item in client.EnumerateDriveItemsAsync(id))
        Console.WriteLine($"  Drive item: {item.Name} ({item.Size} bytes)");
}

Console.WriteLine("Running one delta sync pass over /users/delta:");
var delta = await client.GetDeltaAsync<GraphUser>("users");
foreach (var changed in delta.Items)
    Console.WriteLine($"  Changed user: {changed.DisplayName}");
Console.WriteLine($"  Delta link captured for next sync pass: {delta.DeltaLink is not null}");

// v1.0-vs-beta version selection (MGR-003/MGR-026): beta is opt-in only and explicitly not supported in
// production, per Microsoft's own "beta is not supported in production" guidance - demonstrated here purely
// to confirm the version-selection mechanism itself, never as a recommendation to ship against beta.
using var betaHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/beta/organization")
        return Reply("""{"value":[{"id":"22222222-2222-2222-2222-222222222222","displayName":"Contoso (beta)","verifiedDomains":[{"name":"contoso.com"}]}]}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));
var betaClient = new GraphClient(new IntegrationTransport(betaHttp, tokens), new GraphOptions(UseBetaEndpoint: true));
var betaOrganization = await betaClient.GetOrganizationAsync();
Console.WriteLine();
Console.WriteLine($"Beta-endpoint organization read (non-production demo only): {betaOrganization.DisplayName}");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;

// Offline demonstration of the Microsoft Graph connector's advanced-HTTP-pattern surface: $batch requests
// (including id-based, not array-position-based, response mapping), the resumable large-file upload-session
// lifecycle (create -> sequential fragments -> status/resume -> cancel), and permission/consent diagnostics -
// none of which is touched by samples/MicrosoftGraph.OfflineWalkthrough, .TeamsAndPlannerCollaboration, or
// .SharePointAndOutlook. Every HTTP response below is synthetic; this sample never contacts a live Entra tenant.
Console.WriteLine("Koras.Connect.Microsoft.Graph offline sample: $batch -> resumable uploads -> permission diagnostics.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const long multiple = GraphClient.UploadFragmentSizeMultiple; // 327,680 bytes (320 KiB)
var fragmentHistory = new List<string>();

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Post && path == "/v1.0/$batch")
        // Responses are deliberately returned out of request order (Graph does not guarantee response order) -
        // the client must map each one back to its original request by "id", not by array position.
        return Reply("""
            {"responses":[
              {"id":"2","status":200,"body":{"value":[{"id":"item-1"}]}},
              {"id":"0","status":200,"body":{"id":"org-1","displayName":"Contoso"}},
              {"id":"1","status":200,"body":{"id":"site-root","displayName":"Contoso Site"}}
            ]}
            """);
    if (request.Method == HttpMethod.Post && path == "/v1.0/drives/drive-1/root:/Documents/quarterly-report.pdf:/createUploadSession")
        return Reply("""{"uploadUrl":"https://upload.example.com/sessions/session-1","expirationDateTime":"2026-11-21T00:00:00Z"}""");
    if (request.Method == HttpMethod.Put && path == "/sessions/session-1")
    {
        fragmentHistory.Add(request.Content!.Headers.ContentRange!.ToString());
        var isFinal = request.Content!.Headers.ContentRange!.Length == request.Content.Headers.ContentRange!.To! + 1;
        return Reply("{}", isFinal ? HttpStatusCode.Created : HttpStatusCode.Accepted);
    }
    if (request.Method == HttpMethod.Post && path == "/v1.0/drives/drive-1/root:/Documents/resumable-notes.txt:/createUploadSession")
        return Reply("""{"uploadUrl":"https://upload.example.com/sessions/session-2","expirationDateTime":"2026-11-21T00:00:00Z"}""");
    if (request.Method == HttpMethod.Get && path == "/sessions/session-2")
        return Reply("""{"nextExpectedRanges":["0-"],"expirationDateTime":"2026-11-21T00:00:00Z"}""");
    if (request.Method == HttpMethod.Put && path == "/sessions/session-2")
        return Reply("{}", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Post && path == "/v1.0/drives/drive-1/root:/Documents/abandoned-draft.docx:/createUploadSession")
        return Reply("""{"uploadUrl":"https://upload.example.com/sessions/session-3"}""");
    if (request.Method == HttpMethod.Delete && path == "/sessions/session-3")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Get && path == "/v1.0/oauth2PermissionGrants")
        return Reply("""{"value":[{"clientId":"sample-client-id","consentType":"AllPrincipals","resourceId":"resource-1","scope":"User.Read.All Group.Read.All"}]}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/me/oauth2PermissionGrants")
        return Reply("""{"value":[{"clientId":"sample-client-id","consentType":"Principal","principalId":"user-1","resourceId":"resource-1","scope":"Mail.Read"}]}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var tokens = new StaticTokenSource("sample-access-token");
var client = new GraphClient(new IntegrationTransport(http, tokens));

Console.WriteLine("Executing a $batch call with 3 sub-requests:");
var batchRequests = new[]
{
    new GraphBatchRequest("0", HttpMethod.Get, "/organization"),
    new GraphBatchRequest("1", HttpMethod.Get, "/sites/root"),
    new GraphBatchRequest("2", HttpMethod.Get, "/sites/root/lists/list-1/items", DependsOn: ["1"]),
};
var batchResults = await client.ExecuteBatchAsync(batchRequests);
for (var i = 0; i < batchResults.Count; i++)
{
    var result = batchResults[i];
    Console.WriteLine($"  Request '{batchRequests[i].Id}' -> succeeded={result.Succeeded}, body={result.Value.GetRawText()}");
}

Console.WriteLine();
Console.WriteLine("Uploading a file via a resumable session (two sequential fragments):");
var session = await client.CreateUploadSessionAsync("drive-1", "Documents", "quarterly-report.pdf", multiple + 100, writesEnabled: true);
using var firstFragment = new MemoryStream(new byte[multiple]);
await client.UploadSessionFragmentAsync(session, firstFragment, 0, multiple - 1, multiple + 100, writesEnabled: true);
using var finalFragment = new MemoryStream(new byte[100]);
await client.UploadSessionFragmentAsync(session, finalFragment, multiple, multiple + 99, multiple + 100, writesEnabled: true);
Console.WriteLine($"  Uploaded {fragmentHistory.Count} fragment(s): {string.Join(", ", fragmentHistory)}");

Console.WriteLine();
Console.WriteLine("Checking status on a second session, then completing it (simulates resume-after-interruption):");
var resumableSession = await client.CreateUploadSessionAsync("drive-1", "Documents", "resumable-notes.txt", 100, writesEnabled: true);
var nextExpectedRanges = await client.GetUploadSessionStatusAsync(resumableSession);
Console.WriteLine($"  Server reports nextExpectedRanges: {string.Join(", ", nextExpectedRanges)}");
using var resumedFragment = new MemoryStream(new byte[100]);
await client.UploadSessionFragmentAsync(resumableSession, resumedFragment, 0, 99, 100, writesEnabled: true);
Console.WriteLine("  Completed the resumed session.");

Console.WriteLine();
Console.WriteLine("Cancelling an abandoned session:");
var abandonedSession = await client.CreateUploadSessionAsync("drive-1", "Documents", "abandoned-draft.docx", 1_000_000, writesEnabled: true);
await client.CancelUploadSessionAsync(abandonedSession, writesEnabled: true);
Console.WriteLine("  Cancelled.");

Console.WriteLine();
Console.WriteLine("Listing the current app's tenant-wide permission grants:");
await foreach (var grant in client.ListGrantsForCurrentAppAsync())
    Console.WriteLine($"  Grant: clientId={grant.ClientId}, consentType={grant.ConsentType}, scope={grant.Scope}");

Console.WriteLine("Listing the signed-in user's own grants (requires a delegated context):");
var delegatedClient = new GraphClient(new IntegrationTransport(http, tokens), new GraphOptions(IsDelegatedContext: true));
await foreach (var grant in delegatedClient.ListMyGrantsAsync())
    Console.WriteLine($"  Grant: clientId={grant.ClientId}, principalId={grant.PrincipalId}, scope={grant.Scope}");

var adminConsentUrl = GraphClient.BuildAdminConsentUrl("contoso-tenant", "sample-client-id", ["User.Read.All", "Group.Read.All"], new Uri("https://contoso-admin-portal.example.com/callback"));
Console.WriteLine($"Admin-consent URL (non-production demo only, never POSTed to anywhere real): {adminConsentUrl}");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

sealed class StaticTokenSource(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

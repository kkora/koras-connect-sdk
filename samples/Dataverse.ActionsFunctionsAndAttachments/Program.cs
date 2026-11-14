using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;

// Offline demonstration of the Dataverse connector's actions/functions and attachment surface: bound/unbound
// actions, parameterized functions, file/image column upload-download-delete, and legacy annotation (Note)
// attachments - none of which is touched by samples/Dataverse.OfflineWalkthrough, .BatchAndRelationships, or
// .MetadataChoicesAndDelta. Every HTTP response below is synthetic; this sample never contacts a live Dataverse
// environment or Entra tenant.
Console.WriteLine("Koras.Connect.Dataverse offline sample: actions/functions -> file columns -> annotations.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var environmentRoot = "https://sample.api.crm.dynamics.com/";
var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
var annotationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
var logoBytes = Encoding.UTF8.GetBytes("synthetic-image-bytes");

using var oauthHttp = new HttpClient(new SyntheticHandler((request, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3599}""")));
var credentials = new DataverseClientCredentialsOptions(
    new Uri("https://login.microsoftonline.com/sample-tenant/oauth2/v2.0/token"),
    "sample-client-id", environmentRoot, ClientSecret: "sample-client-secret");
var tokens = DataverseAuthentication.CreateTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Post && path.EndsWith("/Merge", StringComparison.Ordinal))
        return Reply("{}");
    if (request.Method == HttpMethod.Post && path.EndsWith($"accounts({accountId:D})/Microsoft.Dynamics.CRM.EscalateAccount", StringComparison.Ordinal))
        return Reply("""{"EscalatedOn":"2026-11-14T00:00:00Z"}""");
    if (request.Method == HttpMethod.Get && path.Contains("RetrieveTotalRecordCount", StringComparison.Ordinal))
        return Reply("""{"EntityRecordCountCollection":{"Values":[12345]}}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/WhoAmI()", StringComparison.Ordinal))
        return Reply($$"""{"UserId":"{{accountId:D}}"}""");
    if (request.Method == HttpMethod.Patch && path.EndsWith($"accounts({accountId:D})/entityimage", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Get && path.EndsWith($"accounts({accountId:D})/entityimage/$value", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(logoBytes) { Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") } } };
    if (request.Method == HttpMethod.Delete && path.EndsWith($"accounts({accountId:D})/entityimage", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Post && path.EndsWith("/annotations", StringComparison.Ordinal))
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        response.Headers.Add("OData-EntityId", $"{environmentRoot}api/data/v9.2/annotations({annotationId:D})");
        return response;
    }
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new DataverseClient(new IntegrationTransport(apiHttp, tokens), new DataverseOptions(new Uri(environmentRoot)));

Console.WriteLine("Invoking actions and functions:");
using var mergeParameters = JsonDocument.Parse($$"""{"Target":{"accountid":"{{accountId:D}}","@odata.type":"Microsoft.Dynamics.CRM.account"},"PerformParentingChecks":false}""");
using var mergeResult = await client.InvokeUnboundActionAsync("Merge", mergeParameters.RootElement, writesEnabled: true);
Console.WriteLine($"  Invoked unbound action Merge, response had {mergeResult.RootElement.EnumerateObject().Count()} top-level property/properties.");

using var escalateParameters = JsonDocument.Parse("{}");
using var escalateResult = await client.InvokeBoundActionAsync("accounts", accountId, "EscalateAccount", escalateParameters.RootElement, writesEnabled: true);
Console.WriteLine($"  Invoked bound action EscalateAccount: {escalateResult.RootElement.GetProperty("EscalatedOn").GetString()}");

using var countResult = await client.InvokeFunctionAsync("RetrieveTotalRecordCount", new Dictionary<string, string> { ["EntityNames"] = "['account']" });
Console.WriteLine($"  RetrieveTotalRecordCount: {countResult.RootElement.GetProperty("EntityRecordCountCollection").GetProperty("Values")[0].GetInt32()}");

using var whoResult = await client.InvokeFunctionAsync("WhoAmI");
Console.WriteLine($"  WhoAmI (parameterless function): UserId={whoResult.RootElement.GetProperty("UserId").GetString()}");

Console.WriteLine();
Console.WriteLine("Managing a file column on the account:");
using var uploadStream = new MemoryStream(logoBytes);
await client.UploadFileColumnAsync("accounts", accountId, "entityimage", uploadStream, "logo.png", writesEnabled: true);
Console.WriteLine($"  Uploaded {logoBytes.Length} byte(s) to the entityimage column.");
using var downloadStream = new MemoryStream();
await client.DownloadFileColumnAsync("accounts", accountId, "entityimage", downloadStream);
Console.WriteLine($"  Downloaded {downloadStream.Length} byte(s) back from the entityimage column.");
await client.DeleteFileColumnAsync("accounts", accountId, "entityimage", writesEnabled: true);
Console.WriteLine("  Deleted the entityimage column's content.");

Console.WriteLine();
Console.WriteLine("Attaching a legacy annotation (Note) - reuses generic row CRUD against the annotations entity set:");
var annotation = new DataverseAnnotation
{
    FileName = "report.pdf",
    MimeType = "application/pdf",
    DocumentBodyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("synthetic-pdf-bytes")),
    IsDocument = true,
};
var createdAnnotationId = await client.CreateRowAsync("annotations", JsonSerializer.SerializeToElement(annotation), writesEnabled: true);
Console.WriteLine($"  Created annotation {createdAnnotationId}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Dataverse environment or Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

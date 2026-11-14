using System.Net;
using System.Text;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;

// Offline demonstration of the Salesforce connector's Bulk API 2.0 surface: an ingest job's full lifecycle
// (create -> upload CSV batch -> close -> poll state -> download results) and a query job's lifecycle
// (create -> poll state -> paginate results via the Sforce-Locator header) - none of which is touched by
// samples/Salesforce.OfflineWalkthrough (that sample covers auth/diagnostics/CRUD/SOQL/describe instead). Every
// HTTP response below is synthetic; this sample never contacts a live Salesforce org.
Console.WriteLine("Koras.Connect.Salesforce offline sample: Bulk API 2.0 ingest job -> Bulk API 2.0 query job.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

var instanceRoot = new Uri("https://sample.my.salesforce.com/");
const string ingestJobId = "750gL000003OpvBQAS";
const string queryJobId = "750gL000003OpvBQAT";

using var oauthHttp = new HttpClient(new SyntheticHandler((_, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":7200,"instance_url":"https://sample.my.salesforce.com","issued_at":"1700000000000"}""")));
var credentials = new SalesforceClientCredentialsOptions(
    new Uri("https://sample.my.salesforce.com/services/oauth2/token"), "sample-client-id", "sample-client-secret");
var tokens = SalesforceAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    var query = request.RequestUri!.Query;
    if (request.Method == HttpMethod.Post && path == "/services/data/v68.0/jobs/ingest")
        return Reply($$"""{"id":"{{ingestJobId}}","state":"Open"}""");
    if (request.Method == HttpMethod.Put && path == $"/services/data/v68.0/jobs/ingest/{ingestJobId}/batches")
        return new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
    if (request.Method == HttpMethod.Patch && path == $"/services/data/v68.0/jobs/ingest/{ingestJobId}")
        return Reply("{}");
    if (request.Method == HttpMethod.Get && path == $"/services/data/v68.0/jobs/ingest/{ingestJobId}")
        return Reply("""{"state":"JobComplete"}""");
    if (request.Method == HttpMethod.Get && path == $"/services/data/v68.0/jobs/ingest/{ingestJobId}/successfulResults/")
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("sf__Id,sf__Created,FirstName,LastName\n003gL000004TmSAAU,true,Avery,Chen\n", Encoding.UTF8, "text/csv") };
    if (request.Method == HttpMethod.Post && path == "/services/data/v68.0/jobs/query")
        return Reply($$"""{"id":"{{queryJobId}}","state":"UploadComplete"}""");
    if (request.Method == HttpMethod.Get && path == $"/services/data/v68.0/jobs/query/{queryJobId}")
        return Reply("""{"state":"JobComplete"}""");
    if (request.Method == HttpMethod.Get && path == $"/services/data/v68.0/jobs/query/{queryJobId}/results")
    {
        var csv = query.Contains("locator=page2token", StringComparison.Ordinal)
            ? "Id,Name\n001gL000004Fabrikam,Fabrikam Inc\n"
            : "Id,Name\n001gL000004Contoso,Contoso Ltd\n";
        var locator = query.Contains("locator=page2token", StringComparison.Ordinal) ? "null" : "page2token";
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(csv, Encoding.UTF8, "text/csv") };
        response.Headers.TryAddWithoutValidation("Sforce-Locator", locator);
        return response;
    }
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SalesforceClient(new IntegrationTransport(apiHttp, tokens), new SalesforceOptions(instanceRoot));

Console.WriteLine("Running a Bulk API 2.0 ingest job:");
var ingestJob = await client.CreateIngestJobAsync("Contact", SalesforceBulkIngestOperation.Insert, writesEnabled: true);
Console.WriteLine($"  Created ingest job {ingestJob.JobId}.");
using var csvBatch = new MemoryStream(Encoding.UTF8.GetBytes("FirstName,LastName\nAvery,Chen\n"));
await client.UploadIngestBatchAsync(ingestJob, csvBatch, writesEnabled: true);
Console.WriteLine("  Uploaded a CSV batch.");
await client.CloseIngestJobAsync(ingestJob, writesEnabled: true);
Console.WriteLine("  Closed the job (state -> UploadComplete).");
var ingestState = await client.GetIngestJobStateAsync(ingestJob);
Console.WriteLine($"  Job state: {ingestState}.");
using var successfulResults = new MemoryStream();
await client.DownloadSuccessfulResultsAsync(ingestJob, successfulResults);
Console.WriteLine($"  Downloaded successful-results CSV ({successfulResults.Length} byte(s)):");
Console.WriteLine($"    {Encoding.UTF8.GetString(successfulResults.ToArray()).TrimEnd().Replace("\n", "\n    ")}");

Console.WriteLine();
Console.WriteLine("Running a Bulk API 2.0 query job:");
var queryJob = await client.CreateQueryJobAsync("SELECT Id, Name FROM Account");
Console.WriteLine($"  Created query job {queryJob.JobId}.");
var queryState = await client.GetQueryJobStateAsync(queryJob);
Console.WriteLine($"  Job state: {queryState}.");
Console.WriteLine("  Paginating results via the Sforce-Locator header:");
await foreach (var resultPage in client.EnumerateQueryResultsAsync(queryJob))
{
    using var reader = new StreamReader(resultPage);
    Console.WriteLine($"    Page: {(await reader.ReadToEndAsync()).TrimEnd().Replace("\n", " | ")}");
}

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Salesforce org.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

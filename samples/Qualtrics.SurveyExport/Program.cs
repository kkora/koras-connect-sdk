using System.Net;
using System.Text;
using Koras.Connect.Qualtrics;
using Koras.Connect.Transport;

// Offline demonstration of the survey -> export -> poll (with a simulated mid-export interruption and resume) ->
// download pipeline. Every HTTP response below is synthetic; this sample never contacts a live Qualtrics tenant,
// never sends a distribution/message, and requires no API token, data center or survey ID to run.
Console.WriteLine("Koras.Connect.Qualtrics offline sample: enumerate -> export -> poll (simulated interruption/resume) -> download.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live Qualtrics tenant is contacted.");
Console.WriteLine();

var calls = 0;
using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    calls++;
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path.EndsWith("/surveys", StringComparison.Ordinal))
        return Reply("""{"result":{"elements":[{"id":"SV_sample","name":"Sample engagement survey","isActive":true}],"nextPage":null}}""");
    if (request.Method == HttpMethod.Post && path.EndsWith("/export-responses", StringComparison.Ordinal))
        return Reply("""{"result":{"progressId":"progress-sample"}}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/export-responses/progress-sample", StringComparison.Ordinal))
        // Calls 3 and 4 simulate the export still running (including one poll right before a simulated process
        // restart); call 5 onward simulates the job finishing after the caller resumes with the same progress ID.
        return Reply(calls <= 4
            ? """{"result":{"status":"inProgress","percentComplete":40}}"""
            : """{"result":{"status":"complete","percentComplete":100,"fileId":"file-sample"}}""");
    if (request.Method == HttpMethod.Get && path.EndsWith("/file", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("id,response\n1,sample\n")) };
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = QualtricsClient.Create(http, new StaticToken("synthetic-sample-token"), new QualtricsOptions(new Uri("https://sample.qualtrics.test/API/v3/")));

SurveySummary? survey = null;
await foreach (var candidate in client.EnumerateSurveysAsync())
{
    survey = candidate;
    Console.WriteLine($"Discovered survey: {survey.Id} ({survey.Name})");
    break;
}
if (survey?.Id is null) throw new InvalidOperationException("The synthetic survey feed produced no survey.");

var progressId = await client.StartResponseExportAsync(survey.Id, new ResponseExportOptions("csv"));
Console.WriteLine($"Started export job {progressId}.");

// Simulate an interruption: poll once directly, then pretend the host process restarted. Resuming means calling
// GetExportProgressAsync/WaitForExportAsync again with the SAME progress ID rather than starting a brand-new export.
var firstPoll = await client.GetExportProgressAsync(survey.Id, progressId);
Console.WriteLine($"Observed status before the simulated interruption: {firstPoll.Status} ({firstPoll.PercentComplete}%).");
Console.WriteLine("Simulating a process restart now; resuming with the existing progress ID instead of starting a new export.");

var completed = await client.WaitForExportAsync(survey.Id, progressId, TimeSpan.Zero, maxPolls: 10);
Console.WriteLine($"Export completed: status={completed.Status}, fileId={completed.FileId}.");

using var output = new MemoryStream();
await client.DownloadExportAsync(survey.Id, completed.FileId!, output);
var firstLine = Encoding.UTF8.GetString(output.ToArray()).Split('\n')[0];
Console.WriteLine($"Downloaded {output.Length} synthetic bytes. First line: {firstLine}");
Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Qualtrics tenant and without sending any message.");

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

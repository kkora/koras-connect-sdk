using System.Net;
using System.Text;
using Koras.Connect.Adp.Export;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Export;
using Koras.Connect.Transport;

// Offline demonstration of ADP-007/008/013: stream workers from a mocked AdpWorkersClient through
// WorkerIdentityMapper (neutral identity projection) and WorkerExportProjection (the fixed, documented export
// column set) into a CSV. Every HTTP response below is synthetic; this sample never contacts a live ADP tenant.
Console.WriteLine("Koras.Connect.Adp offline sample: EnumerateTypedWorkersAsync -> WorkerIdentityMapper / WorkerExportProjection -> CSV.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

// The second worker intentionally carries an extension field (a truncated, already-masked SSN fragment) that a
// tenant might legitimately return. It must never appear in the CSV: WorkerExportProjection only ever emits its
// documented, fixed column set and never surfaces Worker.AdditionalData.
var page1 = """
    {"workers":[
      {"associateOID":"w1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}},"workAssignments":[{"jobTitle":"Engineer","hireDate":"2024-01-01"}]},
      {"associateOID":"w2","person":{"legalName":{"givenName":"Grace","familyName1":"Hopper"}},"workAssignments":[{"jobTitle":"Admiral","hireDate":"2020-01-01"}],"ssnLastFour":"***1234"}
    ]}
    """;
const string emptyPage = """{"workers":[]}""";

// Each fresh enumeration below restarts paging at $skip=0, so the synthetic handler keys its reply off that query
// parameter (not a global call counter) to give page1 every time the export is re-run from the start.
using var http = new HttpClient(new SyntheticHandler(request =>
    Reply(request.RequestUri!.Query.Contains("$skip=0", StringComparison.Ordinal) ? page1 : emptyPage)));
var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));

Console.WriteLine("Mapping each worker to a neutral identity (WorkerIdentityMapper):");
var mapper = new WorkerIdentityMapper();
await foreach (var worker in workers.EnumerateTypedWorkersAsync())
{
    var result = mapper.Map(worker);
    Console.WriteLine(result.IsSuccess
        ? $"  {worker.AssociateOid}: mapped to {result.Value!.GivenName} {result.Value.FamilyName} (externalId={result.Value.ExternalId ?? "<none>"})"
        : $"  {worker.AssociateOid}: not mapped ({(result.IsOmitted ? "omitted: " + result.OmittedReason : string.Join("; ", result.Errors.Select(e => $"{e.Field}: {e.Reason}")))})");
}
Console.WriteLine();

Console.WriteLine("Streaming the same population through WorkerExportProjection + CsvExportProvider (safe columns only):");
using var csvOutput = new MemoryStream();
await WorkerExportProjection.ExportAsync(new CsvExportProvider(), csvOutput, workers.EnumerateTypedWorkersAsync());
var csv = Encoding.UTF8.GetString(csvOutput.ToArray());
Console.WriteLine(csv);
Console.WriteLine($"Safe-column-only check: the raw worker's extension field ('ssnLastFour') never appears in the export (confirmed absent: {!csv.Contains("1234", StringComparison.Ordinal)}).");
Console.WriteLine();

Console.WriteLine("Demonstrating cancellation: requesting the export again with a token cancelled before the first page is read.");
using var cancelledOutput = new MemoryStream();
using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await WorkerExportProjection.ExportAsync(new CsvExportProvider(), cancelledOutput, workers.EnumerateTypedWorkersAsync(cts.Token), cancellationToken: cts.Token);
    Console.WriteLine("Unexpected: the export completed despite the token already being cancelled.");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancellation was honored before any row was written; no partial export was left half-complete.");
}

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class FixedToken : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("sample-token");
}

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

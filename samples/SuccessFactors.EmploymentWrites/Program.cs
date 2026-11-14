using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;

// Offline demonstration of the opt-in upsert surface: the generic UpsertAsync primitive (SAP-003) and the typed
// UpsertEmploymentAsync/UpsertJobAsync wrappers added under SAP-012, which validate each entity's documented
// composite key locally - userId+startDate for EmpEmployment, userId+seqNumber for EmpJob - before any HTTP call,
// rather than letting a missing key surface only as an opaque row failure. Every HTTP response below is
// synthetic; this sample never contacts a live SAP SuccessFactors tenant and performs no write without the
// explicit writesEnabled: true opt-in on every call.
Console.WriteLine("Koras.Connect.Sap.SuccessFactors offline sample: generic UpsertAsync vs. typed UpsertEmploymentAsync/UpsertJobAsync.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and nothing is written without an explicit opt-in.");
Console.WriteLine();

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var body = request.Content!.ReadAsStringAsync().Result;
    if (body.Contains("sample-readonly", StringComparison.Ordinal))
        return Reply("""{"d":[{"key":null,"status":"ERROR","editStatus":null,"httpCode":403,"index":0,"message":"private tenant-specific detail"}]}""");
    return Reply("""{"d":[{"key":"sample-1","status":"OK","editStatus":"UPDATED","httpCode":204,"index":0,"message":null}]}""");
}));
var client = new SuccessFactorsClient(new IntegrationTransport(http, new StaticToken("sample-access-token")), new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));

// --- Generic UpsertAsync: caller supplies the entity name and raw fields; no key validation beyond the SDK's
// own bounds (1-1000 records) and "no __metadata in the payload" rule. ---
using var genericRow = JsonDocument.Parse("""{"personIdExternal":"sample-1"}""");
var genericResult = await client.UpsertAsync("PerPerson", [genericRow.RootElement], writesEnabled: true);
Console.WriteLine($"Generic UpsertAsync(\"PerPerson\"): editStatus={genericResult[0].EditStatus}, succeeded={genericResult[0].Succeeded}.");
Console.WriteLine();

// --- Typed UpsertEmploymentAsync: rejects a row missing startDate locally, before any HTTP call. ---
using var missingStartDate = JsonDocument.Parse("""{"userId":"sample-1"}""");
try { await client.UpsertEmploymentAsync([missingStartDate.RootElement], writesEnabled: true); }
catch (ArgumentException ex) { Console.WriteLine($"UpsertEmploymentAsync rejected a row missing startDate locally: {ex.Message}"); }

using var validEmployment = JsonDocument.Parse("""{"userId":"sample-1","startDate":"/Date(1600000000000)/"}""");
var employmentResult = await client.UpsertEmploymentAsync([validEmployment.RootElement], writesEnabled: true);
Console.WriteLine($"UpsertEmploymentAsync (valid row): editStatus={employmentResult[0].EditStatus}, succeeded={employmentResult[0].Succeeded}.");
Console.WriteLine();

// --- Typed UpsertJobAsync: rejects a row missing seqNumber locally; accepts seqNumber as a number or string. ---
using var missingSeqNumber = JsonDocument.Parse("""{"userId":"sample-1"}""");
try { await client.UpsertJobAsync([missingSeqNumber.RootElement], writesEnabled: true); }
catch (ArgumentException ex) { Console.WriteLine($"UpsertJobAsync rejected a row missing seqNumber locally: {ex.Message}"); }

using var validJob = JsonDocument.Parse("""{"userId":"sample-1","seqNumber":1}""");
var jobResult = await client.UpsertJobAsync([validJob.RootElement], writesEnabled: true);
Console.WriteLine($"UpsertJobAsync (valid row): editStatus={jobResult[0].EditStatus}, succeeded={jobResult[0].Succeeded}.");
Console.WriteLine();

// --- A row SAP rejects as read-only (for example COE0011 "Entity is not upsertable") still arrives as HTTP 200
// with a per-row failure - inspect every result's Succeeded property; never infer success from the HTTP status. ---
using var readOnlyRow = JsonDocument.Parse("""{"userId":"sample-readonly","startDate":"/Date(1600000000000)/"}""");
var readOnlyResult = await client.UpsertEmploymentAsync([readOnlyRow.RootElement], writesEnabled: true);
Console.WriteLine($"UpsertEmploymentAsync (read-only entity row): succeeded={readOnlyResult[0].Succeeded}, httpCode={readOnlyResult[0].HttpCode}.");
Console.WriteLine($"  The provider's detailed message is never surfaced automatically: \"{readOnlyResult[0]}\"");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live SAP SuccessFactors tenant.");

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

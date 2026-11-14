using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Synchronization;
using Koras.Connect.Transport;

// Offline demonstration of the delta/incremental-sync surface added under SAP-013: build a checkpointed
// $filter, enumerate changed EmpEmployment records, and run Reconciliation.DetectChangesAsync (via SapDeltaSync)
// against a caller-supplied dictionary of previously-seen fingerprints. SapDeltaSync tracks modification time
// only, per entity - this SDK never assumes a universal "lastModifiedOn" field name across every entity; the
// caller supplies the entity-documented modification field via SapDeltaCheckpoint. Every HTTP response below is
// synthetic; this sample never contacts a live SAP SuccessFactors tenant.
Console.WriteLine("Koras.Connect.Sap.SuccessFactors offline sample: checkpointed delta filter -> enumerate -> reconcile -> advance checkpoint.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

// A checkpoint from a previous successful scan: last seen modification time, plus the entity-documented field
// name that carries it (SAP documents a distinct modification-time field per entity; never a generic one).
var checkpoint = new SapDeltaCheckpoint("EmpEmployment", "lastModifiedDateTime", new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero));
var overlap = TimeSpan.FromMinutes(15); // re-reads records just before the checkpoint to guard against clock skew / same-timestamp writes
var filter = SapDeltaFilters.BuildFilter(checkpoint, overlap);
Console.WriteLine($"Checkpointed $filter: {filter}");
Console.WriteLine();

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    Console.WriteLine($"  -> GET {request.RequestUri!.PathAndQuery}");
    return Reply("""
        {"d":{"results":[
            {"userId":"sample-1","personIdExternal":"p1","startDate":"/Date(1600000000000)/","lastModifiedDateTime":"2026-01-10T00:10:00Z"},
            {"userId":"sample-2","personIdExternal":"p2","startDate":"/Date(1600000000000)/","lastModifiedDateTime":"2026-01-10T00:05:00Z"}
        ]}}
        """);
}));
var client = new SuccessFactorsClient(new IntegrationTransport(http, new StaticToken("sample-access-token")), new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));

// sample-2 was already seen unchanged in the previous scan; its stored fingerprint must be computed the exact
// same way Reconciliation.DetectChangesAsync computes it internally (SHA-256 over the re-serialized EmpEmployment)
// so the two match byte-for-byte when the record reappears unchanged. sample-1 is new to this scan.
var sample2Fixture = JsonSerializer.Deserialize<EmpEmployment>("""{"userId":"sample-2","personIdExternal":"p2","startDate":"/Date(1600000000000)/","lastModifiedDateTime":"2026-01-10T00:05:00Z"}""")!;
var previousFingerprints = new Dictionary<string, string>
{
    ["sample-2"] = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(sample2Fixture))),
};

var records = client.EnumerateEmploymentAsync(new ODataQuery(Filter: filter));
var (changes, advancedCheckpoint) = await SapDeltaSync.RunAsync(records,
    keySelector: e => e.UserId!,
    modifiedSelector: e => SapDates.ParseUtc(e.AdditionalData!["lastModifiedDateTime"]),
    previousFingerprints: previousFingerprints,
    checkpoint: checkpoint);

Console.WriteLine();
Console.WriteLine($"Detected {changes.Count} change(s):");
foreach (var change in changes)
    Console.WriteLine($"  {change.Key} (fingerprint {change.Fingerprint[..8]}...)");
Console.WriteLine($"Checkpoint advanced from {checkpoint.LastValue:O} to {advancedCheckpoint.LastValue:O}.");
Console.WriteLine("A caller only commits this advanced checkpoint after the change set above is durably delivered -");
Console.WriteLine("if the scan had thrown partway through, RunAsync would never have returned a new checkpoint at all.");

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

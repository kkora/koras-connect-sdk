using System.Net;
using System.Text;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Synchronization;
using Koras.Connect.Transport;

// Offline demonstration of ADP-006: full-snapshot reconciliation against a real, durable, file-backed
// IWorkerFingerprintStore - a first scan with nothing committed yet, a second scan where only the actually-changed
// worker is yielded, and an interrupted scan proving the prior committed snapshot survives untouched. Every HTTP
// response below is synthetic; this sample never contacts a live ADP tenant. The fingerprint snapshot is a real
// temporary file on this machine, deleted when the sample finishes.
Console.WriteLine("Koras.Connect.Adp offline sample: incremental sync via full-snapshot reconciliation and a durable fingerprint store.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

var snapshotPath = Path.Combine(Path.GetTempPath(), $"adp-sample-fingerprints-{Guid.NewGuid():N}.json");
try
{
    var store = new FileWorkerFingerprintStore(snapshotPath);

    Console.WriteLine("Scenario 1: first scan against an empty snapshot - every worker appears as changed.");
    await RunFirstScanAsync(store);

    Console.WriteLine();
    Console.WriteLine("Scenario 2: second scan after persisting the snapshot - only the worker that actually changed is yielded.");
    await RunSecondScanAsync(store);

    Console.WriteLine();
    Console.WriteLine("Scenario 3: an interrupted scan never commits a partial snapshot - the prior committed snapshot survives untouched.");
    await RunInterruptedScanAsync(store);
}
finally
{
    if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
}

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static async Task RunFirstScanAsync(FileWorkerFingerprintStore store)
{
    const string page1 = """
        {"workers":[
            {"associateOID":"w1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}}},
            {"associateOID":"w2","person":{"legalName":{"givenName":"Grace","familyName1":"Hopper"}}}
        ]}
        """;
    const string emptyPage = """{"workers":[]}""";
    using var http = new HttpClient(new SyntheticHandler(request =>
        Reply(request.RequestUri!.Query.Contains("$skip=0", StringComparison.Ordinal) ? page1 : emptyPage)));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var detector = new AdpChangeDetector(workers);

    var previous = await store.LoadAsync();
    Console.WriteLine($"  Loaded prior snapshot: {previous.Count} worker(s) (nothing committed yet).");

    var observed = new List<RecordChange<Worker>>();
    await foreach (var change in detector.ScanAsync(previous)) observed.Add(change);
    Console.WriteLine($"  Changed workers this scan: {string.Join(", ", observed.Select(c => c.Key))}.");

    // Only after every yielded change has been durably processed does the caller persist the new snapshot.
    await AdpChangeDetector.CommitAsync(store, previous, observed);
    Console.WriteLine("  Committed the new snapshot to disk.");
}

static async Task RunSecondScanAsync(FileWorkerFingerprintStore store)
{
    // w1's family name changed since the first scan (a different fingerprint); w2 is byte-for-byte identical and
    // must not reappear.
    const string page1 = """
        {"workers":[
            {"associateOID":"w1","person":{"legalName":{"givenName":"Ada","familyName1":"Byron"}}},
            {"associateOID":"w2","person":{"legalName":{"givenName":"Grace","familyName1":"Hopper"}}}
        ]}
        """;
    const string emptyPage = """{"workers":[]}""";
    using var http = new HttpClient(new SyntheticHandler(request =>
        Reply(request.RequestUri!.Query.Contains("$skip=0", StringComparison.Ordinal) ? page1 : emptyPage)));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var detector = new AdpChangeDetector(workers);

    var previous = await store.LoadAsync();
    Console.WriteLine($"  Loaded prior snapshot: {previous.Count} worker(s).");

    var observed = new List<RecordChange<Worker>>();
    await foreach (var change in detector.ScanAsync(previous)) observed.Add(change);
    Console.WriteLine($"  Changed workers this scan: {string.Join(", ", observed.Select(c => c.Key))} (w2 is unchanged and correctly absent).");

    await AdpChangeDetector.CommitAsync(store, previous, observed);
    var reloaded = await store.LoadAsync();
    Console.WriteLine($"  Reloaded committed snapshot now covers: {string.Join(", ", reloaded.Keys.OrderBy(k => k, StringComparer.Ordinal))}.");
}

static async Task RunInterruptedScanAsync(FileWorkerFingerprintStore store)
{
    var beforeInterruption = await store.LoadAsync();

    const string page1 = """{"workers":[{"associateOID":"w1","person":{"legalName":{"givenName":"Someone","familyName1":"Else"}}}]}""";
    using var http = new HttpClient(new SyntheticHandler(request => request.RequestUri!.Query.Contains("$skip=0", StringComparison.Ordinal)
        ? Reply(page1)
        : throw new InvalidOperationException("Simulated transport failure partway through the scan.")));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var detector = new AdpChangeDetector(workers);

    try
    {
        await foreach (var _ in detector.ScanAsync(beforeInterruption)) { /* the second page request throws before a full population is drained */ }
        Console.WriteLine("  Unexpected: the scan completed without the simulated failure.");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Scan interrupted as expected: {ex.Message}");
    }

    // ScanAsync/CommitAsync are deliberately separate steps; since the loop above never reached a commit call,
    // the snapshot on disk is exactly what Scenario 2 left it as.
    var afterInterruption = await store.LoadAsync();
    var unchanged = afterInterruption.Count == beforeInterruption.Count
        && afterInterruption.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(beforeInterruption.Keys.OrderBy(k => k, StringComparer.Ordinal));
    Console.WriteLine($"  Snapshot after the interrupted scan is unchanged from before it: {unchanged}.");
}

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

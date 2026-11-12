using System.Net;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-006: notification-triggered re-read plus a periodic full-snapshot reconciliation backstop,
/// composed from <see cref="AdpWorkersClient"/> and the provider-neutral <see cref="Koras.Connect.Synchronization.Reconciliation"/>.</summary>
public sealed class AdpChangeDetectorTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }

    private sealed class InMemoryFingerprintStore : IWorkerFingerprintStore
    {
        public IReadOnlyDictionary<string, string> Snapshot { get; private set; } = new Dictionary<string, string>();
        public Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Snapshot);
        public Task CommitAsync(IReadOnlyDictionary<string, string> fingerprints, CancellationToken cancellationToken = default)
        { Snapshot = fingerprints; return Task.CompletedTask; }
    }

    private static AdpWorkersClient WorkersClient(Func<HttpRequestMessage, HttpResponseMessage> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new Token()), new AdpWorkersOptions(new Uri("https://api.test")));

    /// <summary>A fresh client (own call counter) whose single page of workers never repeats across its own
    /// pagination loop, so each call to this factory supports exactly one full, independent scan.</summary>
    private static AdpWorkersClient OnePageWorkersClient(string workersJsonArray)
    {
        var calls = 0;
        return WorkersClient(_ =>
        {
            calls++;
            var body = calls == 1 ? "{\"workers\":" + workersJsonArray + "}" : """{"workers":[]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        });
    }

    [Fact]
    public async Task DuplicateMessageIdSuppressesSecondReReadWithoutIssuingARequest()
    {
        var calls = 0;
        var workers = WorkersClient(_ => { calls++; return new(HttpStatusCode.OK) { Content = new StringContent("""{"workers":[{"associateOID":"w1"}]}""") }; });
        var detector = new AdpChangeDetector(workers);
        var seen = new HashSet<string>();

        var first = await detector.HandleNotificationAsync("msg-1", "w1", seen);
        var second = await detector.HandleNotificationAsync("msg-1", "w1", seen);

        Assert.NotNull(first);
        Assert.Null(second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OutOfOrderNotificationsEachTriggerAnIndependentReReadFromTheSourceOfTruth()
    {
        var calls = 0;
        // Each re-read always reflects whatever the server has right now; the detector never trusts notification order.
        var workers = WorkersClient(_ => { calls++; return new(HttpStatusCode.OK) { Content = new StringContent("""{"workers":[{"associateOID":"w1"}]}""") }; });
        var detector = new AdpChangeDetector(workers);
        var seen = new HashSet<string>();

        // "newer" notification arrives first, "older" one arrives after (out-of-order delivery is possible with ADP notifications).
        var newer = await detector.HandleNotificationAsync("msg-newer", "w1", seen);
        var older = await detector.HandleNotificationAsync("msg-older", "w1", seen);

        Assert.NotNull(newer);
        Assert.NotNull(older);
        Assert.Equal(2, calls); // distinct message ids each trigger their own re-read, regardless of arrival order
    }

    [Fact]
    public async Task FullyDrainedScanCanBeCommittedAndReflectsOnlyChangedWorkers()
    {
        const string workersJson = """[{"associateOID":"w1"},{"associateOID":"w2"}]""";
        var detector = new AdpChangeDetector(OnePageWorkersClient(workersJson));
        var store = new InMemoryFingerprintStore();

        var previous = await store.LoadAsync();
        var observed = new List<Koras.Connect.Synchronization.RecordChange<Worker>>();
        await foreach (var change in detector.ScanAsync(previous)) observed.Add(change);
        Assert.Equal(2, observed.Count); // both workers are new relative to an empty snapshot

        await AdpChangeDetector.CommitAsync(store, previous, observed);
        Assert.Equal(2, store.Snapshot.Count);

        // A second, independent scan against the committed snapshot with identical worker data detects no changes:
        // every worker's fingerprint matches what was just committed.
        var secondScanDetector = new AdpChangeDetector(OnePageWorkersClient(workersJson));
        var secondPass = new List<string>();
        await foreach (var change in secondScanDetector.ScanAsync(store.Snapshot)) secondPass.Add(change.Key);
        Assert.Empty(secondPass);
    }

    [Fact]
    public async Task InterruptedScanNeverPartiallyCommitsAndThePriorSnapshotSurvives()
    {
        var calls = 0;
        var workers = WorkersClient(_ =>
        {
            calls++;
            return calls == 1
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"workers":[{"associateOID":"w1"}]}""") }
                : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var detector = new AdpChangeDetector(workers);
        var store = new InMemoryFingerprintStore();
        // Seed a prior successful snapshot so we can prove it survives the interrupted scan untouched.
        await store.CommitAsync(new Dictionary<string, string> { ["w0"] = "prior-fingerprint" });

        var previous = await store.LoadAsync();
        var observed = new List<Koras.Connect.Synchronization.RecordChange<Worker>>();
        await Assert.ThrowsAsync<AdpApiException>(async () =>
        {
            await foreach (var change in detector.ScanAsync(previous)) observed.Add(change);
            // Only after a full, successful drain would real code call CommitAsync; the scan throws before that point.
            await AdpChangeDetector.CommitAsync(store, previous, observed);
        });

        // The store still holds exactly the prior snapshot: the interrupted scan committed nothing, partial or otherwise.
        Assert.Equal(["w0"], store.Snapshot.Keys);
        Assert.Equal("prior-fingerprint", store.Snapshot["w0"]);
    }
}

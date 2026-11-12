using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Synchronization;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapDeltaSyncTests
{
    private sealed record DeltaRecord(string Id, string Payload, DateTimeOffset ModifiedAt);
    private static string Fingerprint(DeltaRecord record) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
    private static async IAsyncEnumerable<DeltaRecord> From(IEnumerable<DeltaRecord> records)
    { foreach (var r in records) { await Task.Yield(); yield return r; } }

    [Fact] public void BuildFilterCombinesModificationFieldWithOverlapLowerBound()
    {
        var checkpoint = new SapDeltaCheckpoint("EmpJob","lastModifiedDateTime", new DateTimeOffset(2026,1,10,12,0,0,TimeSpan.Zero));
        var filter = SapDeltaFilters.BuildFilter(checkpoint, TimeSpan.FromMinutes(30));
        Assert.Equal("lastModifiedDateTime ge datetime'2026-01-10T11:30:00.000'", filter);
        Assert.Throws<ArgumentOutOfRangeException>(()=>SapDeltaFilters.BuildFilter(checkpoint,TimeSpan.FromMinutes(-1)));
    }

    [Fact] public async Task OverlapWindowDedupsUnchangedRecordsButStillDetectsALateArrivingSameTimestampChange()
    {
        var t0 = new DateTimeOffset(2026,1,10,12,0,0,TimeSpan.Zero);
        var originalR1 = new DeltaRecord("r1","original",t0);
        var unchangedR3 = new DeltaRecord("r3","stable",t0);
        var previous = new Dictionary<string,string>
        {
            ["r1"] = Fingerprint(originalR1),
            ["r3"] = Fingerprint(unchangedR3),
        };
        // Second scan: r1 was edited but SAP reports the same modification timestamp (same-timestamp change);
        // r3 reappears unchanged only because of the overlap window and must be deduped.
        var editedR1 = new DeltaRecord("r1","edited",t0);
        var checkpoint = new SapDeltaCheckpoint("Entity","modifiedAt", t0.AddSeconds(-10));
        var (changes, next) = await SapDeltaSync.RunAsync(From([editedR1, unchangedR3]), r=>r.Id, r=>r.ModifiedAt, previous, checkpoint);
        var change = Assert.Single(changes);
        Assert.Equal("r1", change.Key);
        Assert.Equal("edited", change.Value.Payload);
        Assert.Equal(t0, next.LastValue);
    }

    [Fact] public async Task FailedScanNeverProducesAnAdvancedCheckpoint()
    {
        var originalCheckpoint = new SapDeltaCheckpoint("Entity","modifiedAt", new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SapDeltaSync.RunAsync(
            FailingAfterFirst(), r=>r.Id, r=>r.ModifiedAt, new Dictionary<string,string>(), originalCheckpoint));
        // The only way a caller observes an advanced checkpoint is via RunAsync's returned tuple; since it threw,
        // no such tuple was ever produced and the caller's last committed checkpoint remains the original.
        Assert.Equal(new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero), originalCheckpoint.LastValue);
    }

    private static async IAsyncEnumerable<DeltaRecord> FailingAfterFirst([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new DeltaRecord("r1","first", new DateTimeOffset(2026,1,2,0,0,0,TimeSpan.Zero));
        await Task.Yield();
        throw new InvalidOperationException("synthetic transport failure mid-scan");
    }
}

using Koras.Connect.Adp;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-006: the durable, file-backed <see cref="IWorkerFingerprintStore"/> implementation.</summary>
public sealed class FileWorkerFingerprintStoreTests
{
    private static string TempPath() => Path.Combine(Path.GetTempPath(), "koras-connect-tests", Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public async Task LoadingBeforeAnyCommitReturnsAnEmptySnapshotRatherThanThrowing()
    {
        var store = new FileWorkerFingerprintStore(TempPath());
        var loaded = await store.LoadAsync();
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task CommittedSnapshotRoundTripsThroughLoad()
    {
        var path = TempPath();
        try
        {
            var store = new FileWorkerFingerprintStore(path);
            await store.CommitAsync(new Dictionary<string, string> { ["w1"] = "fp1", ["w2"] = "fp2" });
            var loaded = await store.LoadAsync();
            Assert.Equal(2, loaded.Count);
            Assert.Equal("fp1", loaded["w1"]);
            Assert.Equal("fp2", loaded["w2"]);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task ASecondCommitFullyReplacesTheFirstRatherThanMerging()
    {
        var path = TempPath();
        try
        {
            var store = new FileWorkerFingerprintStore(path);
            await store.CommitAsync(new Dictionary<string, string> { ["w1"] = "fp1" });
            await store.CommitAsync(new Dictionary<string, string> { ["w2"] = "fp2" });
            var loaded = await store.LoadAsync();
            Assert.Single(loaded);
            Assert.False(loaded.ContainsKey("w1"));
            Assert.Equal("fp2", loaded["w2"]);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task CreatesMissingParentDirectoriesOnFirstCommit()
    {
        var path = Path.Combine(Path.GetTempPath(), "koras-connect-tests", Guid.NewGuid().ToString("N"), "nested", "snapshot.json");
        try
        {
            var store = new FileWorkerFingerprintStore(path);
            await store.CommitAsync(new Dictionary<string, string> { ["w1"] = "fp1" });
            Assert.True(File.Exists(path));
        }
        finally { var dir = Path.GetDirectoryName(Path.GetDirectoryName(path)); if (dir is not null && Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public async Task NoTemporaryFileIsLeftBehindAfterASuccessfulCommit()
    {
        var path = TempPath();
        var directory = Path.GetDirectoryName(path)!;
        try
        {
            var store = new FileWorkerFingerprintStore(path);
            await store.CommitAsync(new Dictionary<string, string> { ["w1"] = "fp1" });
            Assert.DoesNotContain(Directory.GetFiles(directory), f => f.EndsWith(".tmp", StringComparison.Ordinal));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task AnAlreadyCanceledCommitLeavesThePriorSnapshotCompletelyUntouched()
    {
        var path = TempPath();
        try
        {
            var store = new FileWorkerFingerprintStore(path);
            await store.CommitAsync(new Dictionary<string, string> { ["w1"] = "fp1" }); // establish a prior snapshot

            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => store.CommitAsync(new Dictionary<string, string> { ["w2"] = "fp2" }, cts.Token));

            var loaded = await store.LoadAsync();
            Assert.Single(loaded);
            Assert.Equal("fp1", loaded["w1"]); // the canceled commit never touched the committed file
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}

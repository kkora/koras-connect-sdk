using System.Text.Json;
using Koras.Connect.Adp.Workers;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-013: cycle-safe reporting-relationship traversal via <see cref="ManagerChainTraversal.WalkUpAsync"/>.</summary>
public sealed class ManagerChainTraversalTests
{
    private static Worker Worker(string oid, string? managerOid, bool hasAssignment = true) => JsonSerializer.Deserialize<Worker>(
        hasAssignment
            ? $"{{\"associateOID\":\"{oid}\",\"workAssignments\":[{{\"itemID\":\"1\"{(managerOid is null ? "" : $",\"reportsTo\":[{{\"workerID\":{{\"idValue\":\"{managerOid}\"}}}}]")}}}]}}"
            : $"{{\"associateOID\":\"{oid}\",\"workAssignments\":[]}}")!;

    private static Func<string, CancellationToken, Task<Worker?>> Lookup(IReadOnlyDictionary<string, Worker> population)
        => (oid, _) => Task.FromResult(population.TryGetValue(oid, out var w) ? w : null);

    [Fact]
    public async Task WalksUpwardUntilAWorkerHasNoReportingRelationship()
    {
        var ceo = Worker("ceo", managerOid: null);
        var director = Worker("director", managerOid: "ceo");
        var manager = Worker("manager", managerOid: "director");
        var population = new[] { ceo, director, manager }.ToDictionary(w => w.AssociateOid!);

        var chain = new List<string>();
        await foreach (var w in ManagerChainTraversal.WalkUpAsync("manager", Lookup(population)))
            chain.Add(w.AssociateOid!);

        Assert.Equal(["manager", "director", "ceo"], chain);
    }

    [Fact]
    public async Task ACyclicalReportingGraphStopsSafelyInsteadOfLoopingForever()
    {
        // Data-entry error: A reports to B, B reports to A.
        var a = Worker("a", managerOid: "b");
        var b = Worker("b", managerOid: "a");
        var population = new[] { a, b }.ToDictionary(w => w.AssociateOid!);

        var chain = new List<string>();
        await foreach (var w in ManagerChainTraversal.WalkUpAsync("a", Lookup(population)))
            chain.Add(w.AssociateOid!);

        Assert.Equal(["a", "b"], chain); // visits each exactly once, never loops back to "a"
    }

    [Fact]
    public async Task AnUnresolvableManagerStopsTheChainWithoutThrowing()
    {
        var worker = Worker("w1", managerOid: "missing-manager");
        var population = new[] { worker }.ToDictionary(w => w.AssociateOid!);

        var chain = new List<string>();
        await foreach (var w in ManagerChainTraversal.WalkUpAsync("w1", Lookup(population)))
            chain.Add(w.AssociateOid!);

        Assert.Equal(["w1"], chain);
    }

    [Fact]
    public async Task AWorkerWithNoWorkAssignmentsYieldsOnlyThemselves()
    {
        var worker = Worker("w1", managerOid: null, hasAssignment: false);
        var population = new[] { worker }.ToDictionary(w => w.AssociateOid!);

        var chain = new List<string>();
        await foreach (var w in ManagerChainTraversal.WalkUpAsync("w1", Lookup(population)))
            chain.Add(w.AssociateOid!);

        Assert.Equal(["w1"], chain);
    }

    [Fact]
    public async Task AnUnresolvableStartingWorkerYieldsAnEmptySequence()
    {
        var chain = new List<string>();
        await foreach (var w in ManagerChainTraversal.WalkUpAsync("ghost", (_, _) => Task.FromResult<Worker?>(null)))
            chain.Add(w.AssociateOid!);

        Assert.Empty(chain);
    }

    [Fact]
    public async Task EmptyStartingIdentifierIsRejectedBeforeAnyResolution()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in ManagerChainTraversal.WalkUpAsync("", (_, _) => throw new InvalidOperationException("Should not resolve."))) { }
        });
    }
}

using Koras.Connect.Abstractions;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-002: the <see cref="IConnector"/>/<see cref="ConnectorDescriptor"/>/<see cref="ConnectionDiagnostic"/>
/// contract exercised using only <c>Koras.Connect.Abstractions</c> &#8212; no ADP/SAP/Qualtrics type is referenced
/// anywhere in this file, proving a host can discover and substitute connectors without coupling to a specific
/// provider implementation.</summary>
public sealed class ConnectorContractTests
{
    /// <summary>A minimal, provider-independent <see cref="IConnector"/> built only from Abstractions types.</summary>
    private sealed class FakeConnector(ConnectorDescriptor descriptor, IReadOnlyList<ConnectionDiagnostic> diagnostics) : IConnector
    {
        public ConnectorDescriptor Descriptor { get; } = descriptor;
        public int DiagnoseCallCount { get; private set; }
        public ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken cancellationToken = default)
        {
            DiagnoseCallCount++;
            return ValueTask.FromResult(diagnostics);
        }
    }

    private static ConnectorDescriptor Descriptor(params (string Operation, EntitlementState State)[] entitlements)
        => new("Fake", "Fake Product", "v1", entitlements.Select(e => e.Operation).ToArray(),
            entitlements.ToDictionary(e => e.Operation, e => new EntitlementEvidence(e.State)));

    /// <summary>Host-side code that only knows about <see cref="IConnector"/> &#8212; the same method runs against
    /// any number of unrelated implementations, which is the actual substitution guarantee this feature requires.</summary>
    private static async Task<bool> AnyDiagnosticFailedAsync(IConnector connector)
    {
        var diagnostics = await connector.DiagnoseAsync();
        return diagnostics.Any(d => d.Outcome == DiagnosticOutcome.Failed);
    }

    [Fact]
    public async Task HostCodeSubstitutesBetweenUnrelatedConnectorImplementationsUsingOnlyAbstractions()
    {
        var healthy = new FakeConnector(Descriptor(("read", EntitlementState.Verified)),
            [new ConnectionDiagnostic(DiagnosticStage.Connectivity, DiagnosticOutcome.Passed, "ok", null, DateTimeOffset.UtcNow)]);
        var broken = new FakeConnector(Descriptor(("read", EntitlementState.Denied)),
            [new ConnectionDiagnostic(DiagnosticStage.Authentication, DiagnosticOutcome.Failed, "auth_failed", "corr-1", DateTimeOffset.UtcNow)]);

        IReadOnlyList<IConnector> connectors = [healthy, broken];
        var results = await Task.WhenAll(connectors.Select(AnyDiagnosticFailedAsync));

        Assert.False(results[0]);
        Assert.True(results[1]);
        Assert.Equal(1, healthy.DiagnoseCallCount);
        Assert.Equal(1, broken.DiagnoseCallCount);
    }

    [Fact]
    public void UnknownIsTheStructuralDefaultEntitlementStateAndIsNeverImpliedBySilence()
    {
        // The enum's zero value is Unknown, so any uninitialized/default EntitlementState is never Verified.
        Assert.Equal(EntitlementState.Unknown, default(EntitlementState));

        var descriptor = Descriptor(("read", EntitlementState.Verified));
        // An operation this descriptor never recorded evidence for is simply absent, not inferred Verified.
        Assert.False(descriptor.Entitlements.ContainsKey("write"));
        Assert.Contains("read", descriptor.SupportedOperations);
    }

    [Fact]
    public void DeniedEntitlementIsNeverTreatedAsVerified()
    {
        var descriptor = Descriptor(("delete", EntitlementState.Denied));
        Assert.Equal(EntitlementState.Denied, descriptor.Entitlements["delete"].State);
        Assert.NotEqual(EntitlementState.Verified, descriptor.Entitlements["delete"].State);
    }

    [Fact]
    public void ConnectorDescriptorIsImmutableUnderWith()
    {
        var original = Descriptor(("read", EntitlementState.Unknown));
        var updated = original with { Entitlements = new Dictionary<string, EntitlementEvidence> { ["read"] = new(EntitlementState.Verified) } };

        Assert.Equal(EntitlementState.Unknown, original.Entitlements["read"].State);
        Assert.Equal(EntitlementState.Verified, updated.Entitlements["read"].State);
        Assert.Equal(original.Provider, updated.Provider);
    }

    [Fact]
    public void ConnectionDiagnosticIsImmutableUnderWith()
    {
        var original = new ConnectionDiagnostic(DiagnosticStage.Configuration, DiagnosticOutcome.Passed, "ok", null, DateTimeOffset.UnixEpoch);
        var updated = original with { Outcome = DiagnosticOutcome.Failed, SanitizedCode = "config_invalid" };

        Assert.Equal(DiagnosticOutcome.Passed, original.Outcome);
        Assert.Equal(DiagnosticOutcome.Failed, updated.Outcome);
        Assert.Equal(original.Stage, updated.Stage);
        Assert.Equal(original.Timestamp, updated.Timestamp);
    }

    [Fact]
    public void EntitlementEvidenceIsImmutableUnderWithAndRequiresAnExplicitState()
    {
        var recorded = new EntitlementEvidence(EntitlementState.Verified, DateTimeOffset.UnixEpoch, "ref-1");
        var revoked = recorded with { State = EntitlementState.Denied };

        Assert.Equal(EntitlementState.Verified, recorded.State);
        Assert.Equal(EntitlementState.Denied, revoked.State);
        Assert.Equal(recorded.RecordedAt, revoked.RecordedAt);
    }
}

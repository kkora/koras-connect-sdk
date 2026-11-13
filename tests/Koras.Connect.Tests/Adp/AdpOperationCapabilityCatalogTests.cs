using Koras.Connect.Adp;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-021: the richer matrix fields (product/version, family, grant/access note, paging/quota, evidence
/// source and date) on <see cref="AdpOperationCapabilityCatalog.Capabilities"/>, closing out the fields the matrix
/// documents beyond the state-machine primitive (<see cref="AdpEntitlementTracker"/>) and the candidate-scope
/// lookup (<see cref="AdpOperationCatalog.CandidateScopes"/>) already covered.</summary>
public sealed class AdpOperationCapabilityCatalogTests
{
    [Fact]
    public void EveryAdpOperationHasExactlyOneCapabilityEntry_MatrixToCatalogConsistency()
    {
        var operations = Enum.GetValues<AdpOperation>();
        foreach (var operation in operations)
            Assert.True(AdpOperationCapabilityCatalog.Capabilities.ContainsKey(operation), $"{operation} is missing a capability entry.");
        Assert.Equal(operations.Length, AdpOperationCapabilityCatalog.Capabilities.Count);

        // Matrix-to-catalog consistency: the capability set must describe exactly the same operations the
        // dispatch catalog actually exposes - neither a stale extra row nor a missing one.
        var dispatched = AdpOperationCatalog.Contracts.Select(c => c.Operation).ToHashSet();
        Assert.Equal(dispatched, AdpOperationCapabilityCatalog.Capabilities.Keys.ToHashSet());
    }

    [Theory]
    [MemberData(nameof(AllOperations))]
    public void EveryCapabilityCarriesNonEmptyProductFamilyAndEvidence(AdpOperation operation)
    {
        var capability = AdpOperationCapabilityCatalog.Capabilities[operation];
        Assert.Equal(operation, capability.Operation);
        Assert.False(string.IsNullOrWhiteSpace(capability.Product));
        Assert.False(string.IsNullOrWhiteSpace(capability.Family));
        Assert.False(string.IsNullOrWhiteSpace(capability.GrantOrAccessNote));
        Assert.False(string.IsNullOrWhiteSpace(capability.EvidenceSource));
        Assert.Equal(new DateOnly(2026, 11, 11), capability.EvidenceDate);
    }

    [Theory]
    [MemberData(nameof(AllOperations))]
    public void NoOperationInThisCatalogDocumentsAPagingOrQuotaBound(AdpOperation operation)
        // The matrix's only documented paging bound (worker list/search's $top 1-100) applies to
        // AdpWorkersClient's read surfaces, not to any operation dispatched through this AdpOperation enum -
        // asserting null here keeps that distinction explicit rather than silently implying a bound exists.
        => Assert.Null(AdpOperationCapabilityCatalog.Capabilities[operation].PagingOrQuota);

    [Theory]
    [InlineData(AdpOperation.Hire, "Lifecycle")]
    [InlineData(AdpOperation.ChangePayDataInput, "Payroll")]
    [InlineData(AdpOperation.TimeOffBalances, "Time")]
    [InlineData(AdpOperation.SpendingAccountEnrollments, "Benefits")]
    [InlineData(AdpOperation.EventNotifications, "Events")]
    public void FamilyGroupingMatchesTheDocumentedOperationCategory(AdpOperation operation, string expectedFamily)
        => Assert.Equal(expectedFamily, AdpOperationCapabilityCatalog.Capabilities[operation].Family);

    public static IEnumerable<object[]> AllOperations() => Enum.GetValues<AdpOperation>().Select(o => new object[] { o });
}

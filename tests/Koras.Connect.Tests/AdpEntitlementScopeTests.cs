using Koras.Connect.Adp;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-021: documented candidate CAR scope metadata on <see cref="AdpOperationCatalog.CandidateScopes"/>.
/// Proves every <see cref="AdpOperation"/> has exactly one entry, that the three operations with an explicitly
/// documented candidate (per docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md section 13) carry the exact cited
/// string, and that every other operation is explicitly <c>null</c> rather than a guessed string.</summary>
public sealed class AdpEntitlementScopeTests
{
    [Fact]
    public void EveryAdpOperationHasExactlyOneCandidateScopeEntry()
    {
        foreach (var operation in Enum.GetValues<AdpOperation>())
            Assert.True(AdpOperationCatalog.CandidateScopes.ContainsKey(operation), $"{operation} is missing a CandidateScopes entry.");
        Assert.Equal(Enum.GetValues<AdpOperation>().Length, AdpOperationCatalog.CandidateScopes.Count);
    }

    [Theory]
    [InlineData(AdpOperation.ModifyWorkAssignment, "/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.modify")]
    [InlineData(AdpOperation.ChangeManager, "/hr/workerInformationManagement/workerManagement/workRelationshipManagement/worker.reportsTo.modify")]
    [InlineData(AdpOperation.ModifyDepartmentAllocations, "/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.assignedOrganizationalUnits.modify")]
    public void OperationsWithADocumentedCandidateCarryTheExactCitedScopeString(AdpOperation operation, string expectedScope)
        => Assert.Equal(expectedScope, AdpOperationCatalog.CandidateScopes[operation]);

    [Theory]
    [InlineData(AdpOperation.Hire)]
    [InlineData(AdpOperation.Rehire)]
    [InlineData(AdpOperation.TerminateAssignment)]
    [InlineData(AdpOperation.ChangePersonalEmail)]
    [InlineData(AdpOperation.ChangePayDataInput)]
    [InlineData(AdpOperation.PayrollInstructions)]
    [InlineData(AdpOperation.PayDistributions)]
    [InlineData(AdpOperation.TimeOffBalances)]
    [InlineData(AdpOperation.TimeOffRequests)]
    [InlineData(AdpOperation.OrganizationDepartments)]
    [InlineData(AdpOperation.EventNotifications)]
    [InlineData(AdpOperation.SpendingAccountEnrollments)]
    public void OperationsWithoutADocumentedCandidateAreExplicitlyNullRatherThanGuessed(AdpOperation operation)
        => Assert.Null(AdpOperationCatalog.CandidateScopes[operation]);
}

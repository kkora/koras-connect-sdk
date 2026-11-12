using System.Text.Json;
using Koras.Connect.Adp.Workers;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-007 golden-fixture tests: missing legal name, multiple assignments, masked/absent identifier.</summary>
public sealed class AdpWorkerMappingTests
{
    private static Worker Parse(string json) => JsonSerializer.Deserialize<Worker>(json)!;

    [Fact]
    public void MissingLegalNameIsOmittedRatherThanGuessed()
    {
        var worker = Parse("""{"associateOID":"w1","workAssignments":[]}""");
        var result = new WorkerIdentityMapper().Map(worker);
        Assert.True(result.IsOmitted);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Contains("legal name", result.OmittedReason);
    }

    [Fact]
    public void MissingGivenOrFamilyNameReportsFieldErrorsInsteadOfDefaulting()
    {
        var worker = Parse("""{"associateOID":"w1","person":{"legalName":{"givenName":"Ada"}},"workAssignments":[]}""");
        var result = new WorkerIdentityMapper().Map(worker);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOmitted);
        Assert.Contains(result.Errors, e => e.Field == "person.legalName.familyName1");
    }

    [Fact]
    public void MaskedOrAbsentIdentifierMapsToNullWithoutFailingTheMapping()
    {
        var worker = Parse("""{"associateOID":"w1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}},"workAssignments":[]}""");
        var result = new WorkerIdentityMapper().Map(worker);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.ExternalId);
        Assert.Equal("w1", result.Value.AssociateOid);
        Assert.Equal("Ada", result.Value.GivenName);
        Assert.Equal("Lovelace", result.Value.FamilyName);
    }

    [Fact]
    public void PresentWorkerIdSurvivesTheMapping()
    {
        var worker = Parse("""{"associateOID":"w1","workerID":{"idValue":"EMP-42"},"person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}},"workAssignments":[]}""");
        var result = new WorkerIdentityMapper().Map(worker);
        Assert.True(result.IsSuccess);
        Assert.Equal("EMP-42", result.Value!.ExternalId);
    }

    [Fact]
    public void MultipleAssignmentsSelectTheOneEffectiveOnTheGivenDate()
    {
        var assignments = new[]
        {
            new WorkAssignment { ItemId = "1", JobTitle = "Engineer I", HireDate = "2020-01-01", TerminationDate = "2022-12-31" },
            new WorkAssignment { ItemId = "2", JobTitle = "Engineer II", HireDate = "2023-01-01" }, // currently open-ended
        };
        var effectiveIn2021 = EffectiveAssignmentSelector.SelectEffective(assignments, new DateOnly(2021, 6, 1));
        var effectiveNow = EffectiveAssignmentSelector.SelectEffective(assignments, new DateOnly(2026, 1, 1));

        Assert.Equal("Engineer I", effectiveIn2021?.JobTitle);
        Assert.Equal("Engineer II", effectiveNow?.JobTitle);
    }

    [Fact]
    public void NoAssignmentCoveringTheDateYieldsNull()
    {
        var assignments = new[] { new WorkAssignment { ItemId = "1", JobTitle = "Engineer I", HireDate = "2023-01-01" } };
        var beforeHire = EffectiveAssignmentSelector.SelectEffective(assignments, new DateOnly(2020, 1, 1));
        Assert.Null(beforeHire);
    }

    [Fact]
    public void MissingOrUnparseableHireDateIsNeverTreatedAsEffective()
    {
        var assignments = new[] { new WorkAssignment { ItemId = "1", JobTitle = "Mystery", HireDate = null } };
        Assert.Null(EffectiveAssignmentSelector.SelectEffective(assignments, new DateOnly(2026, 1, 1)));
    }
}

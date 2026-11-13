using System.Text.Json;
using Koras.Connect.Sap.SuccessFactors;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapMappingTests
{
    private static JsonElement DateElement(string raw) => JsonDocument.Parse($"\"{raw}\"").RootElement.Clone();

    [Fact] public void MissingBothIdentifiersIsOmittedRatherThanMappedWithANullKey()
    {
        var result = new PerPersonMapper().Map(new PerPerson());
        Assert.True(result.IsOmitted);
        Assert.False(result.IsSuccess);
    }

    [Fact] public void MissingDateOfBirthFieldMapsToNullRatherThanThrowing()
    {
        var person = new PerPerson { PersonIdExternal = "synthetic-person" };
        var result = new PerPersonMapper().Map(person);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.DateOfBirth);
    }

    [Fact] public void UnrecognizedDateShapeIsAFieldLevelFailureNotASilentNull()
    {
        var person = new PerPerson { PersonIdExternal = "synthetic-person", DateOfBirth = DateElement("not-a-date") };
        var result = new PerPersonMapper().Map(person);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOmitted);
        Assert.Equal("dateOfBirth", Assert.Single(result.Errors).Field);
    }

    [Theory]
    [InlineData("/Date(946684800000)/")]
    [InlineData("2000-01-01T00:00:00Z")]
    public void BothSupportedSapDateShapesMapToTheSameUtcInstant(string raw)
    {
        var person = new PerPerson { PersonId = "synthetic-person", DateOfBirth = DateElement(raw) };
        var result = new PerPersonMapper().Map(person);
        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTimeOffset(2000,1,1,0,0,0,TimeSpan.Zero), result.Value!.DateOfBirth);
    }

    [Fact] public void ExternalIdentifierIsPreferredOverInternalPersonId()
    {
        var person = new PerPerson { PersonId = "internal", PersonIdExternal = "external" };
        var result = new PerPersonMapper().Map(person);
        Assert.Equal("external", result.Value!.PersonId);
    }

    [Fact] public void EmpEmploymentMapperOmitsARecordWithNoUserIdAtAll()
    {
        var result = new EmpEmploymentMapper().Map(new EmpEmployment { StartDate = DateElement("2026-01-01T00:00:00Z") });
        Assert.True(result.IsOmitted);
    }

    [Fact] public void EmpEmploymentMapperFailsWithAFieldErrorWhenStartDateIsMissingBecauseTheCompositeKeyCannotForm()
    {
        var result = new EmpEmploymentMapper().Map(new EmpEmployment { UserId = "synthetic-1" });
        Assert.False(result.IsSuccess);
        Assert.Equal("startDate", Assert.Single(result.Errors).Field);
    }

    [Fact] public void EmpEmploymentMapperBuildsTheCompositeKeyFromUserIdAndStartDateAndPreservesOpenEndedEndDate()
    {
        var employment = new EmpEmployment { UserId = "synthetic-1", PersonIdExternal = "p1", StartDate = DateElement("2026-01-01T00:00:00Z") };
        var result = new EmpEmploymentMapper().Map(employment);
        Assert.True(result.IsSuccess);
        Assert.Equal("synthetic-1|2026-01-01T00:00:00.0000000+00:00", result.Value!.Key);
        Assert.Null(result.Value.EndDate);
    }

    [Fact] public void EmpJobMapperOmitsARecordWithNoUserIdAtAll()
    {
        var result = new EmpJobMapper().Map(new EmpJob { SequenceNumber = 1 });
        Assert.True(result.IsOmitted);
    }

    [Fact] public void EmpJobMapperFailsWithAFieldErrorWhenSeqNumberIsMissingBecauseTheCompositeKeyCannotForm()
    {
        var result = new EmpJobMapper().Map(new EmpJob { UserId = "synthetic-1" });
        Assert.False(result.IsSuccess);
        Assert.Equal("seqNumber", Assert.Single(result.Errors).Field);
    }

    [Fact] public void EmpJobMapperBuildsTheCompositeKeyFromUserIdAndSeqNumberAndToleratesAnUnparseableStartDate()
    {
        // Unlike EmpEmployment's startDate (part of the key), EmpJob's startDate is not part of the key and an
        // unparseable value is reported as null rather than failing the whole mapping.
        var job = new EmpJob { UserId = "synthetic-1", SequenceNumber = 2, ManagerId = "mgr", JobTitle = "Engineer",
            StartDate = System.Text.Json.JsonDocument.Parse("\"not-a-date\"").RootElement.Clone() };
        var result = new EmpJobMapper().Map(job);
        Assert.True(result.IsSuccess);
        Assert.Equal("synthetic-1|2", result.Value!.Key);
        Assert.Null(result.Value.StartDate);
        Assert.Equal("mgr", result.Value.ManagerId);
    }
}

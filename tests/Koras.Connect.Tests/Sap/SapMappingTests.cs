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
}

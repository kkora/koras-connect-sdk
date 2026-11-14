using System.Text.Json;
using Koras.Connect.Dataverse;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-021: typed models and mappings - a missing/unparsable identifier is <c>Omitted</c> (never mapped
/// with a null/default key), and unknown fields remain inspectable via <see cref="DataverseRecordProjection.Fields"/>.</summary>
public sealed class DataverseMappingTests
{
    private sealed record Account(Guid Id, string? Name);

    private static readonly DataverseRowMapper<Account> Mapper = new("accountid", projection =>
    {
        if (!projection.Fields.TryGetValue("name", out var name) || name.ValueKind != JsonValueKind.String)
            return Koras.Connect.Abstractions.MappingResult<Account>.Failure([new("name", "missing or not a string")]);
        return Koras.Connect.Abstractions.MappingResult<Account>.Success(new Account(projection.Id, name.GetString()));
    });

    [Fact]
    public void RowWithIdAndNameMapsSuccessfully()
    {
        var row = JsonDocument.Parse("""{"accountid":"11111111-1111-1111-1111-111111111111","name":"Contoso","revenue":1000}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsSuccess);
        Assert.Equal("Contoso", result.Value!.Name);
    }

    [Fact]
    public void RowMissingTheIdFieldIsOmittedNotMappedWithADefaultKey()
    {
        var row = JsonDocument.Parse("""{"name":"Contoso"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsOmitted);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RowWithAnUnparsableIdIsOmitted()
    {
        var row = JsonDocument.Parse("""{"accountid":"not-a-guid","name":"Contoso"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsOmitted);
    }

    [Fact]
    public void NonObjectSourceIsOmitted()
    {
        var row = JsonDocument.Parse("""[1,2,3]""").RootElement;
        Assert.True(Mapper.Map(row).IsOmitted);
    }

    [Fact]
    public void FieldLevelFailureIsReturnedWithoutThrowing()
    {
        var row = JsonDocument.Parse("""{"accountid":"11111111-1111-1111-1111-111111111111","revenue":1000}""").RootElement;
        var result = Mapper.Map(row);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOmitted);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void UnknownFieldsArePreservedInTheProjection()
    {
        DataverseRecordProjection? captured = null;
        var mapper = new DataverseRowMapper<Account>("accountid", projection => { captured = projection; return Koras.Connect.Abstractions.MappingResult<Account>.Success(new Account(projection.Id, null)); });
        var row = JsonDocument.Parse("""{"accountid":"11111111-1111-1111-1111-111111111111","custom_field":"keepme"}""").RootElement;
        mapper.Map(row);
        Assert.True(captured!.Fields.ContainsKey("custom_field"));
        Assert.DoesNotContain("accountid", captured.Fields.Keys);
    }
}

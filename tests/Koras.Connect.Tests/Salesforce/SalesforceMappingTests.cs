using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-020: typed CRM/custom object models - a missing/unparsable identifier is <c>Omitted</c> (never
/// mapped with a null/default key), and unknown (including custom <c>__c</c>) fields remain inspectable via
/// <see cref="SalesforceRecordProjection.Fields"/>.</summary>
public sealed class SalesforceMappingTests
{
    private sealed record Account(string Id, string? Name);

    private static readonly SalesforceRecordMapper<Account> Mapper = new(projection =>
    {
        if (!projection.Fields.TryGetValue("Name", out var name) || name.ValueKind != JsonValueKind.String)
            return MappingResult<Account>.Failure([new("Name", "missing or not a string")]);
        return MappingResult<Account>.Success(new Account(projection.Id!, name.GetString()));
    });

    [Fact]
    public void RowWithIdAndNameMapsSuccessfully()
    {
        var row = JsonDocument.Parse("""{"Id":"001gL000003OpvBQAS","Name":"Acme","AnnualRevenue":1000}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsSuccess);
        Assert.Equal("Acme", result.Value!.Name);
    }

    [Fact]
    public void RowMissingTheIdFieldIsOmittedNotMappedWithADefaultKey()
    {
        var row = JsonDocument.Parse("""{"Name":"Acme"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsOmitted);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RowWithABlankIdIsOmitted()
    {
        var row = JsonDocument.Parse("""{"Id":"","Name":"Acme"}""").RootElement;
        Assert.True(Mapper.Map(row).IsOmitted);
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
        var row = JsonDocument.Parse("""{"Id":"001gL000003OpvBQAS","AnnualRevenue":1000}""").RootElement;
        var result = Mapper.Map(row);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOmitted);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void CustomFieldsRoundTripIdenticallyToStandardFields()
    {
        SalesforceRecordProjection? captured = null;
        var mapper = new SalesforceRecordMapper<Account>(projection => { captured = projection; return MappingResult<Account>.Success(new Account(projection.Id!, null)); });
        var row = JsonDocument.Parse("""{"Id":"001gL000003OpvBQAS","Custom_Field__c":"keepme"}""").RootElement;
        mapper.Map(row);
        Assert.True(captured!.Fields.ContainsKey("Custom_Field__c"));
        Assert.Equal("keepme", captured.Fields["Custom_Field__c"].GetString());
    }

    [Fact]
    public void AttributesEnvelopeAndIdAreExcludedFromTheProjectedFields()
    {
        SalesforceRecordProjection? captured = null;
        var mapper = new SalesforceRecordMapper<Account>(projection => { captured = projection; return MappingResult<Account>.Success(new Account(projection.Id!, null)); });
        var row = JsonDocument.Parse("""{"attributes":{"type":"Account","url":"/services/data/v68.0/sobjects/Account/001"},"Id":"001gL000003OpvBQAS","Name":"Acme"}""").RootElement;
        mapper.Map(row);
        Assert.DoesNotContain("attributes", captured!.Fields.Keys);
        Assert.DoesNotContain("Id", captured.Fields.Keys);
    }
}

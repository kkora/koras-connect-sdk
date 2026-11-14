using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-025: typed models and extensibility - a missing/unparsable identifier is <c>Omitted</c> (never
/// mapped with a null/default key), unknown fields (including a v1.0-vs-beta field-set difference) remain
/// inspectable via <see cref="GraphRecordProjection.Fields"/>, and OData annotation properties are excluded.</summary>
public sealed class GraphMappingTests
{
    private sealed record User(string Id, string? DisplayName);

    private static readonly GraphRecordMapper<User> Mapper = new(projection =>
    {
        if (!projection.Fields.TryGetValue("displayName", out var name) || name.ValueKind != JsonValueKind.String)
            return MappingResult<User>.Failure([new("displayName", "missing or not a string")]);
        return MappingResult<User>.Success(new User(projection.Id!, name.GetString()));
    });

    [Fact]
    public void RowWithIdAndDisplayNameMapsSuccessfully()
    {
        var row = JsonDocument.Parse("""{"id":"user-1","displayName":"Ada Lovelace","mail":"ada@example.com"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsSuccess);
        Assert.Equal("Ada Lovelace", result.Value!.DisplayName);
    }

    [Fact]
    public void RowMissingTheIdFieldIsOmittedNotMappedWithADefaultKey()
    {
        var row = JsonDocument.Parse("""{"displayName":"Ada Lovelace"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.True(result.IsOmitted);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void RowWithABlankIdIsOmitted()
    {
        var row = JsonDocument.Parse("""{"id":"","displayName":"Ada Lovelace"}""").RootElement;
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
        var row = JsonDocument.Parse("""{"id":"user-1","mail":"ada@example.com"}""").RootElement;
        var result = Mapper.Map(row);
        Assert.False(result.IsSuccess);
        Assert.False(result.IsOmitted);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void UnknownFieldsRoundTripIdenticallyToDocumentedFields()
    {
        GraphRecordProjection? captured = null;
        var mapper = new GraphRecordMapper<User>(projection => { captured = projection; return MappingResult<User>.Success(new User(projection.Id!, null)); });
        var row = JsonDocument.Parse("""{"id":"user-1","betaOnlyField":"keepme"}""").RootElement;
        mapper.Map(row);
        Assert.True(captured!.Fields.ContainsKey("betaOnlyField"));
        Assert.Equal("keepme", captured.Fields["betaOnlyField"].GetString());
    }

    [Fact]
    public void ODataAnnotationsAndIdAreExcludedFromTheProjectedFields()
    {
        GraphRecordProjection? captured = null;
        var mapper = new GraphRecordMapper<User>(projection => { captured = projection; return MappingResult<User>.Success(new User(projection.Id!, null)); });
        var row = JsonDocument.Parse("""{"@odata.context":"https://graph.microsoft.com/v1.0/$metadata#users/$entity","@odata.etag":"W/\"abc\"","id":"user-1","displayName":"Ada Lovelace"}""").RootElement;
        mapper.Map(row);
        Assert.DoesNotContain("@odata.context", captured!.Fields.Keys);
        Assert.DoesNotContain("@odata.etag", captured.Fields.Keys);
        Assert.DoesNotContain("id", captured.Fields.Keys);
    }

    [Fact]
    public void CustomIdFieldNameIsRespected()
    {
        var mapper = new GraphRecordMapper<User>(
            projection => MappingResult<User>.Success(new User(projection.Id!, null)),
            idField: "userPrincipalName");
        var row = JsonDocument.Parse("""{"userPrincipalName":"ada@example.com","id":"user-1"}""").RootElement;
        var result = mapper.Map(row);
        Assert.True(result.IsSuccess);
        Assert.Equal("ada@example.com", result.Value!.Id);
    }
}

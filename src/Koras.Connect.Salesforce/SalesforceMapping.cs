using System.Text.Json;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Salesforce;

/// <summary>A Salesforce record projected to its id plus every remaining field, untyped. Intermediate shape
/// between a raw <see cref="JsonElement"/> row and a caller-defined target type (SFD-020).</summary>
public sealed record SalesforceRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);

/// <summary>Maps a raw sObject row (<see cref="JsonElement"/>) to a neutral target type, mirroring Dataverse's
/// explicit field-by-field <c>DataverseRowMapper&lt;TTarget&gt;</c> approach rather than reflection-based binding
/// (SFD-020). A row missing or carrying an unparsable identifier field is <see cref="MappingResult{TTarget}.Omitted"/>,
/// never mapped with a null/default key. Unknown fields - standard or custom (<c>__c</c>-suffixed) - are preserved
/// verbatim in <see cref="SalesforceRecordProjection.Fields"/>, never dropped.</summary>
public sealed class SalesforceRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
{
    private readonly string _idField;
    private readonly Func<SalesforceRecordProjection, MappingResult<TTarget>> _project;

    public SalesforceRecordMapper(Func<SalesforceRecordProjection, MappingResult<TTarget>> project, string idField = "Id")
    {
        if (string.IsNullOrWhiteSpace(idField)) throw new ArgumentException("An id field name is required.", nameof(idField));
        _idField = idField;
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public MappingResult<TTarget> Map(JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object) return MappingResult<TTarget>.Omitted("Source is not an sObject row object.");
        if (!source.TryGetProperty(_idField, out var idElement) || idElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idElement.GetString()))
            return MappingResult<TTarget>.Omitted($"Missing or unparsable identifier field '{_idField}'.");
        var fields = source.EnumerateObject().Where(p => p.Name != _idField && p.Name != "attributes").ToDictionary(p => p.Name, p => p.Value.Clone());
        return _project(new SalesforceRecordProjection(idElement.GetString(), fields));
    }
}

using System.Text.Json;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Dataverse;

/// <summary>A Dataverse row projected to its primary id plus every remaining field, untyped. Intermediate shape
/// between a raw <see cref="JsonElement"/> row and a caller-defined target type (DVT-021).</summary>
public sealed record DataverseRecordProjection(Guid Id, IReadOnlyDictionary<string, JsonElement> Fields);

/// <summary>Maps a raw Dataverse row (<see cref="JsonElement"/>) to a neutral target type, mirroring
/// <c>PerPersonMapper</c>'s explicit field-by-field approach rather than reflection-based binding (DVT-021). A row
/// missing or carrying an unparsable identifier field is <see cref="MappingResult{TTarget}.Omitted"/>, never mapped
/// with a null/default key.</summary>
public sealed class DataverseRowMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
{
    private readonly string _idField;
    private readonly Func<DataverseRecordProjection, MappingResult<TTarget>> _project;

    public DataverseRowMapper(string idField, Func<DataverseRecordProjection, MappingResult<TTarget>> project)
    {
        if (string.IsNullOrWhiteSpace(idField)) throw new ArgumentException("An id field name is required.", nameof(idField));
        _idField = idField;
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public MappingResult<TTarget> Map(JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object) return MappingResult<TTarget>.Omitted("Source is not a Dataverse row object.");
        if (!source.TryGetProperty(_idField, out var idElement) || idElement.ValueKind != JsonValueKind.String || !Guid.TryParse(idElement.GetString(), out var id))
            return MappingResult<TTarget>.Omitted($"Missing or unparsable identifier field '{_idField}'.");
        var fields = source.EnumerateObject().Where(p => p.Name != _idField).ToDictionary(p => p.Name, p => p.Value.Clone());
        return _project(new DataverseRecordProjection(id, fields));
    }
}

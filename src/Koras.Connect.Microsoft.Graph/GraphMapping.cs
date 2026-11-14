using System.Text.Json;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Graph resource row projected to its id plus every remaining field, untyped. Intermediate shape
/// between a raw <see cref="JsonElement"/> row and a caller-defined target type (MGR-025), mirroring
/// Dataverse's and Salesforce's explicit field-by-field mapper approach rather than reflection-based binding
/// or a dependency on the official Microsoft Graph SDK.</summary>
public sealed record GraphRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);

/// <summary>Maps a raw Graph resource row (<see cref="JsonElement"/>) to a neutral target type. A row missing
/// or carrying an unparsable identifier field is <see cref="MappingResult{TTarget}.Omitted"/>, never mapped with
/// a null/default key. Unknown fields - including any not present in a given API version's response shape,
/// such as a field added in <c>beta</c> but absent from <c>v1.0</c> - are preserved verbatim in
/// <see cref="GraphRecordProjection.Fields"/>, never dropped. OData annotation properties (keys starting with
/// <c>@odata.</c>, e.g. <c>@odata.context</c>/<c>@odata.etag</c>/<c>@odata.type</c>) are excluded from
/// <see cref="GraphRecordProjection.Fields"/>, mirroring Salesforce's exclusion of its own envelope
/// (<c>attributes</c>) rather than treating response metadata as a resource field.</summary>
public sealed class GraphRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
{
    private readonly string _idField;
    private readonly Func<GraphRecordProjection, MappingResult<TTarget>> _project;

    public GraphRecordMapper(Func<GraphRecordProjection, MappingResult<TTarget>> project, string idField = "id")
    {
        if (string.IsNullOrWhiteSpace(idField)) throw new ArgumentException("An id field name is required.", nameof(idField));
        _idField = idField;
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public MappingResult<TTarget> Map(JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object) return MappingResult<TTarget>.Omitted("Source is not a Graph resource row object.");
        if (!source.TryGetProperty(_idField, out var idElement) || idElement.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(idElement.GetString()))
            return MappingResult<TTarget>.Omitted($"Missing or unparsable identifier field '{_idField}'.");
        var fields = source.EnumerateObject().Where(p => p.Name != _idField && !p.Name.StartsWith("@odata.", StringComparison.Ordinal)).ToDictionary(p => p.Name, p => p.Value.Clone());
        return _project(new GraphRecordProjection(idElement.GetString(), fields));
    }
}

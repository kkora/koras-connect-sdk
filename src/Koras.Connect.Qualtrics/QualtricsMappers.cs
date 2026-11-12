using System.Text.Json;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Qualtrics;

/// <summary>Provider-neutral projection of a Qualtrics survey summary. Unknown Qualtrics fields are preserved in
/// <see cref="Extensions"/> rather than silently dropped.</summary>
public sealed record NeutralSurveyRecord(string Id, string? Name, bool IsActive, DateTimeOffset? LastModified, IReadOnlyDictionary<string, JsonElement> Extensions);

/// <summary>Provider-neutral projection of a Qualtrics XM Directory contact. <see cref="OptedOut"/> intentionally
/// collapses an explicit JSON null and an absent field to the same neutral <c>null</c>; callers that must distinguish
/// those two cases should map from <see cref="QualtricsContact"/> directly rather than through this neutral record.
/// Unknown Qualtrics fields are preserved in <see cref="Extensions"/> rather than silently dropped.</summary>
public sealed record NeutralContactRecord(string Id, string? Email, string? FirstName, string? LastName, bool? OptedOut, IReadOnlyDictionary<string, JsonElement> Extensions);

/// <summary>Maps a <see cref="SurveySummary"/> into a neutral record. Omits the record when the source carries no
/// stable identifier rather than inventing one.</summary>
public sealed class SurveySummaryMapper : IRecordMapper<SurveySummary, NeutralSurveyRecord>
{
    public MappingResult<NeutralSurveyRecord> Map(SurveySummary source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.Id)) return MappingResult<NeutralSurveyRecord>.Omitted("Survey has no identifier.");
        var extensions = source.AdditionalData is null
            ? new Dictionary<string, JsonElement>()
            : new Dictionary<string, JsonElement>(source.AdditionalData);
        return MappingResult<NeutralSurveyRecord>.Success(new NeutralSurveyRecord(source.Id, source.Name, source.IsActive, source.LastModified, extensions));
    }
}

/// <summary>Maps a <see cref="QualtricsContact"/> into a neutral record. Fails with a field-level error when the
/// source carries no stable identifier, since a contact without an ID cannot be safely reconciled downstream.</summary>
public sealed class QualtricsContactMapper : IRecordMapper<QualtricsContact, NeutralContactRecord>
{
    public MappingResult<NeutralContactRecord> Map(QualtricsContact source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (string.IsNullOrWhiteSpace(source.Id))
            return MappingResult<NeutralContactRecord>.Failure([new FieldError("id", "Contact is missing a stable identifier.")]);
        bool? optedOut = source.Unsubscribed.ValueKind is JsonValueKind.True or JsonValueKind.False ? source.Unsubscribed.GetBoolean() : null;
        var extensions = source.AdditionalData is null
            ? new Dictionary<string, JsonElement>()
            : new Dictionary<string, JsonElement>(source.AdditionalData);
        return MappingResult<NeutralContactRecord>.Success(new NeutralContactRecord(source.Id, source.Email, source.FirstName, source.LastName, optedOut, extensions));
    }
}

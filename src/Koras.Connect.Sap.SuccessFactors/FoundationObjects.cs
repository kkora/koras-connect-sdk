using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Sap.SuccessFactors;

// SAP-010 scaffolding only. The entity-set names below (FODepartment, FODivision, FOLocation) match SAP's
// commonly documented Employee Central foundation-object naming convention, but have NOT been verified against a
// live tenant's $metadata. Treat these types as a best-effort starting point - confirm the actual entity-set name
// and field shapes via GetMetadataAsync()/ParseEntityDescriptors before relying on them in production, and adjust
// the entity-set string passed to EnumerateFoundationObjectAsync if the tenant uses a different name or version.

/// <summary>Best-effort Employee Central foundation-object projection for a department code: an effective-dated
/// record with an optional parent reference and a retired/active status. Field names are not verified against a
/// live tenant - see the remarks at the top of this file.</summary>
public sealed class FODepartment
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("parentExternalCode")] public string? ParentExternalCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Best-effort Employee Central foundation-object projection for a division code. See the remarks at the
/// top of this file - the entity-set name is not verified against a live tenant.</summary>
public sealed class FODivision
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Best-effort Employee Central foundation-object projection for a location code. See the remarks at the
/// top of this file - the entity-set name is not verified against a live tenant.</summary>
public sealed class FOLocation
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class SuccessFactorsClient
{
    /// <summary>Generic foundation-object enumeration. <paramref name="entitySet"/> is caller-supplied and is NOT
    /// validated against a confirmed foundation-object contract - see the remarks at the top of
    /// <c>FoundationObjects.cs</c>. This is a thin wrapper over <see cref="EnumerateAsync{T}"/>; it exists only to
    /// give foundation-object reads a documented, named entry point distinct from person/employment/job data.</summary>
    public IAsyncEnumerable<T> EnumerateFoundationObjectAsync<T>(string entitySet, ODataQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<T>(entitySet, query, cancellationToken);
}

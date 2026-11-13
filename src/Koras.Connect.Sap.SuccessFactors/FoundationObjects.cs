using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Sap.SuccessFactors;

// SAP-010: the entity-set names below (FODepartment, FODivision, FOLocation) and their externalCode/
// effectiveStartDate business keys are confirmed against the official "SAP SuccessFactors API Reference Guide"
// (2H 2026 edition) - FODepartment and FODivision are listed as MDF Foundation Object entities in section 6.4.2
// "MDF Foundation Object Entities" (externalCode + effectiveStartDate, and + country for FODivision); FOLocation
// is confirmed in the "Snapshot-Based Pagination in Employee Central OData API" entity list (section 6.3.5.3.2.2)
// and the full API reference's business-key table (section 6.7, "externalCode + effectiveStartDate"). This
// resolves the functional requirement "select actual organization entity sets from metadata" for the entity-set
// names and these two core key fields specifically. What remains genuinely unverified against a live tenant:
// the complete field list beyond the confirmed key fields (RBP/visibility may hide fields this DTO does not
// model, surfaced only via AdditionalData), and whether a given tenant has actually enabled/customized these
// entities. Confirm via GetMetadataAsync()/ParseEntityDescriptors before relying on any field not listed below.

/// <summary>Employee Central foundation-object projection for a department code: an effective-dated record with
/// an optional parent reference and a retired/active status. Entity-set name and the externalCode+effectiveStartDate
/// business key are confirmed against the official API Reference Guide - see the remarks at the top of this file
/// for exactly what remains unverified.</summary>
public sealed class FODepartment
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("parentExternalCode")] public string? ParentExternalCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Employee Central foundation-object projection for a division code. Entity-set name and business key
/// (externalCode + effectiveStartDate + country) are confirmed against the official API Reference Guide - see the
/// remarks at the top of this file.</summary>
public sealed class FODivision
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Employee Central foundation-object projection for a location code. Entity-set name and business key
/// (externalCode + effectiveStartDate) are confirmed against the official API Reference Guide - see the remarks
/// at the top of this file.</summary>
public sealed class FOLocation
{
    [JsonPropertyName("externalCode")] public string? ExternalCode { get; set; }
    [JsonPropertyName("effectiveStartDate")] public JsonElement? EffectiveStartDate { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>One externalCode's parent-reference edge, resolved by <see cref="SuccessFactorsClient.ResolveDepartmentParentChainAsync"/>.</summary>
public sealed record FoundationObjectParentLink(string ExternalCode, string? ParentExternalCode);

public sealed partial class SuccessFactorsClient
{
    /// <summary>Generic foundation-object enumeration. <paramref name="entitySet"/> is caller-supplied; only
    /// FODepartment/FODivision/FOLocation have a confirmed entity-set name (see the remarks at the top of
    /// <c>FoundationObjects.cs</c>) - any other value is NOT validated against a confirmed foundation-object
    /// contract. This is a thin wrapper over <see cref="EnumerateAsync{T}"/>; it exists only to give
    /// foundation-object reads a documented, named entry point distinct from person/employment/job data.</summary>
    public IAsyncEnumerable<T> EnumerateFoundationObjectAsync<T>(string entitySet, ODataQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<T>(entitySet, query, cancellationToken);

    /// <summary>Walks the FODepartment.ParentExternalCode chain for a department, up to <paramref name="maxDepth"/>
    /// hops, using the same visited-set cycle guard already used for manager-chain traversal
    /// (<see cref="ResolveManagerChainAsync"/>) and OData pagination. A revisited externalCode - including an
    /// immediate self-reference - throws <see cref="InvalidDataException"/>. A department with multiple
    /// effective-dated versions reporting different parents for the same externalCode is ambiguous without a
    /// documented "current version" selection rule for this entity (unlike EmpJob's seqNumber tie-break); this
    /// method takes the first matching record returned and does not attempt to resolve that ambiguity itself.</summary>
    public async Task<IReadOnlyList<FoundationObjectParentLink>> ResolveDepartmentParentChainAsync(string externalCode, int maxDepth = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalCode)) throw new ArgumentException("An externalCode is required.", nameof(externalCode));
        if (maxDepth is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maxDepth));
        var chain = new List<FoundationObjectParentLink>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = externalCode;
        for (var depth = 0; depth < maxDepth; depth++)
        {
            if (!visited.Add(current)) throw new InvalidDataException("Department parent chain cycle (including self-reference) detected.");
            var filter = $"externalCode eq '{current.Replace("'", "''", StringComparison.Ordinal)}'";
            FODepartment? found = null;
            await foreach (var department in EnumerateFoundationObjectAsync<FODepartment>("FODepartment", new(Filter: filter), cancellationToken))
            { found = department; break; }
            if (found is null) break;
            chain.Add(new FoundationObjectParentLink(current, found.ParentExternalCode));
            if (string.IsNullOrWhiteSpace(found.ParentExternalCode)) break;
            current = found.ParentExternalCode;
        }
        return chain;
    }
}

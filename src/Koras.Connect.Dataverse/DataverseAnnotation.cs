using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Dataverse;

/// <summary>The legacy <c>annotation</c> (Note) entity's attached-file fields (DVT-017), distinct from DVT-016's
/// native file-column mechanism. <see cref="DocumentBodyBase64"/> is the raw Base64 string Dataverse returns/expects
/// - the SDK never auto-decodes it into a <see cref="Stream"/>, so the caller controls when that (potentially large)
/// conversion happens. Field names (<c>documentbody</c>/<c>filename</c>/<c>mimetype</c>/<c>isdocument</c>) are
/// corroborated by vendor-research search but not independently re-verified via a full page fetch or live metadata
/// query this session - a documented gap, not silently resolved; re-verify before this feature ships as fully
/// `verified`. Reuses DVT-004's generic CRUD against the <c>annotations</c> entity set - no dedicated client method
/// exists or is needed for this entity.</summary>
public sealed class DataverseAnnotation
{
    [JsonPropertyName("filename")] public string? FileName { get; set; }
    [JsonPropertyName("mimetype")] public string? MimeType { get; set; }
    [JsonPropertyName("documentbody")] public string? DocumentBodyBase64 { get; set; }
    [JsonPropertyName("isdocument")] public bool? IsDocument { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

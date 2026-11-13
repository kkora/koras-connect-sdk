using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>WFN "code table" value shape reused across several typed response models. Unknown vendor fields are
/// preserved for forward compatibility, matching the pattern used by <see cref="Worker"/>. Schema source: the
/// recurring <c>codeValue</c>/<c>shortName</c> pair documented in S1 (worker guide) and confirmed against the S2
/// sample payloads referenced in <c>docs/features/ADP-FEATURE-MATRIX.md</c>; this shape is not independently
/// re-verified per consuming operation, since the matrix describes it as a shared convention rather than a
/// per-endpoint schema.</summary>
public sealed class CodeValue
{
    [JsonPropertyName("codeValue")] public string? Code { get; set; }
    [JsonPropertyName("shortName")] public string? ShortName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>WFN quantity-with-unit shape (used by time-off balances, among others). Schema source: time-off
/// balances row (S2) in <c>docs/features/ADP-FEATURE-MATRIX.md</c>.</summary>
public sealed class QuantityValue
{
    [JsonPropertyName("quantityValue")] public double? Value { get; set; }
    [JsonPropertyName("unitCode")] public CodeValue? UnitCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

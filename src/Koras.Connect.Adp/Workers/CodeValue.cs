using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>WFN "code table" value shape reused across several typed response models. Unknown vendor fields are
/// preserved for forward compatibility, matching the pattern used by <see cref="Worker"/>.</summary>
public sealed class CodeValue
{
    [JsonPropertyName("codeValue")] public string? Code { get; set; }
    [JsonPropertyName("shortName")] public string? ShortName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>WFN quantity-with-unit shape (used by time-off balances, among others).</summary>
public sealed class QuantityValue
{
    [JsonPropertyName("quantityValue")] public double? Value { get; set; }
    [JsonPropertyName("unitCode")] public CodeValue? UnitCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

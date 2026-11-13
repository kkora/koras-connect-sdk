using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>Typed view of one entry in the existing <c>GET /benefits/v1/associates/{worker}/spending-account-enrollments</c>
/// response (<see cref="AdpOperation.SpendingAccountEnrollments"/>, dispatched by
/// <see cref="AdpOperationsClient.ReadSpendingAccountEnrollmentsAsync"/>). No new endpoint is introduced; coverage
/// detail is preserved as a raw <see cref="JsonElement"/> because its shape varies by plan type, while unknown
/// top-level vendor fields are preserved via <c>[JsonExtensionData]</c> as elsewhere in this package. Schema
/// source: "Spending-account enrollments" row (S5, the dedicated HROpen benefit spending-account guide) in
/// <c>docs/features/ADP-FEATURE-MATRIX.md</c> — only the two top-level fields S5 documents consistently across plan
/// types are modeled; everything plan-specific stays in <see cref="AccountBasedCoverage"/>.</summary>
public sealed class SpendingAccountEnrollment
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("accountBasedCoverage")] public JsonElement? AccountBasedCoverage { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed class SpendingAccountEnrollmentsResponse
{
    [JsonPropertyName("spendingAccountEnrollments")] public List<SpendingAccountEnrollment> Enrollments { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>Typed view of one entry in the existing <c>GET /hr/v1/organization-departments</c> response
/// (<see cref="AdpOperation.OrganizationDepartments"/>). No new endpoint is introduced; this is a typed
/// deserialization target for the documented response already dispatched by <see cref="AdpOperationsClient"/>.
/// Unknown vendor fields are preserved, matching the pattern used by <see cref="Worker"/>.</summary>
public sealed class OrganizationDepartment
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("nameCode")] public CodeValue? NameCode { get; set; }
    [JsonPropertyName("effectiveDate")] public string? EffectiveDate { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Typed envelope for the full organization-departments response.</summary>
public sealed class OrganizationDepartmentsResponse
{
    [JsonPropertyName("organizationDepartments")] public List<OrganizationDepartment> Departments { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

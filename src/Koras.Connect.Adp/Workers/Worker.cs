using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>WFN worker fields; unknown vendor fields are preserved for forward compatibility.</summary>
public sealed class Worker
{
    [JsonPropertyName("associateOID")] public string? AssociateOid { get; set; }
    [JsonPropertyName("workerID")] public Identifier? WorkerId { get; set; }
    [JsonPropertyName("person")] public Person? Person { get; set; }
    [JsonPropertyName("workAssignments")] public List<WorkAssignment> WorkAssignments { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class Identifier
{
    [JsonPropertyName("idValue")] public string? Value { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class Person
{
    [JsonPropertyName("legalName")] public LegalName? LegalName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class LegalName
{
    [JsonPropertyName("givenName")] public string? GivenName { get; set; }
    [JsonPropertyName("familyName1")] public string? FamilyName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class WorkAssignment
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("jobTitle")] public string? JobTitle { get; set; }
    [JsonPropertyName("hireDate")] public string? HireDate { get; set; }
    [JsonPropertyName("terminationDate")] public string? TerminationDate { get; set; }
    [JsonPropertyName("assignedOrganizationalUnits")] public List<JsonElement> OrganizationalUnits { get; set; } = [];
    [JsonPropertyName("reportsTo")] public List<JsonElement> ReportsTo { get; set; } = [];
    [JsonPropertyName("homeWorkLocation")] public JsonElement? HomeWorkLocation { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>Typed view of one entry in a work assignment's documented "reportsTo" collection (the same shape used
/// both when reading <see cref="WorkAssignment.ReportsTo"/> and when composing a
/// <see cref="AdpOperation.ChangeManager"/> event payload). No new endpoint is introduced; this is a typed
/// deserialization target for data already returned or submitted by existing, documented operations. Unknown
/// vendor fields are preserved, matching the pattern used by <see cref="Worker"/>.</summary>
public sealed class ManagerRelationship
{
    [JsonPropertyName("workerID")] public Identifier? WorkerId { get; set; }
    [JsonPropertyName("reportsToWorkerName")] public string? ReportsToWorkerName { get; set; }
    [JsonPropertyName("reportingRelationshipCode")] public CodeValue? ReportingRelationshipCode { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Typed deserialization helpers for the raw <see cref="JsonElement"/> collections already present on
/// <see cref="WorkAssignment"/>. Added as extension points rather than changing the existing property types, so
/// callers who depend on the raw shape are unaffected.</summary>
public static class WorkAssignmentTypedExtensions
{
    public static IReadOnlyList<ManagerRelationship> GetManagerRelationships(this WorkAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        return assignment.ReportsTo.Select(e => e.Deserialize<ManagerRelationship>() ?? throw new JsonException("Null reportsTo entry.")).ToArray();
    }
}

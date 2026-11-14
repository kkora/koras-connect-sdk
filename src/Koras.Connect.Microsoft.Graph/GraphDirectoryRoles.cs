using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Directory role assignment (MGR-006).</summary>
public sealed class GraphDirectoryRole
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates <c>GET /directoryRoles</c> (MGR-006/019).</summary>
    public IAsyncEnumerable<GraphDirectoryRole> EnumerateDirectoryRolesAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphDirectoryRole>("directoryRoles", cancellationToken: cancellationToken);

    /// <summary>Enumerates administrative units via the nested <c>GET /directory/administrativeUnits</c> (MGR-006)
    /// - not the flat top-level <c>/administrativeUnits</c> path, a documentation correction independently
    /// confirmed in the Graph research pass.</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateAdministrativeUnitsAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<JsonDocument>("directory/administrativeUnits", cancellationToken: cancellationToken);
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A team summary as returned by the tenant-wide <c>GET /teams</c> list call (MGR-015). This list call
/// returns only these four fields - every other property on the underlying resource comes back <c>null</c>
/// rather than omitted, regardless of permission level; richer per-team detail requires
/// <see cref="GraphClient.GetTeamAsync"/>.</summary>
public sealed record GraphTeamSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("visibility")] string? Visibility);

public sealed partial class GraphClient
{
    /// <summary>Enumerates every team in the tenant via <c>GET /teams</c> (MGR-015/019). Only <c>id</c>,
    /// <c>displayName</c>, <c>description</c> and <c>visibility</c> are populated on this list call - every other
    /// field on the underlying resource comes back <c>null</c>, not a parsing gap. Use <see cref="GetTeamAsync"/>
    /// for full per-team detail. Supported for application permissions; not supported for personal Microsoft
    /// accounts.</summary>
    public IAsyncEnumerable<GraphTeamSummary> EnumerateTeamsAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphTeamSummary>("teams", cancellationToken: cancellationToken);

    /// <summary>Reads full detail for a single team via <c>GET /teams/{teamId}</c> (MGR-015) - the documented
    /// redirect for callers needing fields the tenant-wide list call always returns <c>null</c>.</summary>
    public Task<JsonDocument> GetTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("A team id is required.", nameof(teamId));
        return ReadResourceAsync<JsonDocument>(new Uri(_root, $"teams/{Uri.EscapeDataString(teamId)}"), cancellationToken);
    }

    /// <summary>Enumerates a team's channels via <c>GET /teams/{teamId}/channels</c> (MGR-015/019).</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateChannelsAsync(string teamId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("A team id is required.", nameof(teamId));
        return EnumerateAsync<JsonDocument>($"teams/{Uri.EscapeDataString(teamId)}/channels", cancellationToken: cancellationToken);
    }
}

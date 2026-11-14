using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A granted OAuth2 permission (MGR-024). <see cref="Scope"/> is a space-delimited string, documented
/// with a confirmed exact maximum of 3,850 characters - not independently enforced here, since this is a read
/// path parsing whatever Graph actually returns, not a value this SDK composes.</summary>
public sealed record GraphPermissionGrant(
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("consentType")] string ConsentType,
    [property: JsonPropertyName("principalId")] string? PrincipalId,
    [property: JsonPropertyName("resourceId")] string ResourceId,
    [property: JsonPropertyName("scope")] string Scope);

public sealed partial class GraphClient
{
    /// <summary>Lists every <c>oAuth2PermissionGrant</c> in the tenant via <c>GET /oauth2PermissionGrants</c>
    /// (MGR-024/019) - the tenant-wide view of which app registrations hold which delegated scopes, requiring
    /// <c>Directory.Read.All</c>.</summary>
    public IAsyncEnumerable<GraphPermissionGrant> ListGrantsForCurrentAppAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphPermissionGrant>("oauth2PermissionGrants", cancellationToken: cancellationToken);

    /// <summary>Lists the signed-in user's own grants via <c>GET /me/oauth2PermissionGrants</c> (MGR-024/019).</summary>
    public IAsyncEnumerable<GraphPermissionGrant> ListMyGrantsAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphPermissionGrant>("me/oauth2PermissionGrants", cancellationToken: cancellationToken);

    /// <summary>Builds the documented admin-consent redirect URL (MGR-024) - a pure, local URL composition, no
    /// HTTP call. <paramref name="tenant"/> must be a GUID, a verified domain, or the literal <c>organizations</c>;
    /// <c>common</c> is explicitly documented as never valid for this specific endpoint and is rejected locally.</summary>
    public static Uri BuildAdminConsentUrl(string tenant, string clientId, IReadOnlyList<string> scopes, Uri redirectUri, string? state = null)
    {
        if (string.IsNullOrWhiteSpace(tenant)) throw new ArgumentException("A tenant is required.", nameof(tenant));
        if (string.Equals(tenant, "common", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("'common' is never valid for the admin-consent endpoint.", nameof(tenant));
        if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("A client id is required.", nameof(clientId));
        ArgumentNullException.ThrowIfNull(scopes);
        ArgumentNullException.ThrowIfNull(redirectUri);
        var query = $"client_id={Uri.EscapeDataString(clientId)}&scope={Uri.EscapeDataString(string.Join(" ", scopes))}&redirect_uri={Uri.EscapeDataString(redirectUri.AbsoluteUri)}";
        if (!string.IsNullOrWhiteSpace(state)) query += $"&state={Uri.EscapeDataString(state)}";
        return new Uri($"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/v2.0/adminconsent?{query}");
    }
}

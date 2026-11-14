using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A SharePoint site (MGR-011). <see cref="Id"/> is Graph's documented 3-part composite string
/// (<c>{hostname},{site-collection-guid},{site-guid}</c>).</summary>
public sealed record GraphSite(string Id, string? DisplayName, string? WebUrl);

public sealed partial class GraphClient
{
    /// <summary>Resolves the tenant's root site via <c>GET /sites/root</c> (MGR-011).</summary>
    public Task<GraphSite> GetRootSiteAsync(CancellationToken cancellationToken = default)
        => ReadSiteAsync(new Uri(_root, "sites/root"), cancellationToken);

    /// <summary>Resolves a site via <c>GET /sites/{hostname}</c>, or
    /// <c>GET /sites/{hostname}:/{serverRelativePath}</c> when <paramref name="serverRelativePath"/> is supplied
    /// (MGR-011).</summary>
    public Task<GraphSite> GetSiteByPathAsync(string hostname, string? serverRelativePath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hostname)) throw new ArgumentException("A hostname is required.", nameof(hostname));
        var relative = string.IsNullOrWhiteSpace(serverRelativePath)
            ? $"sites/{Uri.EscapeDataString(hostname)}"
            : $"sites/{Uri.EscapeDataString(hostname)}:/{string.Join("/", serverRelativePath.Split('/').Where(s => s.Length > 0).Select(Uri.EscapeDataString))}";
        return ReadSiteAsync(new Uri(_root, relative), cancellationToken);
    }

    /// <summary>Enumerates a site's document libraries, exposed as Drive resources via
    /// <c>GET /sites/{siteId}/drives</c> (MGR-011/009/019).</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateSiteDrivesAsync(string siteId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(siteId)) throw new ArgumentException("A site id is required.", nameof(siteId));
        return EnumerateAsync<JsonDocument>($"sites/{Uri.EscapeDataString(siteId)}/drives", cancellationToken: cancellationToken);
    }

    private async Task<GraphSite> ReadSiteAsync(Uri address, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            var id = RequireString(root, "id");
            var displayName = root.TryGetProperty("displayName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
            var webUrl = root.TryGetProperty("webUrl", out var url) && url.ValueKind == JsonValueKind.String ? url.GetString() : null;
            return new GraphSite(id, displayName, webUrl);
        }
    }
}

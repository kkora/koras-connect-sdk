using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Entra app registration (MGR-007).</summary>
public sealed class GraphApplication
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("appId")] public string? AppId { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>The service principal corresponding to an app registration (MGR-007).</summary>
public sealed class GraphServicePrincipal
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("appId")] public string? AppId { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates <c>GET /applications</c> (MGR-007/019).</summary>
    public IAsyncEnumerable<GraphApplication> EnumerateApplicationsAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphApplication>("applications", query, cancellationToken);

    /// <summary>Reads a single app registration via <c>GET /applications/{id}</c> (MGR-007).</summary>
    public Task<GraphApplication> GetApplicationAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateDirectoryObjectId(id);
        return ReadResourceAsync<GraphApplication>(new Uri(_root, $"applications/{Uri.EscapeDataString(id)}"), cancellationToken);
    }

    /// <summary>Creates an app registration via <c>POST /applications</c> (MGR-007) - this feature is inherently
    /// privileged; it is disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> CreateApplicationAsync(JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the app-registration privilege and business impact.");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, new Uri(_root, "applications"), fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Enumerates <c>GET /servicePrincipals</c> (MGR-007/019).</summary>
    public IAsyncEnumerable<GraphServicePrincipal> EnumerateServicePrincipalsAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphServicePrincipal>("servicePrincipals", query, cancellationToken);

    /// <summary>Reads a single service principal via <c>GET /servicePrincipals/{id}</c> (MGR-007).</summary>
    public Task<GraphServicePrincipal> GetServicePrincipalAsync(string id, CancellationToken cancellationToken = default)
    {
        ValidateDirectoryObjectId(id);
        return ReadResourceAsync<GraphServicePrincipal>(new Uri(_root, $"servicePrincipals/{Uri.EscapeDataString(id)}"), cancellationToken);
    }

    /// <summary>Creates a service principal via <c>POST /servicePrincipals</c> (MGR-007). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> CreateServicePrincipalAsync(JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the app-registration privilege and business impact.");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, new Uri(_root, "servicePrincipals"), fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }
}

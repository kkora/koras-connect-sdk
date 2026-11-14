using System.Net.Http.Headers;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>One available Salesforce REST API version (SFD-002).</summary>
public sealed record SalesforceApiVersion(string Label, string Url, string Version);

/// <summary>Version discovery (SFD-002): <c>GET {host}/services/data/</c> requires no authentication and lists
/// every API version the host currently serves. Uses a plain, caller-supplied <see cref="HttpClient"/> directly
/// rather than <see cref="IntegrationTransport"/>, since the transport always authenticates every request and
/// this call is explicitly documented as unauthenticated.</summary>
public static class SalesforceDiscovery
{
    public static async Task<IReadOnlyList<SalesforceApiVersion>> ListApiVersionsAsync(HttpClient httpClient, Uri host, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        var root = EndpointSafety.ValidateRoot(host);
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(root, "services/data/"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response;
        try { response = await httpClient.SendAsync(request, cancellationToken); }
        catch (HttpRequestException ex)
        {
            // This call bypasses IntegrationTransport (it is intentionally unauthenticated), so it must wrap a
            // connection-level failure itself - otherwise an unreachable host would surface as a raw
            // HttpRequestException instead of this SDK's own exception hierarchy, matching the fix applied to
            // IntegrationTransport.ExecuteAsync for every other, authenticated call path.
            throw new TransportException("Failed to connect to the Salesforce version-discovery endpoint.", new ErrorDetails("transport_connection_failed"), ex);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode) throw SalesforceApiException.Classify(response.StatusCode, 1);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                throw new ContractException("Version-discovery response was not a JSON array.", new ErrorDetails("version_discovery_contract"));
            var versions = new List<SalesforceApiVersion>();
            foreach (var item in document.RootElement.EnumerateArray())
                versions.Add(new SalesforceApiVersion(RequireString(item, "label"), RequireString(item, "url"), RequireString(item, "version")));
            return versions;
        }
    }

    private static string RequireString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ContractException($"Version-discovery entry missing required field '{property}'.", new ErrorDetails("version_discovery_contract"));
        return value.GetString()!;
    }
}

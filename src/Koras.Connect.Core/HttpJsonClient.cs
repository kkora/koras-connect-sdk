using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Core;
public sealed class HttpJsonClient(HttpClient http)
{
    public async Task<JsonDocument> GetAsync(Uri uri, string bearerToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new IntegrationApiException($"API request failed with HTTP {(int)response.StatusCode}.", (int)response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }
}

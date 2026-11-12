using System.Net.Http.Headers;
using System.Text.Json;

namespace Koras.Connect.Authentication;
public sealed record OAuthClientCredentialsOptions(Uri TokenEndpoint, string ClientId, string ClientSecret, string? Scope = null);
public sealed class OAuthClientCredentials(HttpClient http, OAuthClientCredentialsOptions options)
{
    public async Task<string> AcquireTokenAsync(CancellationToken ct = default)
    {
        var form = new Dictionary<string,string> { ["grant_type"]="client_credentials" };
        if (options.Scope is not null) form["scope"] = options.Scope;
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}")));
        request.Content = new FormUrlEncodedContent(form);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Token endpoint returned HTTP {(int)response.StatusCode}.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return json.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Token response missing access_token.");
    }
}

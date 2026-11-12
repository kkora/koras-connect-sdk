using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Koras.Connect.Transport;

namespace Koras.Connect.Authentication;

public sealed record ClientCredentialsOptions(Uri Endpoint, string ClientId, string ClientSecret, string? Scope = null)
{
    public void Validate()
    {
        if (Endpoint.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("Token endpoint must use HTTPS.");
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException("Client ID and secret are required.");
    }
}

public sealed class ClientCredentialsTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly ClientCredentialsOptions _options;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;
    public ClientCredentialsTokenSource(HttpClient http, ClientCredentialsOptions options)
    {
        options.Validate();
        _http = http;
        _options = options;
    }
    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _validUntil) return _token;
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _validUntil) return _token;
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")));
            var values = new Dictionary<string, string> { ["grant_type"] = "client_credentials" };
            if (!string.IsNullOrWhiteSpace(_options.Scope)) values["scope"] = _options.Scope;
            request.Content = new FormUrlEncodedContent(values);
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"OAuth token endpoint returned HTTP {(int)response.StatusCode}.");
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            var root = payload.RootElement;
            var token = root.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token)) throw new JsonException("Token endpoint returned no access_token.");
            var lifetime = root.TryGetProperty("expires_in", out var expires) && expires.TryGetInt32(out var seconds) ? seconds : 300;
            _token = token;
            _validUntil = DateTimeOffset.UtcNow.AddSeconds(Math.Max(0, lifetime - Math.Min(60, lifetime / 10)));
            return token;
        }
        finally { _mutex.Release(); }
    }
    public void Dispose() => _mutex.Dispose();
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Koras.Connect.Transport;

namespace Koras.Connect.Authentication;

public sealed record ClientCredentialsOptions(Uri Endpoint, string ClientId, string ClientSecret, string? Scope = null)
{
    public override string ToString() => "ClientCredentialsOptions { credentials = [REDACTED] }";
    public void Validate()
    {
        if (!Endpoint.IsAbsoluteUri || Endpoint.Scheme != Uri.UriSchemeHttps || Endpoint.UserInfo.Length != 0) throw new ArgumentException("Token endpoint must use HTTPS.");
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
            throw new ArgumentException("Client ID and secret are required.");
    }
}

public sealed class ClientCredentialsTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly ClientCredentialsOptions _options;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;
    public ClientCredentialsTokenSource(HttpClient http, ClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        options.Validate();
        _clock = timeProvider ?? TimeProvider.System;
        _http = http;
        _options = options;
    }
    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _clock.GetUtcNow() < _validUntil) return _token;
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
            if (root.TryGetProperty("token_type", out var type) && !string.Equals(type.GetString(), "Bearer", StringComparison.OrdinalIgnoreCase))
                throw new JsonException("Unsupported token type.");
            if (!root.TryGetProperty("expires_in", out var expires) || !expires.TryGetInt32(out var lifetime) || lifetime <= 0)
                throw new JsonException("A positive expires_in is required.");
            _token = token;
            _validUntil = _clock.GetUtcNow().AddSeconds(Math.Max(0, lifetime - Math.Min(60, lifetime / 10)));
            return token;
        }
        finally { _mutex.Release(); }
    }
    public void Dispose() => _mutex.Dispose();
}


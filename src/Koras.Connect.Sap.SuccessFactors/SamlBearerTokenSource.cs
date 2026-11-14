using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>Exchanges host/IdP-generated Base64 SAML assertions using SAP's documented OAuth grant.
/// The SDK does not generate/sign assertions, bypass RBP, or treat unrelated IAS tokens as interchangeable.</summary>
public sealed class SamlBearerTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly string _company;
    private readonly string _client;
    private readonly Func<CancellationToken, ValueTask<string>> _assertions;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _expires;
    public SamlBearerTokenSource(HttpClient http, Uri endpoint, string companyId, string clientId,
        Func<CancellationToken, ValueTask<string>> assertions, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        EndpointSafety.ValidateRoot(endpoint);
        ArgumentNullException.ThrowIfNull(assertions);
        if (!endpoint.AbsolutePath.EndsWith("/oauth/token", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("Configure the exact SAP OAuth endpoint, company and registered client ID.");
        _http = http; _endpoint = endpoint; _company = companyId; _client = clientId; _assertions = assertions; _clock = clock ?? TimeProvider.System;
    }
    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _clock.GetUtcNow() < _expires) return _token;
            var assertion = await _assertions(cancellationToken);
            if (string.IsNullOrWhiteSpace(assertion)) throw new InvalidOperationException("The assertion source returned no assertion.");
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            { Content = new FormUrlEncodedContent(new Dictionary<string, string>
                { ["company_id"] = _company, ["client_id"] = _client, ["grant_type"] = "urn:ietf:params:oauth:grant-type:saml2-bearer", ["assertion"] = assertion }) };
            HttpResponseMessage response;
            try { response = await _http.SendAsync(request, cancellationToken); }
            catch (HttpRequestException ex) { throw new TransportException("Failed to connect to the SAP OAuth token endpoint.", new ErrorDetails("transport_connection_failed"), ex); }
            using (response)
            {
                if (!response.IsSuccessStatusCode) throw new IntegrationRequestException(response.StatusCode, 1);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken); var root = doc.RootElement;
                var token = root.GetProperty("access_token").GetString();
                if (string.IsNullOrWhiteSpace(token) || !root.TryGetProperty("expires_in", out var expiry) || !expiry.TryGetInt32(out var seconds) || seconds <= 0
                    || !root.TryGetProperty("token_type", out var type) || !string.Equals(type.GetString(), "Bearer", StringComparison.OrdinalIgnoreCase))
                    throw new JsonException("Invalid SAP token response.");
                _token = token; _expires = _clock.GetUtcNow().AddSeconds(seconds - Math.Min(60, seconds / 10)); return token;
            }
        }
        finally { _mutex.Release(); }
    }
    public void Dispose() => _mutex.Dispose();
}

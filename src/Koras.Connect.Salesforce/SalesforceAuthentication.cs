using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>Client Credentials OAuth2 settings for a Salesforce External Client App (or legacy Connected App)
/// (SFD-001). <c>{host}</c> is <c>login.salesforce.com</c>, <c>test.salesforce.com</c> (sandbox), or the org's My
/// Domain host. Reuses <see cref="ClientCredentialsTokenSource"/> unchanged, per this feature's own direction.</summary>
public sealed record SalesforceClientCredentialsOptions(Uri TokenEndpoint, string ClientId, string ClientSecret) : IValidatableOptions
{
    public override string ToString() => "SalesforceClientCredentialsOptions { credentials = [REDACTED] }";

    public void Validate()
    {
        var failures = new List<string>();
        if (TokenEndpoint is null || !TokenEndpoint.IsAbsoluteUri || TokenEndpoint.Scheme != Uri.UriSchemeHttps || TokenEndpoint.UserInfo.Length != 0)
            failures.Add($"{nameof(TokenEndpoint)}: must be an absolute HTTPS URI without user information.");
        if (string.IsNullOrWhiteSpace(ClientId)) failures.Add($"{nameof(ClientId)}: is required.");
        if (string.IsNullOrWhiteSpace(ClientSecret)) failures.Add($"{nameof(ClientSecret)}: is required.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(SalesforceClientCredentialsOptions), failures);
    }
}

/// <summary>JWT Bearer flow settings (SFD-001): certificate-signed assertion impersonating <see cref="Subject"/>
/// (the pre-authorized running user), with no interactive step and no stored secret. The SDK never generates,
/// signs or caches the assertion ahead of time - a fresh one is minted on every token request.</summary>
public sealed record SalesforceJwtBearerOptions(Uri TokenEndpoint, string ClientId, string Subject,
    Func<CancellationToken, ValueTask<X509Certificate2>> CertificateSource) : IValidatableOptions
{
    public void Validate()
    {
        var failures = new List<string>();
        if (TokenEndpoint is null || !TokenEndpoint.IsAbsoluteUri || TokenEndpoint.Scheme != Uri.UriSchemeHttps || TokenEndpoint.UserInfo.Length != 0)
            failures.Add($"{nameof(TokenEndpoint)}: must be an absolute HTTPS URI without user information.");
        if (string.IsNullOrWhiteSpace(ClientId)) failures.Add($"{nameof(ClientId)}: is required.");
        if (string.IsNullOrWhiteSpace(Subject)) failures.Add($"{nameof(Subject)}: is required.");
        if (CertificateSource is null) failures.Add($"{nameof(CertificateSource)}: is required.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(SalesforceJwtBearerOptions), failures);
    }
}

/// <summary>Builds the <see cref="IAccessTokenSource"/> for the Client Credentials flow (SFD-001). The resolved
/// <c>instance_url</c> that normally accompanies a real Salesforce token response is not captured here - matching
/// Dataverse's equivalent design decision (DVT-001), the environment/instance root is a separate, explicitly
/// configured input (<see cref="SalesforceOptions.InstanceRoot"/>) rather than something re-derived from a token
/// refresh, since it does not change for a given org between ordinary refreshes.</summary>
public static class SalesforceAuthentication
{
    public static IAccessTokenSource CreateClientCredentialsTokenSource(HttpClient httpClient, SalesforceClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return new ClientCredentialsTokenSource(httpClient, new ClientCredentialsOptions(options.TokenEndpoint, options.ClientId, options.ClientSecret), timeProvider);
    }
}

/// <summary>Salesforce JWT Bearer token acquisition (SFD-001): an RFC 7523 JWT assertion signed with the
/// certificate's RSA private key (RS256), impersonating <see cref="SalesforceJwtBearerOptions.Subject"/>. Mirrors
/// <c>DataverseCertificateAssertionTokenSource</c>'s structure for the equivalent Dataverse flow.</summary>
public sealed class SalesforceJwtBearerTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly SalesforceJwtBearerOptions _options;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;

    public SalesforceJwtBearerTokenSource(HttpClient httpClient, SalesforceJwtBearerOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _http = httpClient;
        _options = options;
        _clock = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _clock.GetUtcNow() < _validUntil) return _token;
            var certificate = await _options.CertificateSource(cancellationToken);
            if (certificate is null || !certificate.HasPrivateKey) throw new ArgumentException("Client certificate must include a private key.");
            var assertion = BuildAssertion(certificate, _options.TokenEndpoint, _options.ClientId, _options.Subject, _clock.GetUtcNow());
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["assertion"] = assertion,
                })
            };
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new AuthenticationException($"OAuth token endpoint returned HTTP {(int)response.StatusCode}.", new ErrorDetails($"oauth_http_{(int)response.StatusCode}"));
            await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
            var root = payload.RootElement;
            var token = root.GetProperty("access_token").GetString();
            if (string.IsNullOrWhiteSpace(token)) throw new JsonException("Token endpoint returned no access_token.");
            _token = token;
            // Salesforce does not document a JWT Bearer expires_in; apply a conservative fixed cache window and
            // let a 401 on actual use force reacquisition, matching the documented assertion-replay-window pattern.
            _validUntil = _clock.GetUtcNow().AddMinutes(30);
            return token;
        }
        finally { _mutex.Release(); }
    }

    /// <summary>Forces the next <see cref="GetTokenAsync"/> call to re-acquire a token.</summary>
    public void Invalidate()
    {
        _mutex.Wait();
        try { _token = null; _validUntil = default; }
        finally { _mutex.Release(); }
    }

    /// <summary>Builds and signs the RFC 7523 JWT Bearer assertion. Public, pure function for standalone testing.
    /// <c>aud</c> is the token endpoint's scheme+host (for example <c>https://login.salesforce.com</c>), matching
    /// Salesforce's documented audience value - not the full token endpoint path.</summary>
    public static string BuildAssertion(X509Certificate2 certificate, Uri tokenEndpoint, string clientId, string subject, DateTimeOffset now)
    {
        var header = JsonSerializer.Serialize(new { alg = "RS256" });
        var audience = $"{tokenEndpoint.Scheme}://{tokenEndpoint.Host}";
        var payload = JsonSerializer.Serialize(new
        {
            iss = clientId,
            sub = subject,
            aud = audience,
            exp = now.AddMinutes(3).ToUnixTimeSeconds(),
        });
        var unsigned = Base64UrlEncode(Encoding.UTF8.GetBytes(header)) + "." + Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        using var rsa = certificate.GetRSAPrivateKey() ?? throw new ArgumentException("Certificate must have an RSA private key.", nameof(certificate));
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return unsigned + "." + Base64UrlEncode(signature);
    }

    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose() => _mutex.Dispose();
}

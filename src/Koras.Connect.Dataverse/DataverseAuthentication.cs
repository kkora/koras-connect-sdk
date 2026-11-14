using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>Microsoft Entra ID app-only (client-credentials) connection settings for a single Dataverse
/// environment (DVT-001). Exactly one of <see cref="ClientSecret"/>/<see cref="CertificateSource"/> must be
/// supplied - the SDK never generates, stores or caches a secret/certificate itself; <see cref="CertificateSource"/>
/// is invoked fresh on every token acquisition so an external rotation (for example a renewed certificate file) is
/// picked up without recreating the token source. <see cref="EnvironmentUrl"/> is used only to derive the
/// <c>{environment-url}/.default</c> token scope [D4]; it is not validated as reachable by this type.</summary>
public sealed record DataverseClientCredentialsOptions(
    Uri TenantTokenEndpoint,
    string ClientId,
    string EnvironmentUrl,
    string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions
{
    public override string ToString() => "DataverseClientCredentialsOptions { credentials = [REDACTED] }";

    public void Validate()
    {
        var failures = new List<string>();
        if (TenantTokenEndpoint is null || !TenantTokenEndpoint.IsAbsoluteUri || TenantTokenEndpoint.Scheme != Uri.UriSchemeHttps || TenantTokenEndpoint.UserInfo.Length != 0)
            failures.Add($"{nameof(TenantTokenEndpoint)}: must be an absolute HTTPS URI without user information.");
        if (string.IsNullOrWhiteSpace(ClientId)) failures.Add($"{nameof(ClientId)}: is required.");
        if (string.IsNullOrWhiteSpace(EnvironmentUrl) || !Uri.TryCreate(EnvironmentUrl, UriKind.Absolute, out var environmentUri) || environmentUri.Scheme != Uri.UriSchemeHttps)
            failures.Add($"{nameof(EnvironmentUrl)}: must be an absolute HTTPS URI.");
        var hasSecret = !string.IsNullOrWhiteSpace(ClientSecret);
        var hasCertificate = CertificateSource is not null;
        if (hasSecret == hasCertificate) failures.Add("Exactly one of ClientSecret or CertificateSource must be supplied.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(DataverseClientCredentialsOptions), failures);
    }
}

/// <summary>Builds the <see cref="IAccessTokenSource"/> for a Dataverse environment per DVT-001: the client-secret
/// case reuses <see cref="ClientCredentialsTokenSource"/> unchanged; the certificate case uses the
/// connector-owned <see cref="DataverseCertificateAssertionTokenSource"/> since JWT client-assertion is an
/// Entra-wide mechanism not otherwise implemented in this repository.</summary>
public static class DataverseAuthentication
{
    public static IAccessTokenSource CreateTokenSource(HttpClient httpClient, DataverseClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var scope = options.EnvironmentUrl.TrimEnd('/') + "/.default";
        return !string.IsNullOrWhiteSpace(options.ClientSecret)
            ? new ClientCredentialsTokenSource(httpClient, new ClientCredentialsOptions(options.TenantTokenEndpoint, options.ClientId, options.ClientSecret, scope), timeProvider)
            : new DataverseCertificateAssertionTokenSource(httpClient, options, scope, timeProvider);
    }
}

/// <summary>Microsoft Entra ID client-credentials token acquisition using an X.509 certificate client assertion
/// (RFC 7523 JWT bearer), since no interactive/MSAL dependency exists in this repository. The assertion is signed
/// with the certificate's RSA private key (RS256) and is never persisted; a fresh assertion is minted on every
/// token request, not reused across requests. Concurrent callers single-flight the acquisition, matching
/// <see cref="ClientCredentialsTokenSource"/>'s existing semantics.</summary>
public sealed class DataverseCertificateAssertionTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly DataverseClientCredentialsOptions _options;
    private readonly string _scope;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;

    public DataverseCertificateAssertionTokenSource(HttpClient httpClient, DataverseClientCredentialsOptions options, string scope, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.CertificateSource is null) throw new ArgumentException("A certificate source is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(scope)) throw new ArgumentException("A token scope is required.", nameof(scope));
        _http = httpClient;
        _options = options;
        _scope = scope;
        _clock = timeProvider ?? TimeProvider.System;
    }

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_token is not null && _clock.GetUtcNow() < _validUntil) return _token;
            // The source (for example Koras.Connect.Authentication.CertificateCredentialProvider) may cache and
            // return the same certificate instance across calls - this type never owns or disposes it.
            var certificate = await _options.CertificateSource!(cancellationToken);
            if (certificate is null || !certificate.HasPrivateKey) throw new ArgumentException("Client certificate must include a private key.");
            var assertion = BuildClientAssertion(certificate, _options.TenantTokenEndpoint, _options.ClientId, _clock.GetUtcNow());
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TenantTokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    ["client_assertion"] = assertion,
                    ["scope"] = _scope,
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

    /// <summary>Forces the next <see cref="GetTokenAsync"/> call to re-acquire a token, for use after a
    /// certificate-rotation event.</summary>
    public void Invalidate()
    {
        _mutex.Wait();
        try { _token = null; _validUntil = default; }
        finally { _mutex.Release(); }
    }

    /// <summary>Builds and signs an RFC 7523 JWT client assertion. Exposed as a standalone, public, pure function so
    /// it can be unit-tested independently of any network call.</summary>
    public static string BuildClientAssertion(X509Certificate2 certificate, Uri audience, string clientId, DateTimeOffset now)
    {
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", x5t = Base64UrlEncode(certificate.GetCertHash(HashAlgorithmName.SHA1)) });
        var payload = JsonSerializer.Serialize(new
        {
            aud = audience.AbsoluteUri,
            iss = clientId,
            sub = clientId,
            jti = Guid.NewGuid().ToString(),
            nbf = now.ToUnixTimeSeconds(),
            exp = now.AddMinutes(10).ToUnixTimeSeconds(),
        });
        var unsigned = Base64UrlEncode(Encoding.UTF8.GetBytes(header)) + "." + Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        using var rsa = certificate.GetRSAPrivateKey() ?? throw new ArgumentException("Certificate must have an RSA private key.", nameof(certificate));
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return unsigned + "." + Base64UrlEncode(signature);
    }

    private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose() => _mutex.Dispose();
}

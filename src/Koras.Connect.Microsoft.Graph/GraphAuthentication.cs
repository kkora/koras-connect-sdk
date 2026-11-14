using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>App-only (client-credentials) connection settings for a single Entra tenant/app registration
/// (MGR-001). <see cref="TenantId"/> must be a GUID, verified domain, or <c>organizations</c>/<c>consumers</c> -
/// <c>common</c> is invalid for any call requiring app-only consent and is rejected locally. Exactly one of
/// <see cref="ClientSecret"/>/<see cref="CertificateSource"/> must be supplied. Delegated (authorization-code or
/// device-code) acquisition requires a browser or device-flow step and is out of this SDK's scope by design
/// (matching DVT-001/SFD-001's identical decision) - a caller running a delegated flow supplies their own
/// <see cref="IAccessTokenSource"/> directly to <see cref="GraphClient"/> instead of using this type.</summary>
public sealed record GraphClientCredentialsOptions(string TenantId, string ClientId, string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions
{
    public override string ToString() => "GraphClientCredentialsOptions { credentials = [REDACTED] }";

    public void Validate()
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(TenantId)) failures.Add($"{nameof(TenantId)}: is required.");
        else if (string.Equals(TenantId, "common", StringComparison.OrdinalIgnoreCase))
            failures.Add($"{nameof(TenantId)}: 'common' is invalid for app-only (client-credentials) authentication.");
        if (string.IsNullOrWhiteSpace(ClientId)) failures.Add($"{nameof(ClientId)}: is required.");
        var hasSecret = !string.IsNullOrWhiteSpace(ClientSecret);
        var hasCertificate = CertificateSource is not null;
        if (hasSecret == hasCertificate) failures.Add("Exactly one of ClientSecret or CertificateSource must be supplied.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(GraphClientCredentialsOptions), failures);
    }

    internal Uri TokenEndpoint => new($"https://login.microsoftonline.com/{Uri.EscapeDataString(TenantId)}/oauth2/v2.0/token");
}

/// <summary>Builds the app-only <see cref="IAccessTokenSource"/> for a Graph app registration (MGR-001): the
/// client-secret case reuses <see cref="ClientCredentialsTokenSource"/> unchanged with the documented
/// <c>https://graph.microsoft.com/.default</c> scope; the certificate case uses the connector-owned
/// <see cref="GraphCertificateAssertionTokenSource"/>, since JWT client-assertion is an Entra-wide mechanism this
/// repository already implements identically for Dataverse (DVT-001).</summary>
public static class GraphAuthentication
{
    public const string DefaultScope = "https://graph.microsoft.com/.default";

    public static IAccessTokenSource CreateClientCredentialsTokenSource(HttpClient httpClient, GraphClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return !string.IsNullOrWhiteSpace(options.ClientSecret)
            ? new ClientCredentialsTokenSource(httpClient, new ClientCredentialsOptions(options.TokenEndpoint, options.ClientId, options.ClientSecret, DefaultScope), timeProvider)
            : new GraphCertificateAssertionTokenSource(httpClient, options, timeProvider);
    }
}

/// <summary>Entra ID client-credentials token acquisition using an X.509 certificate client assertion (RFC 7523
/// JWT bearer), mirroring <c>DataverseCertificateAssertionTokenSource</c> exactly - the same Entra tenant token
/// endpoint and assertion shape apply regardless of which Microsoft API the resulting token targets. The
/// assertion is signed with the certificate's RSA private key (RS256) and is never persisted; a fresh assertion
/// is minted on every token request.</summary>
public sealed class GraphCertificateAssertionTokenSource : IAccessTokenSource, IDisposable
{
    private readonly HttpClient _http;
    private readonly GraphClientCredentialsOptions _options;
    private readonly TimeProvider _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private string? _token;
    private DateTimeOffset _validUntil;

    public GraphCertificateAssertionTokenSource(HttpClient httpClient, GraphClientCredentialsOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.CertificateSource is null) throw new ArgumentException("A certificate source is required.", nameof(options));
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
            var certificate = await _options.CertificateSource!(cancellationToken);
            if (certificate is null || !certificate.HasPrivateKey) throw new ArgumentException("Client certificate must include a private key.");
            var assertion = BuildClientAssertion(certificate, _options.TokenEndpoint, _options.ClientId, _clock.GetUtcNow());
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.ClientId,
                    ["client_assertion_type"] = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    ["client_assertion"] = assertion,
                    ["scope"] = GraphAuthentication.DefaultScope,
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

    /// <summary>Builds and signs an RFC 7523 JWT client assertion. Exposed as a standalone, public, pure function
    /// so it can be unit-tested independently of any network call.</summary>
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

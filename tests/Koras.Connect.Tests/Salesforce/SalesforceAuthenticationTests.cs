using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-001: OAuth authentication - Client Credentials (reusing the shared token source) and the
/// connector-owned JWT Bearer flow.</summary>
public sealed class SalesforceAuthenticationTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri TokenEndpoint = new("https://contoso.my.salesforce.com/services/oauth2/token");

    private static X509Certificate2 SelfSigned()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=koras-salesforce-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public void ClientSecretAndCertificateOptionsValidateIndependently()
    {
        Assert.Throws<OptionsValidationException>(() => new SalesforceClientCredentialsOptions(TokenEndpoint, "", "secret").Validate());
        Assert.Throws<OptionsValidationException>(() => new SalesforceClientCredentialsOptions(new Uri("http://insecure"), "id", "secret").Validate());
        using var certificate = SelfSigned();
        Assert.Throws<OptionsValidationException>(() => new SalesforceJwtBearerOptions(TokenEndpoint, "id", "", _ => ValueTask.FromResult(certificate)).Validate());
    }

    [Fact]
    public void CreateClientCredentialsTokenSourceReturnsTheSharedTokenSource()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}"))));
        var options = new SalesforceClientCredentialsOptions(TokenEndpoint, "client", "secret");
        using var source = (IDisposable)SalesforceAuthentication.CreateClientCredentialsTokenSource(http, options);
        Assert.IsType<Koras.Connect.Authentication.ClientCredentialsTokenSource>(source);
    }

    [Fact]
    public void BuildAssertionProducesAValidThreePartJwtSignedByTheCertificate()
    {
        using var certificate = SelfSigned();
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var assertion = SalesforceJwtBearerTokenSource.BuildAssertion(certificate, TokenEndpoint, "client-id", "user@contoso.com", now);
        var parts = assertion.Split('.');
        Assert.Equal(3, parts.Length);

        var header = JsonDocument.Parse(Base64UrlDecode(parts[0])).RootElement;
        Assert.Equal("RS256", header.GetProperty("alg").GetString());

        var payload = JsonDocument.Parse(Base64UrlDecode(parts[1])).RootElement;
        Assert.Equal("client-id", payload.GetProperty("iss").GetString());
        Assert.Equal("user@contoso.com", payload.GetProperty("sub").GetString());
        Assert.Equal("https://contoso.my.salesforce.com", payload.GetProperty("aud").GetString());

        using var rsa = certificate.GetRSAPublicKey()!;
        var signedPortion = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
        Assert.True(rsa.VerifyData(signedPortion, Base64UrlDecodeBytes(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public async Task JwtBearerSendsTheDocumentedGrantTypeAndCachesUntilExpiry()
    {
        var calls = 0; var clock = new Clock();
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler(async (r, ct) =>
        {
            calls++;
            var body = await r.Content!.ReadAsStringAsync(ct);
            Assert.Contains("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer", body);
            Assert.Contains("assertion=", body);
            return Json("""{"access_token":"synthetic","token_type":"Bearer"}""");
        }));
        var options = new SalesforceJwtBearerOptions(TokenEndpoint, "client", "user@contoso.com", _ => ValueTask.FromResult(certificate));
        using var source = new SalesforceJwtBearerTokenSource(http, options, clock);
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal(1, calls);
        source.Invalidate();
        await source.GetTokenAsync();
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task JwtBearerCertificateWithoutPrivateKeyIsRejectedBeforeAnyNetworkCall()
    {
        var calls = 0;
        using var publicOnly = X509CertificateLoader.LoadCertificate(SelfSigned().Export(X509ContentType.Cert));
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json("{}")); }));
        var options = new SalesforceJwtBearerOptions(TokenEndpoint, "client", "user@contoso.com", _ => ValueTask.FromResult(publicOnly));
        using var source = new SalesforceJwtBearerTokenSource(http, options);
        await Assert.ThrowsAsync<ArgumentException>(() => source.GetTokenAsync().AsTask());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task JwtBearerNonSuccessResponseThrowsAuthenticationException()
    {
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized))));
        var options = new SalesforceJwtBearerOptions(TokenEndpoint, "client", "user@contoso.com", _ => ValueTask.FromResult(certificate));
        using var source = new SalesforceJwtBearerTokenSource(http, options);
        await Assert.ThrowsAsync<AuthenticationException>(() => source.GetTokenAsync().AsTask());
    }

    private static byte[] Base64UrlDecodeBytes(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
    private static string Base64UrlDecode(string value) => Encoding.UTF8.GetString(Base64UrlDecodeBytes(value));
}

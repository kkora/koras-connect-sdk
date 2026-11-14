using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-001: delegated and app-only authentication - client-secret (reusing the shared token source) and
/// the connector-owned certificate client-assertion flow.</summary>
public sealed class GraphAuthenticationTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static X509Certificate2 SelfSigned()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=koras-graph-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public void CommonTenantIsRejectedForAppOnlyAuthentication()
    {
        var ex = Assert.Throws<OptionsValidationException>(() => new GraphClientCredentialsOptions("common", "client", "secret").Validate());
        Assert.Contains("common", ex.Message);
    }

    [Fact]
    public void ExactlyOneOfSecretOrCertificateMustBeSupplied()
    {
        Assert.Throws<OptionsValidationException>(() => new GraphClientCredentialsOptions("tenant", "client").Validate());
        using var certificate = SelfSigned();
        Assert.Throws<OptionsValidationException>(() => new GraphClientCredentialsOptions("tenant", "client", "secret", _ => ValueTask.FromResult(certificate)).Validate());
    }

    [Fact]
    public void CreateClientCredentialsTokenSourceReturnsTheSharedTokenSourceForASecret()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}"))));
        var options = new GraphClientCredentialsOptions("tenant-id", "client-id", "secret");
        using var source = (IDisposable)GraphAuthentication.CreateClientCredentialsTokenSource(http, options);
        Assert.IsType<Koras.Connect.Authentication.ClientCredentialsTokenSource>(source);
    }

    [Fact]
    public async Task ClientSecretFlowPostsToTheTenantTokenEndpointWithTheDefaultScope()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        using var http = new HttpClient(new Handler(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"access_token":"synthetic","token_type":"Bearer","expires_in":3600}"""); }));
        var options = new GraphClientCredentialsOptions("contoso-tenant", "client-id", "secret");
        using var source = (IDisposable)GraphAuthentication.CreateClientCredentialsTokenSource(http, options);
        var token = await ((Koras.Connect.Transport.IAccessTokenSource)source).GetTokenAsync();
        Assert.Equal("synthetic", token);
        Assert.Equal("https://login.microsoftonline.com/contoso-tenant/oauth2/v2.0/token", request!.RequestUri!.AbsoluteUri);
        Assert.Contains("scope=https%3A%2F%2Fgraph.microsoft.com%2F.default", body);
    }

    [Fact]
    public void BuildClientAssertionProducesAValidThreePartJwtSignedByTheCertificate()
    {
        using var certificate = SelfSigned();
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var audience = new Uri("https://login.microsoftonline.com/contoso-tenant/oauth2/v2.0/token");
        var assertion = GraphCertificateAssertionTokenSource.BuildClientAssertion(certificate, audience, "client-id", now);
        var parts = assertion.Split('.');
        Assert.Equal(3, parts.Length);

        var header = JsonDocument.Parse(Base64UrlDecode(parts[0])).RootElement;
        Assert.Equal("RS256", header.GetProperty("alg").GetString());

        var payload = JsonDocument.Parse(Base64UrlDecode(parts[1])).RootElement;
        Assert.Equal("client-id", payload.GetProperty("iss").GetString());
        Assert.Equal("client-id", payload.GetProperty("sub").GetString());
        Assert.Equal(audience.AbsoluteUri, payload.GetProperty("aud").GetString());

        using var rsa = certificate.GetRSAPublicKey()!;
        var signedPortion = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
        Assert.True(rsa.VerifyData(signedPortion, Base64UrlDecodeBytes(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public async Task CertificateFlowSendsTheDocumentedClientAssertionGrantAndCachesUntilExpiry()
    {
        var calls = 0; var clock = new Clock();
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler(async (r, ct) =>
        {
            calls++;
            var body = await r.Content!.ReadAsStringAsync(ct);
            Assert.Contains("client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer", body);
            Assert.Contains("client_assertion=", body);
            return Json("""{"access_token":"synthetic","token_type":"Bearer","expires_in":3600}""");
        }));
        var options = new GraphClientCredentialsOptions("contoso-tenant", "client-id", CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new GraphCertificateAssertionTokenSource(http, options, clock);
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal(1, calls);
        source.Invalidate();
        await source.GetTokenAsync();
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CertificateWithoutPrivateKeyIsRejectedBeforeAnyNetworkCall()
    {
        var calls = 0;
        using var publicOnly = X509CertificateLoader.LoadCertificate(SelfSigned().Export(X509ContentType.Cert));
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json("{}")); }));
        var options = new GraphClientCredentialsOptions("contoso-tenant", "client-id", CertificateSource: _ => ValueTask.FromResult(publicOnly));
        using var source = new GraphCertificateAssertionTokenSource(http, options);
        await Assert.ThrowsAsync<ArgumentException>(() => source.GetTokenAsync().AsTask());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CertificateFlowNonSuccessResponseThrowsAuthenticationException()
    {
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized))));
        var options = new GraphClientCredentialsOptions("contoso-tenant", "client-id", CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new GraphCertificateAssertionTokenSource(http, options);
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

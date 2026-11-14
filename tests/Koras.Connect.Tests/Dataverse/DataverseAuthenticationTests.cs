using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-001: Microsoft Entra OAuth authentication - options validation, client-secret delegation to
/// <see cref="Koras.Connect.Authentication.ClientCredentialsTokenSource"/>, and the connector-owned certificate
/// client-assertion token source.</summary>
public sealed class DataverseAuthenticationTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri TokenEndpoint = new("https://login.microsoftonline.com/contoso-tenant/oauth2/v2.0/token");

    private static X509Certificate2 SelfSigned()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=koras-dataverse-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public void ExactlyOneOfSecretOrCertificateIsRequired()
    {
        Assert.Throws<OptionsValidationException>(() =>
            new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/").Validate());
        Assert.Throws<OptionsValidationException>(() =>
            new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
                ClientSecret: "secret", CertificateSource: _ => ValueTask.FromResult(SelfSigned())).Validate());
    }

    [Fact]
    public void NonHttpsTokenEndpointIsRejected()
    {
        Assert.Throws<OptionsValidationException>(() =>
            new DataverseClientCredentialsOptions(new Uri("http://login.microsoftonline.com/x/oauth2/v2.0/token"),
                "client", "https://contoso.api.crm.dynamics.com/", ClientSecret: "secret").Validate());
    }

    [Theory]
    [InlineData("common")]
    [InlineData("organizations")]
    [InlineData("consumers")]
    public void TenantAliasesInvalidForAppOnlyAreRejected(string alias)
    {
        Assert.Throws<OptionsValidationException>(() =>
            new DataverseClientCredentialsOptions(new Uri($"https://login.microsoftonline.com/{alias}/oauth2/v2.0/token"),
                "client", "https://contoso.api.crm.dynamics.com/", ClientSecret: "secret").Validate());
    }

    [Fact]
    public void CreateTokenSourceWithSecretReturnsClientCredentialsTokenSource()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}"))));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/", ClientSecret: "secret");
        using var source = (IDisposable)DataverseAuthentication.CreateTokenSource(http, options);
        Assert.IsType<Koras.Connect.Authentication.ClientCredentialsTokenSource>(source);
    }

    [Fact]
    public void CreateTokenSourceWithCertificateReturnsCertificateAssertionTokenSource()
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}"))));
        using var certificate = SelfSigned();
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = (IDisposable)DataverseAuthentication.CreateTokenSource(http, options);
        Assert.IsType<DataverseCertificateAssertionTokenSource>(source);
    }

    [Fact]
    public void BuildClientAssertionProducesAValidThreePartJwtSignedByTheCertificate()
    {
        using var certificate = SelfSigned();
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var assertion = DataverseCertificateAssertionTokenSource.BuildClientAssertion(certificate, TokenEndpoint, "client-id", now);
        var parts = assertion.Split('.');
        Assert.Equal(3, parts.Length);

        var header = JsonDocument.Parse(Base64UrlDecode(parts[0])).RootElement;
        Assert.Equal("RS256", header.GetProperty("alg").GetString());
        Assert.Equal("JWT", header.GetProperty("typ").GetString());
        Assert.Equal(Base64Url(certificate.GetCertHash(HashAlgorithmName.SHA1)), header.GetProperty("x5t").GetString());

        var payload = JsonDocument.Parse(Base64UrlDecode(parts[1])).RootElement;
        Assert.Equal(TokenEndpoint.AbsoluteUri, payload.GetProperty("aud").GetString());
        Assert.Equal("client-id", payload.GetProperty("iss").GetString());
        Assert.Equal("client-id", payload.GetProperty("sub").GetString());
        Assert.Equal(now.ToUnixTimeSeconds(), payload.GetProperty("nbf").GetInt64());

        using var rsa = certificate.GetRSAPublicKey()!;
        var signedPortion = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
        Assert.True(rsa.VerifyData(signedPortion, Base64UrlDecodeBytes(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public async Task CertificateAssertionTokenSourceCachesUntilExpiryThenRotatesTheAssertion()
    {
        var calls = 0; var clock = new Clock();
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler(async (r, ct) =>
        {
            calls++;
            var body = await r.Content!.ReadAsStringAsync(ct);
            Assert.Contains("client_assertion_type=urn", body);
            Assert.Contains("grant_type=client_credentials", body);
            return Json("""{"access_token":"synthetic","token_type":"Bearer","expires_in":100}""");
        }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "https://contoso.api.crm.dynamics.com/.default", clock);
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal(1, calls);
        clock.Now = clock.Now.AddSeconds(50);
        Assert.Equal("synthetic", await source.GetTokenAsync());
        Assert.Equal(1, calls); // still cached
        clock.Now = clock.Now.AddSeconds(50);
        await source.GetTokenAsync();
        Assert.Equal(2, calls); // margin crossed - real renewal
    }

    [Fact]
    public async Task ConcurrentCallersShareOneTokenRequest()
    {
        var calls = 0;
        var gate = new SemaphoreSlim(0);
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler(async (_, ct) =>
        {
            Interlocked.Increment(ref calls);
            await gate.WaitAsync(ct);
            return Json("""{"access_token":"shared","token_type":"Bearer","expires_in":3600}""");
        }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        var first = source.GetTokenAsync().AsTask();
        var second = source.GetTokenAsync().AsTask();
        gate.Release(2);
        var tokens = await Task.WhenAll(first, second);
        Assert.Equal(1, calls);
        Assert.Equal(["shared", "shared"], tokens);
    }

    [Fact]
    public async Task InvalidateForcesReacquisition()
    {
        var calls = 0;
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json("""{"access_token":"t","token_type":"Bearer","expires_in":3600}""")); }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        await source.GetTokenAsync();
        await source.GetTokenAsync();
        Assert.Equal(1, calls);
        source.Invalidate();
        await source.GetTokenAsync();
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task CertificateWithoutAPrivateKeyIsRejectedBeforeAnyNetworkCall()
    {
        var calls = 0;
        using var publicOnly = X509CertificateLoader.LoadCertificate(SelfSigned().Export(X509ContentType.Cert));
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json("{}")); }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(publicOnly));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        await Assert.ThrowsAsync<ArgumentException>(() => source.GetTokenAsync().AsTask());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task NonSuccessTokenResponseThrowsAuthenticationException()
    {
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized))));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        await Assert.ThrowsAsync<AuthenticationException>(() => source.GetTokenAsync().AsTask());
    }

    [Theory]
    [InlineData("""{"token_type":"Bearer","expires_in":100}""")]
    [InlineData("""{"access_token":"x","token_type":"Basic","expires_in":100}""")]
    [InlineData("""{"access_token":"x","token_type":"Bearer","expires_in":0}""")]
    public async Task MalformedTokenResponseNeverPoisonsTheCache(string malformedBody)
    {
        var calls = 0;
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json(malformedBody)); }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        await Assert.ThrowsAnyAsync<Exception>(() => source.GetTokenAsync().AsTask());
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CanceledAcquisitionNeverReachesTheHandler()
    {
        var calls = 0;
        using var certificate = SelfSigned();
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json("{}")); }));
        var options = new DataverseClientCredentialsOptions(TokenEndpoint, "client", "https://contoso.api.crm.dynamics.com/",
            CertificateSource: _ => ValueTask.FromResult(certificate));
        using var source = new DataverseCertificateAssertionTokenSource(http, options, "scope");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetTokenAsync(cts.Token).AsTask());
        Assert.Equal(0, calls);
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Base64UrlDecodeBytes(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }
    private static string Base64UrlDecode(string value) => Encoding.UTF8.GetString(Base64UrlDecodeBytes(value));
}

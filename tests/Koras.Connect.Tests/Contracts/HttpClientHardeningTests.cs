using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Adp;
using Koras.Connect.Authentication;
using Koras.Connect.Qualtrics;
using Koras.Connect.Sap.SuccessFactors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-004: every named <see cref="IHttpClientFactory"/> client registered by a provider's DI extension
/// must disable automatic redirects and cookie-container reuse (master spec functional requirement: "disable
/// redirects and unsafe URL loggers; disable credential-bearing cookie reuse"). Auditing this surfaced that SAP's
/// two named clients had no handler customization at all (default <see cref="HttpClientHandler"/>: redirects and
/// cookies both enabled) and that ADP's/Qualtrics's handlers disabled redirects but not cookies - both fixed
/// alongside this test file.</summary>
public sealed class HttpClientHardeningTests
{
    private static HttpClientHandler ResolvePrimaryHandler(IServiceProvider provider, string clientName)
    {
        var handler = provider.GetRequiredService<IHttpMessageHandlerFactory>().CreateHandler(clientName);
        while (handler is DelegatingHandler delegating) handler = delegating.InnerHandler!;
        return Assert.IsType<HttpClientHandler>(handler);
    }

    private static void AssertHardened(IServiceProvider provider, string clientName)
    {
        var handler = ResolvePrimaryHandler(provider, clientName);
        Assert.False(handler.AllowAutoRedirect, $"{clientName}: AllowAutoRedirect must be false.");
        Assert.False(handler.UseCookies, $"{clientName}: UseCookies must be false.");
    }

    [Fact]
    public void AdpNamedClientsDisableRedirectsAndCookiesAndCarryTheClientCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var services = new ServiceCollection();
        services.AddKorasAdp(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"),
            new Koras.Connect.Adp.Workers.AdpWorkersOptions(new Uri("https://api.test")), cert);
        using var provider = services.BuildServiceProvider();

        AssertHardened(provider, "Koras.Adp.OAuth");
        AssertHardened(provider, "Koras.Adp.Api");
        Assert.Contains(cert, ResolvePrimaryHandler(provider, "Koras.Adp.Api").ClientCertificates.Cast<X509Certificate2>());
    }

    [Fact]
    public void SapNamedClientsDisableRedirectsAndCookies()
    {
        var options = new SuccessFactorsConnectionOptions(
            ODataRoot: new Uri("https://sap.test/odata/v2/"),
            TokenEndpoint: new Uri("https://sap.test/oauth/token"),
            CompanyId: "synthetic-company",
            ClientId: "synthetic-client",
            AssertionSource: ct => ValueTask.FromResult("synthetic-assertion"));
        var services = new ServiceCollection();
        services.AddKorasSuccessFactors(options);
        using var provider = services.BuildServiceProvider();

        AssertHardened(provider, "Koras.SuccessFactors.OAuth");
        AssertHardened(provider, "Koras.SuccessFactors.Api");
    }

    [Fact]
    public void QualtricsNamedClientDisablesRedirectsAndCookies()
    {
        var services = new ServiceCollection();
        services.AddKorasQualtrics(o => o.ApiRoot = new Uri("https://qualtrics.test/API/v3/"),
            _ => new FakeTokenSource());
        using var provider = services.BuildServiceProvider();

        AssertHardened(provider, "Koras.Qualtrics.Api");
    }

    private sealed class FakeTokenSource : Koras.Connect.Transport.IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("fake"); }

    [Fact]
    public void ACallerCanOverrideTheRegisteredHandlerAfterAddKorasAdpByReRegisteringTheSameNamedClient()
    {
        // Documents the functional requirement's "caller overrides" clause: the last ConfigurePrimaryHttpMessageHandler
        // registered for a given named client wins, so a host can intentionally replace the SDK's default handler
        // (for example to route through a corporate proxy) without forking the extension method.
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var services = new ServiceCollection();
        services.AddKorasAdp(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"),
            new Koras.Connect.Adp.Workers.AdpWorkersOptions(new Uri("https://api.test")), cert);

        var replacement = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true };
        services.AddHttpClient("Koras.Adp.Api").ConfigurePrimaryHttpMessageHandler(() => replacement);
        using var provider = services.BuildServiceProvider();

        Assert.Same(replacement, ResolvePrimaryHandler(provider, "Koras.Adp.Api"));
    }
}

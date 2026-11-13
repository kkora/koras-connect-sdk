using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Authentication;
using Koras.Connect.Qualtrics;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-003: disposal-ownership coverage missing from the per-provider DI test files &#8212; proves each
/// provider's container-created, container-registered token source is actually disposed when the host disposes
/// the <see cref="ServiceProvider"/> (observed indirectly: a disposed token source throws <see cref="ObjectDisposedException"/>
/// from its internal <see cref="SemaphoreSlim"/> rather than silently leaking it), and that host-supplied resources
/// the SDK never registers as its own service (a certificate, an assertion delegate) are left untouched.</summary>
public sealed class DependencyInjectionDisposalTests
{
    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public async Task DisposingTheServiceProviderDisposesAdpsContainerOwnedTokenSourceButNotTheHostOwnedCertificate()
    {
        using var certificate = CreateSelfSignedCertificate();
        var credentials = new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret");
        var services = new ServiceCollection();
        services.AddKorasAdp(credentials, new Koras.Connect.Adp.Workers.AdpWorkersOptions(new Uri("https://api.test")), certificate);
        var provider = services.BuildServiceProvider();
        var tokenSource = provider.GetRequiredService<AdpRotatingTokenSource>();

        provider.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => tokenSource.GetTokenAsync().AsTask());
        // The certificate is never registered as its own DI service (only captured by a handler factory closure),
        // so the container has no way to dispose it; it must still be fully usable after provider disposal.
        Assert.True(certificate.HasPrivateKey);
    }

    [Fact]
    public async Task DisposingTheServiceProviderDisposesSapsContainerOwnedTokenSourceButNotTheHostOwnedAssertionDelegate()
    {
        var assertionCalls = 0;
        Func<CancellationToken, ValueTask<string>> assertionSource = ct => { assertionCalls++; return ValueTask.FromResult("synthetic-assertion"); };
        var options = new SuccessFactorsConnectionOptions(
            ODataRoot: new Uri("https://sap.test/odata/v2/"),
            TokenEndpoint: new Uri("https://sap.test/oauth/token"),
            CompanyId: "synthetic-company",
            ClientId: "synthetic-client",
            AssertionSource: assertionSource);
        var services = new ServiceCollection();
        services.AddKorasSuccessFactors(options);
        var provider = services.BuildServiceProvider();
        var tokenSource = provider.GetRequiredService<SamlBearerTokenSource>();

        provider.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => tokenSource.GetTokenAsync().AsTask());
        // The assertion delegate is a plain host-owned Func, never registered with the container; disposal of the
        // provider must not affect its ability to still be invoked directly by the host.
        Assert.Equal("synthetic-assertion", await assertionSource(CancellationToken.None));
        Assert.Equal(1, assertionCalls);
    }

    private sealed class DisposableFakeTokenSource : IAccessTokenSource, IDisposable
    {
        public bool Disposed { get; private set; }
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("fake-token");
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void QualtricsTokenSourceLifetimeIsHostOwnedNotContainerManaged()
    {
        // Unlike AddKorasAdp/AddKorasSuccessFactors, AddKorasQualtrics never registers the resolved token source as
        // its own service (it is only passed, already-constructed, into QualtricsClient.Create) - so a disposable
        // token source supplied via tokenSourceFactory is NOT disposed when the container is disposed. This test
        // documents that real, existing asymmetry (see SDK-003 remaining-work notes) rather than asserting an
        // idealized behavior; a host supplying a disposable Qualtrics token source must dispose it independently.
        var fakeTokenSource = new DisposableFakeTokenSource();
        var services = new ServiceCollection();
        services.AddKorasQualtrics(o => o.ApiRoot = new Uri("https://qualtrics.test/API/v3/"), _ => fakeTokenSource);
        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<QualtricsClient>();

        provider.Dispose();

        Assert.False(fakeTokenSource.Disposed);
    }

    [Fact]
    public void AddKorasAdpRejectsAMissingPrivateKeyBeforeAnyRegistration()
    {
        // Parity with SapServiceCollectionTests.UnsafeRootIsRejectedBeforeAnyHttpAttemptOrRegistration and
        // QualtricsExpansionTests.AddKorasQualtricsRejectsUnsafeOrWrongRootBeforeAnyHttpAttempt - ADP's own eager
        // validation was exercised only incidentally by existing tests, never under a dedicated name.
        using var publicOnly = X509CertificateLoader.LoadCertificate(CreateSelfSignedCertificate().Export(X509ContentType.Cert));
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddKorasAdp(
            new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"),
            new Koras.Connect.Adp.Workers.AdpWorkersOptions(new Uri("https://api.test")), publicOnly));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(Koras.Connect.Adp.Workers.AdpWorkersClient));
    }
}

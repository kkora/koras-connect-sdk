using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Authentication;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class CertificateCredentialProviderTests
{
    private static X509Certificate2 SelfSigned()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=koras-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    [Fact]
    public async Task LoadIsCachedAcrossCalls()
    {
        var loadCount = 0;
        using var provider = new CertificateCredentialProvider(() => { loadCount++; return SelfSigned(); });
        var first = await provider.GetAsync();
        var second = await provider.GetAsync();
        Assert.Same(first, second);
        Assert.Equal(1, loadCount);
    }

    [Fact]
    public async Task InvalidateForcesReloadOnNextCall()
    {
        var loadCount = 0;
        using var provider = new CertificateCredentialProvider(() => { loadCount++; return SelfSigned(); });
        await provider.GetAsync();
        provider.Invalidate();
        await provider.GetAsync();
        Assert.Equal(2, loadCount);
    }

    [Fact]
    public void RedactedCredentialNeverExposesValueInToString()
    {
        var redacted = new RedactedCredential<string>("super-secret-value");
        Assert.DoesNotContain("super-secret-value", redacted.ToString());
        Assert.Equal("super-secret-value", redacted.Reveal());
    }
}

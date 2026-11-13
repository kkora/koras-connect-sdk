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

    [Fact]
    public async Task CanceledRetrievalThrowsBeforeTheLoadDelegateEverRuns()
    {
        var loadCount = 0;
        using var provider = new CertificateCredentialProvider(() => { loadCount++; return SelfSigned(); });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetAsync(cts.Token).AsTask());
        Assert.Equal(0, loadCount);
    }

    [Fact]
    public async Task ALoadFailureIsNotCachedSoTheNextCallRetriesInsteadOfStayingPermanentlyBroken()
    {
        var attempt = 0;
        using var provider = new CertificateCredentialProvider(() =>
        {
            attempt++;
            if (attempt == 1) throw new CryptographicException("corrupt certificate file");
            return SelfSigned();
        });

        await Assert.ThrowsAsync<CryptographicException>(() => provider.GetAsync().AsTask());
        var recovered = await provider.GetAsync();
        Assert.NotNull(recovered);
        Assert.Equal(2, attempt);
    }

    [Fact]
    public async Task ConcurrentFirstCallsLoadExactlyOnceRatherThanRacing()
    {
        var loadCount = 0;
        var gate = new SemaphoreSlim(0);
        using var provider = new CertificateCredentialProvider(() =>
        {
            Interlocked.Increment(ref loadCount);
            gate.Wait(); // holds the mutex open while 7 more callers queue up behind it, so the mutex's
                         // serialization - not accidental timing - is what keeps loadCount at 1 below.
            return SelfSigned();
        });

        // Task.Run so the synchronous prefix of GetAsync (including the blocking gate.Wait() inside _load)
        // runs on a thread-pool thread instead of blocking this test method before gate.Release can execute.
        var callers = Enumerable.Range(0, 8).Select(_ => Task.Run(async () => await provider.GetAsync())).ToArray();
        gate.Release(8);
        var results = await Task.WhenAll(callers);

        Assert.Equal(1, loadCount);
        Assert.True(results.Distinct().Count() == 1, "All concurrent callers must observe the same cached certificate instance.");
    }
}

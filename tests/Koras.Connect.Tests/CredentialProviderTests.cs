using Koras.Connect.Abstractions;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-006: direct unit coverage for <see cref="StaticCredentialProvider{TCredential}"/> and
/// <see cref="DelegatingCredentialProvider{TCredential}"/> &#8212; previously exercised only incidentally through
/// ADP's own rotation tests, never under "canceled retrieval" or "source failure" by name, despite both being
/// explicit unit-test requirements for this ticket.</summary>
public sealed class CredentialProviderTests
{
    [Fact]
    public async Task StaticCredentialProviderAlwaysReturnsTheSameConstructedValue()
    {
        var provider = new StaticCredentialProvider<string>("fixed-value");
        Assert.Equal("fixed-value", await provider.GetAsync());
        Assert.Equal("fixed-value", await provider.GetAsync());
    }

    [Fact]
    public void StaticCredentialProviderRejectsANullValueAtConstruction()
        => Assert.Throws<ArgumentNullException>(() => new StaticCredentialProvider<string>(null!));

    [Fact]
    public async Task DelegatingCredentialProviderResolvesACurrentValueOnEveryCall()
    {
        var current = "first";
        var provider = new DelegatingCredentialProvider<string>(_ => ValueTask.FromResult(current));
        Assert.Equal("first", await provider.GetAsync());
        current = "second";
        Assert.Equal("second", await provider.GetAsync());
    }

    [Fact]
    public async Task DelegatingCredentialProviderPropagatesCancellationFromTheResolveDelegateRatherThanSwallowingIt()
    {
        var provider = new DelegatingCredentialProvider<string>(ct => { ct.ThrowIfCancellationRequested(); return ValueTask.FromResult("unused"); });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetAsync(cts.Token).AsTask());
    }

    [Fact]
    public async Task DelegatingCredentialProviderPropagatesResolveFailureWithoutLeakingAnySecretIntoTheExceptionAndRetriesOnTheNextCall()
    {
        // Source failure must not permanently poison the provider: a transient secret-store outage on one call
        // should not prevent a later call (after the store recovers) from succeeding.
        var attempt = 0;
        var provider = new DelegatingCredentialProvider<string>(_ =>
        {
            attempt++;
            if (attempt == 1) throw new InvalidOperationException("secret store unavailable");
            return ValueTask.FromResult("recovered-value");
        });

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetAsync().AsTask());
        Assert.DoesNotContain("recovered-value", failure.Message);

        Assert.Equal("recovered-value", await provider.GetAsync());
    }

    [Fact]
    public void RedactedCredentialRejectsANullValueAtConstruction()
        => Assert.Throws<ArgumentNullException>(() => new Koras.Connect.Authentication.RedactedCredential<string>(null!));
}

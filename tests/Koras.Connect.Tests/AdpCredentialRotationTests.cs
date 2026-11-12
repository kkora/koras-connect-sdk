using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-003 (rotatable client credentials, explicit invalidation) and ADP-011 (bounded token acquisition
/// even under a generous outer cancellation token).</summary>
public sealed class AdpCredentialRotationTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private sealed class SlowHandler : HttpMessageHandler
    { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return new(HttpStatusCode.OK); } }

    private static string BasicSecret(HttpRequestMessage request)
    {
        var parameter = request.Headers.Authorization!.Parameter!;
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(parameter));
        return decoded.Split(':')[1];
    }

    [Fact]
    public async Task RotatingCredentialProviderReacquiresTokenWhenResolvedCredentialsChange()
    {
        var secret = "secret-a";
        var calls = new List<string>();
        using var http = new HttpClient(new Handler(r =>
        {
            calls.Add(BasicSecret(r));
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") };
        }));
        var provider = new DelegatingCredentialProvider<ClientCredentialsOptions>(_ =>
            ValueTask.FromResult(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", secret)));
        using var source = new AdpRotatingTokenSource(provider, () => http, TimeSpan.FromSeconds(10));

        await source.GetTokenAsync();
        secret = "secret-b";
        await source.GetTokenAsync();

        Assert.Equal(["secret-a", "secret-b"], calls);
    }

    [Fact]
    public async Task InvalidateForcesReacquisitionEvenWithoutAResolvedCredentialChange()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(r =>
        {
            calls++;
            return new(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") };
        }));
        var provider = new StaticCredentialProvider<ClientCredentialsOptions>(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"));
        using var source = new AdpRotatingTokenSource(provider, () => http, TimeSpan.FromSeconds(10));

        await source.GetTokenAsync();
        await source.GetTokenAsync();
        Assert.Equal(1, calls); // still cached; no rotation signaled yet

        source.Invalidate();
        await source.GetTokenAsync();
        Assert.Equal(2, calls); // explicit rotation signal forced reacquisition
    }

    [Fact]
    public async Task HangingTokenEndpointDoesNotBlockForeverEvenUnderAGenerousOuterCancellation()
    {
        using var http = new HttpClient(new SlowHandler());
        var provider = new StaticCredentialProvider<ClientCredentialsOptions>(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"));
        using var source = new AdpRotatingTokenSource(provider, () => http, TimeSpan.FromMilliseconds(50));

        using var generous = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var stopwatch = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetTokenAsync(generous.Token).AsTask());
        stopwatch.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Token acquisition should fail fast on its own timeout, took {stopwatch.Elapsed}.");
        Assert.False(generous.IsCancellationRequested); // the outer token never fired; the bound came from acquisitionTimeout
    }

    [Fact]
    public void AddKorasAdpWithRotatingCredentialsRegistersWithoutNetworkAndRejectsDuplicateRegistration()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var provider = new StaticCredentialProvider<ClientCredentialsOptions>(new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"));
        var services = new ServiceCollection();
        services.AddKorasAdpWithRotatingCredentials(provider, new AdpWorkersOptions(new Uri("https://api.test")), cert);

        using var built = services.BuildServiceProvider();
        Assert.NotNull(built.GetRequiredService<AdpWorkersClient>());
        Assert.NotNull(built.GetRequiredService<AdpRotatingTokenSource>());
        Assert.NotNull(built.GetRequiredService<AdpConnector>());
        Assert.NotNull(built.GetRequiredService<AdpEntitlementTracker>());

        Assert.Throws<InvalidOperationException>(() =>
            services.AddKorasAdpWithRotatingCredentials(provider, new AdpWorkersOptions(new Uri("https://api.test")), cert));
    }
}

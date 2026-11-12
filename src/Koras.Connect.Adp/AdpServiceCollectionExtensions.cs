using Koras.Connect.Abstractions;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
namespace Koras.Connect.Adp;

public static class AdpServiceCollectionExtensions
{
    private static readonly TimeSpan DefaultTokenAcquisitionTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Registers isolated mTLS token/API clients for a fixed, non-rotating client credential value. Caller
    /// owns and retains <paramref name="certificate"/> until service provider disposal. <paramref name="tokenAcquisitionTimeout"/>
    /// bounds every OAuth token-endpoint call independently of any caller-supplied cancellation token, so a hanging
    /// token endpoint cannot block a caller forever even under a generous outer <see cref="CancellationToken"/>.</summary>
    public static IServiceCollection AddKorasAdp(this IServiceCollection services, ClientCredentialsOptions credentials,
        AdpWorkersOptions workers, X509Certificate2 certificate, TransportOptions? transport = null, TimeSpan? tokenAcquisitionTimeout = null,
        Func<AdpWorkersClient, CancellationToken, Task>? connectivityProbe = null)
    {
        credentials.Validate();
        return services.AddKorasAdpWithRotatingCredentials(new StaticCredentialProvider<ClientCredentialsOptions>(credentials), workers, certificate, transport,
            tokenAcquisitionTimeout, connectivityProbe);
    }

    /// <summary>Registers isolated mTLS token/API clients resolving client credentials through
    /// <paramref name="credentials"/>, so secrets can be rotated (secret-manager refresh, configuration reload)
    /// without rebuilding the service provider: the registered <see cref="AdpRotatingTokenSource"/> re-resolves
    /// <paramref name="credentials"/> on every cache miss and exposes <see cref="AdpRotatingTokenSource.Invalidate"/>
    /// for an explicit out-of-band rotation signal. Caller owns and retains <paramref name="certificate"/> until
    /// service provider disposal. <paramref name="connectivityProbe"/>, when supplied, becomes the single
    /// caller-selected authorized GET used by the registered <see cref="AdpConnector"/>'s connectivity stage; no
    /// endpoint is hardcoded by the SDK. Named distinctly from <see cref="AddKorasAdp"/> rather than overloaded,
    /// because a target-typed <c>new(...)</c> credentials argument would otherwise be ambiguous between the two.</summary>
    public static IServiceCollection AddKorasAdpWithRotatingCredentials(this IServiceCollection services, ICredentialProvider<ClientCredentialsOptions> credentials,
        AdpWorkersOptions workers, X509Certificate2 certificate, TransportOptions? transport = null, TimeSpan? tokenAcquisitionTimeout = null,
        Func<AdpWorkersClient, CancellationToken, Task>? connectivityProbe = null)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        workers.Validate();
        if (!certificate.HasPrivateKey) throw new ArgumentException("Certificate requires a private key.", nameof(certificate));
        if (services.Any(d => d.ServiceType == typeof(AdpWorkersClient))) throw new InvalidOperationException("ADP registration already exists. Use separate service providers for separate tenants.");
        var acquisitionTimeout = tokenAcquisitionTimeout ?? DefaultTokenAcquisitionTimeout;
        services.AddHttpClient("Koras.Adp.OAuth").RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => Handler(certificate));
        services.AddHttpClient("Koras.Adp.Api").RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => Handler(certificate));
        services.AddSingleton(sp => new AdpRotatingTokenSource(credentials,
            () => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Adp.OAuth"), acquisitionTimeout));
        services.AddSingleton<IAccessTokenSource>(sp => sp.GetRequiredService<AdpRotatingTokenSource>());
        services.AddSingleton(sp => new AdpWorkersClient(new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Adp.Api"),
            sp.GetRequiredService<IAccessTokenSource>(), transport), workers));
        services.AddSingleton<AdpEntitlementTracker>();
        services.AddSingleton(sp => new AdpConnector(
            AdpConnector.CreateDescriptor(sp.GetRequiredService<AdpEntitlementTracker>()),
            credentials, certificate, sp.GetRequiredService<IAccessTokenSource>(), workers,
            connectivityProbe is null ? null : ct => connectivityProbe(sp.GetRequiredService<AdpWorkersClient>(), ct)));
        services.AddSingleton<IConnector>(sp => sp.GetRequiredService<AdpConnector>());
        return services;
    }

    private static HttpClientHandler Handler(X509Certificate2 certificate)
    { var h = new HttpClientHandler { AllowAutoRedirect = false }; h.ClientCertificates.Add(certificate); return h; }
}

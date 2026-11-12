using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;
namespace Koras.Connect.Adp;

public static class AdpServiceCollectionExtensions
{
    /// <summary>Registers isolated mTLS token/API clients. Caller owns and retains certificate until service provider disposal.</summary>
    public static IServiceCollection AddKorasAdp(this IServiceCollection services, ClientCredentialsOptions credentials,
        AdpWorkersOptions workers, X509Certificate2 certificate, TransportOptions? transport = null)
    {
        credentials.Validate(); workers.Validate();
        if (!certificate.HasPrivateKey) throw new ArgumentException("Certificate requires a private key.", nameof(certificate));
        if (services.Any(d => d.ServiceType == typeof(AdpWorkersClient))) throw new InvalidOperationException("ADP registration already exists. Use separate service providers for separate tenants.");
        services.AddHttpClient("Koras.Adp.OAuth").RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => Handler(certificate));
        services.AddHttpClient("Koras.Adp.Api").RemoveAllLoggers().ConfigurePrimaryHttpMessageHandler(() => Handler(certificate));
        services.AddSingleton(sp => new ClientCredentialsTokenSource(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Adp.OAuth"), credentials));
        services.AddSingleton(sp => new AdpWorkersClient(new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Adp.Api"),
            sp.GetRequiredService<ClientCredentialsTokenSource>(), transport), workers));
        return services;
    }
    private static HttpClientHandler Handler(X509Certificate2 certificate)
    { var h = new HttpClientHandler { AllowAutoRedirect = false }; h.ClientCertificates.Add(certificate); return h; }
}

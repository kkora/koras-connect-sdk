using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>App-only connection settings for a single Entra tenant/app registration. Exactly one of
/// <see cref="ClientSecret"/>/<see cref="CertificateSource"/> must be supplied.</summary>
public sealed record GraphAppOnlyConnectionOptions(
    string TenantId,
    string ClientId,
    string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null,
    bool UseBetaEndpoint = false);

public static class GraphServiceCollectionExtensions
{
    /// <summary>Registers a single-tenant, app-only <see cref="GraphClient"/> plus its token source behind named
    /// <see cref="IHttpClientFactory"/> clients. All configuration is validated synchronously, before any
    /// <see cref="HttpClient"/> or handler is created.</summary>
    public static IServiceCollection AddKorasMicrosoftGraph(this IServiceCollection services, GraphAppOnlyConnectionOptions options, TransportOptions? transport = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        if (services.Any(d => d.ServiceType == typeof(GraphClient)))
            throw new InvalidOperationException("Microsoft Graph registration already exists. Use separate service providers for separate tenants.");
        var credentials = new GraphClientCredentialsOptions(options.TenantId, options.ClientId, options.ClientSecret, options.CertificateSource);
        credentials.Validate(); // fail fast - before any HttpClient/handler is created
        var clientOptions = new GraphOptions(options.UseBetaEndpoint);
        services.AddHttpClient("Koras.MicrosoftGraph.OAuth").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddHttpClient("Koras.MicrosoftGraph.Api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton(sp =>
        {
            var oauthHttp = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.MicrosoftGraph.OAuth");
            return GraphAuthentication.CreateClientCredentialsTokenSource(oauthHttp, credentials);
        });
        services.AddSingleton(sp => new GraphClient(
            new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.MicrosoftGraph.Api"),
                sp.GetRequiredService<IAccessTokenSource>(), transport), clientOptions));
        services.AddSingleton<IConnector>(sp => sp.GetRequiredService<GraphClient>());
        return services;
    }
}

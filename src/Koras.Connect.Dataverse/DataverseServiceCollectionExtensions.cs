using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Koras.Connect.Dataverse;

/// <summary>Connection settings for a single Dataverse environment: the environment root plus Microsoft Entra ID
/// app-only credentials. Exactly one of <see cref="ClientSecret"/>/<see cref="CertificateSource"/> must be
/// supplied - see <see cref="DataverseClientCredentialsOptions"/> for the full validation rule.</summary>
public sealed record DataverseConnectionOptions(
    Uri EnvironmentRoot,
    Uri TenantTokenEndpoint,
    string ClientId,
    string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null,
    int MaxPageSize = 5000);

public static class DataverseServiceCollectionExtensions
{
    /// <summary>Registers a single-environment <see cref="DataverseClient"/> plus its token source behind named
    /// <see cref="IHttpClientFactory"/> clients. All configuration is validated synchronously, before any
    /// <see cref="HttpClient"/> or handler is created. Call once per environment per service provider; register a
    /// second environment in a separate service provider/host.</summary>
    public static IServiceCollection AddKorasDataverse(this IServiceCollection services, DataverseConnectionOptions options, TransportOptions? transport = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        if (services.Any(d => d.ServiceType == typeof(DataverseClient)))
            throw new InvalidOperationException("Dataverse registration already exists. Use separate service providers for separate environments.");
        var clientOptions = new DataverseOptions(options.EnvironmentRoot, options.MaxPageSize);
        DataverseClient.ValidateOptions(clientOptions); // fail fast - before any HttpClient/handler is created
        var credentialOptions = new DataverseClientCredentialsOptions(
            options.TenantTokenEndpoint, options.ClientId, options.EnvironmentRoot.AbsoluteUri, options.ClientSecret, options.CertificateSource);
        credentialOptions.Validate();
        services.AddHttpClient("Koras.Dataverse.OAuth").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddHttpClient("Koras.Dataverse.Api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton(sp => DataverseAuthentication.CreateTokenSource(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Dataverse.OAuth"), credentialOptions));
        services.AddSingleton(sp => new DataverseClient(
            new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Dataverse.Api"),
                sp.GetRequiredService<IAccessTokenSource>(), transport), clientOptions));
        services.AddSingleton<IConnector>(sp => sp.GetRequiredService<DataverseClient>());
        return services;
    }
}

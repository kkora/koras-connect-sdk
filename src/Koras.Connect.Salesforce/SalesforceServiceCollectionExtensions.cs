using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Koras.Connect.Salesforce;

/// <summary>Connection settings for a single Salesforce org: the resolved instance root plus either Client
/// Credentials or JWT Bearer flow settings. Exactly one of <see cref="ClientSecret"/>/<see cref="CertificateSource"/>
/// must be supplied.</summary>
public sealed record SalesforceConnectionOptions(
    Uri InstanceRoot,
    Uri TokenEndpoint,
    string ClientId,
    string? ClientSecret = null,
    string? JwtSubject = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null,
    string ApiVersion = "v68.0");

public static class SalesforceServiceCollectionExtensions
{
    /// <summary>Registers a single-org <see cref="SalesforceClient"/> plus its token source behind named
    /// <see cref="IHttpClientFactory"/> clients. All configuration is validated synchronously, before any
    /// <see cref="HttpClient"/> or handler is created.</summary>
    public static IServiceCollection AddKorasSalesforce(this IServiceCollection services, SalesforceConnectionOptions options, TransportOptions? transport = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        if (services.Any(d => d.ServiceType == typeof(SalesforceClient)))
            throw new InvalidOperationException("Salesforce registration already exists. Use separate service providers for separate orgs.");
        var hasSecret = !string.IsNullOrWhiteSpace(options.ClientSecret);
        var hasCertificate = options.CertificateSource is not null;
        if (hasSecret == hasCertificate) throw new ArgumentException("Exactly one of ClientSecret or CertificateSource must be supplied.", nameof(options));
        var clientOptions = new SalesforceOptions(options.InstanceRoot, options.ApiVersion);
        SalesforceClient.ValidateOptions(clientOptions); // fail fast - before any HttpClient/handler is created
        services.AddHttpClient("Koras.Salesforce.OAuth").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddHttpClient("Koras.Salesforce.Api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton(sp =>
        {
            var oauthHttp = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Salesforce.OAuth");
            if (hasSecret)
                return SalesforceAuthentication.CreateClientCredentialsTokenSource(oauthHttp,
                    new SalesforceClientCredentialsOptions(options.TokenEndpoint, options.ClientId, options.ClientSecret!));
            if (string.IsNullOrWhiteSpace(options.JwtSubject)) throw new ArgumentException("JwtSubject is required for the JWT Bearer flow.", nameof(options));
            return (IAccessTokenSource)new SalesforceJwtBearerTokenSource(oauthHttp,
                new SalesforceJwtBearerOptions(options.TokenEndpoint, options.ClientId, options.JwtSubject, options.CertificateSource!));
        });
        services.AddSingleton(sp => new SalesforceClient(
            new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Salesforce.Api"),
                sp.GetRequiredService<IAccessTokenSource>(), transport), clientOptions));
        services.AddSingleton<IConnector>(sp => sp.GetRequiredService<SalesforceClient>());
        return services;
    }
}

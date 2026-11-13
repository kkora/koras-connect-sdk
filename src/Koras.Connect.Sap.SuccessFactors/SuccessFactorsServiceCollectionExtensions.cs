using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>Connection settings for a single SuccessFactors tenant: the OAuth SAML2-bearer token endpoint plus the
/// OData V2 read/write root. The SDK never generates, signs or caches SAML assertions on disk -
/// <see cref="AssertionSource"/> must return a freshly minted, host-issued Base64 SAML2 assertion on every call.</summary>
public sealed record SuccessFactorsConnectionOptions(
    Uri ODataRoot,
    Uri TokenEndpoint,
    string CompanyId,
    string ClientId,
    Func<CancellationToken, ValueTask<string>> AssertionSource,
    int PageSize = 100,
    int MaxPages = 10000);

public static class SuccessFactorsServiceCollectionExtensions
{
    /// <summary>Registers a single-tenant <see cref="SuccessFactorsClient"/> plus its <see cref="SamlBearerTokenSource"/>
    /// behind named <see cref="IHttpClientFactory"/> clients. All configuration is validated synchronously, before
    /// any <see cref="HttpClient"/> or handler is created, so an unsafe OData root or a missing assertion source is
    /// rejected before any HTTP attempt. Call once per tenant per service provider; register a second tenant in a
    /// separate service provider/host.</summary>
    public static IServiceCollection AddKorasSuccessFactors(this IServiceCollection services, SuccessFactorsConnectionOptions options, TransportOptions? transport = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AssertionSource);
        if (services.Any(d => d.ServiceType == typeof(SuccessFactorsClient)))
            throw new InvalidOperationException("SuccessFactors registration already exists. Use separate service providers for separate tenants.");
        var clientOptions = new SuccessFactorsOptions(options.ODataRoot, options.PageSize, options.MaxPages);
        SuccessFactorsClient.ValidateOptions(clientOptions); // fail fast - before any HttpClient/handler is created
        services.AddHttpClient("Koras.SuccessFactors.OAuth").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddHttpClient("Koras.SuccessFactors.Api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton(sp => new SamlBearerTokenSource(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.SuccessFactors.OAuth"),
            options.TokenEndpoint, options.CompanyId, options.ClientId, options.AssertionSource));
        services.AddSingleton(sp => new SuccessFactorsClient(
            new IntegrationTransport(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.SuccessFactors.Api"),
                sp.GetRequiredService<SamlBearerTokenSource>(), transport), clientOptions));
        return services;
    }
}

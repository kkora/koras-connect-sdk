using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace Koras.Connect.Qualtrics;

/// <summary>Mutable configuration surface for <see cref="QualtricsServiceCollectionExtensions.AddKorasQualtrics"/>.
/// Converted to the immutable, validated <see cref="QualtricsOptions"/> record once populated by the caller's
/// <c>configure</c> delegate.</summary>
public sealed class QualtricsOptionsBuilder
{
    public Uri? ApiRoot { get; set; }
    public int MaxPages { get; set; } = 10000;

    internal QualtricsOptions Build()
    {
        if (ApiRoot is null) throw new ArgumentException("ApiRoot is required.", nameof(ApiRoot));
        return new QualtricsOptions(ApiRoot, MaxPages);
    }
}

/// <summary>Dependency-injection registration for an independent Qualtrics API v3 client, mirroring the pattern in
/// <c>AdpServiceCollectionExtensions.AddKorasAdp</c>: a named <see cref="HttpClient"/> from <see cref="IHttpClientFactory"/>
/// with default logging handlers removed, validated configuration rejected before any HTTP client is built, and a
/// duplicate registration refused outright rather than silently sharing state across tenants.</summary>
public static class QualtricsServiceCollectionExtensions
{
    /// <summary>Registers a singleton <see cref="QualtricsClient"/>. The caller-selected <paramref name="tokenSourceFactory"/>
    /// supplies and owns token rotation; this extension never infers a data center or product entitlement and never
    /// contacts the network itself.</summary>
    public static IServiceCollection AddKorasQualtrics(this IServiceCollection services, Action<QualtricsOptionsBuilder> configure,
        Func<IServiceProvider, IAccessTokenSource> tokenSourceFactory, TransportOptions? transport = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(tokenSourceFactory);
        var builder = new QualtricsOptionsBuilder();
        configure(builder);
        var options = builder.Build();
        options.Validate();
        if (services.Any(d => d.ServiceType == typeof(QualtricsClient)))
            throw new InvalidOperationException("Qualtrics registration already exists. Use separate service providers for separate tenants.");
        services.AddHttpClient("Koras.Qualtrics.Api").RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton(sp => QualtricsClient.Create(sp.GetRequiredService<IHttpClientFactory>().CreateClient("Koras.Qualtrics.Api"),
            tokenSourceFactory(sp), options, transport));
        return services;
    }
}

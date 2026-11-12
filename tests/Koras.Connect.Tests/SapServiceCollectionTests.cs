using Koras.Connect.Sap.SuccessFactors;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class SapServiceCollectionTests
{
    private static SuccessFactorsConnectionOptions ValidOptions() => new(
        ODataRoot: new Uri("https://sap.test/odata/v2/"),
        TokenEndpoint: new Uri("https://sap.test/oauth/token"),
        CompanyId: "synthetic-company",
        ClientId: "synthetic-client",
        AssertionSource: ct => ValueTask.FromResult("synthetic-assertion"));

    [Fact] public void DuplicateRegistrationIsRejected()
    {
        var services = new ServiceCollection();
        services.AddKorasSuccessFactors(ValidOptions());
        Assert.Throws<InvalidOperationException>(() => services.AddKorasSuccessFactors(ValidOptions()));
    }

    [Fact] public void UnsafeRootIsRejectedBeforeAnyHttpAttemptOrRegistration()
    {
        var services = new ServiceCollection();
        var bad = ValidOptions() with { ODataRoot = new Uri("http://sap.test/odata/v2/") }; // not https
        Assert.Throws<ArgumentException>(() => services.AddKorasSuccessFactors(bad));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(SuccessFactorsClient));
    }

    [Fact] public void MissingAssertionSourceThrowsBeforeAnyRegistration()
    {
        var services = new ServiceCollection();
        var bad = ValidOptions() with { AssertionSource = null! };
        Assert.Throws<ArgumentNullException>(() => services.AddKorasSuccessFactors(bad));
        Assert.Empty(services);
    }

    [Fact] public void ValidRegistrationResolvesClientAndTokenSourceWithoutAnyHttpCall()
    {
        var services = new ServiceCollection();
        services.AddKorasSuccessFactors(ValidOptions());
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<SuccessFactorsClient>());
        Assert.NotNull(provider.GetRequiredService<SamlBearerTokenSource>());
    }
}

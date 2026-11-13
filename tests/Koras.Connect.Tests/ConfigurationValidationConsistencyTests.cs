using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Qualtrics;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-014's acceptance criterion "DI and direct construction enforce the same rules" had never been
/// proven empirically for any provider - only by code inspection. Writing this test for Qualtrics surfaced a real
/// discrepancy: <see cref="QualtricsClient"/>'s constructor re-implemented its own, weaker validation (a bare
/// <see cref="ArgumentException"/> with a generic message) instead of calling the same <see cref="QualtricsOptions.Validate"/>
/// the DI extension calls, so a host using <see cref="QualtricsClient.Create"/> directly got a different exception
/// type and no per-field detail compared to going through <see cref="QualtricsServiceCollectionExtensions.AddKorasQualtrics"/>
/// for the identical bad configuration. Fixed by making the constructor delegate to <c>Validate()</c> like ADP's
/// <c>AdpWorkersClient</c> and SAP's <c>SuccessFactorsClient</c> already did.</summary>
public sealed class ConfigurationValidationConsistencyTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }

    [Fact]
    public void QualtricsRejectsAnUnsafeRootIdenticallyThroughDirectConstructionAndDependencyInjection()
    {
        var badRoot = new Uri("http://qualtrics.test/API/v3/"); // not https

        var direct = Assert.Throws<OptionsValidationException>(() =>
            QualtricsClient.Create(new HttpClient(), new Token(), new QualtricsOptions(badRoot)));

        var viaDi = Assert.Throws<OptionsValidationException>(() =>
            new ServiceCollection().AddKorasQualtrics(o => o.ApiRoot = badRoot, _ => new Token()));

        Assert.Equal(direct.Message, viaDi.Message);
        Assert.Equal(direct.Failures, viaDi.Failures);
    }

    [Fact]
    public void SapRejectsAnUnsafeRootIdenticallyThroughDirectConstructionAndDependencyInjection()
    {
        var badOptions = new SuccessFactorsOptions(new Uri("http://sap.test/odata/v2/"), 100, 1000); // not https

        var direct = Assert.Throws<ArgumentException>(() => new SuccessFactorsClient(
            new Koras.Connect.Transport.IntegrationTransport(new HttpClient(), new Token()), badOptions));

        var viaDi = Assert.Throws<ArgumentException>(() => SuccessFactorsClient.ValidateOptions(badOptions));

        Assert.Equal(direct.Message, viaDi.Message);
    }

    [Fact]
    public void AdpRejectsInvalidWorkersOptionsIdenticallyThroughDirectConstructionAndDependencyInjection()
    {
        var badOptions = new Koras.Connect.Adp.Workers.AdpWorkersOptions(new Uri("http://api.test")); // not https

        var direct = Assert.Throws<ArgumentException>(() => new Koras.Connect.Adp.Workers.AdpWorkersClient(
            new Koras.Connect.Transport.IntegrationTransport(new HttpClient(), new Token()), badOptions));

        var viaDi = Assert.Throws<ArgumentException>(badOptions.Validate);

        Assert.Equal(direct.Message, viaDi.Message);
    }
}

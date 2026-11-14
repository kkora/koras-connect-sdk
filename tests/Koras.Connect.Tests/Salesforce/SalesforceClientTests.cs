using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-003: REST API client foundation - base URL composition, required headers, resource-map parsing,
/// and the DiagnoseAsync wiring.</summary>
public sealed class SalesforceClientTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    [Theory]
    [InlineData("http://contoso.my.salesforce.com/")]
    [InlineData("https://user:pass@contoso.my.salesforce.com/")]
    public void NonHttpsOrCredentialBearingRootIsRejectedAtConstruction(string root)
        => Assert.ThrowsAny<Exception>(() => new SalesforceOptions(new Uri(root)).Validate());

    [Fact]
    public void InvalidApiVersionIsRejected()
        => Assert.Throws<OptionsValidationException>(() => new SalesforceOptions(InstanceRoot, "not-a-version").Validate());

    [Fact]
    public async Task BaseUrlIsComposedWithTheConfiguredApiVersion()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"sobjects":"/services/data/v68.0/sobjects"}""")); });
        await client.GetResourceMapAsync();
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task ResourceMapParsesDocumentedKeys()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"sobjects":"/services/data/v68.0/sobjects","query":"/services/data/v68.0/query","limits":"/services/data/v68.0/limits"}""")));
        var map = await client.GetResourceMapAsync();
        Assert.Equal("/services/data/v68.0/sobjects", map["sobjects"]);
        Assert.Equal("/services/data/v68.0/query", map["query"]);
    }

    [Fact]
    public async Task EveryRequestCarriesABearerAuthorizationHeader()
    {
        string? scheme = null;
        var client = Client((r, _) => { scheme = r.Headers.Authorization?.Scheme; return Task.FromResult(Json("{}")); });
        await client.GetResourceMapAsync();
        Assert.Equal("Bearer", scheme);
    }

    [Fact]
    public async Task UnauthorizedResponseClassifiesAsSalesforceApiException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.GetResourceMapAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsAllThreeStagesPassedOnSuccess()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(3, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticOutcome.Passed, d.Outcome));
    }

    [Fact]
    public async Task DiagnoseAsyncReportsAuthenticationFailedAndSkipsConnectivityOn401()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Failed, diagnostics.Single(d => d.Stage == DiagnosticStage.Authentication).Outcome);
        Assert.Equal(DiagnosticOutcome.Skipped, diagnostics.Single(d => d.Stage == DiagnosticStage.Connectivity).Outcome);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsConnectivityFailedOnNonAuthenticationFailure()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.ServiceUnavailable)));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Passed, diagnostics.Single(d => d.Stage == DiagnosticStage.Authentication).Outcome);
        Assert.Equal(DiagnosticOutcome.Failed, diagnostics.Single(d => d.Stage == DiagnosticStage.Connectivity).Outcome);
    }

    [Fact]
    public void DescriptorIdentifiesSalesforceAtTheConfiguredApiVersion()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        Assert.Equal("Salesforce", client.Descriptor.Provider);
        Assert.Equal("v68.0", client.Descriptor.ApiVersion);
    }
}

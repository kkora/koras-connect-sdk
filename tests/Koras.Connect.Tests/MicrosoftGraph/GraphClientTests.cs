using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-003: Graph HTTP client - v1.0/beta version-root selection and the dual-casing error-body model.</summary>
public sealed class GraphClientTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }

    [Fact]
    public void DefaultOptionsTargetTheGaV1Root()
    {
        var client = new GraphClient(new IntegrationTransport(new HttpClient(new Handler((_, _) => throw new InvalidOperationException())), new StaticToken("token")));
        Assert.Equal("v1.0", client.Descriptor.ApiVersion);
    }

    [Fact]
    public void BetaOptInTargetsTheBetaRoot()
    {
        var client = new GraphClient(new IntegrationTransport(new HttpClient(new Handler((_, _) => throw new InvalidOperationException())), new StaticToken("token")), new GraphOptions(UseBetaEndpoint: true));
        Assert.Equal("beta", client.Descriptor.ApiVersion);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsConfigurationPassedAndProbesOrganization()
    {
        var client = new GraphClient(new IntegrationTransport(new HttpClient(new Handler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":[{"id":"org1","displayName":"Contoso","verifiedDomains":[]}]}""", Encoding.UTF8, "application/json") }))),
            new StaticToken("token")));
        var results = await client.DiagnoseAsync();
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Configuration && r.Outcome == DiagnosticOutcome.Passed);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Authentication && r.Outcome == DiagnosticOutcome.Passed);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Connectivity && r.Outcome == DiagnosticOutcome.Passed);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Permissions && r.Outcome == DiagnosticOutcome.Passed);
    }

    [Fact]
    public async Task DiagnoseAsyncSkipsConnectivityAndPermissionsAfterAnAuthenticationFailure()
    {
        var client = new GraphClient(new IntegrationTransport(new HttpClient(new Handler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("{}", Encoding.UTF8, "application/json") }))),
            new StaticToken("token")));
        var results = await client.DiagnoseAsync();
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Authentication && r.Outcome == DiagnosticOutcome.Failed);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Connectivity && r.Outcome == DiagnosticOutcome.Skipped);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Permissions && r.Outcome == DiagnosticOutcome.Skipped);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsPermissionsFailedWhenGrantListingFails()
    {
        var calls = 0;
        var client = new GraphClient(new IntegrationTransport(new HttpClient(new Handler((r, _) =>
        {
            calls++;
            if (r.RequestUri!.AbsolutePath.Contains("oauth2PermissionGrants"))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":[{"id":"org1"}]}""", Encoding.UTF8, "application/json") });
        })), new StaticToken("token")));
        var results = await client.DiagnoseAsync();
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Connectivity && r.Outcome == DiagnosticOutcome.Passed);
        Assert.Contains(results, r => r.Stage == DiagnosticStage.Permissions && r.Outcome == DiagnosticOutcome.Failed);
    }

    [Theory]
    [InlineData("""{"error":{"code":"InvalidAuthenticationToken","message":"secret stuff","innerError":{"request-id":"abc"}}}""")]
    [InlineData("""{"error":{"code":"InvalidAuthenticationToken","message":"secret stuff","innererror":{"request-id":"abc"}}}""")]
    public void TryExtractErrorCodeAcceptsBothInnerErrorCasings(string body)
    {
        using var document = JsonDocument.Parse(body);
        var code = GraphApiException.TryExtractErrorCode(document.RootElement);
        Assert.Equal("InvalidAuthenticationToken", code);
    }

    [Fact]
    public void TryExtractErrorCodeReturnsNullForAMalformedBody()
    {
        using var document = JsonDocument.Parse("""{"notAnError":true}""");
        Assert.Null(GraphApiException.TryExtractErrorCode(document.RootElement));
    }

    [Fact]
    public void FromTransportFailureClassifiesByHttpStatus()
    {
        var failure = new IntegrationRequestException(HttpStatusCode.Forbidden, 1);
        var ex = GraphApiException.FromTransportFailure(failure);
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.IsType<Koras.Connect.Exceptions.AuthorizationException>(ex.InnerException);
    }
}

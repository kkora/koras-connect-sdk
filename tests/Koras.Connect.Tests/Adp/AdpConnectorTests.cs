using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-012 (staged, read-only connection diagnostics) and ADP-021 (per-operation entitlement state).</summary>
public sealed class AdpConnectorTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private sealed class SlowHandler : HttpMessageHandler
    { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return new(HttpStatusCode.OK); } }
    private sealed class FaultingHandler : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => throw new HttpRequestException("DNS resolution failed."); }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }

    private static X509Certificate2 SelfSignedCertificate(TimeSpan notBeforeOffset, TimeSpan notAfterOffset)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.Add(notBeforeOffset), DateTimeOffset.UtcNow.Add(notAfterOffset));
    }

    private static ClientCredentialsOptions ValidCredentials => new(new Uri("https://auth.test"), "id", "secret");

    [Fact]
    public async Task ExpiredCertificateShortCircuitsBeforeAnyNetworkCall()
    {
        var certificate = SelfSignedCertificate(TimeSpan.FromDays(-10), TimeSpan.FromDays(-1)); // already expired
        var tokenSource = new ThrowingTokenSource();
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource);

        var results = await connector.DiagnoseAsync();

        Assert.Single(results);
        Assert.Equal(DiagnosticStage.Configuration, results[0].Stage);
        Assert.Equal(DiagnosticOutcome.Failed, results[0].Outcome);
        Assert.False(tokenSource.WasCalled); // no network call was attempted
        certificate.Dispose();
    }

    [Fact]
    public async Task MissingCredentialsShortCircuitBeforeAnyNetworkCall()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        var invalidCredentials = new ClientCredentialsOptions(new Uri("https://auth.test"), "", "");
        var tokenSource = new ThrowingTokenSource();
        var descriptor = AdpConnector.CreateDescriptor();
        // Bypass the convenience constructor's own validation by supplying the invalid value through a provider,
        // so DiagnoseAsync itself is what rejects it.
        var connector = new AdpConnector(descriptor, new StaticCredentialProvider<ClientCredentialsOptions>(invalidCredentials), certificate, tokenSource);

        var results = await connector.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Failed, Assert.Single(results).Outcome);
        Assert.False(tokenSource.WasCalled);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticationStageFailsOnUnauthorizedOrForbiddenWithoutReachingConnectivity(HttpStatusCode status)
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        using var http = new HttpClient(new Handler(_ => new(status)));
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials);
        var probeCalled = false;
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource,
            connectivityProbe: _ => { probeCalled = true; return Task.CompletedTask; });

        var results = await connector.DiagnoseAsync();

        Assert.Equal(2, results.Count);
        Assert.Equal(DiagnosticOutcome.Passed, results[0].Outcome);
        Assert.Equal(DiagnosticStage.Authentication, results[1].Stage);
        Assert.Equal(DiagnosticOutcome.Failed, results[1].Outcome);
        Assert.False(probeCalled);
    }

    [Fact]
    public async Task TimeoutOrDnsFaultAtAuthenticationIsReportedAsAFailedStageNotAnUnhandledException()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        using var http = new HttpClient(new FaultingHandler());
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials, acquisitionTimeout: TimeSpan.FromSeconds(5));
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource);

        var results = await connector.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Failed, results[^1].Outcome);
        Assert.Equal(DiagnosticStage.Authentication, results[^1].Stage);
    }

    [Fact]
    public async Task CancellationMidProbePropagatesRatherThanBeingSwallowedAsAFailure()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") }));
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials);
        using var cts = new CancellationTokenSource();
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource,
            connectivityProbe: async ct => { cts.Cancel(); await Task.Delay(Timeout.InfiniteTimeSpan, ct); });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connector.DiagnoseAsync(cts.Token).AsTask());
    }

    [Fact]
    public async Task MalformedConnectivityProbeResponseIsReportedAsAFailedConnectivityStage()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") }));
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials);
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource,
            connectivityProbe: _ => throw new System.Text.Json.JsonException("Malformed response."));

        var results = await connector.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Failed, results[^1].Outcome);
        Assert.Equal(DiagnosticStage.Connectivity, results[^1].Stage);
    }

    [Fact]
    public async Task SuccessfulThreeStageDiagnosisNeverIssuesANonGetRequest()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        var methods = new List<HttpMethod>();
        using var http = new HttpClient(new Handler(r =>
        {
            methods.Add(r.Method);
            return r.Method == HttpMethod.Post
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }));
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials);
        var transport = new IntegrationTransport(http, tokenSource);
        var workers = new AdpWorkersClient(transport, new AdpWorkersOptions(new Uri("https://api.test")));
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource,
            connectivityProbe: ct => workers.GetMetadataAsync(ct));

        var results = await connector.DiagnoseAsync();

        Assert.All(results, r => Assert.Equal(DiagnosticOutcome.Passed, r.Outcome));
        Assert.Equal(3, results.Count);
        // The only non-GET request across the whole diagnosis is the inherent OAuth token POST; everything the
        // connector itself drives (the connectivity stage) is a GET.
        Assert.All(methods.Where(m => m != HttpMethod.Post), m => Assert.Equal(HttpMethod.Get, m));
        Assert.Contains(HttpMethod.Get, methods);
    }

    [Fact]
    public async Task ConnectivityProbeNotConfiguredYieldsASkippedStageNotAFailure()
    {
        using var certificate = SelfSignedCertificate(TimeSpan.FromDays(-1), TimeSpan.FromDays(1));
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"t","expires_in":3600}""") }));
        using var tokenSource = new ClientCredentialsTokenSource(http, ValidCredentials);
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(), ValidCredentials, certificate, tokenSource);

        var results = await connector.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Skipped, results[^1].Outcome);
        Assert.Equal(DiagnosticStage.Connectivity, results[^1].Stage);
    }

    [Fact]
    public void EveryAdpOperationHasExactlyOneEntitlementEntry()
    {
        var tracker = new AdpEntitlementTracker();
        var snapshot = tracker.ToDescriptorEntitlements();
        var allOperations = Enum.GetValues<AdpOperation>();

        Assert.Equal(allOperations.Length, snapshot.Count);
        foreach (var operation in allOperations)
        {
            Assert.True(snapshot.ContainsKey(operation.ToString()));
            Assert.Equal(EntitlementState.Unknown, tracker.Get(operation).State);
        }
    }

    [Fact]
    public async Task RecordedDeniedEntitlementShortCircuitsBeforeAnyNetworkCall()
    {
        var tracker = new AdpEntitlementTracker();
        tracker.Record(AdpOperation.PayrollInstructions, EntitlementState.Denied, reference: "tenant-grant-check-2026-11-12");
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("No network call was expected.")));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.PayrollInstructions], tracker);

        var ex = await Assert.ThrowsAsync<Koras.Connect.Exceptions.AuthorizationException>(() => client.ReadAsync(AdpOperation.PayrollInstructions, "w1"));
        Assert.Equal("entitlement_denied", ex.Details.Code);
    }

    [Fact]
    public void DescriptorSurfacesEntitlementEvidenceFromTheSharedTracker()
    {
        var tracker = new AdpEntitlementTracker();
        tracker.Record(AdpOperation.Hire, EntitlementState.Verified, reference: "sandbox-read-2026-11-12");
        var descriptor = AdpConnector.CreateDescriptor(tracker);

        Assert.Equal("ADP", descriptor.Provider);
        Assert.Equal(EntitlementState.Verified, descriptor.Entitlements[nameof(AdpOperation.Hire)].State);
        Assert.Equal(EntitlementState.Unknown, descriptor.Entitlements[nameof(AdpOperation.Rehire)].State);
        Assert.Contains(nameof(AdpOperation.OrganizationDepartments), descriptor.SupportedOperations);
    }

    private sealed class ThrowingTokenSource : IAccessTokenSource
    {
        public bool WasCalled { get; private set; }
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
        { WasCalled = true; throw new InvalidOperationException("Stage 1 should have short-circuited before this was ever called."); }
    }
}

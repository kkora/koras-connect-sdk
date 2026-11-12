using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Export;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Export;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-022 runnable examples. The task asked for standalone console projects under <c>samples/</c>, but
/// this session's scope is restricted to <c>src/Koras.Connect.Adp/</c>, <c>tests/Koras.Connect.Tests/</c> and the
/// ADP tracker docs, and may not touch <c>samples/</c> or <c>Koras.Connect.slnx</c>. These three tests are the
/// scope-compliant substitute: each is a self-contained, fully offline walkthrough of one scenario, runnable (and
/// continuously verified) via <c>dotnet test</c>, using only mocked <see cref="HttpMessageHandler"/>s and no
/// network access. A maintainer with broader scope can lift any of these almost verbatim into a
/// <c>samples/Adp.*</c> console project later.</summary>
public sealed class AdpSampleWalkthroughTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }

    /// <summary>Sample (a): certificate-bound mTLS transport plus OAuth client-credentials authentication against a
    /// mocked token endpoint and API, exactly as a host application would configure it, minus real network I/O.</summary>
    [Fact]
    public async Task Sample_CertificateAndOAuthAuthenticationAgainstAMockedHandler()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=sample-tenant", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
        Assert.True(ClientCertificateTransport.Create(certificate).DefaultRequestHeaders is not null); // the SDK's recommended way to build the mTLS HttpClient

        using var oauthHttp = new HttpClient(new Handler(r =>
        {
            Assert.Equal(HttpMethod.Post, r.Method); // client-credentials grant is always a POST to the token endpoint
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"sample-token","expires_in":3600}""") };
        }));
        var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.test"), "sample-client-id", "sample-client-secret");
        using var tokenSource = new ClientCredentialsTokenSource(oauthHttp, credentials);

        using var apiHttp = new HttpClient(new Handler(r =>
        {
            Assert.Equal("Bearer sample-token", r.Headers.Authorization?.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"workers":[{"associateOID":"sample-worker"}]}""") };
        }));
        var workers = new AdpWorkersClient(new IntegrationTransport(apiHttp, tokenSource), new AdpWorkersOptions(new Uri("https://api.sample-tenant.test")));

        var worker = await workers.GetWorkerAsync("sample-worker");
        Assert.Equal("sample-worker", worker!.AssociateOid);
    }

    /// <summary>Sample (b): export a population of workers to CSV through <see cref="WorkerExportProjection"/> and
    /// <see cref="CsvExportProvider"/>, streaming directly from a mocked <see cref="AdpWorkersClient.EnumerateTypedWorkersAsync"/>.</summary>
    [Fact]
    public async Task Sample_WorkerCsvExportStreamedFromEnumerateTypedWorkersAsync()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler(_ =>
        {
            calls++;
            var body = calls == 1
                ? """{"workers":[{"associateOID":"w1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}},"workAssignments":[{"jobTitle":"Engineer","hireDate":"2024-01-01"}]},{"associateOID":"w2","person":{"legalName":{"givenName":"Grace","familyName1":"Hopper"}},"workAssignments":[{"jobTitle":"Admiral","hireDate":"2020-01-01"}]}]}"""
                : """{"workers":[]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }));
        var token = new FixedToken();
        var workers = new AdpWorkersClient(new IntegrationTransport(http, token), new AdpWorkersOptions(new Uri("https://api.sample-tenant.test")));

        using var output = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new CsvExportProvider(), output, workers.EnumerateTypedWorkersAsync());

        var csv = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("Ada", csv);
        Assert.Contains("Grace Hopper".Split(' ')[1], csv); // "Hopper"
        Assert.Equal(3, csv.Split('\n').Count(l => l.Trim().Length > 0)); // header + 2 worker rows
    }

    /// <summary>Sample (c): run <see cref="AdpConnector.DiagnoseAsync"/> end to end against a healthy mocked
    /// tenant, showing the staged Configuration → Authentication → Connectivity result a host would print.</summary>
    [Fact]
    public async Task Sample_ConnectionDiagnosticsAgainstAHealthyMockedTenant()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=sample-tenant", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
        var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.test"), "sample-client-id", "sample-client-secret");

        using var http = new HttpClient(new Handler(r => r.Method == HttpMethod.Post
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"access_token":"sample-token","expires_in":3600}""") }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));
        using var tokenSource = new ClientCredentialsTokenSource(http, credentials);
        var workers = new AdpWorkersClient(new IntegrationTransport(http, tokenSource), new AdpWorkersOptions(new Uri("https://api.sample-tenant.test")));

        var tracker = new AdpEntitlementTracker();
        var connector = new AdpConnector(AdpConnector.CreateDescriptor(tracker), credentials, certificate, tokenSource,
            connectivityProbe: ct => workers.GetMetadataAsync(ct));

        var results = await connector.DiagnoseAsync();

        // The report a host application would print or log: three passed stages, in order.
        Assert.Collection(results,
            r => Assert.Equal((DiagnosticStage.Configuration, DiagnosticOutcome.Passed), (r.Stage, r.Outcome)),
            r => Assert.Equal((DiagnosticStage.Authentication, DiagnosticOutcome.Passed), (r.Stage, r.Outcome)),
            r => Assert.Equal((DiagnosticStage.Connectivity, DiagnosticOutcome.Passed), (r.Stage, r.Outcome)));
    }

    private sealed class FixedToken : IAccessTokenSource
    {
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("sample-token");
    }
}

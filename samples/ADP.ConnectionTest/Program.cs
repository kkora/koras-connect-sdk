using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;

// Offline demonstration of ADP-012: run AdpConnector.DiagnoseAsync's three staged checks (Configuration ->
// Authentication -> Connectivity) and print the outcome of each, the way a host application's own connection-test
// command would. Every HTTP response below is synthetic; this sample never contacts a live ADP tenant.
Console.WriteLine("Koras.Connect.Adp offline sample: AdpConnector.DiagnoseAsync against a mocked tenant.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

Console.WriteLine("Scenario 1: a healthy tenant (all three stages pass).");
await RunScenarioAsync(await BuildHealthyConnectorAsync());

Console.WriteLine();
Console.WriteLine("Scenario 2: an expired client certificate (Configuration fails locally; no network call is ever made).");
await RunScenarioAsync(await BuildExpiredCertificateConnectorAsync());

Console.WriteLine();
Console.WriteLine("Scenario 3: the OAuth endpoint rejects the credentials (Configuration passes; Authentication fails).");
await RunScenarioAsync(await BuildAuthenticationFailureConnectorAsync());

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static async Task RunScenarioAsync(AdpConnector connector)
{
    var results = await connector.DiagnoseAsync();
    foreach (var result in results)
    {
        var detail = result.CorrelationId is null ? string.Empty : $" (correlationId={result.CorrelationId})";
        Console.WriteLine($"  [{result.Stage}] {result.Outcome}: {result.SanitizedCode}{detail}");
        if (result.RecommendedNextStep is not null) Console.WriteLine($"    next step: {result.RecommendedNextStep}");
    }
}

// The certificates/HttpClients built below are intentionally not disposed here: AdpConnector retains them for the
// DiagnoseAsync call that happens after each builder returns. This short-lived console process releases them on exit.
static Task<AdpConnector> BuildHealthyConnectorAsync()
{
    var certificate = CreateSelfSignedCertificate(validFrom: DateTimeOffset.UtcNow.AddDays(-1), validTo: DateTimeOffset.UtcNow.AddDays(365));
    var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", "sample-client-secret");
    var http = new HttpClient(new SyntheticHandler(request => request.Method == HttpMethod.Post
        ? Reply(HttpStatusCode.OK, """{"access_token":"sample-token","expires_in":3600}""")
        : Reply(HttpStatusCode.OK, "{}")));
    var tokenSource = new ClientCredentialsTokenSource(http, credentials);
    var workers = new AdpWorkersClient(new IntegrationTransport(http, tokenSource), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    return Task.FromResult(new AdpConnector(AdpConnector.CreateDescriptor(new AdpEntitlementTracker()), credentials, certificate, tokenSource,
        connectivityProbe: ct => workers.GetMetadataAsync(ct)));
}

static Task<AdpConnector> BuildExpiredCertificateConnectorAsync()
{
    var certificate = CreateSelfSignedCertificate(validFrom: DateTimeOffset.UtcNow.AddDays(-400), validTo: DateTimeOffset.UtcNow.AddDays(-30));
    var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", "sample-client-secret");
    var http = new HttpClient(new SyntheticHandler(_ => throw new InvalidOperationException("Configuration must fail locally before any network call is attempted.")));
    var tokenSource = new ClientCredentialsTokenSource(http, credentials);
    return Task.FromResult(new AdpConnector(AdpConnector.CreateDescriptor(), credentials, certificate, tokenSource));
}

static Task<AdpConnector> BuildAuthenticationFailureConnectorAsync()
{
    var certificate = CreateSelfSignedCertificate(validFrom: DateTimeOffset.UtcNow.AddDays(-1), validTo: DateTimeOffset.UtcNow.AddDays(365));
    var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", "wrong-client-secret");
    var http = new HttpClient(new SyntheticHandler(_ => Reply(HttpStatusCode.Unauthorized, "")));
    var tokenSource = new ClientCredentialsTokenSource(http, credentials);
    return Task.FromResult(new AdpConnector(AdpConnector.CreateDescriptor(), credentials, certificate, tokenSource));
}

static X509Certificate2 CreateSelfSignedCertificate(DateTimeOffset validFrom, DateTimeOffset validTo)
{
    using var rsa = RSA.Create(2048);
    var request = new CertificateRequest("CN=sample-tenant.adp.sample", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    return request.CreateSelfSigned(validFrom, validTo);
}

static HttpResponseMessage Reply(HttpStatusCode status, string body) => new(status) { Content = new StringContent(body) };

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

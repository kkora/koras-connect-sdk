using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;

// Offline demonstration of ADP-001/002/003: load a client certificate the same way a host application would, build
// the OAuth client-credentials token source, then make one authenticated API call. Every HTTP response below is
// synthetic; this sample never contacts a live ADP tenant, never requires an on-disk certificate checked into the
// repo, and requires no network access to run.
Console.WriteLine("Koras.Connect.Adp offline sample: certificate loading -> OAuth client-credentials -> one authenticated worker read.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

// Step 1: produce a disposable, password-protected PKCS#12 bundle on disk (standing in for a certificate a host
// would already have provisioned) and load it with the SDK's recommended ClientCertificateTransport.LoadPkcs12 --
// the loading path the SDK documents as avoiding the Windows key-store persistence side effect of LoadPem.
var pkcs12Path = Path.Combine(Path.GetTempPath(), $"adp-sample-{Guid.NewGuid():N}.pfx");
const string pkcs12Password = "sample-password";
try
{
    using (var rsa = RSA.Create(2048))
    {
        var request = new CertificateRequest("CN=sample-tenant.adp.sample", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var selfSigned = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(365));
        await File.WriteAllBytesAsync(pkcs12Path, selfSigned.Export(X509ContentType.Pkcs12, pkcs12Password));
    }

    using var certificate = ClientCertificateTransport.LoadPkcs12(pkcs12Path, pkcs12Password);
    Console.WriteLine($"Loaded client certificate: subject={certificate.Subject}, hasPrivateKey={certificate.HasPrivateKey}.");

    // ClientCertificateTransport.Create is the SDK's recommended way to build the mTLS-bound HttpClient a host would
    // actually send requests with; this sample only inspects it (no live endpoint exists to connect to).
    using var mtlsHttpClient = ClientCertificateTransport.Create(certificate);
    Console.WriteLine($"Built an mTLS-bound HttpClient (timeout={mtlsHttpClient.Timeout}).");
    Console.WriteLine();

    // Step 2: acquire an OAuth client-credentials token against a mocked token endpoint.
    using var oauthHttp = new HttpClient(new SyntheticHandler(request =>
    {
        if (request.Method != HttpMethod.Post) throw new InvalidOperationException("Client-credentials grant is always a POST to the token endpoint.");
        return Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":3600}""");
    }));
    var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", "sample-client-secret");
    using var tokenSource = new ClientCredentialsTokenSource(oauthHttp, credentials);
    var token = await tokenSource.GetTokenAsync();
    Console.WriteLine($"Acquired OAuth token (length={token.Length} characters; the raw value is never printed).");
    Console.WriteLine();

    // Step 3: make exactly one authenticated, minimal API read using that token.
    using var apiHttp = new HttpClient(new SyntheticHandler(request =>
    {
        if (request.Headers.Authorization?.ToString() != "Bearer sample-access-token")
            throw new InvalidOperationException("Expected the acquired token on the Authorization header.");
        return Reply("""{"workers":[{"associateOID":"sample-worker-1"}]}""");
    }));
    var workers = new AdpWorkersClient(new IntegrationTransport(apiHttp, tokenSource), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    var worker = await workers.GetWorkerAsync("sample-worker-1");
    Console.WriteLine($"Authenticated read succeeded: associateOID={worker!.AssociateOid}.");
    Console.WriteLine();
    Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");
}
finally
{
    if (File.Exists(pkcs12Path)) File.Delete(pkcs12Path);
}

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

// Offline demonstration of ADP-003/010/011: credential rotation without recreating dependents, structured error
// classification with correlation ids, the GET-retries/POST-never-retries distinction, and a bounded token
// acquisition timeout that fires independently of a generous outer cancellation token. Every HTTP response below
// is synthetic; this sample never contacts a live ADP tenant.
Console.WriteLine("Koras.Connect.Adp offline sample: credential rotation, structured errors, retry/cancellation bounds.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

Console.WriteLine("Scenario 1: credential rotation without recreating dependents (ADP-003, AdpRotatingTokenSource).");
await RunCredentialRotationAsync();

Console.WriteLine();
Console.WriteLine("Scenario 2: structured error classification with correlation ids (ADP-010, AdpApiException).");
await RunErrorClassificationAsync();

Console.WriteLine();
Console.WriteLine("Scenario 3: reads retry transient failures automatically; writes never do (ADP-011).");
await RunRetryVersusNoRetryAsync();

Console.WriteLine();
Console.WriteLine("Scenario 4: a bounded token-acquisition timeout fires on its own, independent of a generous outer cancellation (ADP-011).");
await RunBoundedAcquisitionTimeoutAsync();

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static async Task RunCredentialRotationAsync()
{
    var secret = "secret-a";
    var secretsUsed = new List<string>();
    using var http = new HttpClient(new SyntheticHandler(request =>
    {
        var parameter = request.Headers.Authorization!.Parameter!;
        secretsUsed.Add(Encoding.UTF8.GetString(Convert.FromBase64String(parameter)).Split(':')[1]);
        return Reply("""{"access_token":"sample-token","expires_in":3600}""");
    }));
    var provider = new DelegatingCredentialProvider<ClientCredentialsOptions>(_ =>
        ValueTask.FromResult(new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", secret)));
    using var tokenSource = new AdpRotatingTokenSource(provider, () => http, TimeSpan.FromSeconds(10));

    await tokenSource.GetTokenAsync();
    secret = "secret-b"; // simulates an external rotation, e.g. a secret-manager refresh the host observes
    await tokenSource.GetTokenAsync();
    Console.WriteLine($"  Reacquired automatically when the resolved credential changed: {string.Join(" -> ", secretsUsed)}.");

    var callsBeforeInvalidate = secretsUsed.Count;
    await tokenSource.GetTokenAsync(); // same credential, still cached - no new call expected
    tokenSource.Invalidate(); // an explicit out-of-band rotation signal, even without a resolved credential change
    await tokenSource.GetTokenAsync();
    Console.WriteLine($"  Invalidate() forced reacquisition without a credential change: {secretsUsed.Count - callsBeforeInvalidate} new call(s) issued.");
}

static async Task RunErrorClassificationAsync()
{
    foreach (var (status, expectedCategory) in new[]
    {
        (HttpStatusCode.Unauthorized, "AuthenticationException"),
        (HttpStatusCode.Forbidden, "AuthorizationException"),
        (HttpStatusCode.NotFound, "ContractException"),
        (HttpStatusCode.TooManyRequests, "ThrottleException"),
        (HttpStatusCode.InternalServerError, "TransportException"),
    })
    {
        using var http = new HttpClient(new SyntheticHandler(_ =>
        {
            var response = new HttpResponseMessage(status);
            response.Headers.TryAddWithoutValidation("request-id", "sample-correlation-id");
            return response;
        }));
        var client = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken(), new TransportOptions { MaxRetries = 0 }),
            new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
        try
        {
            await client.SearchWorkersAsync("associateOID eq 'w1'");
            Console.WriteLine($"  Unexpected: HTTP {(int)status} did not fail.");
        }
        catch (AdpApiException ex)
        {
            Console.WriteLine($"  HTTP {(int)status} classified as {ex.Category} (expected {expectedCategory}); correlationId={ex.Details.CorrelationId}.");
        }
    }
}

static async Task RunRetryVersusNoRetryAsync()
{
    var getAttempts = 0;
    using var getHttp = new HttpClient(new SyntheticHandler(_ => ++getAttempts < 3 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Reply("{}")));
    var getTransport = new IntegrationTransport(getHttp, new FixedToken(), new TransportOptions { BaseDelay = TimeSpan.Zero });
    using var getResult = await getTransport.ReadJsonAsync(new Uri("https://api.sample-tenant.adp.test/hr/v2/workers/meta"));
    Console.WriteLine($"  GET succeeded after {getAttempts} attempt(s): transient 503s are retried automatically.");

    var postAttempts = 0;
    using var postHttp = new HttpClient(new SyntheticHandler(_ => { postAttempts++; return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable); }));
    var client = new AdpOperationsClient(new IntegrationTransport(postHttp, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.Hire]);
    using var payload = JsonDocument.Parse("""{"events":[{"data":{"transform":{}}}]}""");
    try
    {
        await client.SubmitEventAsync(AdpOperation.Hire, payload.RootElement);
        Console.WriteLine("  Unexpected: the write succeeded despite a persistent 503.");
    }
    catch (IntegrationRequestException)
    {
        Console.WriteLine($"  POST failed after exactly {postAttempts} attempt: writes are never automatically retried, to avoid a duplicate submission.");
    }
}

static async Task RunBoundedAcquisitionTimeoutAsync()
{
    using var http = new HttpClient(new SlowHandler());
    var credentials = new ClientCredentialsOptions(new Uri("https://auth.sample-tenant.adp.test"), "sample-client-id", "sample-client-secret");
    using var tokenSource = new ClientCredentialsTokenSource(http, credentials, acquisitionTimeout: TimeSpan.FromMilliseconds(100));

    using var generousOuterToken = new CancellationTokenSource(TimeSpan.FromMinutes(10)); // never fires in this run
    var stopwatch = Stopwatch.StartNew();
    try
    {
        await tokenSource.GetTokenAsync(generousOuterToken.Token);
        Console.WriteLine("  Unexpected: the hanging token endpoint did not time out.");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"  Token acquisition failed after {stopwatch.ElapsedMilliseconds}ms, bounded by its own acquisitionTimeout rather than the 10-minute outer token (which never fired: {!generousOuterToken.IsCancellationRequested}).");
    }
}

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class FixedToken : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("sample-token");
}

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

sealed class SlowHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

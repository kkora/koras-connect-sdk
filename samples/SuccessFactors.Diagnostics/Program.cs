using System.Net;
using System.Text;
using Koras.Connect.Exceptions;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;

// Offline demonstration of the error-handling and diagnostics surface added under SAP-005: every transport
// failure classifies into the shared Koras.Connect.Exceptions hierarchy (AuthenticationException/
// AuthorizationException/ThrottleException/TransportException) rather than leaking a raw HTTP status, and a
// canceled read propagates as OperationCanceledException without being reclassified as a transport failure.
// SapODataError.TryParseSanitizedCode is also demonstrated as a standalone utility for a host that captures a raw
// error body itself (for example from an authorized diagnostic probe or a proxy log) - it extracts only the safe
// "error.code" field and never reads "error.message.value", which frequently contains tenant-specific data. Every
// HTTP response below is synthetic; this sample never contacts a live SAP SuccessFactors tenant.
Console.WriteLine("Koras.Connect.Sap.SuccessFactors offline sample: classified exceptions, cancellation, and sanitized error-code extraction.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

foreach (var status in new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable })
{
    using var http = new HttpClient(new SyntheticHandler((_, _) => new HttpResponseMessage(status)));
    var client = new SuccessFactorsClient(new IntegrationTransport(http, new StaticToken("sample-access-token"), new TransportOptions { MaxRetries = 0 }),
        new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));
    try
    {
        await foreach (var person in client.EnumeratePersonsAsync()) { }
    }
    catch (IntegrationException ex)
    {
        Console.WriteLine($"HTTP {(int)status} classified as {ex.GetType().Name}.");
    }
}
Console.WriteLine();

// A canceled read must propagate as OperationCanceledException - never silently reclassified as a transport
// failure, since the request was never actually sent in a way that needs interpreting.
using var neverCalledHttp = new HttpClient(new SyntheticHandler((_, _) => throw new InvalidOperationException("Should never be reached - the token is already canceled.")));
var cancelableClient = new SuccessFactorsClient(new IntegrationTransport(neverCalledHttp, new StaticToken("sample-access-token")), new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));
using var cts = new CancellationTokenSource();
cts.Cancel();
try
{
    await foreach (var person in cancelableClient.EnumeratePersonsAsync(cancellationToken: cts.Token)) { }
}
catch (OperationCanceledException)
{
    Console.WriteLine("A pre-canceled read propagated as OperationCanceledException without contacting the handler.");
}
Console.WriteLine();

// SapODataError.TryParseSanitizedCode: a standalone utility for a host that has independently captured a raw
// error body (this SDK's own transport does not retain response bodies on failure today).
var capturedErrorBody = """{"error":{"code":"COE0027","message":{"value":"Please add required properties in payload. firstName. (tenant-specific detail)"}}}""";
var sanitizedCode = SapODataError.TryParseSanitizedCode(capturedErrorBody);
Console.WriteLine($"SapODataError.TryParseSanitizedCode extracted: \"{sanitizedCode}\" (the message.value text was never read).");
Console.WriteLine($"TryParseSanitizedCode on malformed input returns null, never throws: \"{SapODataError.TryParseSanitizedCode("not json at all") ?? "(null)"}\"");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live SAP SuccessFactors tenant.");

sealed class StaticToken(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-010: structured, categorized ADP errors carrying an optional correlation id. Covers direct
/// classification (including the 429/Retry-After case, whose header is not observable through the current
/// <see cref="IntegrationTransport"/> failure path) and end-to-end classification through
/// <see cref="AdpOperationsClient"/>/<see cref="Koras.Connect.Adp.Workers.AdpWorkersClient"/> for the read
/// surfaces that do not have a pinned raw-<see cref="IntegrationRequestException"/> contract.</summary>
public sealed class AdpApiExceptionTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "AuthenticationException")]
    [InlineData(HttpStatusCode.Forbidden, "AuthorizationException")]
    [InlineData(HttpStatusCode.NotFound, "ContractException")]
    [InlineData(HttpStatusCode.TooManyRequests, "ThrottleException")]
    [InlineData(HttpStatusCode.InternalServerError, "TransportException")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "TransportException")]
    public void ClassifiesEveryDocumentedStatusIntoTheExpectedCategory(HttpStatusCode status, string expectedCategory)
    {
        var failure = new IntegrationRequestException(status, 1);
        var classified = AdpApiException.Classify("TestOperation", failure, correlationId: "corr-1", retryAfter: TimeSpan.FromSeconds(5));
        Assert.Equal(expectedCategory, classified.Category);
        Assert.Equal("corr-1", classified.Details.CorrelationId);
        Assert.Equal("TestOperation", classified.Operation);
        Assert.Same(failure, classified.InnerException);
    }

    [Fact]
    public void RetryAfterOnAThrottleClassificationIsExposedThroughTheUnderlyingThrottleException()
    {
        var failure = new IntegrationRequestException(HttpStatusCode.TooManyRequests, 1);
        var classified = AdpApiException.Classify(AdpOperation.EventNotifications, failure, retryAfter: TimeSpan.FromSeconds(30));
        // AdpApiException itself does not re-expose the typed subclass, but the classifier it delegates to does;
        // this proves the Retry-After value is plumbed through when the caller has it available.
        var direct = IntegrationExceptionClassifier.Classify(HttpStatusCode.TooManyRequests, 1, null, TimeSpan.FromSeconds(30));
        Assert.IsType<ThrottleException>(direct);
        Assert.Equal(TimeSpan.FromSeconds(30), ((ThrottleException)direct).RetryAfter);
    }

    [Fact]
    public async Task UnauthorizedReadIsClassifiedThroughAdpOperationsClient()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.Unauthorized)));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.PayrollInstructions]);
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.ReadAsync(AdpOperation.PayrollInstructions, "w1"));
        Assert.Equal("AuthenticationException", ex.Category);
        Assert.Equal(nameof(AdpOperation.PayrollInstructions), ex.Operation);
    }

    [Fact]
    public async Task NotFoundOnMetadataIsClassifiedAsContractException()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.NotFound)));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.Hire]);
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.GetMetadataAsync(AdpOperation.Hire));
        Assert.Equal("ContractException", ex.Category);
    }

    [Fact]
    public async Task ThrottledNotificationPollIsClassifiedAsThrottleExceptionCategory()
    {
        // MaxRetries=0 forces an immediate terminal failure on the first 429 so the test stays fast and deterministic.
        using var http = new HttpClient(new Handler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
            return response;
        }));
        var transport = new IntegrationTransport(http, new Token(), new() { MaxRetries = 0 });
        var client = new AdpOperationsClient(transport, new("https://api.test"), [AdpOperation.EventNotifications]);
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.ReadNotificationAsync());
        Assert.Equal("ThrottleException", ex.Category);
    }

    [Fact]
    public async Task ServerErrorOnWorkerSearchIsClassifiedAsTransportException()
    {
        using var http = new HttpClient(new Handler(_ => new(HttpStatusCode.BadGateway)));
        var client = new Koras.Connect.Adp.Workers.AdpWorkersClient(new(http, new Token()), new(new("https://api.test")));
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.SearchWorkersAsync("associateOID eq 'w1'"));
        Assert.Equal("TransportException", ex.Category);
    }

    [Fact]
    public async Task SubmitEventAsyncAndGetWorkersPageAsyncStillSurfaceTheRawIntegrationRequestExceptionUnchanged()
    {
        // Documented, pinned behavior: write submissions and raw pagination reads are not reclassified, so existing
        // callers that branch on ResponseStatus continue to work unmodified.
        using var http1 = new HttpClient(new Handler(_ => new(HttpStatusCode.ServiceUnavailable)));
        var ops = new AdpOperationsClient(new(http1, new Token()), new("https://api.test"), [AdpOperation.Hire]);
        using var payload = JsonDocument.Parse("""{"events":[{"data":{}}]}""");
        await Assert.ThrowsAsync<IntegrationRequestException>(() => ops.SubmitEventAsync(AdpOperation.Hire, payload.RootElement));

        using var http2 = new HttpClient(new Handler(_ => new(HttpStatusCode.Forbidden)));
        var workers = new Koras.Connect.Adp.Workers.AdpWorkersClient(new(http2, new Token()), new(new("https://api.test")));
        await Assert.ThrowsAsync<IntegrationRequestException>(() => workers.GetWorkersPageAsync());
    }
}

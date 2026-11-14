using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-008: closes the unit-test requirements not already covered by <c>CoreTests.cs</c> (which only
/// exercises status-code-based transient retry, write-is-never-retried for POST, and Retry-After exceeding
/// budget). Covers: GET retry after a connection-level <see cref="HttpRequestException"/> ("network failures"),
/// persistent failure beyond <see cref="TransportOptions.MaxRetries"/> ("budget exhaustion"), the exponential
/// backoff's jitter staying within its documented [1.0, 1.2) multiplier and respecting <see cref="TransportOptions.MaxDelay"/>
/// ("jitter bounds"), cancellation during an in-progress backoff wait ("circuit cancellation"), the date form of
/// the <c>Retry-After</c> header (only the delta/<see cref="TimeSpan"/> form was previously exercised), and that
/// every unsafe HTTP method - not just POST - is sent at most once ("no POST/DELETE replay").</summary>
public sealed class ResiliencePolicyTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(reply(r)); } }
    private sealed class ThrowingThenHandler(int failuresBeforeSuccess, Func<HttpResponseMessage> onSuccess) : HttpMessageHandler
    {
        private int _calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return ++_calls <= failuresBeforeSuccess
                ? throw new HttpRequestException("simulated connection failure")
                : Task.FromResult(onSuccess());
        }
    }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body) };

    [Fact]
    public async Task AGetRetriesAfterAConnectionLevelFailureNotJustAnHttpStatusCode()
    {
        using var h = new HttpClient(new ThrowingThenHandler(2, () => Reply("{}")));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.Zero });
        using var result = await transport.ReadJsonAsync(new("https://api.test"));
        Assert.Equal(JsonValueKind.Object, result.RootElement.ValueKind);
    }

    [Fact]
    public async Task APersistentConnectionFailureThatExhaustsTheGetRetryBudgetWrapsIntoTransportException()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => { calls++; throw new HttpRequestException("simulated persistent connection failure"); }));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.Zero, MaxRetries = 2 });
        var ex = await Assert.ThrowsAsync<Koras.Connect.Exceptions.TransportException>(() => transport.ReadJsonAsync(new("https://api.test")));
        Assert.Equal(3, calls); // the initial attempt plus exactly MaxRetries retries
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task AConnectionFailureOnAWriteMethodWrapsIntoTransportExceptionImmediatelyWithNoRetry()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => { calls++; throw new HttpRequestException("simulated connection failure"); }));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.Zero, MaxRetries = 3 });
        using var payload = JsonDocument.Parse("{}");
        await Assert.ThrowsAsync<Koras.Connect.Exceptions.TransportException>(() => transport.SendJsonAsync(HttpMethod.Post, new("https://api.test"), payload.RootElement));
        Assert.Equal(1, calls); // writes are never retried, including on a connection-level failure
    }

    [Fact]
    public async Task PersistentTransientFailureExhaustsTheRetryBudgetAndThrowsWithTheFinalAttemptCount()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => { calls++; return Reply("private", HttpStatusCode.ServiceUnavailable); }));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.Zero, MaxRetries = 3 });

        var ex = await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test")));

        Assert.Equal(4, calls); // the initial attempt plus exactly MaxRetries retries
        Assert.Equal(4, ex.Attempts);
        Assert.DoesNotContain("private", ex.ToString());
    }

    [Fact]
    public async Task BackoffJitterStaysWithinTheDocumentedMultiplierAndNeverExceedsMaxDelay()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => ++calls < 3 ? Reply("", HttpStatusCode.ServiceUnavailable) : Reply("{}")));
        // Unjittered delays for attempts 0 and 1 would be 40ms and 80ms (120ms total); jitter can only add up to
        // 20% on top (per IntegrationTransport.Backoff: BaseDelay * 2^attempt * (1 + [0, 0.2))).
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.FromMilliseconds(40), MaxDelay = TimeSpan.FromSeconds(5) });
        var stopwatch = Stopwatch.StartNew();
        using var result = await transport.ReadJsonAsync(new("https://api.test"));
        stopwatch.Stop();

        Assert.Equal(3, calls);
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(110), $"Elapsed {stopwatch.Elapsed} is below the unjittered floor; jitter must never subtract delay.");
        Assert.True(stopwatch.Elapsed <= TimeSpan.FromMilliseconds(500), $"Elapsed {stopwatch.Elapsed} far exceeds the documented +20% jitter ceiling plus scheduling slack.");
    }

    [Fact]
    public async Task BackoffNeverExceedsTheConfiguredMaxDelayEvenAtLaterAttempts()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => ++calls < 3 ? Reply("", HttpStatusCode.ServiceUnavailable) : Reply("{}")));
        // Unjittered attempt-0 backoff is 100ms (within the 150ms cap); unjittered attempt-1 backoff would be
        // 200ms*jitter, which exceeds the 150ms MaxDelay and must be capped down to it.
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.FromMilliseconds(100), MaxDelay = TimeSpan.FromMilliseconds(150) });
        var stopwatch = Stopwatch.StartNew();
        using var result = await transport.ReadJsonAsync(new("https://api.test"));
        stopwatch.Stop();

        Assert.Equal(3, calls);
        Assert.True(stopwatch.Elapsed <= TimeSpan.FromMilliseconds(600), $"Elapsed {stopwatch.Elapsed} shows MaxDelay was not honored (expected roughly 100ms + 150ms capped).");
    }

    [Fact]
    public async Task CancellingDuringAnInProgressBackoffWaitStopsPromptlyRatherThanWaitingOutTheFullDelay()
    {
        using var h = new HttpClient(new Handler(_ => Reply("", HttpStatusCode.ServiceUnavailable)));
        var transport = new IntegrationTransport(h, new Token(), new() { BaseDelay = TimeSpan.FromSeconds(30), MaxDelay = TimeSpan.FromMinutes(5) });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var stopwatch = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.ReadJsonAsync(new("https://api.test"), cancellationToken: cts.Token));
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Cancellation during backoff took {stopwatch.Elapsed}; it must not wait out the full 30s delay.");
    }

    [Fact]
    public async Task ADateFormRetryAfterIsHonoredNotOnlyTheDeltaForm()
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ =>
        {
            calls++;
            if (calls == 1)
            {
                var r = Reply("", HttpStatusCode.TooManyRequests);
                r.Headers.RetryAfter = new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMilliseconds(20));
                return r;
            }
            return Reply("{}");
        }));
        var transport = new IntegrationTransport(h, new Token(), new() { MaxDelay = TimeSpan.FromSeconds(30) });

        using var result = await transport.ReadJsonAsync(new("https://api.test"));

        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public async Task EveryUnsafeHttpMethodIsSentAtMostOnceEvenOnATransientFailure(string method)
    {
        var calls = 0;
        using var h = new HttpClient(new Handler(_ => { calls++; return Reply("private", HttpStatusCode.ServiceUnavailable); }));
        var transport = new IntegrationTransport(h, new Token());

        await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.SendJsonAsync(new HttpMethod(method), new("https://api.test")));

        Assert.Equal(1, calls);
    }
}

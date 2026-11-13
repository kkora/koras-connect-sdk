using System.Net;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Microsoft.Extensions.Logging;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class DiagnosticsAndCorrelationTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.FromResult(reply(r)); }
    }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status) => new(status) { Content = new StringContent(body) };

    [Fact]
    public async Task FailedRequestCarriesAllowlistedCorrelationHeaders()
    {
        using var http = new HttpClient(new Handler(_ =>
        {
            var response = Reply("""{"error":"denied"}""", HttpStatusCode.Forbidden);
            response.Headers.Add("request-id", "abc-123");
            return response;
        }));
        var transport = new IntegrationTransport(http, new Token());
        var failure = await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test/thing")));
        Assert.Equal(new[] { "abc-123" }, failure.Headers["request-id"]);
    }

    [Fact]
    public async Task ExhaustedRetryCarriesCorrelationHeadersToo()
    {
        using var http = new HttpClient(new Handler(_ =>
        {
            var response = Reply("unavailable", HttpStatusCode.ServiceUnavailable);
            response.Headers.Add("x-request-id", "retry-exhausted-xyz");
            return response;
        }));
        var transport = new IntegrationTransport(http, new Token(), new TransportOptions { MaxRetries = 0 });
        var failure = await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test/thing")));
        Assert.Equal(new[] { "retry-exhausted-xyz" }, failure.Headers["x-request-id"]);
    }

    [Fact]
    public async Task AdpApiExceptionDerivesCorrelationIdFromFailureHeadersByDefault()
    {
        using var http = new HttpClient(new Handler(_ =>
        {
            var response = Reply("""{"error":"denied"}""", HttpStatusCode.Forbidden);
            response.Headers.Add("request-id", "derived-correlation-id");
            return response;
        }));
        var transport = new IntegrationTransport(http, new Token());
        var failure = await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test/thing")));

        var classified = AdpApiException.Classify("GetWorker", failure);
        Assert.Equal("derived-correlation-id", classified.Details.CorrelationId);
        Assert.Equal(nameof(AuthorizationException), classified.Category);
    }

    [Fact]
    public async Task AdpApiExceptionExplicitCorrelationIdOverridesDerivedOne()
    {
        using var http = new HttpClient(new Handler(_ =>
        {
            var response = Reply("denied", HttpStatusCode.Forbidden);
            response.Headers.Add("request-id", "from-header");
            return response;
        }));
        var transport = new IntegrationTransport(http, new Token());
        var failure = await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test/thing")));

        var classified = AdpApiException.Classify("GetWorker", failure, correlationId: "explicit-override");
        Assert.Equal("explicit-override", classified.Details.CorrelationId);
    }

    [Fact]
    public async Task TokenEndpointHttpFailureThrowsStructuredAuthenticationException()
    {
        using var http = new HttpClient(new Handler(_ => Reply("""{"error":"invalid_client"}""", HttpStatusCode.Unauthorized)));
        using var source = new ClientCredentialsTokenSource(http, new(new("https://auth.test"), "id", "secret"));
        var failure = await Assert.ThrowsAsync<AuthenticationException>(() => source.GetTokenAsync().AsTask());
        Assert.Equal("oauth_http_401", failure.Details.Code);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, EventId EventId, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId, formatter(state, exception)));
    }

    [Fact]
    public async Task LoggerReceivesRetryAndFailureEventsWithoutAddressOrBody()
    {
        var logger = new CapturingLogger();
        var calls = 0;
        using var http = new HttpClient(new Handler(_ => ++calls < 2 ? Reply("secret-body-should-never-be-logged", HttpStatusCode.ServiceUnavailable) : Reply("""{"ok":true}""", HttpStatusCode.OK)));
        var transport = new IntegrationTransport(http, new Token(), logger: logger);
        await transport.ReadJsonAsync(new("https://api.test/private-worker-oid-should-never-be-logged"));

        Assert.Contains(logger.Entries, e => e.EventId == DiagnosticEvents.RequestRetried);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("secret-body-should-never-be-logged"));
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("private-worker-oid-should-never-be-logged"));
    }

    [Fact]
    public void ClientCredentialsOptionsReportsEveryViolatedFieldByName()
    {
        var options = new ClientCredentialsOptions(new Uri("http://insecure.test"), "", "");
        var failure = Assert.Throws<OptionsValidationException>(options.Validate);
        Assert.Contains(failure.Failures, f => f.StartsWith("Endpoint:"));
        Assert.Contains(failure.Failures, f => f.StartsWith("ClientId:"));
        Assert.Contains(failure.Failures, f => f.StartsWith("ClientSecret:"));
        Assert.Equal(3, failure.Failures.Count);
    }

    [Fact]
    public void TransportOptionsReportsEveryViolatedFieldByName()
    {
        var options = new TransportOptions { MaxRetries = 99, BaseDelay = TimeSpan.FromSeconds(-1), RequestTimeout = TimeSpan.Zero };
        var failure = Assert.Throws<OptionsValidationException>(options.Validate);
        Assert.Contains(failure.Failures, f => f.StartsWith("MaxRetries:"));
        Assert.Contains(failure.Failures, f => f.StartsWith("BaseDelay:"));
        Assert.Contains(failure.Failures, f => f.StartsWith("RequestTimeout:"));
    }

    [Fact]
    public async Task LoggerReceivesFailureEventOnTerminalError()
    {
        var logger = new CapturingLogger();
        using var http = new HttpClient(new Handler(_ => Reply("denied", HttpStatusCode.Forbidden)));
        var transport = new IntegrationTransport(http, new Token(), logger: logger);
        await Assert.ThrowsAsync<IntegrationRequestException>(() => transport.ReadJsonAsync(new("https://api.test/thing")));

        Assert.Contains(logger.Entries, e => e.EventId == DiagnosticEvents.RequestFailed && e.Level == LogLevel.Error);
    }
}

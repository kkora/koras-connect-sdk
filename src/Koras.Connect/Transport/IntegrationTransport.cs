using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Microsoft.Extensions.Logging;

namespace Koras.Connect.Transport;

/// <summary>Stable event IDs for the optional <see cref="ILogger"/> passed into <see cref="IntegrationTransport"/>.
/// Every associated log message is address/body/token/employee-data free by construction &#8212; only method, status
/// code, attempt count and retry delay are ever logged.</summary>
public static class DiagnosticEvents
{
    public static readonly EventId RequestRetried = new(1001, "Koras.Connect.RequestRetried");
    public static readonly EventId RequestFailed = new(1002, "Koras.Connect.RequestFailed");
    public static readonly EventId DeadlineExceeded = new(1003, "Koras.Connect.DeadlineExceeded");
}

public sealed class IntegrationRequestException(HttpStatusCode statusCode, int attempts, IReadOnlyDictionary<string, string[]>? headers = null)
    : HttpRequestException($"Integration endpoint returned HTTP {(int)statusCode} after {attempts} attempt(s).", null, statusCode)
{
    public HttpStatusCode ResponseStatus { get; } = statusCode;
    public int Attempts { get; } = attempts;

    /// <summary>The same allowlisted, sanitized response headers (for example <c>request-id</c>) that a successful
    /// response carries on <see cref="IntegrationJsonResponse.Headers"/> &#8212; captured here too so a failed call
    /// still has a correlation id available to classify into, where the server provided one. Never contains the
    /// response body.</summary>
    public IReadOnlyDictionary<string, string[]> Headers { get; } = headers ?? new Dictionary<string, string[]>();
}
public sealed record IntegrationJsonResponse(JsonDocument Body, IReadOnlyDictionary<string, string[]> Headers, HttpStatusCode StatusCode) : IDisposable
{
    public void Dispose() => Body.Dispose();
}
public interface IAccessTokenSource
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
public sealed record TransportOptions : Koras.Connect.Abstractions.IValidatableOptions
{
    public int MaxRetries { get; init; } = 3;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(100);
    public void Validate()
    {
        var failures = new List<string>();
        if (MaxRetries is < 0 or > 10) failures.Add($"{nameof(MaxRetries)}: must be between 0 and 10.");
        if (BaseDelay < TimeSpan.Zero) failures.Add($"{nameof(BaseDelay)}: must not be negative.");
        if (MaxDelay < BaseDelay) failures.Add($"{nameof(MaxDelay)}: must not be less than {nameof(BaseDelay)}.");
        if (MaxDelay > TimeSpan.FromMinutes(5)) failures.Add($"{nameof(MaxDelay)}: must not exceed 5 minutes.");
        if (RequestTimeout <= TimeSpan.Zero) failures.Add($"{nameof(RequestTimeout)}: must be positive.");
        if (RequestTimeout > TimeSpan.FromMinutes(30)) failures.Add($"{nameof(RequestTimeout)}: must not exceed 30 minutes.");
        if (failures.Count > 0) throw new Koras.Connect.Abstractions.OptionsValidationException(nameof(TransportOptions), failures);
    }
}
/// <summary>Safe JSON transport. Only GET requests are automatically retried. Hosts own HttpClient and token source lifetimes.</summary>
public sealed class IntegrationTransport
{
    public const string InstrumentationName = "Koras.Connect";
    public static readonly ActivitySource Activities = new(InstrumentationName);
    private static readonly Meter Meter = new(InstrumentationName);
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("koras.connect.requests");
    private readonly HttpClient _http;
    private readonly IRequestAuthenticator _authentication;
    private readonly TransportOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger? _logger;
    public IntegrationTransport(HttpClient httpClient, IAccessTokenSource tokens, TransportOptions? options = null, TimeProvider? timeProvider = null, ILogger? logger = null)
        : this(httpClient, new BearerTokenAuthenticator(tokens), options, timeProvider, logger) { }
    public IntegrationTransport(HttpClient httpClient, IRequestAuthenticator authentication, TransportOptions? options = null, TimeProvider? timeProvider = null, ILogger? logger = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _options = options ?? new(); _options.Validate();
        _clock = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }
    public Task<JsonDocument> ReadJsonAsync(Uri address, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => SendJsonAsync(HttpMethod.Get, address, null, headers, cancellationToken);
    public async Task<JsonDocument> SendJsonAsync(HttpMethod method, Uri address, JsonElement? payload = null,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => (await SendResponseAsync(method, address, payload, headers, cancellationToken)).Body;
    public Task<IntegrationJsonResponse> SendResponseAsync(HttpMethod method, Uri address, JsonElement? payload = null,
        IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        => ExecuteAsync(method, address, payload, headers, async (response, ct) =>
        {
            var safeHeaders = SafeHeaders(response);
            if (response.StatusCode == HttpStatusCode.NoContent) return new IntegrationJsonResponse(JsonDocument.Parse("null"), safeHeaders, response.StatusCode);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return new IntegrationJsonResponse(await JsonDocument.ParseAsync(stream, cancellationToken: ct), safeHeaders, response.StatusCode);
        }, cancellationToken);
    /// <summary>Extracts the fixed, sanitized correlation-header allowlist from a response. Never includes the body
    /// or any header outside this allowlist, successful or not, so a failed <see cref="IntegrationRequestException"/>
    /// carries the same correlation evidence a success would.</summary>
    private static Dictionary<string, string[]> SafeHeaders(HttpResponseMessage response)
    {
        var safeHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { "adp-msg-msgid", "request-id", "x-request-id" })
            if (response.Headers.TryGetValues(name, out var values)) safeHeaders[name] = values.ToArray();
        return safeHeaders;
    }
    /// <summary>Copies a successful GET response to a caller-owned stream. No replay after any output has been written.</summary>
    public Task CopyToAsync(Uri address, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        return ExecuteAsync(HttpMethod.Get, address, null, new Dictionary<string, string> { ["Accept"] = "*/*" }, async (response, ct) =>
        { await response.Content.CopyToAsync(output, ct); return true; }, cancellationToken);
    }
    public Task<string> ReadTextAsync(Uri address, CancellationToken cancellationToken = default)
        => ExecuteAsync(HttpMethod.Get, address, null, new Dictionary<string, string> { ["Accept"] = "application/xml" },
            (response, ct) => response.Content.ReadAsStringAsync(ct), cancellationToken);
    private async Task<T> ExecuteAsync<T>(HttpMethod method, Uri address, JsonElement? payload,
        IReadOnlyDictionary<string, string>? headers, Func<HttpResponseMessage, CancellationToken, Task<T>> consume, CancellationToken cancellationToken)
    {
        if (!address.IsAbsoluteUri || address.Scheme != Uri.UriSchemeHttps || address.UserInfo.Length != 0)
            throw new ArgumentException("Endpoint must be an absolute HTTPS URI without user information.", nameof(address));
        if (headers is not null && headers.Keys.Any(k => k.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
            || k.Equals("Host", StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Reserved request header.", nameof(headers));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.RequestTimeout);
        var ct = timeout.Token;
        using var activity = Activities.StartActivity("integration.request", ActivityKind.Client);
        // No addresses, identifiers, request bodies, tokens or employee data in SDK telemetry.
        activity?.SetTag("http.request.method", method.Method);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(method, address);
                await _authentication.AuthenticateAsync(request, ct);
                if (headers is null || !headers.Keys.Any(k => k.Equals("Accept", StringComparison.OrdinalIgnoreCase)))
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (headers is not null) foreach (var h in headers)
                    if (!request.Headers.TryAddWithoutValidation(h.Key, h.Value)) throw new ArgumentException("Invalid request header.", nameof(headers));
                if (payload is not null) request.Content = new StringContent(payload.Value.GetRawText(), System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response;
                try { response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); }
                catch (HttpRequestException) when (method == HttpMethod.Get && attempt < _options.MaxRetries)
                {
                    _logger?.LogWarning(DiagnosticEvents.RequestRetried, "Request attempt {Attempt} failed to connect; retrying.", attempt + 1);
                    await Task.Delay(Backoff(attempt), _clock, ct); continue;
                }
                using (response)
                {
                    Requests.Add(1, new KeyValuePair<string, object?>("http.response.status_code", (int)response.StatusCode));
                    activity?.SetTag("http.response.status_code", (int)response.StatusCode);
                    var transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
                        or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;
                    if (method == HttpMethod.Get && transient && attempt < _options.MaxRetries)
                    {
                        var retry = response.Headers.RetryAfter;
                        var delay = retry?.Delta ?? (retry?.Date - DateTimeOffset.UtcNow) ?? Backoff(attempt);
                        // Never retry sooner than a server's requested delay. Fail if outside the configured budget.
                        if (delay > _options.MaxDelay) throw new IntegrationRequestException(response.StatusCode, attempt + 1, SafeHeaders(response));
                        _logger?.LogWarning(DiagnosticEvents.RequestRetried, "Request attempt {Attempt} received HTTP {StatusCode}; retrying after {Delay}.",
                            attempt + 1, (int)response.StatusCode, delay);
                        await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, _clock, ct); continue;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        activity?.SetStatus(ActivityStatusCode.Error);
                        _logger?.LogError(DiagnosticEvents.RequestFailed, "Request failed with HTTP {StatusCode} after {Attempts} attempt(s).", (int)response.StatusCode, attempt + 1);
                        throw new IntegrationRequestException(response.StatusCode, attempt + 1, SafeHeaders(response));
                    }
                    return await consume(response, ct);
                }
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger?.LogError(DiagnosticEvents.DeadlineExceeded, "The configured request timeout elapsed before the operation completed.");
            throw new DeadlineExceededException("The configured request timeout elapsed before the operation completed.");
        }
    }
    private TimeSpan Backoff(int attempt) => TimeSpan.FromMilliseconds(Math.Min(_options.MaxDelay.TotalMilliseconds,
        _options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt) * (1 + Random.Shared.NextDouble() * .2)));
}

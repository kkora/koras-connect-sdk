using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Koras.Connect.Transport;

public sealed class IntegrationRequestException(HttpStatusCode statusCode, int attempts)
    : HttpRequestException($"Integration endpoint returned HTTP {(int)statusCode} after {attempts} attempt(s).", null, statusCode)
{
    public HttpStatusCode ResponseStatus { get; } = statusCode;
    public int Attempts { get; } = attempts;
}
public sealed record IntegrationJsonResponse(JsonDocument Body, IReadOnlyDictionary<string, string[]> Headers, HttpStatusCode StatusCode) : IDisposable
{
    public void Dispose() => Body.Dispose();
}
public interface IAccessTokenSource
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
public sealed record TransportOptions
{
    public int MaxRetries { get; init; } = 3;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(100);
    internal void Validate()
    {
        if (MaxRetries is < 0 or > 10 || BaseDelay < TimeSpan.Zero || MaxDelay < BaseDelay || MaxDelay > TimeSpan.FromMinutes(5)
            || RequestTimeout <= TimeSpan.Zero || RequestTimeout > TimeSpan.FromMinutes(30))
            throw new ArgumentOutOfRangeException(nameof(TransportOptions));
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
    public IntegrationTransport(HttpClient httpClient, IAccessTokenSource tokens, TransportOptions? options = null)
        : this(httpClient, new BearerTokenAuthenticator(tokens), options) { }
    public IntegrationTransport(HttpClient httpClient, IRequestAuthenticator authentication, TransportOptions? options = null)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _options = options ?? new(); _options.Validate();
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
            var safeHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in new[] { "adp-msg-msgid", "request-id", "x-request-id" })
                if (response.Headers.TryGetValues(name, out var values)) safeHeaders[name] = values.ToArray();
            if (response.StatusCode == HttpStatusCode.NoContent) return new IntegrationJsonResponse(JsonDocument.Parse("null"), safeHeaders, response.StatusCode);
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return new IntegrationJsonResponse(await JsonDocument.ParseAsync(stream, cancellationToken: ct), safeHeaders, response.StatusCode);
        }, cancellationToken);
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
            { await Task.Delay(Backoff(attempt), ct); continue; }
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
                    if (delay > _options.MaxDelay) throw new IntegrationRequestException(response.StatusCode, attempt + 1);
                    await Task.Delay(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, ct); continue;
                }
                if (!response.IsSuccessStatusCode)
                { activity?.SetStatus(ActivityStatusCode.Error); throw new IntegrationRequestException(response.StatusCode, attempt + 1); }
                return await consume(response, ct);
            }
        }
    }
    private TimeSpan Backoff(int attempt) => TimeSpan.FromMilliseconds(Math.Min(_options.MaxDelay.TotalMilliseconds,
        _options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt) * (1 + Random.Shared.NextDouble() * .2)));
}

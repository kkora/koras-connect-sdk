using System.Net;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Version-root selection for every Graph request (MGR-003). <see cref="UseBetaEndpoint"/> defaults to
/// <c>false</c> (GA <c>v1.0</c>) - the beta endpoint is explicit opt-in only, since Microsoft documents beta as
/// not supported in production (carried forward honestly in MGR-026's sample/test documentation, not silently
/// treated as production-safe). <see cref="IsDelegatedContext"/> declares whether the configured token source
/// represents a signed-in user (delegated) rather than an unattended daemon (app-only, the default) - the SDK
/// cannot introspect an arbitrary caller-supplied <c>IAccessTokenSource</c> to tell the two apart itself, so a
/// delegated caller must opt in explicitly (MGR-004's <c>/me</c> guard reads this flag).</summary>
public sealed record GraphOptions(bool UseBetaEndpoint = false, bool IsDelegatedContext = false) : IValidatableOptions
{
    public void Validate() { }
}

/// <summary>Microsoft Graph structured failure. HTTP-status classification is delegated entirely to the shared
/// <see cref="IntegrationExceptionClassifier"/>; this type adds a stable <see cref="StatusCode"/>/<see cref="RetryAfter"/>
/// surface plus crash-proof extraction of Graph's documented error-response shape
/// (<c>{"error":{"code","message","innerError"|"innererror",...}}</c>). Both the <c>innerError</c> and
/// <c>innererror</c> field-name casings are accepted - a genuine, unresolved vendor-documentation inconsistency
/// carried forward here rather than silently resolved one way. The raw <c>message</c> field is never logged by
/// default - only the sanitized <c>code</c>.</summary>
public sealed class GraphApiException : IntegrationException
{
    public HttpStatusCode StatusCode { get; }
    public TimeSpan? RetryAfter { get; }

    private GraphApiException(string message, ErrorDetails details, HttpStatusCode statusCode, TimeSpan? retryAfter, Exception? innerException)
        : base(message, details, innerException)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    public static GraphApiException Classify(HttpStatusCode statusCode, int attempts, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        var classified = IntegrationExceptionClassifier.Classify(statusCode, attempts, correlationId, retryAfter);
        return new GraphApiException(classified.Message, classified.Details, statusCode, retryAfter, classified);
    }

    public static GraphApiException FromTransportFailure(IntegrationRequestException failure, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Classify(failure.ResponseStatus, failure.Attempts, correlationId, retryAfter);
    }

    /// <summary>Best-effort extraction of the documented Graph error envelope's sanitized <c>code</c>. The nested
    /// <c>innerError</c>/<c>innererror</c> object itself is not read here - <c>code</c> lives directly under
    /// <c>error</c> regardless of that casing - but <see cref="TryExtractCorrelationId"/> is where the dual-casing
    /// handling actually matters. Malformed, oversized, wrong-shaped or missing bodies always yield <c>null</c>
    /// rather than throwing. The <c>message</c> field is intentionally never read here.</summary>
    public static string? TryExtractErrorCode(JsonElement body)
    {
        try
        {
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object) return null;
            if (!error.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String) return null;
            var value = code.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.Any(c => c is '\r' or '\n')) return null;
            return value;
        }
        catch (InvalidOperationException) { return null; }
    }

    /// <summary>Best-effort extraction of the documented Graph error envelope's nested <c>request-id</c>, read from
    /// whichever of <c>innerError</c>/<c>innererror</c> is actually present - this is the one place the casing
    /// inconsistency genuinely matters, since <c>code</c> itself (see <see cref="TryExtractErrorCode"/>) never
    /// needs to look inside this nested object at all. Malformed, oversized, wrong-shaped or missing bodies always
    /// yield <c>null</c> rather than throwing.</summary>
    public static string? TryExtractCorrelationId(JsonElement body)
    {
        try
        {
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object) return null;
            if (!TryGetInnerError(error, out var innerError)) return null;
            if (!innerError.TryGetProperty("request-id", out var requestId) || requestId.ValueKind != JsonValueKind.String) return null;
            var value = requestId.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.Any(c => c is '\r' or '\n')) return null;
            return value;
        }
        catch (InvalidOperationException) { return null; }
    }

    private static bool TryGetInnerError(JsonElement error, out JsonElement innerError)
    {
        if (error.TryGetProperty("innerError", out innerError) && innerError.ValueKind == JsonValueKind.Object) return true;
        if (error.TryGetProperty("innererror", out innerError) && innerError.ValueKind == JsonValueKind.Object) return true;
        innerError = default;
        return false;
    }
}

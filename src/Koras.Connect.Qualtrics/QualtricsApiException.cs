using System.Net;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Qualtrics;

/// <summary>Qualtrics API v3 structured failure. HTTP-status classification is delegated entirely to the shared
/// <see cref="IntegrationExceptionClassifier"/> so this type never duplicates retry/error-code logic; it only adds a
/// stable <see cref="StatusCode"/>/<see cref="RetryAfter"/> surface plus Qualtrics' own crash-proof extraction of the
/// provider's "meta.requestId" error field for support correlation.</summary>
public sealed class QualtricsApiException : IntegrationException
{
    /// <summary>The HTTP status code that produced this classification.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>The server's own throttle hint, when one was known at classification time. Never invented.</summary>
    public TimeSpan? RetryAfter { get; }

    private QualtricsApiException(string message, ErrorDetails details, HttpStatusCode statusCode, TimeSpan? retryAfter, Exception? innerException)
        : base(message, details, innerException)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    /// <summary>Classifies a status code into the shared structured exception hierarchy (<see cref="AuthenticationException"/>
    /// for 401, <see cref="AuthorizationException"/> for 403, <see cref="ThrottleException"/> with <see cref="RetryAfter"/> for
    /// 429, <see cref="TransportException"/> for 5xx, <see cref="ContractException"/> otherwise, including 404) while
    /// preserving the original classified exception as <see cref="Exception.InnerException"/>.</summary>
    public static QualtricsApiException Classify(HttpStatusCode statusCode, int attempts, string? sanitizedRequestId = null, TimeSpan? retryAfter = null)
    {
        var classified = IntegrationExceptionClassifier.Classify(statusCode, attempts, sanitizedRequestId, retryAfter);
        return new QualtricsApiException(classified.Message, classified.Details, statusCode, retryAfter, classified);
    }

    /// <summary>Convenience wrapper over <see cref="Classify"/> for the exception shape the shared transport already throws.</summary>
    public static QualtricsApiException FromTransportFailure(IntegrationRequestException failure, string? sanitizedRequestId = null, TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Classify(failure.ResponseStatus, failure.Attempts, sanitizedRequestId, retryAfter);
    }

    /// <summary>Best-effort extraction of the Qualtrics "meta.requestId" error field. Malformed, oversized, wrong-shaped
    /// or entirely missing metadata always yields null rather than throwing — a hostile or malformed provider error
    /// payload must never crash classification.</summary>
    public static string? TryExtractRequestId(JsonElement body)
    {
        try
        {
            if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object) return null;
            if (!meta.TryGetProperty("requestId", out var id) || id.ValueKind != JsonValueKind.String) return null;
            var value = id.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Any(c => c is '\r' or '\n')) return null;
            return value;
        }
        catch (InvalidOperationException) { return null; }
    }
}

using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
namespace Koras.Connect.Adp;

/// <summary>ADP-specific structured error. Wraps the sanitized HTTP-status classification produced by
/// <see cref="IntegrationExceptionClassifier"/> with the ADP operation that failed and, when available, the
/// correlation id already captured from the allowlisted request-id/x-request-id/adp-msg-msgid response headers.
/// <see cref="Category"/> names the underlying <see cref="IntegrationException"/> subtype (for example
/// "AuthenticationException" or "ThrottleException") so callers can branch on category without a type test, while
/// <see cref="IntegrationException.Details"/> still carries the sanitized machine-readable code. Never carries a
/// raw response body or credential material.</summary>
public sealed class AdpApiException : IntegrationException
{
    /// <summary>The ADP operation (an <see cref="AdpOperation"/> name or a worker-read method name) that failed.</summary>
    public string Operation { get; }

    /// <summary>The name of the <see cref="IntegrationException"/> subtype the failure was classified as.</summary>
    public string Category { get; }

    private AdpApiException(string operation, string category, string message, ErrorDetails details, Exception? innerException)
        : base(message, details, innerException)
    {
        Operation = operation;
        Category = category;
    }

    /// <summary>Classifies a failed ADP call by HTTP status. <paramref name="correlationId"/> defaults to the first
    /// populated allowlisted header already captured on <paramref name="failure"/>.<see cref="IntegrationRequestException.Headers"/>
    /// (checked in the order <c>request-id</c>, <c>x-request-id</c>, <c>adp-msg-msgid</c>) when not explicitly
    /// supplied; pass it explicitly only to override that default. <paramref name="retryAfter"/> is the server's
    /// own Retry-After hint for a 429 response, when available.</summary>
    public static AdpApiException Classify(string operation, IntegrationRequestException failure, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        if (string.IsNullOrWhiteSpace(operation)) throw new ArgumentException("An operation name is required.", nameof(operation));
        ArgumentNullException.ThrowIfNull(failure);
        correlationId ??= CorrelationIdFrom(failure.Headers);
        var classified = IntegrationExceptionClassifier.Classify(failure.ResponseStatus, failure.Attempts, correlationId, retryAfter);
        return new AdpApiException(operation, classified.GetType().Name, $"ADP operation '{operation}' failed: {classified.Message}", classified.Details, failure);
    }

    private static string? CorrelationIdFrom(IReadOnlyDictionary<string, string[]> headers)
    {
        foreach (var name in new[] { "request-id", "x-request-id", "adp-msg-msgid" })
            if (headers.TryGetValue(name, out var values) && values.Length > 0) return values[0];
        return null;
    }

    /// <summary>Classifies a failed ADP call for a documented <see cref="AdpOperation"/>.</summary>
    public static AdpApiException Classify(AdpOperation operation, IntegrationRequestException failure, string? correlationId = null, TimeSpan? retryAfter = null)
        => Classify(operation.ToString(), failure, correlationId, retryAfter);
}

using System.Net;

namespace Koras.Connect.Exceptions;

/// <summary>Sanitized, correlation-bound error detail. Never includes a raw response body.</summary>
public sealed record ErrorDetails(string Code, string? CorrelationId = null);

/// <summary>Base of the structured exception hierarchy. All subclasses carry only sanitized detail.</summary>
public abstract class IntegrationException : Exception
{
    public ErrorDetails Details { get; }

    protected IntegrationException(string message, ErrorDetails details, Exception? innerException = null)
        : base(message, innerException) => Details = details;
}

/// <summary>Invalid or incomplete local configuration. Thrown before any credential is sent.</summary>
public sealed class ConfigurationException(string message, ErrorDetails details) : IntegrationException(message, details);

/// <summary>The remote endpoint rejected the presented credentials (401-class failures).</summary>
public sealed class AuthenticationException(string message, ErrorDetails details, Exception? innerException = null)
    : IntegrationException(message, details, innerException);

/// <summary>The remote endpoint accepted the credentials but denied the specific operation (403-class failures).</summary>
public sealed class AuthorizationException(string message, ErrorDetails details) : IntegrationException(message, details);

/// <summary>The remote endpoint requested a slowdown (429-class failures). <see cref="RetryAfter"/> is the server's own hint.</summary>
public sealed class ThrottleException(string message, ErrorDetails details, TimeSpan? retryAfter = null) : IntegrationException(message, details)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;
}

/// <summary>A transport-level or server-side (5xx/connectivity) failure outside caller control.</summary>
public sealed class TransportException(string message, ErrorDetails details, Exception? innerException = null)
    : IntegrationException(message, details, innerException);

/// <summary>The response did not match the expected contract (malformed/missing schema elements).</summary>
public sealed class ContractException(string message, ErrorDetails details) : IntegrationException(message, details);

/// <summary>The configured request deadline elapsed before completion. Distinguishes a timeout from caller-initiated cancellation
/// while still satisfying any caller code that catches <see cref="OperationCanceledException"/>.</summary>
public sealed class DeadlineExceededException(string message) : OperationCanceledException(message);

/// <summary>Classifies a transport failure by HTTP status code into the structured exception hierarchy.</summary>
public static class IntegrationExceptionClassifier
{
    public static IntegrationException Classify(HttpStatusCode statusCode, int attempts, string? correlationId, TimeSpan? retryAfter = null)
    {
        var details = new ErrorDetails($"http_{(int)statusCode}", correlationId);
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException($"Request was not authenticated (HTTP 401 after {attempts} attempt(s)).", details),
            HttpStatusCode.Forbidden => new AuthorizationException($"Request was denied (HTTP 403 after {attempts} attempt(s)).", details),
            HttpStatusCode.TooManyRequests => new ThrottleException($"Request was throttled (HTTP 429 after {attempts} attempt(s)).", details, retryAfter),
            HttpStatusCode.RequestTimeout or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway
                or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout =>
                new TransportException($"Transient transport failure (HTTP {(int)statusCode} after {attempts} attempt(s)).", details),
            _ => new ContractException($"Unexpected response (HTTP {(int)statusCode} after {attempts} attempt(s)).", details)
        };
    }
}

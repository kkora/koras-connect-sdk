using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>Connection settings for a single Dataverse environment. <see cref="EnvironmentRoot"/> is the
/// environment's own API host (for example <c>https://contoso.api.crm.dynamics.com/</c>), not the Web API path
/// itself - <see cref="DataverseClient"/> composes the fixed <see cref="DataverseClient.ApiVersion"/> Web API path
/// onto it (DVT-003).</summary>
public sealed record DataverseOptions(Uri EnvironmentRoot, int MaxPageSize = 5000) : IValidatableOptions
{
    public void Validate()
    {
        var failures = new List<string>();
        if (EnvironmentRoot is null || !EnvironmentRoot.IsAbsoluteUri || EnvironmentRoot.Scheme != Uri.UriSchemeHttps || EnvironmentRoot.UserInfo.Length != 0)
            failures.Add($"{nameof(EnvironmentRoot)}: must be an absolute HTTPS URI without user information.");
        if (MaxPageSize is < 1 or > 5000) failures.Add($"{nameof(MaxPageSize)}: must be between 1 and 5000.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(DataverseOptions), failures);
    }
}

/// <summary>Dataverse Web API structured failure. HTTP-status classification is delegated entirely to the shared
/// <see cref="IntegrationExceptionClassifier"/>; this type only adds a stable <see cref="StatusCode"/>/<see cref="RetryAfter"/>
/// surface plus crash-proof extraction of Dataverse's documented <c>error.code</c> OData-error envelope field for
/// support correlation. <c>error.message</c> is never read or logged (shared safe-logging policy).</summary>
public sealed class DataverseApiException : IntegrationException
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public TimeSpan? RetryAfter { get; }

    private DataverseApiException(string message, ErrorDetails details, System.Net.HttpStatusCode statusCode, TimeSpan? retryAfter, Exception? innerException)
        : base(message, details, innerException)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    public static DataverseApiException Classify(System.Net.HttpStatusCode statusCode, int attempts, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        var classified = IntegrationExceptionClassifier.Classify(statusCode, attempts, correlationId, retryAfter);
        return new DataverseApiException(classified.Message, classified.Details, statusCode, retryAfter, classified);
    }

    public static DataverseApiException FromTransportFailure(IntegrationRequestException failure, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Classify(failure.ResponseStatus, failure.Attempts, correlationId, retryAfter);
    }

    /// <summary>Best-effort extraction of the documented Dataverse/OData <c>error.code</c> field (for example
    /// <c>"0x80040216"</c>). Malformed, oversized, wrong-shaped or missing error envelopes always yield null rather
    /// than throwing. <c>error.message</c> is intentionally never read here.</summary>
    public static string? TryExtractErrorCode(System.Text.Json.JsonElement body)
    {
        try
        {
            if (body.ValueKind != System.Text.Json.JsonValueKind.Object || !body.TryGetProperty("error", out var error) || error.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
            if (!error.TryGetProperty("code", out var code) || code.ValueKind != System.Text.Json.JsonValueKind.String) return null;
            var value = code.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.Any(c => c is '\r' or '\n')) return null;
            return value;
        }
        catch (InvalidOperationException) { return null; }
    }
}

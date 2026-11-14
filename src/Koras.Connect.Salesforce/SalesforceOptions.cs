using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;

namespace Koras.Connect.Salesforce;

/// <summary>Connection settings for a single Salesforce org. <see cref="InstanceRoot"/> is the org's resolved
/// <c>instance_url</c> (or My Domain host) - a separate, explicitly configured input rather than something
/// re-derived from a token refresh (see <see cref="SalesforceAuthentication"/>). <see cref="ApiVersion"/> is
/// isolated to one constant-defaulted field for a future bump; current version at research time is v68.0.</summary>
public sealed record SalesforceOptions(Uri InstanceRoot, string ApiVersion = "v68.0") : IValidatableOptions
{
    public void Validate()
    {
        var failures = new List<string>();
        if (InstanceRoot is null || !InstanceRoot.IsAbsoluteUri || InstanceRoot.Scheme != Uri.UriSchemeHttps || InstanceRoot.UserInfo.Length != 0)
            failures.Add($"{nameof(InstanceRoot)}: must be an absolute HTTPS URI without user information.");
        if (string.IsNullOrWhiteSpace(ApiVersion) || ApiVersion[0] != 'v' || !double.TryParse(ApiVersion.AsSpan(1), out _))
            failures.Add($"{nameof(ApiVersion)}: must be a version string like 'v68.0'.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(SalesforceOptions), failures);
    }
}

/// <summary>Salesforce REST API structured failure. HTTP-status classification is delegated entirely to the shared
/// <see cref="IntegrationExceptionClassifier"/> (SFD-019); this type adds a stable <see cref="StatusCode"/>/
/// <see cref="RetryAfter"/> surface plus crash-proof extraction of Salesforce's documented error-response array
/// shape (<c>[{"message":...,"errorCode":...,"fields":[...]}]</c>) for support correlation. The raw <c>message</c>
/// field is never logged by default - only the sanitized <c>errorCode</c>.</summary>
public sealed class SalesforceApiException : IntegrationException
{
    public System.Net.HttpStatusCode StatusCode { get; }
    public TimeSpan? RetryAfter { get; }

    private SalesforceApiException(string message, ErrorDetails details, System.Net.HttpStatusCode statusCode, TimeSpan? retryAfter, Exception? innerException)
        : base(message, details, innerException)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }

    public static SalesforceApiException Classify(System.Net.HttpStatusCode statusCode, int attempts, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        var classified = IntegrationExceptionClassifier.Classify(statusCode, attempts, correlationId, retryAfter);
        return new SalesforceApiException(classified.Message, classified.Details, statusCode, retryAfter, classified);
    }

    public static SalesforceApiException FromTransportFailure(Koras.Connect.Transport.IntegrationRequestException failure, string? correlationId = null, TimeSpan? retryAfter = null)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return Classify(failure.ResponseStatus, failure.Attempts, correlationId, retryAfter);
    }

    /// <summary>Best-effort extraction of the first documented Salesforce error envelope's sanitized
    /// <c>errorCode</c>. Malformed, oversized, wrong-shaped or missing bodies always yield null rather than
    /// throwing. The <c>message</c> field is intentionally never read here.</summary>
    public static string? TryExtractErrorCode(System.Text.Json.JsonElement body)
    {
        try
        {
            if (body.ValueKind != System.Text.Json.JsonValueKind.Array || body.GetArrayLength() == 0) return null;
            var first = body[0];
            if (first.ValueKind != System.Text.Json.JsonValueKind.Object || !first.TryGetProperty("errorCode", out var code) || code.ValueKind != System.Text.Json.JsonValueKind.String) return null;
            var value = code.GetString();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 64 || value.Any(c => c is '\r' or '\n')) return null;
            return value;
        }
        catch (InvalidOperationException) { return null; }
    }
}

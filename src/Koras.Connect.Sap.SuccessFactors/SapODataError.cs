using System.Text.Json;
namespace Koras.Connect.Sap.SuccessFactors;

/// <summary>Parses SAP's documented OData V2 JSON error envelope (<c>{"error":{"code":...,"message":{"value":...}}}</c>)
/// into a sanitized code. The human-readable "message.value" text is deliberately never read or surfaced by this
/// type - it frequently contains tenant-specific field names, keys or data and must never be logged automatically.
/// <para>Note: <see cref="Koras.Connect.Transport.IntegrationTransport"/> does not retain a response body for a
/// non-success HTTP status today, so this parser is not wired into the client's live error path. It is exposed
/// as a standalone utility for hosts that capture a raw error body themselves (for example from an authorized
/// diagnostic probe, a proxy log, or vendor support export) and still want the sanitized-code extraction
/// behavior without re-implementing it.</para></summary>
public static class SapODataError
{
    /// <summary>Returns the sanitized error code, or null if <paramref name="rawJson"/> is empty, not valid JSON,
    /// or does not match the documented envelope shape. Never throws for malformed input.</summary>
    public static string? TryParseSanitizedCode(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return null;
        try
        {
            using var document = JsonDocument.Parse(rawJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.Object
                || !error.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String)
                return null;
            var value = code.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (JsonException) { return null; }
    }
}

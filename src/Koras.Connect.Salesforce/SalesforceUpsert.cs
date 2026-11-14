using System.Text.Json;

namespace Koras.Connect.Salesforce;

/// <summary>Outcome of <see cref="SalesforceClient.UpsertByExternalIdAsync"/> (SFD-008). Exactly one of
/// <see cref="RecordId"/> (on insert) or <see cref="AmbiguousMatchIds"/> (on a 300 ambiguous match) is populated;
/// an update (204) populates neither - the record id was already known to the caller.</summary>
public sealed record SalesforceUpsertResult(string? RecordId, bool Created, IReadOnlyList<string>? AmbiguousMatchIds);

public sealed partial class SalesforceClient
{
    /// <summary>Creates or updates a record keyed by a custom External ID field via
    /// <c>PATCH .../sobjects/{sObject}/{fieldName}/{fieldValue}</c> (SFD-008). Distinguishes all three documented
    /// outcomes by status code: <c>201</c> (inserted), <c>204</c> (updated), <c>300</c> (ambiguous match - every
    /// matching record id is returned, never collapsed into a generic failure). The SDK does not verify
    /// <paramref name="externalIdField"/> is actually flagged as an External ID on the target org - an
    /// unconfigured field surfaces as the server's own documented error.</summary>
    public async Task<SalesforceUpsertResult> UpsertByExternalIdAsync(string sObject, string externalIdField, string externalIdValue,
        JsonElement fields, bool updateOnly = false, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        ValidateSObject(externalIdField);
        if (string.IsNullOrWhiteSpace(externalIdValue)) throw new ArgumentException("An external id value is required.", nameof(externalIdValue));
        var relative = $"sobjects/{sObject}/{externalIdField}/{Uri.EscapeDataString(externalIdValue)}" + (updateOnly ? "?updateOnly=true" : "");
        var address = new Uri(_root, relative);
        var response = await _transport.SendJsonAllowingAnyStatusAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken);
        using (response)
        {
            return (int)response.StatusCode switch
            {
                201 => new SalesforceUpsertResult(RequireString(response.Body.RootElement, "id"), Created: true, null),
                204 => new SalesforceUpsertResult(null, Created: false, null),
                300 => new SalesforceUpsertResult(null, Created: false, ExtractAmbiguousMatchIds(response.Body.RootElement)),
                _ => throw SalesforceApiException.Classify(response.StatusCode, 1),
            };
        }
    }

    /// <summary>Extracts record ids from the documented 300 ambiguous-match body. The exact element shape (plain
    /// id strings vs. full record objects) was not independently confirmed this pass - both are handled
    /// defensively rather than guessed as a single shape.</summary>
    private static IReadOnlyList<string> ExtractAmbiguousMatchIds(JsonElement body)
    {
        var ids = new List<string>();
        if (body.ValueKind != JsonValueKind.Array) return ids;
        foreach (var item in body.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value)) ids.Add(value!);
            }
            else if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("Id", out var idProperty) && idProperty.ValueKind == JsonValueKind.String)
                ids.Add(idProperty.GetString()!);
        }
        return ids;
    }
}

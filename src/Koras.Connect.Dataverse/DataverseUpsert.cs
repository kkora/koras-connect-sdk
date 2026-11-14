using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>Outcome of <see cref="DataverseClient.UpsertByAlternateKeyAsync"/> (DVT-008).</summary>
public sealed record DataverseUpsertResult(Guid RowId, bool Created);

/// <summary>Forces <see cref="DataverseClient.UpsertByAlternateKeyAsync"/>'s behavior via the documented
/// <c>If-Match</c>/<c>If-None-Match</c> semantics [D10].</summary>
public enum DataverseUpsertMode
{
    /// <summary>Creates the row if the alternate-key combination does not exist, updates it if it does.</summary>
    CreateOrUpdate,
    /// <summary>Forces update-only; errors (<see cref="DataverseConcurrencyException"/>-shaped 412) if the key does not already exist.</summary>
    UpdateOnly,
    /// <summary>Forces create-only; errors if the key already exists.</summary>
    CreateOnly,
}

public sealed partial class DataverseClient
{
    /// <summary>Creates or updates a row keyed by an alternate key instead of its internal GUID (DVT-008),
    /// avoiding a separate existence-check round trip. Always requests <c>Prefer: return=representation</c> so
    /// the <c>201</c>-vs-<c>200</c> status code - otherwise indistinguishable, since the plain success response
    /// is identical for both outcomes - can disambiguate create from update. The SDK does not verify the
    /// alternate key actually exists on the target table before sending; an unconfigured key surfaces as the
    /// server's own documented error, not a silent local assumption.</summary>
    public async Task<DataverseUpsertResult> UpsertByAlternateKeyAsync(string entitySet, IReadOnlyDictionary<string, string> alternateKey,
        JsonElement fields, DataverseUpsertMode mode = DataverseUpsertMode.CreateOrUpdate, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        if (alternateKey is null || alternateKey.Count == 0) throw new ArgumentException("At least one alternate-key field is required.", nameof(alternateKey));
        var address = new Uri(_root, $"{entitySet}({BuildAlternateKeySegment(alternateKey)})");
        ValidateRequestUri(address);
        var headers = new Dictionary<string, string>(RequiredHeaders) { ["Prefer"] = "return=representation" };
        switch (mode)
        {
            case DataverseUpsertMode.UpdateOnly: headers["If-Match"] = "*"; break;
            case DataverseUpsertMode.CreateOnly: headers["If-None-Match"] = "*"; break;
        }
        IntegrationJsonResponse response;
        try { response = await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
        using (response)
        {
            var created = response.StatusCode == System.Net.HttpStatusCode.Created;
            if (response.Body.RootElement.ValueKind == JsonValueKind.Object
                && response.Body.RootElement.TryGetProperty("@odata.id", out var odataId) && odataId.ValueKind == JsonValueKind.String)
                return new DataverseUpsertResult(ExtractGuidFromODataId(odataId.GetString()!), created);
            if (response.Headers.TryGetValue("odata-entityid", out var values) && values.Length > 0)
                return new DataverseUpsertResult(ExtractGuidFromODataId(values[0]), created);
            throw new ContractException("Upsert response did not include a recoverable row id.", new ErrorDetails("upsert_contract"));
        }
    }

    private static string BuildAlternateKeySegment(IReadOnlyDictionary<string, string> alternateKey)
        => string.Join(",", alternateKey.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{ValidateKeyFieldName(kv.Key)}='{EscapeKeyLiteral(kv.Value)}'"));

    private static string ValidateKeyFieldName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !EntitySetPattern.IsMatch(name))
            throw new ArgumentException("Alternate key field name must be a simple lowercase identifier.", nameof(name));
        return name;
    }

    private static string EscapeKeyLiteral(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (value.Any(c => c is '\r' or '\n')) throw new ArgumentException("Alternate key value must not contain control characters.", nameof(value));
        return value.Replace("'", "''"); // OData string-literal escaping: an internal quote is doubled, not percent-encoded.
    }

}

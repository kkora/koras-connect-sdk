using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>sObject CRUD (SFD-004).</summary>
public sealed partial class SalesforceClient
{
    private static readonly System.Text.RegularExpressions.Regex SObjectPattern =
        new("^[A-Za-z][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex RecordIdPattern =
        new("^[A-Za-z0-9]{15}([A-Za-z0-9]{3})?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void ValidateSObject(string sObject)
    {
        if (string.IsNullOrWhiteSpace(sObject) || !SObjectPattern.IsMatch(sObject))
            throw new ArgumentException("sObject must be a simple API name.", nameof(sObject));
    }

    private static void ValidateRecordId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !RecordIdPattern.IsMatch(id))
            throw new ArgumentException("A Salesforce record id must be 15 or 18 alphanumeric characters.", nameof(id));
    }

    /// <summary>Creates a record via <c>POST .../sobjects/{sObject}/</c>, parsing the documented
    /// <c>{"id":...,"errors":[...],"success":...}</c> response shape (SFD-004). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> CreateRecordAsync(string sObject, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        var address = new Uri(_root, $"sobjects/{sObject}/");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            if (root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
                throw new ContractException("Create reported success=false.", new ErrorDetails("record_create_failed"));
            return RequireString(root, "id");
        }
    }

    /// <summary>Reads a record via <c>GET .../sobjects/{sObject}/{id}/</c>, optionally restricted to
    /// <paramref name="fields"/> (SFD-004).</summary>
    public async Task<JsonDocument> GetRecordAsync(string sObject, string id, IReadOnlyList<string>? fields = null, CancellationToken cancellationToken = default)
    {
        ValidateSObject(sObject);
        ValidateRecordId(id);
        var relative = $"sobjects/{sObject}/{id}/" + (fields is { Count: > 0 } ? "?fields=" + Uri.EscapeDataString(string.Join(",", fields)) : "");
        var address = new Uri(_root, relative);
        try { return await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Updates a record via <c>PATCH .../sobjects/{sObject}/{id}/</c> (SFD-004). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        ValidateRecordId(id);
        var address = new Uri(_root, $"sobjects/{sObject}/{id}/");
        try { await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Deletes a record via <c>DELETE .../sobjects/{sObject}/{id}/</c> (SFD-004). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task DeleteRecordAsync(string sObject, string id, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        ValidateRecordId(id);
        var address = new Uri(_root, $"sobjects/{sObject}/{id}/");
        try { await _transport.SendResponseAsync(HttpMethod.Delete, address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static string RequireString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ContractException($"Response missing required field '{property}'.", new ErrorDetails("record_contract"));
        return value.GetString()!;
    }
}

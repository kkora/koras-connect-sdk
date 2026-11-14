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
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried. <paramref name="ifUnmodifiedSince"/>,
    /// when supplied, enforces weak-validation optimistic concurrency (SFD-018) via the documented
    /// <c>If-Unmodified-Since</c> header: a violation surfaces as <see cref="SalesforceConcurrencyException"/>,
    /// distinguishable from every other 4xx failure. Strong validation (<c>ETag</c>/<c>If-Match</c>) is not
    /// exposed here - one source scoped it to Account records only, an unconfirmed and unusually narrow claim
    /// not treated as settled without independent re-verification.</summary>
    public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, string? ifUnmodifiedSince = null,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        ValidateRecordId(id);
        var address = new Uri(_root, $"sobjects/{sObject}/{id}/");
        var headers = ifUnmodifiedSince is null ? null : new Dictionary<string, string> { ["If-Unmodified-Since"] = ifUnmodifiedSince };
        try { await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Distinguishes a <c>412 Precondition Failed</c> conditional-request conflict
    /// (<see cref="SalesforceConcurrencyException"/>, SFD-018) from every other write failure, which still goes
    /// through the shared <see cref="Classify"/> path.</summary>
    private static Koras.Connect.Exceptions.IntegrationException ClassifyWrite(IntegrationRequestException ex)
        => ex.ResponseStatus == System.Net.HttpStatusCode.PreconditionFailed
            ? new SalesforceConcurrencyException("The record was modified since the supplied If-Unmodified-Since value (HTTP 412).", new ErrorDetails("salesforce_concurrency_conflict"))
            : Classify(ex);

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

/// <summary>A <c>412 Precondition Failed</c> conditional-request conflict (SFD-018) - an <c>If-Unmodified-Since</c>
/// violation on a weak-validation update. The shared <see cref="Koras.Connect.Exceptions.IntegrationExceptionClassifier"/>
/// has no dedicated mapping for <c>412</c> (its default arm returns <see cref="ContractException"/>, which is
/// <c>sealed</c> and so cannot be subclassed); this type derives from <see cref="IntegrationException"/> directly,
/// mirroring Dataverse's identical <c>DataverseConcurrencyException</c> precedent (DVT-013).</summary>
public sealed class SalesforceConcurrencyException(string message, ErrorDetails details) : IntegrationException(message, details);

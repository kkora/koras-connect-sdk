using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;

namespace Koras.Connect.Qualtrics;

/// <summary>Qualtrics XM Directory contact record. Endpoint shape based on publicly documented Qualtrics API v3
/// contract (GET/POST <c>/directories/{directoryId}/contacts</c>, GET/PUT <c>/directories/{directoryId}/contacts/{contactId}</c>);
/// NOT verified against a live tenant. Preserves every field Qualtrics returns that this client does not model
/// explicitly, including consent/opt-out-adjacent and embedded-data fields, via <see cref="AdditionalData"/>.</summary>
public sealed class QualtricsContact
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("firstName")] public string? FirstName { get; set; }
    [JsonPropertyName("lastName")] public string? LastName { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("extRef")] public string? ExternalReference { get; set; }
    [JsonPropertyName("language")] public string? Language { get; set; }

    /// <summary>Opt-out/unsubscribe flag. Modeled as a non-nullable <see cref="JsonElement"/> (default value
    /// <see cref="JsonValueKind.Undefined"/>) rather than <c>bool?</c> or <c>JsonElement?</c> so three distinct states
    /// are observable: field absent (<see cref="JsonValueKind.Undefined"/>), field explicitly <c>null</c>
    /// (<see cref="JsonValueKind.Null"/>), and field present with a value. A <c>bool?</c> or a nullable
    /// <c>JsonElement?</c> cannot make the first two distinctions: System.Text.Json's nullable-wrapper handling
    /// collapses an explicit JSON <c>null</c> down to a CLR <c>null</c> before the inner converter ever runs, which is
    /// indistinguishable from the property being absent. <see cref="JsonIgnoreCondition.WhenWritingDefault"/> keeps the
    /// field out of outgoing create/update payloads entirely when it was never set, rather than serializing the
    /// otherwise-unrepresentable <see cref="JsonValueKind.Undefined"/> sentinel.</summary>
    [JsonPropertyName("unsubscribed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Unsubscribed { get; set; }

    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Qualtrics XM Directory mailing list record. Endpoint shape based on publicly documented Qualtrics API v3
/// contract (GET/POST <c>/directories/{directoryId}/mailinglists</c>, GET <c>/directories/{directoryId}/mailinglists/{mailingListId}</c>);
/// NOT verified against a live tenant.</summary>
public sealed class QualtricsMailingList
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("ownerId")] public string? OwnerId { get; set; }
    [JsonPropertyName("contactCount")] public int? ContactCount { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>A partial update to a <see cref="QualtricsContact"/>: only fields the caller explicitly touched via a
/// <c>Set*</c> method are serialized into the outgoing request body, leaving every other field untouched server-side.
/// This avoids the data-loss risk of sending a full <see cref="QualtricsContact"/> on a PUT, where a field the caller
/// never meant to change (left at its CLR default, e.g. <c>null</c>) would overwrite the contact's existing value.
/// Each field uses the same three-state <see cref="JsonElement"/> encoding as <see cref="QualtricsContact.Unsubscribed"/>
/// so a caller can distinguish "leave untouched" (never call the setter) from "explicitly clear to null" (call the
/// setter with <c>null</c>). Endpoint shape based on the publicly documented Qualtrics API v3 contract; NOT verified
/// against a live tenant.</summary>
public sealed class ContactChange
{
    [JsonPropertyName("firstName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement FirstName { get; private set; }
    [JsonPropertyName("lastName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement LastName { get; private set; }
    [JsonPropertyName("email"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Email { get; private set; }
    [JsonPropertyName("extRef"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement ExternalReference { get; private set; }
    [JsonPropertyName("language"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Language { get; private set; }
    [JsonPropertyName("unsubscribed"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public JsonElement Unsubscribed { get; private set; }

    public ContactChange SetFirstName(string? value) { FirstName = ToElement(value); return this; }
    public ContactChange SetLastName(string? value) { LastName = ToElement(value); return this; }
    public ContactChange SetEmail(string? value) { Email = ToElement(value); return this; }
    public ContactChange SetExternalReference(string? value) { ExternalReference = ToElement(value); return this; }
    public ContactChange SetLanguage(string? value) { Language = ToElement(value); return this; }
    public ContactChange SetUnsubscribed(bool? value) { Unsubscribed = value is null ? ToElement((string?)null) : JsonSerializer.SerializeToElement(value.Value); return this; }

    /// <summary>True once at least one <c>Set*</c> call has touched a field; an empty change would be a no-op PUT
    /// that should never be sent.</summary>
    public bool HasAnyChange =>
        FirstName.ValueKind != JsonValueKind.Undefined || LastName.ValueKind != JsonValueKind.Undefined ||
        Email.ValueKind != JsonValueKind.Undefined || ExternalReference.ValueKind != JsonValueKind.Undefined ||
        Language.ValueKind != JsonValueKind.Undefined || Unsubscribed.ValueKind != JsonValueKind.Undefined;

    private static JsonElement ToElement(string? value) => JsonSerializer.SerializeToElement(value);
}

public sealed partial class QualtricsClient
{
    /// <summary>Lists every contact in a directory. Endpoint shape based on publicly documented Qualtrics API v3 contract
    /// (<c>GET /directories/{directoryId}/contacts</c>); NOT verified against a live tenant. Read-only and enabled by
    /// default; reuses the same bounded, cycle-guarded pagination as <see cref="EnumerateSurveysAsync"/>.</summary>
    public IAsyncEnumerable<QualtricsContact> ListContactsAsync(string directoryId, CancellationToken cancellationToken = default)
        => QualtricsPaging.EnumerateAsync<QualtricsContact>(_root, DirectoryAddress(directoryId, "/contacts"), _maxPages, _transport, cancellationToken);

    /// <summary>Fetches exactly one page of contacts; pass the previous call's <see cref="Page{T}.Next"/> as
    /// <paramref name="resumeFrom"/> to pause and resume. Endpoint shape based on publicly documented Qualtrics API v3
    /// contract; NOT verified against a live tenant.</summary>
    public Task<Page<QualtricsContact>> ListContactsPageAsync(string directoryId, ContinuationToken? resumeFrom = null, CancellationToken cancellationToken = default)
    {
        var fingerprint = "contacts:" + EndpointSafety.Segment(directoryId);
        var address = resumeFrom is null ? DirectoryAddress(directoryId, "/contacts") : QualtricsPaging.ResolveResume(_root, fingerprint, resumeFrom);
        return QualtricsPaging.FetchPageAsync<QualtricsContact>(fingerprint, _root, address, _transport, cancellationToken);
    }

    /// <summary>Fetches a single contact by ID. Endpoint shape based on publicly documented Qualtrics API v3 contract
    /// (<c>GET /directories/{directoryId}/contacts/{contactId}</c>); NOT verified against a live tenant.</summary>
    public async Task<QualtricsContact> GetContactAsync(string directoryId, string contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _transport.ReadJsonAsync(ContactAddress(directoryId, contactId), cancellationToken: cancellationToken);
            return doc.RootElement.GetProperty("result").Deserialize<QualtricsContact>() ?? throw new JsonException("Missing contact.");
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Creates a contact. Explicit opt-in write; <paramref name="writesEnabled"/> must be set to <c>true</c> by
    /// the caller after verifying directory write permissions. Endpoint shape based on publicly documented Qualtrics API
    /// v3 contract (<c>POST /directories/{directoryId}/contacts</c>); NOT verified against a live tenant.</summary>
    public async Task<string> CreateContactAsync(string directoryId, QualtricsContact contact, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying XM Directory contact-create support and permissions.");
        EndpointSafety.Segment(directoryId);
        ArgumentNullException.ThrowIfNull(contact);
        if (string.IsNullOrWhiteSpace(contact.Email) && string.IsNullOrWhiteSpace(contact.ExternalReference))
            throw new ArgumentException("A contact requires at least an email address or external reference before any request is sent.", nameof(contact));
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Post, DirectoryAddress(directoryId, "/contacts"),
                JsonSerializer.SerializeToElement(contact), cancellationToken: cancellationToken);
            var id = doc.RootElement.GetProperty("result").GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(id)) throw new JsonException("Missing created contact ID.");
            return id;
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Updates a contact. Explicit opt-in write; <paramref name="writesEnabled"/> must be set to <c>true</c> by
    /// the caller after verifying directory write permissions. Endpoint shape based on publicly documented Qualtrics API
    /// v3 contract (<c>PUT /directories/{directoryId}/contacts/{contactId}</c>); NOT verified against a live tenant.</summary>
    public async Task UpdateContactAsync(string directoryId, string contactId, QualtricsContact contact, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying XM Directory contact-update support and permissions.");
        ArgumentNullException.ThrowIfNull(contact);
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Put, ContactAddress(directoryId, contactId),
                JsonSerializer.SerializeToElement(contact), cancellationToken: cancellationToken);
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Updates only the fields explicitly set on <paramref name="change"/>, leaving every other field on the
    /// contact untouched server-side. Prefer this over the full-<see cref="QualtricsContact"/> overload when only a
    /// subset of fields is known to be changing, since sending a full contact risks silently clearing fields the
    /// caller never meant to touch. Explicit opt-in write; <paramref name="writesEnabled"/> must be set to <c>true</c>
    /// by the caller after verifying directory write permissions. Endpoint shape based on the publicly documented
    /// Qualtrics API v3 contract (<c>PUT /directories/{directoryId}/contacts/{contactId}</c>); NOT verified against a
    /// live tenant.</summary>
    public async Task UpdateContactAsync(string directoryId, string contactId, ContactChange change, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying XM Directory contact-update support and permissions.");
        ArgumentNullException.ThrowIfNull(change);
        if (!change.HasAnyChange) throw new ArgumentException("ContactChange has no fields set; nothing to update.", nameof(change));
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Put, ContactAddress(directoryId, contactId),
                JsonSerializer.SerializeToElement(change), cancellationToken: cancellationToken);
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Lists every mailing list in a directory. Endpoint shape based on publicly documented Qualtrics API v3
    /// contract (<c>GET /directories/{directoryId}/mailinglists</c>); NOT verified against a live tenant.</summary>
    public IAsyncEnumerable<QualtricsMailingList> ListMailingListsAsync(string directoryId, CancellationToken cancellationToken = default)
        => QualtricsPaging.EnumerateAsync<QualtricsMailingList>(_root, DirectoryAddress(directoryId, "/mailinglists"), _maxPages, _transport, cancellationToken);

    /// <summary>Fetches a single mailing list by ID. Endpoint shape based on publicly documented Qualtrics API v3
    /// contract (<c>GET /directories/{directoryId}/mailinglists/{mailingListId}</c>); NOT verified against a live tenant.</summary>
    public async Task<QualtricsMailingList> GetMailingListAsync(string directoryId, string mailingListId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await _transport.ReadJsonAsync(DirectoryAddress(directoryId, "/mailinglists/" + EndpointSafety.Segment(mailingListId)), cancellationToken: cancellationToken);
            return doc.RootElement.GetProperty("result").Deserialize<QualtricsMailingList>() ?? throw new JsonException("Missing mailing list.");
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Creates a mailing list. Explicit opt-in write; <paramref name="writesEnabled"/> must be set to <c>true</c>
    /// by the caller after verifying directory write permissions. Endpoint shape based on publicly documented Qualtrics
    /// API v3 contract (<c>POST /directories/{directoryId}/mailinglists</c>); NOT verified against a live tenant.</summary>
    public async Task<string> CreateMailingListAsync(string directoryId, QualtricsMailingList mailingList, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying XM Directory mailing-list-create support and permissions.");
        ArgumentNullException.ThrowIfNull(mailingList);
        if (string.IsNullOrWhiteSpace(mailingList.Name))
            throw new ArgumentException("A mailing list requires a name before any request is sent.", nameof(mailingList));
        EndpointSafety.Segment(directoryId);
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Post, DirectoryAddress(directoryId, "/mailinglists"),
                JsonSerializer.SerializeToElement(mailingList), cancellationToken: cancellationToken);
            var id = doc.RootElement.GetProperty("result").GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(id)) throw new JsonException("Missing created mailing list ID.");
            return id;
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    private Uri DirectoryAddress(string directoryId, string suffix = "") => new(_root, "directories/" + EndpointSafety.Segment(directoryId) + suffix);
    private Uri ContactAddress(string directoryId, string contactId) => new(_root, "directories/" + EndpointSafety.Segment(directoryId) + "/contacts/" + EndpointSafety.Segment(contactId));
}

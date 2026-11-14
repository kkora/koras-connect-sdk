using System.Text.Json;

namespace Koras.Connect.Salesforce;

public sealed partial class SalesforceClient
{
    /// <summary>Uploads a file as a <c>ContentVersion</c> record (SFD-016), reusing the standard sObject-create
    /// mechanism (SFD-004) with a Base64-encoded <c>VersionData</c> field. <paramref name="content"/> is read
    /// into memory in full to Base64-encode it in one call - true chunked/streamed Base64 encoding is not
    /// implemented this pass, matching the Dataverse file-column connector's identical single-request-only
    /// limitation. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> UploadContentVersionAsync(string title, string pathOnClient, Stream content, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        if (string.IsNullOrWhiteSpace(title) || title.Length > 255) throw new ArgumentException("Title must be 1-255 characters.", nameof(title));
        if (string.IsNullOrWhiteSpace(pathOnClient) || pathOnClient.Length > 500) throw new ArgumentException("PathOnClient must be 1-500 characters.", nameof(pathOnClient));
        ArgumentNullException.ThrowIfNull(content);
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var fields = JsonSerializer.SerializeToElement(new { Title = title, PathOnClient = pathOnClient, VersionData = Convert.ToBase64String(buffer.ToArray()) });
        return await CreateRecordAsync("ContentVersion", fields, writesEnabled, cancellationToken);
    }

    /// <summary>Associates a previously uploaded <c>ContentDocument</c> with another record via a
    /// <c>ContentDocumentLink</c> record (SFD-016). <paramref name="shareType"/>/<paramref name="visibility"/>
    /// are passed through verbatim rather than validated against an enumerated value set - the documented value
    /// sets were not independently re-confirmed this pass, so server-side validation is relied on instead of a
    /// guessed local allowlist. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task LinkContentDocumentAsync(string contentDocumentId, string linkedEntityId, string shareType, string visibility,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateRecordId(contentDocumentId);
        ValidateRecordId(linkedEntityId);
        if (string.IsNullOrWhiteSpace(shareType)) throw new ArgumentException("ShareType is required.", nameof(shareType));
        if (string.IsNullOrWhiteSpace(visibility)) throw new ArgumentException("Visibility is required.", nameof(visibility));
        var fields = JsonSerializer.SerializeToElement(new { ContentDocumentId = contentDocumentId, LinkedEntityId = linkedEntityId, ShareType = shareType, Visibility = visibility });
        await CreateRecordAsync("ContentDocumentLink", fields, writesEnabled, cancellationToken);
    }
}

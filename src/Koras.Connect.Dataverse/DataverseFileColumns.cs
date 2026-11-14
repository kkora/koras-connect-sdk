using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

public sealed partial class DataverseClient
{
    /// <summary>Uploads content to a file/image column via single-request <c>PATCH</c> with
    /// <c>Content-Type: application/octet-stream</c> and <c>x-ms-file-name</c> (DVT-016). Covers files under the
    /// documented 128 MB single-request limit; chunked upload (for larger files or when a tenant's documented
    /// recommended chunk size applies) is not implemented this pass - a documented gap, not a silent truncation.
    /// Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task UploadFileColumnAsync(string entitySet, Guid id, string column, Stream content, string fileName,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and the column's configured size limit.");
        ValidateEntitySet(entitySet);
        ValidateNavigationProperty(column);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Any(c => c is '\r' or '\n'))
            throw new ArgumentException("A single-line file name is required.", nameof(fileName));
        var address = new Uri(_root, $"{entitySet}({id:D})/{column}");
        ValidateRequestUri(address);
        var headers = new Dictionary<string, string>(RequiredHeaders) { ["x-ms-file-name"] = fileName };
        try { await _transport.UploadAsync(HttpMethod.Patch, address, content, "application/octet-stream", headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Downloads a file/image column's content via <c>GET .../{column}/$value</c> (DVT-016), reusing
    /// <see cref="IntegrationTransport.CopyToAsync"/>'s existing streaming primitive unchanged.</summary>
    public async Task DownloadFileColumnAsync(string entitySet, Guid id, string column, Stream destination, CancellationToken cancellationToken = default)
    {
        ValidateEntitySet(entitySet);
        ValidateNavigationProperty(column);
        var address = new Uri(_root, $"{entitySet}({id:D})/{column}/$value");
        ValidateRequestUri(address);
        try { await _transport.CopyToAsync(address, destination, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Removes a file/image column's content via <c>DELETE</c> on the column URL (DVT-016). Disabled
    /// unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task DeleteFileColumnAsync(string entitySet, Guid id, string column, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        ValidateNavigationProperty(column);
        var address = new Uri(_root, $"{entitySet}({id:D})/{column}");
        ValidateRequestUri(address);
        try { await _transport.SendResponseAsync(HttpMethod.Delete, address, null, RequiredHeaders, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }
}

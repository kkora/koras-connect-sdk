using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

public sealed partial class GraphClient
{
    /// <summary>[Flagged conflicting official figures, carried forward honestly - MGR-010.] The v1.0 API
    /// reference states a 250MB simple-upload hard limit via <c>PUT .../content</c>, while an older OneDrive
    /// developer-center page states &lt;4MB; the two were not reconciled during research. This SDK enforces the
    /// more conservative 4MB figure as the simple-upload ceiling until independently reconciled.</summary>
    public const long ConservativeSimpleUploadByteLimit = 4 * 1024 * 1024;

    /// <summary>Uploads a small file via <c>PUT /drives/{driveId}/root:/{parentPath}/{fileName}:/content</c>
    /// (MGR-010). <paramref name="content"/> must be seekable so its length can be checked against
    /// <see cref="ConservativeSimpleUploadByteLimit"/> before any request is sent - a file at or above that
    /// conservative ceiling is rejected locally rather than attempted and failed against the server's actual
    /// (unreconciled) limit; a resumable upload session (MGR-023) is the documented path for larger files, not
    /// yet implemented in this version. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task UploadSmallFileAsync(string driveId, string? parentPath, string fileName, Stream content, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the drive-write privilege and business impact.");
        if (string.IsNullOrWhiteSpace(driveId)) throw new ArgumentException("A drive id is required.", nameof(driveId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("A file name is required.", nameof(fileName));
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek) throw new ArgumentException("content must be seekable so its length can be checked against the conservative simple-upload ceiling.", nameof(content));
        if (content.Length > ConservativeSimpleUploadByteLimit)
            throw new InvalidOperationException($"File exceeds the conservative {ConservativeSimpleUploadByteLimit}-byte simple-upload ceiling (MGR-010); use a resumable upload session (MGR-023) instead.");
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var encodedPath = string.Join("/", path.Split('/').Where(s => s.Length > 0).Select(Uri.EscapeDataString));
        var address = new Uri(_root, $"drives/{Uri.EscapeDataString(driveId)}/root:/{encodedPath}:/content");
        try { await _transport.UploadAsync(HttpMethod.Put, address, content, "application/octet-stream", cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Downloads a file's content via <c>GET /drives/{driveId}/items/{itemId}/content</c> (MGR-010),
    /// streamed directly to <paramref name="destination"/> without whole-file buffering.</summary>
    public async Task DownloadFileAsync(string driveId, string itemId, Stream destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(driveId)) throw new ArgumentException("A drive id is required.", nameof(driveId));
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("An item id is required.", nameof(itemId));
        ArgumentNullException.ThrowIfNull(destination);
        var address = new Uri(_root, $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/content");
        try { await _transport.CopyToAsync(address, destination, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }
}

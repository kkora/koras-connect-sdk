using System.Collections.Concurrent;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A resumable upload session (MGR-023). <see cref="ExpirationDateTime"/> is nullable because no
/// fixed default session duration is documented - expiration is dynamic/sliding, not a constant this SDK can
/// assume.</summary>
public sealed record GraphUploadSession(Uri UploadUrl, DateTimeOffset? ExpirationDateTime);

public sealed partial class GraphClient
{
    /// <summary>Confirmed exactly: every fragment except the final one must be a multiple of this many bytes
    /// (320 KiB) - MGR-023.</summary>
    public const long UploadFragmentSizeMultiple = 327_680;

    /// <summary>Confirmed exactly: the documented per-request cap for a single fragment PUT - MGR-023.</summary>
    public const long MaxUploadFragmentSize = 60 * 1024 * 1024 - 1;

    /// <summary>Tracks the next expected fragment start byte per upload session, since sequential order is a
    /// hard, documented requirement enforced locally before any out-of-order fragment is sent. Keyed by
    /// <see cref="GraphUploadSession.UploadUrl"/>, the session's own identity.</summary>
    private readonly ConcurrentDictionary<Uri, long> _nextExpectedUploadRangeStart = new();

    /// <summary>Creates a resumable upload session via <c>POST .../root:/{parentPath}/{fileName}:/createUploadSession</c>
    /// (MGR-023), for files too large for <see cref="UploadSmallFileAsync"/>'s simple-upload ceiling. Disabled
    /// unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<GraphUploadSession> CreateUploadSessionAsync(string driveId, string? parentPath, string fileName, long totalSize,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the drive-write privilege and business impact.");
        if (string.IsNullOrWhiteSpace(driveId)) throw new ArgumentException("A drive id is required.", nameof(driveId));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("A file name is required.", nameof(fileName));
        if (totalSize <= 0) throw new ArgumentOutOfRangeException(nameof(totalSize), "totalSize must be positive.");
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var encodedPath = string.Join("/", path.Split('/').Where(s => s.Length > 0).Select(Uri.EscapeDataString));
        var address = new Uri(_root, $"drives/{Uri.EscapeDataString(driveId)}/root:/{encodedPath}:/createUploadSession");
        var payload = JsonSerializer.SerializeToElement(new { item = new Dictionary<string, object?>() });
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            var uploadUrl = new Uri(RequireString(root, "uploadUrl"));
            DateTimeOffset? expiration = root.TryGetProperty("expirationDateTime", out var exp) && exp.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(exp.GetString(), out var parsed) ? parsed : null;
            _nextExpectedUploadRangeStart[uploadUrl] = 0;
            return new GraphUploadSession(uploadUrl, expiration);
        }
    }

    /// <summary>Uploads one fragment via <c>PUT {session.UploadUrl}</c> with the documented <c>Content-Range</c>
    /// header (MGR-023). The fragment size (<c>rangeEnd - rangeStart + 1</c>) must be a multiple of
    /// <see cref="UploadFragmentSizeMultiple"/> unless this is the final fragment (<c>rangeEnd + 1 == totalSize</c>),
    /// and fragments must arrive in strict sequential order - both are rejected locally, before any HTTP call,
    /// since the vendor documents them as hard requirements rather than best-effort guidance.</summary>
    public async Task UploadSessionFragmentAsync(GraphUploadSession session, Stream fragment, long rangeStart, long rangeEnd, long totalSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(fragment);
        if (rangeStart < 0 || rangeEnd < rangeStart) throw new ArgumentOutOfRangeException(nameof(rangeEnd), "rangeEnd must be >= rangeStart >= 0.");
        if (rangeEnd >= totalSize) throw new ArgumentOutOfRangeException(nameof(rangeEnd), "rangeEnd must be less than totalSize.");
        var fragmentSize = rangeEnd - rangeStart + 1;
        var isFinalFragment = rangeEnd + 1 == totalSize;
        if (fragmentSize > MaxUploadFragmentSize) throw new ArgumentException($"A fragment must not exceed {MaxUploadFragmentSize} bytes.", nameof(rangeEnd));
        if (!isFinalFragment && fragmentSize % UploadFragmentSizeMultiple != 0)
            throw new ArgumentException($"Every fragment except the final one must be a multiple of {UploadFragmentSizeMultiple} bytes.", nameof(rangeEnd));
        var expectedStart = _nextExpectedUploadRangeStart.GetOrAdd(session.UploadUrl, 0);
        if (rangeStart != expectedStart)
            throw new InvalidOperationException($"Fragments must be uploaded in sequential order; expected rangeStart {expectedStart} but got {rangeStart}.");
        var headers = new Dictionary<string, string> { ["Content-Range"] = $"bytes {rangeStart}-{rangeEnd}/{totalSize}" };
        try { await _transport.UploadAsync(HttpMethod.Put, session.UploadUrl, fragment, "application/octet-stream", headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        _nextExpectedUploadRangeStart[session.UploadUrl] = rangeEnd + 1;
    }

    /// <summary>Reads an upload session's current status via <c>GET {session.UploadUrl}</c> (MGR-023), returning
    /// the documented <c>nextExpectedRanges</c> so an interrupted upload can resume without starting a fresh
    /// session. The first entry's start offset also resynchronizes this client's local sequential-order tracking
    /// with the server's actual state - necessary after a dropped connection or a process restart, since the
    /// in-memory tracking in <see cref="UploadSessionFragmentAsync"/> does not itself persist across either.</summary>
    public async Task<IReadOnlyList<string>> GetUploadSessionStatusAsync(GraphUploadSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(session.UploadUrl, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("nextExpectedRanges", out var ranges) || ranges.ValueKind != JsonValueKind.Array)
                throw new ContractException("Upload session status did not contain nextExpectedRanges.", new ErrorDetails("graph_upload_session_contract"));
            var values = ranges.EnumerateArray().Where(r => r.ValueKind == JsonValueKind.String).Select(r => r.GetString()!).ToArray();
            if (values.Length > 0 && long.TryParse(values[0].Split('-')[0], out var nextStart))
                _nextExpectedUploadRangeStart[session.UploadUrl] = nextStart;
            return values;
        }
    }

    /// <summary>Cancels an upload session via <c>DELETE {session.UploadUrl}</c>, expecting <c>204</c> (MGR-023).</summary>
    public async Task CancelUploadSessionAsync(GraphUploadSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        try { await _transport.SendResponseAsync(HttpMethod.Delete, session.UploadUrl, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        finally { _nextExpectedUploadRangeStart.TryRemove(session.UploadUrl, out _); }
    }
}

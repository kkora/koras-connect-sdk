using System.Text.Json;
namespace Koras.Connect.Adp;

/// <summary>File-backed <see cref="IWorkerFingerprintStore"/> (ADP-006): a minimal, dependency-free durable
/// implementation of the interface's atomic-commit contract, closing the "no caller-owned persistent store exists
/// yet (only an in-memory test double)" gap. <see cref="CommitAsync"/> writes the new snapshot to a temporary file
/// beside the target path, then atomically replaces the target via <see cref="File.Move(string,string,bool)"/>
/// (overwrite) &#8212; on the same volume this is an OS-level rename, so a process crash mid-commit can never leave
/// a partially written or mixed-version file: the target either still holds the old snapshot (crash before the
/// rename completes) or the new one (crash after), never neither and never a mix. Not suitable for concurrent
/// writers to the same path; a single scan/commit cycle at a time matches <see cref="AdpChangeDetector"/>'s own
/// documented contract.</summary>
public sealed class FileWorkerFingerprintStore : IWorkerFingerprintStore
{
    private readonly string _path;

    public FileWorkerFingerprintStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file path is required.", nameof(path));
        _path = path;
    }

    /// <summary>Returns an empty snapshot if the file has never been committed, rather than throwing &#8212; the
    /// documented behavior for a first-ever scan.</summary>
    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return new Dictionary<string, string>();
        await using var stream = File.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: cancellationToken);
        return loaded ?? new Dictionary<string, string>();
    }

    public async Task CommitAsync(IReadOnlyDictionary<string, string> fingerprints, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(_path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, fingerprints, cancellationToken: cancellationToken);
        File.Move(tempPath, fullPath, overwrite: true);
    }
}

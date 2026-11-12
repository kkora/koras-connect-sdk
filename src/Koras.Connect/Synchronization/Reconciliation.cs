using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
namespace Koras.Connect.Synchronization;

public sealed record RecordChange<T>(string Key, string Fingerprint, T Value);
/// <summary>Stateless full-snapshot reconciliation. Caller commits fingerprints only after successful downstream delivery.</summary>
public static class Reconciliation
{
    public static async IAsyncEnumerable<RecordChange<T>> DetectChangesAsync<T>(IAsyncEnumerable<T> records,
        Func<T, string> keySelector, IReadOnlyDictionary<string, string> previous,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            var key = keySelector(record);
            if (string.IsNullOrWhiteSpace(key) || !seen.Add(key)) throw new InvalidDataException("Missing or duplicate record key; snapshot cannot be committed.");
            var fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(record)));
            if (!previous.TryGetValue(key, out var old) || !string.Equals(old, fingerprint, StringComparison.Ordinal))
                yield return new(key, fingerprint, record);
        }
    }
    public static async IAsyncEnumerable<IReadOnlyList<T>> BatchAsync<T>(IAsyncEnumerable<T> records, int size,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (size is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(size));
        var batch = new List<T>(size);
        await foreach (var record in records.WithCancellation(cancellationToken))
        { batch.Add(record); if (batch.Count == size) { yield return batch.ToArray(); batch.Clear(); } }
        if (batch.Count > 0) yield return batch.ToArray();
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>One page of a SOQL query result (SFD-005/006). <see cref="Done"/> mirrors the documented response
/// field exactly; <see cref="NextRecordsUrl"/> is non-null only when <see cref="Done"/> is <c>false</c>.</summary>
public sealed record SalesforceQueryResult<T>(int TotalSize, bool Done, IReadOnlyList<T> Records, ContinuationToken? NextRecordsUrl);

public sealed partial class SalesforceClient
{
    private const int MinBatchSize = 200;
    private const int MaxBatchSize = 2000;

    /// <summary>Executes a SOQL query via <c>GET .../query?q={soql}</c>, or <c>.../queryAll</c> when
    /// <paramref name="includeDeleted"/> additionally includes soft-deleted/archived records (SFD-005). Returns
    /// only the first page - use <see cref="EnumerateQueryAsync{T}"/> to traverse the whole result set.</summary>
    public Task<SalesforceQueryResult<T>> QueryAsync<T>(string soql, bool includeDeleted = false, int? batchSize = null, CancellationToken cancellationToken = default)
        => QueryPageAsync<T>(soql, includeDeleted, resumeFrom: null, batchSize, cancellationToken);

    /// <summary>Enumerates every page of a SOQL query result, transparently following <c>nextRecordsUrl</c>
    /// (SFD-006) - the opaque query locator it carries is never decoded, only forwarded via
    /// <see cref="EndpointSafety.Continuation"/>.</summary>
    public async IAsyncEnumerable<T> EnumerateQueryAsync<T>(string soql, bool includeDeleted = false, int? batchSize = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ContinuationToken? next = null;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= _options.MaxPages) throw new InvalidDataException("Salesforce query pagination limit exceeded.");
            var result = next is null
                ? await QueryPageAsync<T>(soql, includeDeleted, null, batchSize, cancellationToken)
                : await QueryPageAsync<T>(soql, includeDeleted, next, batchSize, cancellationToken);
            foreach (var item in result.Records) { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
            if (result.NextRecordsUrl is null) yield break;
            if (!visited.Add(result.NextRecordsUrl.OpaqueValue)) throw new InvalidDataException("Salesforce query pagination cycle detected.");
            next = result.NextRecordsUrl;
        }
    }

    private async Task<SalesforceQueryResult<T>> QueryPageAsync<T>(string soql, bool includeDeleted, ContinuationToken? resumeFrom, int? batchSize, CancellationToken cancellationToken)
    {
        if (batchSize is not null && (batchSize < MinBatchSize || batchSize > MaxBatchSize))
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"batchSize must be between {MinBatchSize} and {MaxBatchSize}.");
        Uri address;
        if (resumeFrom is not null) address = EndpointSafety.Continuation(_root, resumeFrom.OpaqueValue);
        else
        {
            if (string.IsNullOrWhiteSpace(soql)) throw new ArgumentException("A SOQL statement is required.", nameof(soql));
            var resource = includeDeleted ? "queryAll" : "query";
            address = new Uri(_root, $"{resource}?q={Uri.EscapeDataString(soql)}");
        }
        IReadOnlyDictionary<string, string>? headers = batchSize is not null
            ? new Dictionary<string, string> { ["Sforce-Query-Options"] = $"batchSize={batchSize}" }
            : null;
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
                throw new ContractException("Query response did not contain a records array.", new ErrorDetails("query_contract"));
            if (!root.TryGetProperty("totalSize", out var totalSizeElement) || !totalSizeElement.TryGetInt32(out var totalSize))
                throw new ContractException("Query response missing totalSize.", new ErrorDetails("query_contract"));
            if (!root.TryGetProperty("done", out var doneElement) || doneElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new ContractException("Query response missing done.", new ErrorDetails("query_contract"));
            var done = doneElement.GetBoolean();
            var items = new List<T>();
            foreach (var item in records.EnumerateArray())
                items.Add(item.Deserialize<T>() ?? throw new JsonException("Null record in query result."));
            ContinuationToken? next = null;
            if (!done && root.TryGetProperty("nextRecordsUrl", out var nextUrl) && nextUrl.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(nextUrl.GetString()))
                next = new ContinuationToken("Salesforce", soql, nextUrl.GetString()!);
            return new SalesforceQueryResult<T>(totalSize, done, items, next);
        }
    }
}

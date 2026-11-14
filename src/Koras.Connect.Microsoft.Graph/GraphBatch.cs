using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>One sub-request within a <c>POST /$batch</c> call (MGR-021). <see cref="DependsOn"/> names sibling
/// <see cref="Id"/> values this request must execute after; a broken dependency chain surfaces
/// <c>424 Failed Dependency</c> for the dependent sub-request, reported like any other per-item outcome via
/// <see cref="BatchItemResult{T}"/>.</summary>
public sealed record GraphBatchRequest(string Id, HttpMethod Method, string Url, JsonElement? Body = null, IReadOnlyList<string>? DependsOn = null);

public sealed partial class GraphClient
{
    /// <summary>Maximum sub-requests per <c>/$batch</c> call, confirmed exactly (verified twice in the fetched
    /// documentation text) - MGR-021.</summary>
    public const int MaxBatchRequests = 20;

    /// <summary>Submits multiple sub-requests in one <c>POST /$batch</c> call (MGR-021). Each sub-request is
    /// independently throttle-evaluated server-side - this method does not retry an individual sub-request's
    /// own <c>429</c>; the caller must detect it via that item's <see cref="BatchItemResult{T}.ErrorCode"/>
    /// (<c>http_429</c>) and resubmit just that sub-request, per MGR-022's documented batch-retry caveat. The
    /// whole <c>/$batch</c> POST itself is never auto-retried, matching every other write operation's
    /// never-replay-an-uncertain-write policy. Disabled unless <paramref name="writesEnabled"/> is explicitly
    /// <c>true</c> when any contained sub-request is not a GET.</summary>
    public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
        IReadOnlyList<GraphBatchRequest> requests, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0) throw new ArgumentException("At least one request is required.", nameof(requests));
        if (requests.Count > MaxBatchRequests) throw new ArgumentException($"A batch may contain at most {MaxBatchRequests} requests.", nameof(requests));
        var requestIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < requests.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(requests[i].Id)) throw new ArgumentException("Each batch request id is required.", nameof(requests));
            if (!requestIndexById.TryAdd(requests[i].Id, i)) throw new ArgumentException($"Duplicate batch request id '{requests[i].Id}'.", nameof(requests));
        }
        var hasWrite = requests.Any(r => r.Method != HttpMethod.Get);
        if (hasWrite && !writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the privilege and business impact of every write sub-request.");

        var batchRequests = requests.Select(r => new Dictionary<string, object?>
        {
            ["id"] = r.Id,
            ["method"] = r.Method.Method,
            ["url"] = r.Url,
            ["body"] = r.Body,
            ["dependsOn"] = r.DependsOn,
        }).ToArray();
        var payload = JsonSerializer.SerializeToElement(new { requests = batchRequests });
        var address = new Uri(_root, "$batch");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("responses", out var responses) || responses.ValueKind != JsonValueKind.Array)
                throw new ContractException("Batch response did not contain a responses array.", new ErrorDetails("graph_batch_contract"));
            var results = new List<BatchItemResult<JsonElement>>();
            foreach (var item in responses.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String || !requestIndexById.TryGetValue(idElement.GetString()!, out var index))
                    throw new ContractException("Batch response item id did not match any submitted request.", new ErrorDetails("graph_batch_contract"));
                var status = item.TryGetProperty("status", out var statusElement) && statusElement.TryGetInt32(out var code) ? code : 0;
                var succeeded = status is >= 200 and < 300;
                var body = item.TryGetProperty("body", out var bodyElement) ? bodyElement : (JsonElement?)null;
                var errorCode = succeeded ? null : (body is not null ? GraphApiException.TryExtractErrorCode(body.Value) : null) ?? $"http_{status}";
                results.Add(new BatchItemResult<JsonElement>(index, succeeded, succeeded && body is not null ? body.Value.Clone() : default, errorCode));
            }
            return results.OrderBy(r => r.Index).ToList();
        }
    }
}

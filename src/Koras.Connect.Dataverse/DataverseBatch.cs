using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>One request inside a <c>$batch</c> call (DVT-007). <see cref="RelativeUrl"/> is relative to the Web API
/// root (for example <c>"accounts(...)"</c>), not the environment root.</summary>
public sealed record DataverseBatchRequest(HttpMethod Method, string RelativeUrl, JsonElement? Body = null, string? ContentId = null);

/// <summary>A changeset: an all-or-nothing group of write requests nested inside a batch. <c>GET</c> is not
/// permitted inside a changeset [D8] - rejected locally before any HTTP call.</summary>
public sealed record DataverseChangeset(IReadOnlyList<DataverseBatchRequest> Requests);

public sealed partial class DataverseClient
{
    /// <summary>Maximum individual requests (across top-level requests and every changeset member) permitted in
    /// one <c>$batch</c> call [D8].</summary>
    public const int MaxBatchRequests = 1000;

    /// <summary>Submits multiple create/update/delete/read operations in one HTTP round trip via the documented
    /// multipart/mixed <c>$batch</c> wire format (DVT-007). A caller never hand-constructs multipart text. A
    /// changeset failure is reported as a rollback for every one of its members, never as a partial success.
    /// Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c> when the batch contains any
    /// write, and never auto-retried (it may contain writes).</summary>
    public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
        IReadOnlyList<DataverseBatchRequest>? requests = null, IReadOnlyList<DataverseChangeset>? changesets = null,
        bool continueOnError = false, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        requests ??= [];
        changesets ??= [];
        var totalCount = requests.Count + changesets.Sum(c => c.Requests.Count);
        if (totalCount == 0) throw new ArgumentException("At least one request or changeset member is required.");
        if (totalCount > MaxBatchRequests) throw new ArgumentException($"A batch may contain at most {MaxBatchRequests} individual requests.");
        foreach (var request in requests) ValidateBatchPart(request);
        foreach (var changeset in changesets)
        {
            if (changeset.Requests.Count == 0) throw new ArgumentException("A changeset must contain at least one request.");
            foreach (var member in changeset.Requests)
            {
                if (member.Method == HttpMethod.Get) throw new ArgumentException("GET is not permitted inside a changeset.");
                ValidateBatchPart(member);
            }
        }
        var hasWrite = requests.Concat(changesets.SelectMany(c => c.Requests)).Any(r => r.Method != HttpMethod.Get);
        if (hasWrite && !writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");

        var batchBoundary = "batch_" + Guid.NewGuid().ToString("N");
        var body = BuildBatchBody(requests, changesets, batchBoundary);
        var headers = new Dictionary<string, string>(RequiredHeaders);
        if (continueOnError) headers["Prefer"] = "odata.continue-on-error";
        var address = new Uri(_root, "$batch");
        ValidateRequestUri(address, insideBatch: true);
        IntegrationRawResponse response;
        try { response = await _transport.SendRawAsync(HttpMethod.Post, address, body, $"multipart/mixed; boundary={batchBoundary}", headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        var responseBoundary = ExtractBoundary(response.ContentType)
            ?? throw new ContractException("Batch response did not declare a multipart boundary.", new ErrorDetails("batch_contract"));
        return ParseBatchResponse(response.Body, responseBoundary, requests, changesets);
    }

    /// <summary>Rejects a CR/LF in <see cref="DataverseBatchRequest.RelativeUrl"/>/<see cref="DataverseBatchRequest.ContentId"/>
    /// before either is concatenated into the hand-built multipart/mixed wire body. Both are caller-controlled
    /// strings; without this check a value containing "\r\n" could inject an extra request line or header into
    /// the batch body, undermining the documented guarantee that a caller never hand-constructs multipart text.</summary>
    private static void ValidateBatchPart(DataverseBatchRequest request)
    {
        if (ContainsCrOrLf(request.RelativeUrl)) throw new ArgumentException("RelativeUrl must not contain a carriage return or line feed.", nameof(request));
        if (request.ContentId is not null && ContainsCrOrLf(request.ContentId)) throw new ArgumentException("ContentId must not contain a carriage return or line feed.", nameof(request));
    }

    private static bool ContainsCrOrLf(string value) => value.Contains('\r') || value.Contains('\n');

    private static string BuildBatchBody(IReadOnlyList<DataverseBatchRequest> requests, IReadOnlyList<DataverseChangeset> changesets, string boundary)
    {
        var sb = new StringBuilder();
        foreach (var request in requests) AppendPart(sb, boundary, request, request.ContentId);
        foreach (var changeset in changesets)
        {
            var changesetBoundary = "changeset_" + Guid.NewGuid().ToString("N");
            sb.Append("--").Append(boundary).Append("\r\n");
            sb.Append("Content-Type: multipart/mixed; boundary=").Append(changesetBoundary).Append("\r\n\r\n");
            for (var i = 0; i < changeset.Requests.Count; i++)
                AppendPart(sb, changesetBoundary, changeset.Requests[i], changeset.Requests[i].ContentId ?? (i + 1).ToString(CultureInfo.InvariantCulture));
            sb.Append("--").Append(changesetBoundary).Append("--\r\n\r\n");
        }
        sb.Append("--").Append(boundary).Append("--\r\n");
        return sb.ToString();
    }

    private static void AppendPart(StringBuilder sb, string boundary, DataverseBatchRequest request, string? contentId)
    {
        sb.Append("--").Append(boundary).Append("\r\n");
        sb.Append("Content-Type: application/http\r\n");
        sb.Append("Content-Transfer-Encoding: binary\r\n");
        if (contentId is not null) sb.Append("Content-ID: ").Append(contentId).Append("\r\n");
        sb.Append("\r\n");
        sb.Append(request.Method.Method).Append(' ').Append(request.RelativeUrl).Append(" HTTP/1.1\r\n");
        if (request.Body is not null)
        {
            sb.Append("Content-Type: application/json\r\n\r\n");
            sb.Append(request.Body.Value.GetRawText()).Append("\r\n");
        }
        else sb.Append("\r\n");
        sb.Append("\r\n");
    }

    private static string? ExtractBoundary(string? contentType)
    {
        if (contentType is null) return null;
        const string marker = "boundary=";
        var idx = contentType.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var value = contentType[(idx + marker.Length)..].Trim();
        var semicolon = value.IndexOf(';');
        if (semicolon >= 0) value = value[..semicolon];
        return value.Trim().Trim('"');
    }

    private static IReadOnlyList<(Dictionary<string, string> Headers, string Content)> SplitMultipart(string text, string boundary)
    {
        var parts = new List<(Dictionary<string, string>, string)>();
        var delimiter = "--" + boundary;
        var segments = text.Split(delimiter, StringSplitOptions.None);
        foreach (var raw in segments.Skip(1))
        {
            if (raw.StartsWith("--", StringComparison.Ordinal)) break; // closing delimiter reached
            var segment = raw.TrimStart('\r', '\n');
            var headerEnd = segment.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (headerEnd < 0) continue;
            var headerBlock = segment[..headerEnd];
            var content = segment[(headerEnd + 4)..].TrimEnd('\r', '\n');
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in headerBlock.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
            {
                var colon = line.IndexOf(':');
                if (colon > 0) headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
            parts.Add((headers, content));
        }
        return parts;
    }

    private static (HttpStatusCode Status, JsonElement? Body) ParseHttpResponsePart(string content)
    {
        var lines = content.Split("\r\n");
        var statusParts = lines[0].Split(' ', 3);
        var status = (HttpStatusCode)int.Parse(statusParts[1], CultureInfo.InvariantCulture);
        var blankIndex = Array.IndexOf(lines, "");
        JsonElement? body = null;
        if (blankIndex >= 0 && blankIndex + 1 < lines.Length)
        {
            var bodyText = string.Join("\r\n", lines.Skip(blankIndex + 1)).Trim();
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                using var document = JsonDocument.Parse(bodyText);
                body = document.RootElement.Clone();
            }
        }
        return (status, body);
    }

    private static BatchItemResult<JsonElement> ToBatchItemResult(int index, string content)
    {
        var (status, body) = ParseHttpResponsePart(content);
        var succeeded = (int)status is >= 200 and < 300;
        return new BatchItemResult<JsonElement>(index, succeeded, succeeded && body is not null ? body.Value : default, succeeded ? null : $"http_{(int)status}");
    }

    private static IReadOnlyList<BatchItemResult<JsonElement>> ParseBatchResponse(string responseText, string boundary,
        IReadOnlyList<DataverseBatchRequest> requests, IReadOnlyList<DataverseChangeset> changesets)
    {
        var topParts = SplitMultipart(responseText, boundary);
        var results = new List<BatchItemResult<JsonElement>>();
        var index = 0;
        var partIndex = 0;
        foreach (var _ in requests)
        {
            if (partIndex >= topParts.Count) throw new ContractException("Batch response has fewer parts than requests.", new ErrorDetails("batch_contract"));
            results.Add(ToBatchItemResult(index++, topParts[partIndex++].Content));
        }
        foreach (var changeset in changesets)
        {
            if (partIndex >= topParts.Count) throw new ContractException("Batch response has fewer parts than changesets.", new ErrorDetails("batch_contract"));
            var (headers, content) = topParts[partIndex++];
            var isNestedMultipart = headers.TryGetValue("Content-Type", out var contentType) && contentType.Contains("multipart/mixed", StringComparison.OrdinalIgnoreCase);
            IReadOnlyList<(Dictionary<string, string> Headers, string Content)> nestedParts = [];
            if (isNestedMultipart)
            {
                var nestedBoundary = ExtractBoundary(contentType) ?? throw new ContractException("Changeset response did not declare a nested boundary.", new ErrorDetails("batch_contract"));
                nestedParts = SplitMultipart(content, nestedBoundary);
            }
            if (!isNestedMultipart || nestedParts.Count < changeset.Requests.Count)
            {
                // Fewer responses than members means the changeset rolled back atomically - the single part (when
                // present) is itself the documented error representation for the whole changeset, so it is safe
                // to replicate as every member's failure.
                var failure = isNestedMultipart && nestedParts.Count > 0
                    ? ToBatchItemResult(0, nestedParts[0].Content)
                    : !isNestedMultipart ? ToBatchItemResult(0, content) : new BatchItemResult<JsonElement>(0, false, default, "changeset_rolled_back");
                foreach (var _ in changeset.Requests) results.Add(failure with { Index = index++ });
                continue;
            }
            if (nestedParts.Count > changeset.Requests.Count)
            {
                // More parts than members is never a documented shape and, unlike the fewer-parts case, there is
                // no single part that represents a meaningful error for the whole changeset - nestedParts[0] here
                // is just an ordinary (likely successful) sub-response, not an error body. Accepting it as a
                // clean per-member list would append extra results and desynchronize every later changeset's
                // Index from its originating request, so this is reported as a generic contract mismatch instead
                // of guessing at a shape the vendor documentation never described.
                var failure = new BatchItemResult<JsonElement>(0, false, default, "changeset_part_count_mismatch");
                foreach (var _ in changeset.Requests) results.Add(failure with { Index = index++ });
                continue;
            }
            var memberResults = nestedParts.Select(part => ToBatchItemResult(0, part.Content)).ToArray();
            if (memberResults.Any(r => !r.Succeeded))
            {
                // Equal part count but at least one member failed: the changeset was still rolled back atomically -
                // report every member with that same failure rather than the misleading partial success of letting
                // each part's individual (and otherwise-successful-looking) status stand on its own.
                var failure = memberResults.First(r => !r.Succeeded);
                foreach (var _ in changeset.Requests) results.Add(failure with { Index = index++ });
                continue;
            }
            foreach (var result in memberResults) results.Add(result with { Index = index++ });
        }
        return results;
    }
}

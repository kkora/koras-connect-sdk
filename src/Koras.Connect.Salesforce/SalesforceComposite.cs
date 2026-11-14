using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>One subrequest within a <c>/composite</c> call (SFD-009). <see cref="ReferenceId"/> is used both to
/// identify this subrequest's own result and, via the documented <c>@{referenceId.field}</c> syntax embedded
/// directly in a later subrequest's <see cref="Url"/>/<see cref="Body"/>, to cross-reference this subrequest's
/// not-yet-committed result.</summary>
public sealed record SalesforceCompositeSubrequest(HttpMethod Method, string Url, string ReferenceId, JsonElement? Body = null);

public sealed partial class SalesforceClient
{
    /// <summary>Maximum subrequests per <c>/composite</c> call [snippet-sourced, not independently re-verified
    /// this session - see SFD-009's tracker].</summary>
    public const int MaxCompositeSubrequests = 25;

    private static readonly System.Text.RegularExpressions.Regex ReferenceIdPattern =
        new("^[A-Za-z][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Submits multiple, optionally cross-referencing subrequests in one <c>POST /composite</c> call
    /// (SFD-009). Field names (<c>compositeRequest</c>/<c>compositeResponse</c>, <c>httpStatusCode</c>, etc.) are
    /// snippet-sourced and not independently re-verified against a live page or org this session - a documented
    /// release blocker for calling this <c>verified</c> rather than <c>partial</c>. <c>/composite/batch</c> and
    /// <c>/composite/graph</c> are not implemented: their field names carry the same unverified snippet confidence
    /// and, unlike <c>/composite</c>, this feature's specification did not propose a concrete typed C# contract
    /// for either - implementing them would mean guessing a shape, which this SDK avoids.</summary>
    public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteCompositeAsync(
        IReadOnlyList<SalesforceCompositeSubrequest> requests, bool allOrNone = false, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0) throw new ArgumentException("At least one subrequest is required.", nameof(requests));
        if (requests.Count > MaxCompositeSubrequests) throw new ArgumentException($"A composite request may contain at most {MaxCompositeSubrequests} subrequests.", nameof(requests));
        foreach (var request in requests)
            if (string.IsNullOrWhiteSpace(request.ReferenceId) || !ReferenceIdPattern.IsMatch(request.ReferenceId))
                throw new ArgumentException("Each subrequest referenceId must be a simple identifier.", nameof(requests));
        var hasWrite = requests.Any(r => r.Method != HttpMethod.Get);
        if (hasWrite && !writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");

        var compositeRequest = requests.Select(r => new Dictionary<string, object?>
        {
            ["method"] = r.Method.Method,
            ["url"] = r.Url,
            ["referenceId"] = r.ReferenceId,
            ["body"] = r.Body,
        }).ToArray();
        var payload = JsonSerializer.SerializeToElement(new { allOrNone, compositeRequest });
        var address = new Uri(_root, "composite");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("compositeResponse", out var responses) || responses.ValueKind != JsonValueKind.Array)
                throw new ContractException("Composite response did not contain a compositeResponse array.", new ErrorDetails("composite_contract"));
            var results = new List<BatchItemResult<JsonElement>>();
            var index = 0;
            foreach (var item in responses.EnumerateArray())
            {
                var status = item.TryGetProperty("httpStatusCode", out var statusElement) && statusElement.TryGetInt32(out var code) ? code : 0;
                var succeeded = status is >= 200 and < 300;
                var body = item.TryGetProperty("body", out var bodyElement) ? bodyElement : (JsonElement?)null;
                var errorCode = succeeded ? null : (body is not null ? SalesforceApiException.TryExtractErrorCode(body.Value) : null) ?? $"http_{status}";
                results.Add(new BatchItemResult<JsonElement>(index++, succeeded, succeeded && body is not null ? body.Value.Clone() : default,
                    succeeded ? null : errorCode));
            }
            return results;
        }
    }
}

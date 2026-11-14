using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>A row deleted since the prior checkpoint, as reported by <see cref="DataverseClient.GetChangesAsync"/>
/// (DVT-012). Carries only the row id - no business data, matching Dataverse's own documented shape.</summary>
public sealed record DataverseDeletedRow(Guid Id);

/// <summary>One page of a change-tracked read (DVT-012). <see cref="Next"/> is either a plain
/// <c>@odata.nextLink</c> continuation (more of this same delta response to read) or, once the response is
/// exhausted, the <c>@odata.deltaLink</c> to resume from on the next sync - both are opaque and handled identically
/// by <c>DataverseClient.GetChangesAsync</c>; callers should not assume which kind they received.</summary>
public sealed record DataverseDeltaPage<T>(IReadOnlyList<T> Changed, IReadOnlyList<DataverseDeletedRow> Deleted, ContinuationToken? Next);

public sealed partial class DataverseClient
{
    /// <summary>Reads one page of changes since <paramref name="resumeFrom"/> (or, when null, the first page of a
    /// fresh change-tracked read) via <c>Prefer: odata.track-changes</c> (DVT-012). Change tracking must already be
    /// enabled for <paramref name="entitySet"/> by a tenant administrator (DVT-019's territory, irreversible once
    /// turned on) - the SDK does not enable it and does not pre-verify it is enabled; a "not enabled" response
    /// classifies through the shared path like any other denial. <paramref name="select"/> is the only query
    /// option exposed here: Dataverse documents <c>$filter</c>/<c>$orderby</c>/<c>$expand</c>/<c>$top</c> as
    /// incompatible with <c>odata.track-changes</c>, so - matching DVT-005's "unsupported options have no property
    /// to request them through" precedent - no parameter exists for them. Resuming from a delta token older than
    /// the tenant's retention window (7 days by default) is not distinguished from any other server error in this
    /// pass - the exact expired-token error shape was not independently confirmed (a documented, not fabricated,
    /// gap); callers should be prepared to fall back to a full resync on any unexpected failure here.</summary>
    public async Task<DataverseDeltaPage<JsonElement>> GetChangesAsync(string entitySet, string? select = null,
        ContinuationToken? resumeFrom = null, CancellationToken cancellationToken = default)
    {
        ValidateEntitySet(entitySet);
        Uri address;
        if (resumeFrom is not null) address = EndpointSafety.Continuation(_root, resumeFrom.OpaqueValue);
        else
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(select)) parts.Add("$select=" + Uri.EscapeDataString(select));
            address = new Uri(_root, entitySet + (parts.Count > 0 ? "?" + string.Join("&", parts) : ""));
        }
        ValidateRequestUri(address);
        var headers = new Dictionary<string, string>(RequiredHeaders) { ["Prefer"] = "odata.track-changes" };
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                throw new ContractException("Dataverse delta response did not contain a value array.", new ErrorDetails("delta_contract"));
            var changed = new List<JsonElement>();
            var deleted = new List<DataverseDeletedRow>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String && reason.GetString() == "deleted")
                {
                    if (!item.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String || !Guid.TryParse(idElement.GetString(), out var id))
                        throw new ContractException("Deleted-row entry did not carry a parsable id.", new ErrorDetails("delta_contract"));
                    deleted.Add(new DataverseDeletedRow(id));
                }
                else changed.Add(item.Clone());
            }
            ContinuationToken? next = null;
            if (root.TryGetProperty("@odata.nextLink", out var nextLink) && nextLink.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(nextLink.GetString()))
                next = new ContinuationToken("Dataverse", entitySet, nextLink.GetString()!);
            else if (root.TryGetProperty("@odata.deltaLink", out var deltaLink) && deltaLink.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(deltaLink.GetString()))
                next = new ContinuationToken("Dataverse", entitySet, deltaLink.GetString()!);
            return new DataverseDeltaPage<JsonElement>(changed, deleted, next);
        }
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>A resolved choice/status/state value plus its environment/language-specific display label (DVT-011).</summary>
public sealed record DataverseOptionSetValue(int Value, string? FormattedLabel);

public sealed partial class DataverseClient
{
    private const string FormattedValueAnnotation = "OData.Community.Display.V1.FormattedValue";

    /// <summary>Extracts the formatted-value annotation sibling property for <paramref name="property"/> on
    /// <paramref name="row"/> (DVT-011) - present only when the row was read with <c>includeFormattedValues: true</c>.
    /// Absence is not a parse failure: Dataverse simply omits the annotation on a plain read, so this returns
    /// <c>null</c> rather than throwing.</summary>
    public static string? TryGetFormattedValue(JsonElement row, string property)
    {
        if (row.ValueKind != JsonValueKind.Object) return null;
        if (!row.TryGetProperty($"{property}@{FormattedValueAnnotation}", out var value) || value.ValueKind != JsonValueKind.String) return null;
        return value.GetString();
    }

    /// <summary>Enumerates global option sets via the <c>GlobalOptionSetDefinitions</c> metadata entity set
    /// (DVT-011), optionally filtered to one by <paramref name="filterByName"/>. The entity-set name is corroborated
    /// by vendor-research search but not independently re-fetched this pass - treat as high-confidence, re-verify
    /// against a live tenant before release (documented gap, not silently upgraded to verified).</summary>
    public IAsyncEnumerable<JsonElement> EnumerateGlobalOptionSetsAsync(string? filterByName = null, CancellationToken cancellationToken = default)
    {
        var query = filterByName is null ? null : new DataverseQuery(Filter: $"Name eq '{EscapeKeyLiteral(filterByName)}'");
        return EnumerateMetadataEntityAsync("GlobalOptionSetDefinitions", query, cancellationToken);
    }

    /// <summary>Shared enumerator for Dataverse's PascalCase-named metadata-query entity sets (<c>EntityDefinitions</c>,
    /// <c>GlobalOptionSetDefinitions</c>) - these are SDK-hardcoded, never caller-supplied, so they bypass
    /// <see cref="ValidateEntitySet"/>'s lowercase business-table-name rule. Reuses the same pagination/cycle-guard
    /// discipline as <see cref="EnumerateAsync"/> (DVT-006).</summary>
    private async IAsyncEnumerable<JsonElement> EnumerateMetadataEntityAsync(string metadataEntitySet, DataverseQuery? query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var effectiveQuery = query ?? new DataverseQuery();
        Uri? next = null;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= _options.MaxPages) throw new InvalidDataException("Dataverse pagination limit exceeded.");
            var address = next ?? BuildMetadataEntityAddress(metadataEntitySet, effectiveQuery);
            ValidateRequestUri(address);
            JsonDocument document;
            try { document = await _transport.ReadJsonAsync(address, RequiredHeaders, cancellationToken); }
            catch (IntegrationRequestException ex) { throw Classify(ex); }
            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                    throw new ContractException("Dataverse metadata list response did not contain a value array.", new ErrorDetails("metadata_contract"));
                foreach (var item in value.EnumerateArray()) { cancellationToken.ThrowIfCancellationRequested(); yield return item.Clone(); }
                if (!root.TryGetProperty("@odata.nextLink", out var nextLink) || nextLink.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nextLink.GetString()))
                    yield break;
                var candidate = EndpointSafety.Continuation(_root, nextLink.GetString()!);
                if (!visited.Add(candidate.AbsoluteUri)) throw new InvalidDataException("Dataverse pagination cycle detected.");
                next = candidate;
            }
        }
    }

    private Uri BuildMetadataEntityAddress(string metadataEntitySet, DataverseQuery query)
    {
        var parts = new List<string>();
        void Add(string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) parts.Add(name + "=" + Uri.EscapeDataString(value)); }
        Add("$filter", query.Filter); Add("$select", query.Select);
        return new Uri(_root, metadataEntitySet + (parts.Count > 0 ? "?" + string.Join("&", parts) : ""));
    }
}

using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>OData query options shared by every Graph collection/read endpoint (MGR-019, pulled forward as a
/// structural dependency of MGR-004 and most other resource features - every collection endpoint needs
/// pagination before it needs anything else). <see cref="Count"/> and <see cref="Search"/> trigger the
/// documented <c>ConsistencyLevel: eventual</c> header (MGR-003) - the only two operations this connector
/// currently knows need it; other advanced <c>$filter</c> combinations on directory objects are not detected
/// and remain the caller's responsibility to flag if Graph later rejects them.</summary>
public sealed record GraphQuery(string? Select = null, string? Filter = null, string? OrderBy = null,
    string? Expand = null, string? Search = null, bool? Count = null, int? Top = null)
{
    internal bool RequiresConsistencyLevelEventual => Count == true || !string.IsNullOrWhiteSpace(Search);

    internal string ToQueryString()
    {
        var parameters = new List<string>();
        if (!string.IsNullOrWhiteSpace(Select)) parameters.Add("$select=" + Uri.EscapeDataString(Select));
        if (!string.IsNullOrWhiteSpace(Filter)) parameters.Add("$filter=" + Uri.EscapeDataString(Filter));
        if (!string.IsNullOrWhiteSpace(OrderBy)) parameters.Add("$orderby=" + Uri.EscapeDataString(OrderBy));
        if (!string.IsNullOrWhiteSpace(Expand)) parameters.Add("$expand=" + Uri.EscapeDataString(Expand));
        if (!string.IsNullOrWhiteSpace(Search)) parameters.Add("$search=" + Uri.EscapeDataString(Search));
        if (Count is not null) parameters.Add("$count=" + (Count.Value ? "true" : "false"));
        if (Top is not null) parameters.Add("$top=" + Top.Value);
        return parameters.Count == 0 ? "" : "?" + string.Join("&", parameters);
    }
}

public sealed partial class GraphClient
{
    /// <summary>Bounded max-pages guard, matching every existing connector's pagination safety net.</summary>
    private const int MaxPages = 10000;

    private static readonly IReadOnlyDictionary<string, string> ConsistencyLevelHeaders = new Dictionary<string, string> { ["ConsistencyLevel"] = "eventual" };

    private static void ValidateResourcePath(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || resourcePath.StartsWith('/') || resourcePath.Contains("://", StringComparison.Ordinal))
            throw new ArgumentException("A resource path must be a non-empty, relative Graph path segment.", nameof(resourcePath));
    }

    /// <summary>Enumerates every page of a Graph collection response, transparently following <c>@odata.nextLink</c>
    /// (MGR-019). The opaque continuation link is never decoded, only forwarded via <see cref="EndpointSafety.Continuation"/>.</summary>
    public async IAsyncEnumerable<T> EnumerateAsync<T>(string resourcePath, GraphQuery? query = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateResourcePath(resourcePath);
        var headers = query?.RequiresConsistencyLevelEventual == true ? ConsistencyLevelHeaders : null;
        Uri address = new(_root, resourcePath + (query?.ToQueryString() ?? ""));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= MaxPages) throw new InvalidDataException("Graph pagination limit exceeded.");
            JsonDocument document;
            try { document = await _transport.ReadJsonAsync(address, headers, cancellationToken); }
            catch (IntegrationRequestException ex) { throw Classify(ex); }
            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                    throw new ContractException("Graph response did not contain a value array.", new ErrorDetails("graph_collection_contract"));
                foreach (var item in value.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item.Deserialize<T>() ?? throw new JsonException("Null item in Graph collection response.");
                }
                if (!root.TryGetProperty("@odata.nextLink", out var nextLink) || nextLink.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nextLink.GetString()))
                    yield break;
                var next = EndpointSafety.Continuation(_root, nextLink.GetString()!);
                if (!visited.Add(next.AbsoluteUri)) throw new InvalidDataException("Graph pagination cycle detected.");
                address = next;
            }
        }
    }

    /// <summary>Reads a single Graph resource and deserializes it to <typeparamref name="T"/> - the single-item
    /// counterpart to <see cref="EnumerateAsync{T}"/>'s collection response, shared by every resource feature's
    /// "get one by id" method.</summary>
    private async Task<T> ReadResourceAsync<T>(Uri address, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
            return document.RootElement.Deserialize<T>() ?? throw new JsonException("Graph returned a null resource.");
    }

    private static string RequireString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ContractException($"Response missing required field '{property}'.", new ErrorDetails("graph_resource_contract"));
        return value.GetString()!;
    }
}

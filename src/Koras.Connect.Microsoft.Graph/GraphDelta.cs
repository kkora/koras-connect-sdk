using System.Net;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>One complete delta sync pass's accumulated items, plus the opaque delta link to resume from next
/// time (MGR-019). A single Graph response carries either <c>@odata.nextLink</c> (more pages in this pass) or
/// <c>@odata.deltaLink</c> (pass complete) - never both; this type represents only the terminal state after
/// every <c>nextLink</c> page has been followed.</summary>
public sealed record GraphDeltaPage<T>(IReadOnlyList<T> Items, ContinuationToken? DeltaLink);

/// <summary>A <c>410 Gone</c>/<c>syncStateNotFound</c> expired or evicted delta token (MGR-019). Directory/
/// education-object delta tokens have a fixed 7-day lifetime; Outlook-entity tokens have no fixed limit and can
/// be evicted earlier under server-side LRU pressure - either case surfaces here. Recovery is a fresh delta
/// session (optionally via the documented <c>$deltatoken=latest</c> shortcut), not a retry of the same token.</summary>
public sealed class GraphDeltaTokenExpiredException(string message, ErrorDetails details) : IntegrationException(message, details);

public sealed partial class GraphClient
{
    /// <summary>Runs one full delta sync pass via <c>GET {resourcePath}/delta</c> (or resumes from
    /// <paramref name="resumeFrom"/>), transparently following every <c>@odata.nextLink</c> page until the
    /// response carries <c>@odata.deltaLink</c> instead, and returns the accumulated items plus that delta link
    /// (MGR-019). Resource-type delta-capability (the documented ~26-type allowlist) is not validated locally -
    /// mapping an arbitrary caller-supplied <paramref name="resourcePath"/> to the vendor's singular resource-type
    /// name would require guessing a naming convention not specified anywhere; callers must ensure the target
    /// resource actually supports delta. A <c>410 Gone</c> response (an expired/evicted token) surfaces as
    /// <see cref="GraphDeltaTokenExpiredException"/>, distinct from every other failure.</summary>
    public async Task<GraphDeltaPage<T>> GetDeltaAsync<T>(string resourcePath, ContinuationToken? resumeFrom = null, CancellationToken cancellationToken = default)
    {
        Uri address;
        if (resumeFrom is not null) address = EndpointSafety.Continuation(_root, resumeFrom.OpaqueValue);
        else
        {
            ValidateResourcePath(resourcePath);
            address = new Uri(_root, resourcePath + "/delta");
        }
        var items = new List<T>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= MaxPages) throw new InvalidDataException("Graph delta pagination limit exceeded.");
            JsonDocument document;
            try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
            catch (IntegrationRequestException ex) when (ex.ResponseStatus == HttpStatusCode.Gone)
            {
                throw new GraphDeltaTokenExpiredException("The delta token has expired or been evicted (HTTP 410); restart the sync, optionally via $deltatoken=latest.",
                    new ErrorDetails("graph_delta_token_expired", ex.Headers.TryGetValue("request-id", out var ids) ? ids.FirstOrDefault() : null));
            }
            catch (IntegrationRequestException ex) { throw Classify(ex); }
            using (document)
            {
                var root = document.RootElement;
                if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                    throw new ContractException("Graph delta response did not contain a value array.", new ErrorDetails("graph_delta_contract"));
                foreach (var item in value.EnumerateArray())
                    items.Add(item.Deserialize<T>() ?? throw new JsonException("Null item in Graph delta response."));

                if (root.TryGetProperty("@odata.deltaLink", out var deltaLink) && deltaLink.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(deltaLink.GetString()))
                    return new GraphDeltaPage<T>(items, new ContinuationToken("MicrosoftGraph", resourcePath, deltaLink.GetString()!));

                if (!root.TryGetProperty("@odata.nextLink", out var nextLink) || nextLink.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nextLink.GetString()))
                    throw new ContractException("Graph delta response carried neither @odata.nextLink nor @odata.deltaLink.", new ErrorDetails("graph_delta_contract"));
                var next = EndpointSafety.Continuation(_root, nextLink.GetString()!);
                if (!visited.Add(next.AbsoluteUri)) throw new InvalidDataException("Graph delta pagination cycle detected.");
                address = next;
            }
        }
    }
}

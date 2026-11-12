using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;

namespace Koras.Connect.Qualtrics;

/// <summary>Reusable cursor-pagination engine shared by every Qualtrics API v3 list endpoint that follows the
/// "result.elements" / "result.nextPage" shape (surveys today; directory contacts and mailing lists reuse the same
/// engine rather than re-implementing cycle/bound guards). Every continuation link is confined to the configured
/// tenant API root via <see cref="EndpointSafety.Continuation"/> before it is ever sent.</summary>
internal static class QualtricsPaging
{
    /// <summary>Enumerates every page starting at <paramref name="start"/>, applying the same page-count and
    /// cycle-detection guard previously inlined in each call site.</summary>
    public static async IAsyncEnumerable<T> EnumerateAsync<T>(Uri root, Uri start, int maxPages, IntegrationTransport transport,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var next = start;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= maxPages || !visited.Add(next.AbsoluteUri)) throw new InvalidDataException("Pagination limit or cycle detected.");
            var fetched = await FetchPageAsync<T>("enumerate", root, next, transport, cancellationToken);
            foreach (var item in fetched.Items)
            { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
            if (fetched.Next is null) yield break;
            next = new Uri(fetched.Next.OpaqueValue, UriKind.Absolute);
        }
    }

    /// <summary>Fetches exactly one page from <paramref name="address"/> and projects the result into a
    /// provider-neutral <see cref="Page{T}"/>, carrying forward a bounded, origin-confined continuation.</summary>
    public static async Task<Page<T>> FetchPageAsync<T>(string queryFingerprint, Uri root, Uri address, IntegrationTransport transport,
        CancellationToken cancellationToken = default)
    {
        using var doc = await transport.ReadJsonAsync(address, cancellationToken: cancellationToken);
        var result = doc.RootElement.GetProperty("result");
        var items = new List<T>();
        foreach (var item in result.GetProperty("elements").EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(item.Deserialize<T>() ?? throw new JsonException("Null element in a Qualtrics page."));
        }
        ContinuationToken? next = null;
        if (result.TryGetProperty("nextPage", out var link) && !string.IsNullOrWhiteSpace(link.GetString()))
        {
            var resolved = EndpointSafety.Continuation(root, link.GetString()!);
            next = new ContinuationToken("qualtrics", queryFingerprint, resolved.AbsoluteUri);
        }
        return new Page<T>(items, next);
    }

    /// <summary>Validates a caller-supplied continuation against the query it was issued for before resolving it to an
    /// address, so a contacts continuation can never be replayed against the surveys endpoint (or vice versa).</summary>
    public static Uri ResolveResume(Uri root, string queryFingerprint, ContinuationToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!string.Equals(token.Provider, "qualtrics", StringComparison.Ordinal) || !string.Equals(token.QueryFingerprint, queryFingerprint, StringComparison.Ordinal))
            throw new ArgumentException("Continuation token does not match this query.", nameof(token));
        return EndpointSafety.Continuation(root, token.OpaqueValue);
    }
}

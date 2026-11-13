using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Transport;

namespace Koras.Connect.Adp.Workers;

public sealed record AdpWorkersOptions(Uri ApiRoot, int PageSize = 100)
{
    public void Validate()
    {
        if (!ApiRoot.IsAbsoluteUri || ApiRoot.Scheme != Uri.UriSchemeHttps || ApiRoot.UserInfo.Length != 0 || ApiRoot.Query.Length != 0 || ApiRoot.Fragment.Length != 0) throw new ArgumentException("API root must use HTTPS.");
        if (PageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(PageSize));
    }
}

public sealed class AdpWorkersClient
{
    private readonly IntegrationTransport _transport;
    private readonly AdpWorkersOptions _options;
    public AdpWorkersClient(IntegrationTransport transport, AdpWorkersOptions options)
    {
        options.Validate();
        _transport = transport;
        _options = options;
    }

    public Task<JsonDocument> GetWorkersPageAsync(int skip = 0, int? take = null, CancellationToken cancellationToken = default)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        var size = take ?? _options.PageSize;
        if (size is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(take));
        var root = _options.ApiRoot.AbsoluteUri.TrimEnd('/');
        var uri = new Uri($"{root}/hr/v2/workers?$top={size}&$skip={skip}");
        return ReadWorkersResponseAsync(uri, cancellationToken);
    }

    /// <summary>Reads a worker-read response, failing explicitly rather than attempting to parse a body if ADP
    /// answers with HTTP 202 Accepted &#8212; a documented possibility for an asynchronous retrieval protocol this
    /// SDK does not implement (ADP-005). Preserves the existing contract that a failure status still surfaces the
    /// raw <see cref="IntegrationRequestException"/> unchanged (handled by <see cref="IntegrationTransport"/> itself
    /// before this method's <c>consume</c> callback runs), since this calls the same underlying send path as
    /// <see cref="IntegrationTransport.ReadJsonAsync"/>, only additionally inspecting the success status code.</summary>
    private async Task<JsonDocument> ReadWorkersResponseAsync(Uri uri, CancellationToken cancellationToken)
    {
        var response = await _transport.SendResponseAsync(HttpMethod.Get, uri, cancellationToken: cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            response.Body.Dispose();
            throw new NotSupportedException("ADP returned HTTP 202 Accepted for a worker read, indicating an asynchronous retrieval protocol this SDK does not implement. Only the synchronous worker-read contract is supported.");
        }
        return response.Body;
    }

    /// <summary>Resumable, typed adapter over <see cref="GetWorkersPageAsync(int,int?,CancellationToken)"/>: wraps the
    /// one requested page as a <see cref="Page{T}"/> whose <see cref="Page{T}.Next"/> opaquely encodes the skip
    /// offset for the following page, or <c>null</c> once a page returns no workers. Pass that token to
    /// <see cref="GetWorkersPageAsPageAsync(ContinuationToken,CancellationToken)"/> to resume &#8212; a caller can
    /// persist just the token rather than re-running <see cref="EnumerateTypedWorkersAsync"/> from the start.</summary>
    public async Task<Page<Worker>> GetWorkersPageAsPageAsync(int skip = 0, int? take = null, CancellationToken cancellationToken = default)
    {
        var size = take ?? _options.PageSize;
        using var document = await GuardAsync("GetWorkersPage", () => GetWorkersPageAsync(skip, take, cancellationToken));
        if (!document.RootElement.TryGetProperty("workers", out var workers) || workers.ValueKind != JsonValueKind.Array)
            throw new JsonException("ADP response did not contain a workers array.");
        var items = workers.EnumerateArray().Select(w => w.Deserialize<Worker>() ?? throw new JsonException("Null worker.")).ToArray();
        var next = items.Length == 0 ? null
            : new ContinuationToken("ADP", $"workers:top={size}", (skip + items.Length).ToString(CultureInfo.InvariantCulture));
        return new Page<Worker>(items, next);
    }

    /// <summary>Resumes paging from a <see cref="ContinuationToken"/> previously returned by
    /// <see cref="GetWorkersPageAsPageAsync(int,int?,CancellationToken)"/>.</summary>
    public Task<Page<Worker>> GetWorkersPageAsPageAsync(ContinuationToken token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (token.Provider != "ADP") throw new ArgumentException("Continuation token belongs to a different provider.", nameof(token));
        if (!int.TryParse(token.OpaqueValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var skip) || skip < 0)
            throw new ArgumentException("Continuation token has an invalid opaque value.", nameof(token));
        return GetWorkersPageAsPageAsync(skip, take: null, cancellationToken);
    }

    public Task<JsonDocument> SearchWorkersAsync(string filter, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException("Filter is required.", nameof(filter));
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        return GuardAsync("SearchWorkers", () => ReadWorkersResponseAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers?$top={_options.PageSize}&$skip={skip}&$filter={Uri.EscapeDataString(filter)}"), cancellationToken));
    }

    /// <summary>Typed-whitelist overload of <see cref="SearchWorkersAsync(string,int,CancellationToken)"/>: builds
    /// the <c>$filter</c> fragment from a <see cref="WorkerFilter"/> instead of requiring the caller to hand-escape
    /// OData filter syntax (ADP-005).</summary>
    public Task<JsonDocument> SearchWorkersAsync(WorkerFilter filter, int skip = 0, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        return SearchWorkersAsync(filter.ToODataFilter(), skip, cancellationToken);
    }
    public Task<JsonDocument> GetMetadataAsync(CancellationToken cancellationToken = default)
        => GuardAsync("GetWorkersMetadata", () => _transport.ReadJsonAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers/meta"), cancellationToken: cancellationToken));
    public async Task<Worker?> GetWorkerAsync(string associateOid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(associateOid) || associateOid is "." or ".." || associateOid.Contains('/') || associateOid.Contains('\\')) throw new ArgumentException("Associate OID is required.", nameof(associateOid));
        using var doc = await GuardAsync("GetWorker", () => ReadWorkersResponseAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers/{Uri.EscapeDataString(associateOid)}"), cancellationToken));
        if (!doc.RootElement.TryGetProperty("workers", out var workers) || workers.ValueKind != JsonValueKind.Array) throw new JsonException("Expected workers array.");
        return workers.GetArrayLength() == 0 ? null : workers[0].Deserialize<Worker>();
    }
    /// <summary>Classifies a failed call into the structured <see cref="AdpApiException"/> hierarchy. Deliberately not
    /// applied to <see cref="GetWorkersPageAsync"/>, whose documented contract is to surface the raw
    /// <see cref="IntegrationRequestException"/> unmodified so pagination callers can inspect <c>ResponseStatus</c> directly.</summary>
    private static async Task<T> GuardAsync<T>(string operation, Func<Task<T>> call)
    {
        try { return await call(); }
        catch (IntegrationRequestException failure) { throw AdpApiException.Classify(operation, failure); }
    }
    public async IAsyncEnumerable<Worker> EnumerateTypedWorkersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in EnumerateWorkersAsync(cancellationToken))
            yield return item.Deserialize<Worker>() ?? throw new JsonException("Null worker.");
    }

    public async IAsyncEnumerable<JsonElement> EnumerateWorkersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skip = 0;
        string? previousPage = null;
        while (true)
        {
            using var document = await GuardAsync("EnumerateWorkers", () => GetWorkersPageAsync(skip, cancellationToken: cancellationToken));
            if (!document.RootElement.TryGetProperty("workers", out var workers) || workers.ValueKind != JsonValueKind.Array)
                throw new JsonException("ADP response did not contain a workers array.");
            var pageHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(workers.GetRawText())));
            if (workers.GetArrayLength() > 0 && pageHash == previousPage) throw new InvalidDataException("Server repeated a worker page; pagination stopped.");
            previousPage = pageHash;
            var count = 0;
            foreach (var worker in workers.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return worker.Clone();
                count++;
            }
            if (count == 0) yield break;
            if (skip >= 10_000_000) throw new InvalidOperationException("Pagination safety limit exceeded.");
            skip = checked(skip + count);
        }
    }
}


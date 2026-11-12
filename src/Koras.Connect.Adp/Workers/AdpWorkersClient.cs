using System.Runtime.CompilerServices;
using System.Text.Json;
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
        return _transport.ReadJsonAsync(uri, cancellationToken: cancellationToken);
    }

    public Task<JsonDocument> SearchWorkersAsync(string filter, int skip = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException("Filter is required.", nameof(filter));
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        return _transport.ReadJsonAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers?$top={_options.PageSize}&$skip={skip}&$filter={Uri.EscapeDataString(filter)}"), cancellationToken: cancellationToken);
    }
    public Task<JsonDocument> GetMetadataAsync(CancellationToken cancellationToken = default)
        => _transport.ReadJsonAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers/meta"), cancellationToken: cancellationToken);
    public async Task<Worker?> GetWorkerAsync(string associateOid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(associateOid) || associateOid is "." or ".." || associateOid.Contains('/') || associateOid.Contains('\\')) throw new ArgumentException("Associate OID is required.", nameof(associateOid));
        using var doc = await _transport.ReadJsonAsync(new Uri($"{_options.ApiRoot.AbsoluteUri.TrimEnd('/')}/hr/v2/workers/{Uri.EscapeDataString(associateOid)}"), cancellationToken: cancellationToken);
        if (!doc.RootElement.TryGetProperty("workers", out var workers) || workers.ValueKind != JsonValueKind.Array) throw new JsonException("Expected workers array.");
        return workers.GetArrayLength() == 0 ? null : workers[0].Deserialize<Worker>();
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
            using var document = await GetWorkersPageAsync(skip, cancellationToken: cancellationToken);
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


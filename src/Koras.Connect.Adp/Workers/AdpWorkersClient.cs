using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Transport;

namespace Koras.Connect.Adp.Workers;

public sealed record AdpWorkersOptions(Uri ApiRoot, int PageSize = 100)
{
    public void Validate()
    {
        if (ApiRoot.Scheme != Uri.UriSchemeHttps) throw new ArgumentException("API root must use HTTPS.");
        if (PageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(PageSize));
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
        if (size is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(take));
        var root = _options.ApiRoot.AbsoluteUri.TrimEnd('/');
        var uri = new Uri($"{root}/hr/v2/workers?$top={size}&$skip={skip}");
        return _transport.ReadJsonAsync(uri, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<JsonElement> EnumerateWorkersAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skip = 0;
        while (true)
        {
            using var document = await GetWorkersPageAsync(skip, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("workers", out var workers) || workers.ValueKind != JsonValueKind.Array)
                throw new JsonException("ADP response did not contain a workers array.");
            var count = 0;
            foreach (var worker in workers.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return worker.Clone();
                count++;
            }
            if (count < _options.PageSize || count == 0) yield break;
            skip = checked(skip + count);
        }
    }
}

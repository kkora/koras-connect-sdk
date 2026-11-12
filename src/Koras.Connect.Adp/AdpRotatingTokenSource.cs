using Koras.Connect.Abstractions;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
namespace Koras.Connect.Adp;

/// <summary>Resolves client credentials through a caller-supplied <see cref="ICredentialProvider{T}"/> so secrets
/// can be rotated (secret-manager refresh, configuration reload) without rebuilding the dependency-injection
/// container or recreating <see cref="AdpWorkersClient"/>/<see cref="AdpOperationsClient"/>. Re-resolves the
/// provider on every cache miss and swaps the underlying <see cref="ClientCredentialsTokenSource"/> whenever the
/// resolved credentials change; also exposes <see cref="Invalidate"/> so a host can force the next call to
/// re-acquire a token even when the provider's resolved value has not changed yet (for example immediately after
/// an out-of-band credential-rotation signal).</summary>
public sealed class AdpRotatingTokenSource : IAccessTokenSource, IDisposable
{
    private readonly ICredentialProvider<ClientCredentialsOptions> _credentials;
    private readonly Func<HttpClient> _httpClientFactory;
    private readonly TimeSpan _acquisitionTimeout;
    private readonly TimeProvider? _clock;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private ClientCredentialsOptions? _current;
    private ClientCredentialsTokenSource? _inner;

    public AdpRotatingTokenSource(ICredentialProvider<ClientCredentialsOptions> credentials, Func<HttpClient> httpClientFactory,
        TimeSpan acquisitionTimeout, TimeProvider? clock = null)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _acquisitionTimeout = acquisitionTimeout;
        _clock = clock;
    }

    public async ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var options = await _credentials.GetAsync(cancellationToken);
        options.Validate();
        ClientCredentialsTokenSource inner;
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_inner is null || _current is null || !_current.Equals(options))
            {
                _inner?.Dispose();
                _inner = new ClientCredentialsTokenSource(_httpClientFactory(), options, _clock, _acquisitionTimeout);
                _current = options;
            }
            inner = _inner;
        }
        finally { _mutex.Release(); }
        return await inner.GetTokenAsync(cancellationToken);
    }

    /// <summary>Forces the next <see cref="GetTokenAsync"/> call to re-acquire a token rather than use any cached
    /// value, for use after an external credential-rotation signal.</summary>
    public void Invalidate() => _inner?.Invalidate();

    public void Dispose() { _inner?.Dispose(); _mutex.Dispose(); }
}

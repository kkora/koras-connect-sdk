using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Abstractions;

namespace Koras.Connect.Authentication;

/// <summary>Wraps a credential value so accidental logging, serialization or exception text never exposes it.
/// <see cref="ToString"/> is overridden; nothing else inspects or redacts arbitrary members of <typeparamref name="T"/>,
/// so callers must still avoid dumping <see cref="Reveal"/>'s result anywhere it could be logged.</summary>
public sealed class RedactedCredential<T>(T value)
{
    private readonly T _value = value ?? throw new ArgumentNullException(nameof(value));
    public T Reveal() => _value;
    public override string ToString() => $"RedactedCredential<{typeof(T).Name}> {{ value = [REDACTED] }}";
}

/// <summary>Loads a client certificate from disk on first use and caches it, so repeated token acquisitions don't
/// re-read the file; <see cref="Invalidate"/> disposes the cached certificate and forces the next
/// <see cref="GetAsync"/> call to reload, for use after a certificate-rotation event (for example a renewed file
/// written to the same path). The underlying <see cref="ClientCertificateTransport.LoadPkcs12"/>/<see cref="ClientCertificateTransport.LoadPem"/>
/// calls remain synchronous file I/O; this type only adds caching and an explicit reload signal on top.</summary>
public sealed class CertificateCredentialProvider : ICredentialProvider<X509Certificate2>, IDisposable
{
    private readonly Func<X509Certificate2> _load;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private X509Certificate2? _cached;

    public CertificateCredentialProvider(Func<X509Certificate2> load) => _load = load ?? throw new ArgumentNullException(nameof(load));

    public static CertificateCredentialProvider FromPkcs12(string path, string? password = null)
        => new(() => ClientCertificateTransport.LoadPkcs12(path, password));

    public static CertificateCredentialProvider FromPem(string certificatePath, string privateKeyPath)
        => new(() => ClientCertificateTransport.LoadPem(certificatePath, privateKeyPath));

    public async ValueTask<X509Certificate2> GetAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try { return _cached ??= _load(); }
        finally { _mutex.Release(); }
    }

    /// <summary>Disposes the cached certificate and forces the next <see cref="GetAsync"/> call to reload from disk.</summary>
    public void Invalidate()
    {
        _mutex.Wait();
        try { _cached?.Dispose(); _cached = null; }
        finally { _mutex.Release(); }
    }

    public void Dispose() { _cached?.Dispose(); _mutex.Dispose(); }
}

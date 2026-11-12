namespace Koras.Connect.Abstractions;

/// <summary>Resolves a credential value, allowing reload/rotation without recreating dependents.</summary>
public interface ICredentialProvider<TCredential>
{
    ValueTask<TCredential> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Wraps a credential provider whose source value can change between calls without a reload signal.
/// Dependents that cache derived state (for example a token source) should re-resolve periodically rather than once.</summary>
public sealed class StaticCredentialProvider<TCredential>(TCredential value) : ICredentialProvider<TCredential>
{
    private readonly TCredential _value = value ?? throw new ArgumentNullException(nameof(value));
    public ValueTask<TCredential> GetAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_value);
}

/// <summary>Resolves the current value from a caller-supplied delegate on every call, enabling external rotation
/// (configuration reload, secret-manager refresh) without recreating the provider.</summary>
public sealed class DelegatingCredentialProvider<TCredential>(Func<CancellationToken, ValueTask<TCredential>> resolve) : ICredentialProvider<TCredential>
{
    private readonly Func<CancellationToken, ValueTask<TCredential>> _resolve = resolve ?? throw new ArgumentNullException(nameof(resolve));
    public ValueTask<TCredential> GetAsync(CancellationToken cancellationToken = default) => _resolve(cancellationToken);
}

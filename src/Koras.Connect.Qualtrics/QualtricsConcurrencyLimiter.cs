namespace Koras.Connect.Qualtrics;

/// <summary>Bounded-concurrency helper for fanning out independent Qualtrics calls (for example several surveys'
/// export pipelines running in parallel). Retry, backoff and server Retry-After handling remain the responsibility of
/// the shared <see cref="Koras.Connect.Transport.IntegrationTransport"/>; this type only bounds how many requests a
/// caller can have in flight at once so a loop over many surveys cannot accidentally issue an unbounded burst.</summary>
public sealed class QualtricsConcurrencyLimiter
{
    private readonly SemaphoreSlim _gate;

    public QualtricsConcurrencyLimiter(int maxConcurrency)
    {
        if (maxConcurrency is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Configure a bounded concurrency between 1 and 64.");
        _gate = new SemaphoreSlim(maxConcurrency, maxConcurrency);
    }

    /// <summary>Runs <paramref name="operation"/> once the gate admits it. Cancellation while waiting for a slot never
    /// starts the operation.</summary>
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await _gate.WaitAsync(cancellationToken);
        try { return await operation(cancellationToken); }
        finally { _gate.Release(); }
    }

    /// <summary>Runs <paramref name="operation"/> once per item with at most the configured number of concurrent calls
    /// in flight, awaiting every item before returning (or observing the first failure's exception).</summary>
    public Task RunAllAsync<T>(IEnumerable<T> items, Func<T, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(operation);
        return Task.WhenAll(items.Select(item => RunAsync(ct => Invoke(item, operation, ct), cancellationToken)));
    }

    private static async Task<object?> Invoke<T>(T item, Func<T, CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await operation(item, cancellationToken);
        return null;
    }
}

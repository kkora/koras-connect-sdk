using System.Net;
using System.Runtime.CompilerServices;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Export;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-010: strengthens the existing "deadline versus caller token" coverage and closes the two gaps its
/// own status history flagged as open — "during auth" cancellation and the "partial destination" (canceled
/// mid-export) case.</summary>
public sealed class CancellationAndTimeoutTests
{
    private sealed class SlowHandler : HttpMessageHandler
    { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) { await Task.Delay(Timeout.InfiniteTimeSpan, ct); return new(HttpStatusCode.OK); } }
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }

    [Fact]
    public async Task TheConfiguredDeadlineThrowsTheSpecificDeadlineExceededTypeNotAGenericCancellation()
    {
        // SecurityAndLifecycleTests.DeadlineCancelsInFlightResponse only asserts the OperationCanceledException
        // base type, which a plain caller cancellation would also satisfy - this asserts the specific subtype that
        // actually distinguishes "the SDK's own deadline fired" from "the caller cancelled".
        using var h = new HttpClient(new SlowHandler());
        var transport = new IntegrationTransport(h, new Token(), new() { RequestTimeout = TimeSpan.FromMilliseconds(20) });

        await Assert.ThrowsAsync<DeadlineExceededException>(() => transport.ReadJsonAsync(new("https://api.test")));
    }

    [Fact]
    public async Task AnExplicitCallerCancellationIsNeverMisreportedAsTheConfiguredDeadline()
    {
        using var h = new HttpClient(new SlowHandler());
        var transport = new IntegrationTransport(h, new Token(), new() { RequestTimeout = TimeSpan.FromMinutes(5) });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transport.ReadJsonAsync(new("https://api.test"), cancellationToken: cts.Token));

        Assert.IsNotType<DeadlineExceededException>(ex);
    }

    [Fact]
    public async Task CancellingDuringAnInFlightTokenAcquisitionPropagatesFromTheCallerNotOnlyAnInternalTimeout()
    {
        using var h = new HttpClient(new SlowHandler());
        using var source = new ClientCredentialsTokenSource(h, new ClientCredentialsOptions(new Uri("https://auth.test"), "id", "secret"),
            acquisitionTimeout: TimeSpan.FromMinutes(5));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.GetTokenAsync(cts.Token).AsTask());
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Caller cancellation during auth took {stopwatch.Elapsed}; it must not wait for the internal acquisition timeout.");
    }

    [Fact]
    public async Task CancelingMidExportLeavesWhateverWasAlreadyStreamedInTheDestinationWhichTheCallerMustTreatAsInvalid()
    {
        // TabularExport never buffers a whole population before writing (see its class-level documentation), so a
        // cancellation partway through necessarily leaves a syntactically-incomplete, unusable destination behind -
        // the caller owns the stream and is responsible for discarding it, not the SDK (which cannot un-write bytes
        // another layer may already be reading from a pipe/network stream).
        using var cts = new CancellationTokenSource();
        async IAsyncEnumerable<IReadOnlyList<string?>> Rows([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return ["first"];
            cts.Cancel();
            await Task.Delay(1, CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            yield return ["never-reached"];
        }
        using var destination = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => TabularExport.WriteCsvAsync(destination, ["Column"], Rows(cts.Token), cts.Token));

        Assert.True(destination.Length > 0, "The already-streamed first row must still have reached the destination before cancellation.");
    }
}

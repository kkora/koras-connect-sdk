using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-023: large file transfer and resumability - the 320 KiB fragment-size multiple, strict
/// sequential ordering, resume via <c>nextExpectedRanges</c>, and cancellation.</summary>
public sealed class GraphUploadSessionTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private const long Multiple = GraphClient.UploadFragmentSizeMultiple;

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task CreateUploadSessionRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateUploadSessionAsync("d1", null, "big.bin", 1_000_000));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateUploadSessionComposesTheDocumentedAddressAndParsesTheResponse()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"uploadUrl":"https://up.example.com/session1","expirationDateTime":"2026-11-15T00:00:00Z"}""")); });
        var session = await client.CreateUploadSessionAsync("d1", "Documents", "big.bin", 1_000_000, writesEnabled: true);
        Assert.Equal("https://up.example.com/session1", session.UploadUrl.AbsoluteUri);
        Assert.NotNull(session.ExpirationDateTime);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/root:/Documents/big.bin:/createUploadSession", request!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UploadSessionFragmentWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session-gate"), null);
        using var fragment = new MemoryStream(new byte[Multiple]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadSessionFragmentAsync(session, fragment, 0, Multiple - 1, Multiple));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CancelUploadSessionWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session-gate2"), null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CancelUploadSessionAsync(session));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task NonFinalFragmentNotAMultipleOf320KiBIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session1"), null);
        using var fragment = new MemoryStream(new byte[100]);
        await Assert.ThrowsAsync<ArgumentException>(() => client.UploadSessionFragmentAsync(session, fragment, 0, 99, 1_000_000, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AFinalFragmentSmallerThanTheMultipleIsAllowed()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") }); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session1a"), null);
        var totalSize = Multiple + 100;
        using var first = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, first, 0, Multiple - 1, totalSize, writesEnabled: true);
        using var final = new MemoryStream(new byte[100]);
        await client.UploadSessionFragmentAsync(session, final, Multiple, totalSize - 1, totalSize, writesEnabled: true);
        Assert.Equal($"bytes {Multiple}-{totalSize - 1}/{totalSize}", request!.Content!.Headers.GetValues("Content-Range").Single());
    }

    [Fact]
    public async Task AnOutOfOrderFragmentIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") }); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session2"), null);
        var totalSize = Multiple * 3;
        using var fragment = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, fragment, 0, Multiple - 1, totalSize, writesEnabled: true);
        Assert.Equal(1, calls);
        using var outOfOrder = new MemoryStream(new byte[Multiple]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadSessionFragmentAsync(session, outOfOrder, Multiple * 2, Multiple * 3 - 1, totalSize, writesEnabled: true));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SequentialFragmentsSucceedInOrder()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") }); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session3"), null);
        var totalSize = Multiple * 2;
        using var first = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, first, 0, Multiple - 1, totalSize, writesEnabled: true);
        using var second = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, second, Multiple, totalSize - 1, totalSize, writesEnabled: true);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task TheFinalFragmentsCompletionRemovesTheSessionsTrackingEntriesRatherThanLeakingThem()
    {
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") }));
        var session = new GraphUploadSession(new Uri("https://up.example.com/session-complete"), null);
        using var fragment = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, fragment, 0, Multiple - 1, Multiple, writesEnabled: true);
        // The session completed (final fragment accepted) - a fresh fragment for the same URL claiming to start
        // at 0 again must be treated as a brand-new session (starting offset 0), not rejected as "out of order"
        // against a stale completed-session entry that should no longer exist.
        using var again = new MemoryStream(new byte[Multiple]);
        await client.UploadSessionFragmentAsync(session, again, 0, Multiple - 1, Multiple, writesEnabled: true);
    }

    [Fact]
    public async Task GetUploadSessionStatusReturnsNextExpectedRangesAndResyncsOrderTracking()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"nextExpectedRanges":["327680-"],"expirationDateTime":"2026-11-15T00:00:00Z"}""")));
        var session = new GraphUploadSession(new Uri("https://up.example.com/session4"), null);
        var ranges = await client.GetUploadSessionStatusAsync(session);
        Assert.Equal(["327680-"], ranges);
    }

    [Fact]
    public async Task GetUploadSessionStatusResyncAllowsTheNextFragmentAtTheResumedOffsetRatherThanAtZero()
    {
        // Proves the resync in GetUploadSessionStatusAsync actually changes what UploadSessionFragmentAsync
        // accepts next, not just that it parses nextExpectedRanges correctly.
        var client = Client((r, _) => r.Method == HttpMethod.Get
            ? Task.FromResult(Json($$"""{"nextExpectedRanges":["{{Multiple}}-"],"expirationDateTime":"2026-11-15T00:00:00Z"}"""))
            : Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") }));
        var session = new GraphUploadSession(new Uri("https://up.example.com/session-resync"), null);
        await client.GetUploadSessionStatusAsync(session); // resyncs tracked offset to Multiple, as if resuming after a drop
        using var resumedFragment = new MemoryStream(new byte[Multiple]);
        // Would throw InvalidOperationException("...expected rangeStart 0...") if the resync had no effect.
        await client.UploadSessionFragmentAsync(session, resumedFragment, Multiple, Multiple * 2 - 1, Multiple * 3, writesEnabled: true);
    }

    [Fact]
    public async Task CancelUploadSessionSendsDelete()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session5"), null);
        await client.CancelUploadSessionAsync(session, writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, request!.Method);
        Assert.Equal("https://up.example.com/session5", request.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task CancelUploadSessionWaitsForAnInFlightFragmentUploadRatherThanRacingIt()
    {
        // Found missing by Phase 2 review pass 2: CancelUploadSessionAsync originally tore down the session's
        // tracking state with no synchronization against a concurrent, in-flight UploadSessionFragmentAsync call
        // for the same session. Proven here by gating the fragment PUT mid-flight and asserting the DELETE never
        // starts until the PUT completes - not just that both calls eventually happen.
        var fragmentPutStarted = new TaskCompletionSource();
        var releaseFragmentPut = new TaskCompletionSource();
        var order = new List<string>();
        var client = Client(async (r, ct) =>
        {
            if (r.Method == HttpMethod.Put)
            {
                lock (order) order.Add("PUT started");
                fragmentPutStarted.TrySetResult();
                await releaseFragmentPut.Task;
                lock (order) order.Add("PUT finished");
                return new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("{}") };
            }
            lock (order) order.Add("DELETE");
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var session = new GraphUploadSession(new Uri("https://up.example.com/session-race"), null);
        using var fragment = new MemoryStream(new byte[Multiple]);
        var fragmentTask = client.UploadSessionFragmentAsync(session, fragment, 0, Multiple - 1, Multiple * 2, writesEnabled: true);
        await fragmentPutStarted.Task;
        var cancelTask = client.CancelUploadSessionAsync(session, writesEnabled: true);
        await Task.Delay(50); // give Cancel every opportunity to race ahead if the lock weren't held
        lock (order) Assert.DoesNotContain("DELETE", order); // Cancel must still be waiting on the fragment's lock
        releaseFragmentPut.TrySetResult();
        await Task.WhenAll(fragmentTask, cancelTask);
        Assert.Equal(["PUT started", "PUT finished", "DELETE"], order);
    }
}

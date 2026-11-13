using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-005: direct unit coverage for <see cref="BearerTokenAuthenticator"/> and
/// <see cref="HeaderTokenAuthenticator"/> &#8212; previously exercised only indirectly through full
/// <see cref="IntegrationTransport"/> round-trips, never under their own name, despite "support bearer/header
/// adapters" and "invalid header token" being named unit-test requirements for this ticket. Uses only
/// <c>Koras.Connect.Core</c>/<c>Abstractions</c> types; no provider package is referenced.</summary>
public sealed class RequestAuthenticationTests
{
    private sealed class FakeTokenSource(Func<int, string> tokenForCall) : IAccessTokenSource
    {
        private int _calls;
        public FakeTokenSource(string fixedToken) : this(_ => fixedToken) { }
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(tokenForCall(Interlocked.Increment(ref _calls)));
        }
    }

    [Fact]
    public async Task BearerTokenAuthenticatorSetsTheAuthorizationHeaderFromTheTokenSource()
    {
        var authenticator = new BearerTokenAuthenticator(new FakeTokenSource("synthetic-token"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await authenticator.AuthenticateAsync(request);

        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("synthetic-token", request.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task BearerTokenAuthenticatorResolvesACurrentTokenOnEveryCallRatherThanCachingItself()
    {
        // Per-request token rotation: the authenticator itself must stay stateless and defer entirely to the
        // token source, so a rotating/refreshing source is reflected immediately on the very next request.
        var authenticator = new BearerTokenAuthenticator(new FakeTokenSource(call => $"token-{call}"));
        using var first = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        using var second = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await authenticator.AuthenticateAsync(first);
        await authenticator.AuthenticateAsync(second);

        Assert.Equal("token-1", first.Headers.Authorization!.Parameter);
        Assert.Equal("token-2", second.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task BearerTokenAuthenticatorPropagatesCancellationFromTheTokenSourceRatherThanSwallowingIt()
    {
        var authenticator = new BearerTokenAuthenticator(new FakeTokenSource("unused"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authenticator.AuthenticateAsync(request, cts.Token).AsTask());
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("host")]
    [InlineData("Authorization")]
    [InlineData("")]
    [InlineData("X Invalid Header")]
    [InlineData("X_Invalid_Header")]
    public void HeaderTokenAuthenticatorRejectsReservedOrMalformedHeaderNamesAtConstruction(string header)
        => Assert.Throws<ArgumentException>(() => new HeaderTokenAuthenticator(header, new FakeTokenSource("token")));

    [Fact]
    public async Task HeaderTokenAuthenticatorSetsTheConfiguredHeaderFromTheTokenSource()
    {
        var authenticator = new HeaderTokenAuthenticator("X-API-TOKEN", new FakeTokenSource("synthetic-token"));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await authenticator.AuthenticateAsync(request);

        Assert.Equal("synthetic-token", request.Headers.GetValues("X-API-TOKEN").Single());
        Assert.Null(request.Headers.Authorization); // never emits bearer authorization, per its own contract
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("has\rcarriage-return")]
    [InlineData("has\nnewline")]
    public async Task HeaderTokenAuthenticatorRejectsAnInvalidResolvedToken(string invalidToken)
    {
        var authenticator = new HeaderTokenAuthenticator("X-API-TOKEN", new FakeTokenSource(invalidToken));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await Assert.ThrowsAsync<InvalidOperationException>(() => authenticator.AuthenticateAsync(request).AsTask());
    }

    private sealed class RecordingAuthenticator(List<string> log) : IRequestAuthenticator
    {
        public ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        { log.Add(request.RequestUri!.AbsoluteUri); request.Headers.Add("X-Custom-Auth", "ok"); return ValueTask.CompletedTask; }
    }

    [Fact]
    public async Task IntegrationTransportAcceptsAnyIRequestAuthenticatorImplementationItHasNeverSeenBefore()
    {
        // Proves Koras.Connect.Core depends only on the IRequestAuthenticator contract, never on a concrete
        // Koras.Connect.Authentication/Core adapter type - a third, test-only implementation substitutes cleanly.
        var calls = new List<string>();
        using var handler = new RecordingHandler((_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent)));
        using var http = new HttpClient(handler);
        var transport = new IntegrationTransport(http, new RecordingAuthenticator(calls));

        using var doc = await transport.ReadJsonAsync(new Uri("https://example.test/resource"));

        Assert.Single(calls, "https://example.test/resource");
        Assert.Equal("ok", handler.LastRequestHeaders!.GetValues("X-Custom-Auth").Single());
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    {
        public System.Net.Http.Headers.HttpRequestHeaders? LastRequestHeaders { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { LastRequestHeaders = request.Headers; return reply(request, cancellationToken); }
    }
}

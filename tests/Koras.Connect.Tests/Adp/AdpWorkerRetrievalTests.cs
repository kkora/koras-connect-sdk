using System.Net;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-004: dedicated coverage for <see cref="AdpWorkersClient.GetWorkerAsync"/>'s documented edge cases
/// &#8212; empty and multiple-worker envelopes, malformed payloads and 404 classification &#8212; that were not
/// previously exercised directly (valid-worker shape and 403 classification are already covered by
/// <c>CoreTests.IndividualWorkerPreservesUnknownFieldsAndEscapesFilters</c> and
/// <c>AdpCorrelationTests.AdpWorkersClientGetWorkerAsyncCarriesTheRequestIdHeaderIntoTheClassifiedException</c>).</summary>
public sealed class AdpWorkerRetrievalTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body) };
    private static AdpWorkersClient ClientFor(Func<HttpRequestMessage, HttpResponseMessage> reply)
        => new(new(new HttpClient(new Handler(reply)), new Token()), new(new("https://api.test")));

    [Fact]
    public async Task EmptyWorkersArrayReturnsNullRatherThanThrowing()
    {
        var client = ClientFor(_ => Reply("""{"workers":[]}"""));
        Assert.Null(await client.GetWorkerAsync("w1"));
    }

    [Fact]
    public async Task MultipleWorkerEnvelopeReturnsOnlyTheFirstEntry()
    {
        var client = ClientFor(_ => Reply("""{"workers":[{"associateOID":"first"},{"associateOID":"second"}]}"""));
        var worker = await client.GetWorkerAsync("w1");
        Assert.Equal("first", worker!.AssociateOid);
    }

    [Fact]
    public async Task MissingWorkersArrayThrowsJsonException()
    {
        var client = ClientFor(_ => Reply("""{"unexpected":true}"""));
        await Assert.ThrowsAsync<JsonException>(() => client.GetWorkerAsync("w1"));
    }

    [Fact]
    public async Task NonObjectWorkerEntryThrowsJsonException()
    {
        var client = ClientFor(_ => Reply("""{"workers":["not-an-object"]}"""));
        await Assert.ThrowsAsync<JsonException>(() => client.GetWorkerAsync("w1"));
    }

    [Fact]
    public async Task NotFoundIsClassifiedAsContractException()
    {
        var client = ClientFor(_ => Reply("", HttpStatusCode.NotFound));
        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.GetWorkerAsync("w1"));
        Assert.Equal("ContractException", ex.Category);
        Assert.Equal("GetWorker", ex.Operation);
    }

    [Fact]
    public async Task AssociateOidWithReservedUriCharactersIsPercentEncodedNotInterpretedAsPathOrQuery()
    {
        using var http = new HttpClient(new Handler(r =>
        {
            Assert.Contains("/hr/v2/workers/oid%3Fx%3D1%26y%3D2", r.RequestUri!.AbsoluteUri);
            Assert.Empty(r.RequestUri!.Query);
            return Reply("""{"workers":[]}""");
        }));
        var client = new AdpWorkersClient(new(http, new Token()), new(new("https://api.test")));
        await client.GetWorkerAsync("oid?x=1&y=2");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    public async Task UnsafeOrEmptyIdentifiersAreRejectedBeforeAnyRequestIsSent(string associateOid)
    {
        var client = ClientFor(_ => throw new InvalidOperationException("Should not call the network."));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetWorkerAsync(associateOid));
    }
}

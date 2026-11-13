using System.Net;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-010 follow-up: now that <see cref="IntegrationRequestException.Headers"/> is populated on failure
/// too (not only on success), these tests drive a full mocked HTTP round trip through the actual client surfaces
/// &#8212; <see cref="AdpOperationsClient"/> and <see cref="AdpWorkersClient"/> &#8212; rather than calling the
/// <see cref="AdpApiException"/> classification helper directly, proving the whole pipeline (transport capture ->
/// exception construction -> classification) carries a correlation id through a terminal failure end to end.</summary>
public sealed class AdpCorrelationTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }

    private static HttpResponseMessage ForbiddenWithRequestId(string requestId)
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.TryAddWithoutValidation("request-id", requestId);
        return response;
    }

    [Fact]
    public async Task AdpOperationsClientReadAsyncCarriesTheRequestIdHeaderIntoTheClassifiedException()
    {
        using var http = new HttpClient(new Handler(_ => ForbiddenWithRequestId("corr-ops-403")));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.PayrollInstructions]);

        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.ReadAsync(AdpOperation.PayrollInstructions, "w1"));

        Assert.Equal("AuthorizationException", ex.Category);
        Assert.Equal("corr-ops-403", ex.Details.CorrelationId);
    }

    [Fact]
    public async Task AdpWorkersClientGetWorkerAsyncCarriesTheRequestIdHeaderIntoTheClassifiedException()
    {
        using var http = new HttpClient(new Handler(_ => ForbiddenWithRequestId("corr-workers-403")));
        var workers = new AdpWorkersClient(new(http, new Token()), new(new("https://api.test")));

        var ex = await Assert.ThrowsAsync<AdpApiException>(() => workers.GetWorkerAsync("w1"));

        Assert.Equal("AuthorizationException", ex.Category);
        Assert.Equal("corr-workers-403", ex.Details.CorrelationId);
    }

    [Fact]
    public async Task AnExplicitlySuppliedCorrelationIdOverridesTheOneCapturedFromTheResponseHeader()
    {
        // AdpApiException.Classify only defaults from the header when the caller does not supply one; prove that
        // still holds true when driven through the real transport failure path, not just the direct unit call.
        using var http = new HttpClient(new Handler(_ => ForbiddenWithRequestId("corr-from-header")));
        var transport = new IntegrationTransport(http, new Token());
        IntegrationRequestException? captured = null;
        try { await transport.ReadJsonAsync(new Uri("https://api.test/hr/v2/workers/meta")); }
        catch (IntegrationRequestException failure) { captured = failure; }

        Assert.NotNull(captured);
        Assert.Equal(new[] { "corr-from-header" }, captured!.Headers["request-id"]);
        var classified = AdpApiException.Classify(AdpOperation.EventNotifications, captured, correlationId: "explicit-override");
        Assert.Equal("explicit-override", classified.Details.CorrelationId);
    }

    [Fact]
    public async Task NoCorrelationHeaderMeansNoCorrelationIdRatherThanAFabricatedOne()
    {
        using var http = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.PayrollInstructions]);

        var ex = await Assert.ThrowsAsync<AdpApiException>(() => client.ReadAsync(AdpOperation.PayrollInstructions, "w1"));

        Assert.Null(ex.Details.CorrelationId);
    }
}

using System.Net;
using System.Text;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-002: instance discovery and API versioning - the unauthenticated version-list endpoint.</summary>
public sealed class SalesforceDiscoveryTests
{
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ListApiVersionsParsesTheDocumentedShape()
    {
        var client = new HttpClient(new Handler((_, _) => Task.FromResult(Json("""[{"label":"Summer '14","url":"/services/data/v31.0","version":"31.0"}]"""))));
        var versions = await SalesforceDiscovery.ListApiVersionsAsync(client, new Uri("https://login.salesforce.com/"));
        Assert.Single(versions);
        Assert.Equal("31.0", versions[0].Version);
    }

    [Fact]
    public async Task ListApiVersionsSendsNoAuthorizationHeader()
    {
        AuthenticationHeaderValueHolder? header = null;
        var client = new HttpClient(new Handler((r, _) => { header = new(r.Headers.Authorization); return Task.FromResult(Json("[]")); }));
        await SalesforceDiscovery.ListApiVersionsAsync(client, new Uri("https://login.salesforce.com/"));
        Assert.Null(header!.Value);
    }

    private sealed record AuthenticationHeaderValueHolder(System.Net.Http.Headers.AuthenticationHeaderValue? Value);

    [Fact]
    public async Task UnreachableHostSurfacesATransportExceptionNotASilentEmptyList()
    {
        var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("{}") })));
        await Assert.ThrowsAsync<SalesforceApiException>(() => SalesforceDiscovery.ListApiVersionsAsync(client, new Uri("https://login.salesforce.com/")));
    }

    [Fact]
    public async Task MalformedResponseSurfacesAContractErrorNotACrash()
    {
        var client = new HttpClient(new Handler((_, _) => Task.FromResult(Json("""{"not":"an array"}"""))));
        await Assert.ThrowsAsync<ContractException>(() => SalesforceDiscovery.ListApiVersionsAsync(client, new Uri("https://login.salesforce.com/")));
    }
}

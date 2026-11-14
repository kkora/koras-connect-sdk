using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-008: organization and tenant info - the value-array unwrap and the documented
/// User.Read-only restricted-field (null, not omitted) shape.</summary>
public sealed class GraphOrganizationTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task FullDetailResponseParsesCorrectly()
    {
        Uri? requested = null;
        var client = Client((r, _) =>
        {
            requested = r.RequestUri;
            return Task.FromResult(Json("""
                {"value":[{"id":"org1","displayName":"Contoso","verifiedDomains":[{"name":"contoso.com","isDefault":true,"capability":"Email","type":"Managed"}]}]}
                """));
        });
        var org = await client.GetOrganizationAsync();
        Assert.Equal("org1", org.Id);
        Assert.Equal("Contoso", org.DisplayName);
        Assert.Equal(["contoso.com"], org.VerifiedDomains);
        Assert.Equal("https://graph.microsoft.com/v1.0/organization", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task UserReadOnlyRestrictedResponseMapsMissingFieldsToNullNotDefault()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"id":"org1","displayName":"Contoso","verifiedDomains":[{"name":"contoso.com"}]}]}""")));
        var org = await client.GetOrganizationAsync();
        Assert.Equal("Contoso", org.DisplayName);
        Assert.NotNull(org.VerifiedDomains);
    }

    [Fact]
    public async Task EmptyValueArraySurfacesAContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[]}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.GetOrganizationAsync());
    }

    [Fact]
    public async Task MissingVerifiedDomainsIsNullNotAnEmptyList()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"id":"org1"}]}""")));
        var org = await client.GetOrganizationAsync();
        Assert.Null(org.DisplayName);
        Assert.Null(org.VerifiedDomains);
    }
}

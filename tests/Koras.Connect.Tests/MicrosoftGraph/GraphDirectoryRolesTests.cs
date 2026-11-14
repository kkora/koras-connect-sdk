using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-006: directory roles and admin units - the corrected nested administrative-units path.</summary>
public sealed class GraphDirectoryRolesTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task EnumerateDirectoryRolesHitsTheDocumentedResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"r1","displayName":"Global Administrator"}]}""")); });
        var roles = new List<GraphDirectoryRole>();
        await foreach (var role in client.EnumerateDirectoryRolesAsync()) roles.Add(role);
        Assert.Single(roles);
        Assert.Equal("Global Administrator", roles[0].DisplayName);
        Assert.Equal("https://graph.microsoft.com/v1.0/directoryRoles", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateAdministrativeUnitsUsesTheCorrectedNestedPath()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"au1"}]}""")); });
        var units = new List<JsonDocument>();
        await foreach (var unit in client.EnumerateAdministrativeUnitsAsync()) units.Add(unit);
        Assert.Single(units);
        Assert.Equal("https://graph.microsoft.com/v1.0/directory/administrativeUnits", requested!.AbsoluteUri);
    }
}

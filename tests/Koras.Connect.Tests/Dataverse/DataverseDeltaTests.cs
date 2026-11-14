using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-012: change tracking and delta synchronization - the <c>odata.track-changes</c> request shape,
/// changed-vs-deleted row separation, and delta-link/next-link continuation.</summary>
public sealed class DataverseDeltaTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task RequestSendsTrackChangesPreference()
    {
        string? prefer = null;
        var client = Client((r, _) => { prefer = r.Headers.TryGetValues("Prefer", out var v) ? v.First() : null; return Task.FromResult(Json("""{"value":[],"@odata.deltaLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$deltatoken=abc"}""")); });
        await client.GetChangesAsync("accounts");
        Assert.Equal("odata.track-changes", prefer);
    }

    [Fact]
    public async Task ChangedAndDeletedRowsAreSeparatedCorrectly()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"value":[
              {"accountid":"11111111-1111-1111-1111-111111111111","name":"Contoso"},
              {"@odata.context":"https://contoso.api.crm.dynamics.com/api/data/v9.2/$metadata#$deltaEntity#accounts/$deletedEntity","id":"22222222-2222-2222-2222-222222222222","reason":"deleted"}
            ],"@odata.deltaLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$deltatoken=abc"}
            """)));
        var page = await client.GetChangesAsync("accounts");
        Assert.Single(page.Changed);
        Assert.Single(page.Deleted);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), page.Deleted[0].Id);
        Assert.NotNull(page.Next);
    }

    [Fact]
    public async Task MalformedDeletedRowSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"reason":"deleted"}]}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.GetChangesAsync("accounts"));
    }

    [Fact]
    public async Task NextLinkDuringAnInProgressDeltaResponseIsCarriedAsNext()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[],"@odata.nextLink":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$skiptoken=page2"}""")));
        var page = await client.GetChangesAsync("accounts");
        Assert.Contains("skiptoken", page.Next!.OpaqueValue);
    }

    [Fact]
    public async Task ResumingFromAPriorContinuationForwardsItUnmodified()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        var resumeFrom = new ContinuationToken("Dataverse", "accounts", "https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$deltatoken=abc");
        await client.GetChangesAsync("accounts", resumeFrom: resumeFrom);
        Assert.Equal("https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$deltatoken=abc", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task SelectIsComposedIntoTheQueryString()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await client.GetChangesAsync("accounts", select: "name,revenue");
        Assert.Contains("$select=name,revenue", Uri.UnescapeDataString(requested!.Query));
    }
}

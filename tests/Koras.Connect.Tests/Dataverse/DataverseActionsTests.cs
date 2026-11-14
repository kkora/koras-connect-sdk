using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-015: actions and functions - unbound/bound action URL composition, the fully-qualified namespace
/// requirement, function parameter-alias composition, and the write opt-in gate.</summary>
public sealed class DataverseActionsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");
    private static readonly Guid RowId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    private static JsonElement Parameters(string json = "{}") => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task UnboundActionPostsToTheActionNameDirectly()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(Json("{}")); });
        await client.InvokeUnboundActionAsync("Merge", Parameters("""{"Target":{}}"""), writesEnabled: true);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("https://contoso.api.crm.dynamics.com/api/data/v9.2/Merge", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task UnboundActionWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.InvokeUnboundActionAsync("Merge", Parameters()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task BoundActionUsesTheFullyQualifiedCrmNamespace()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("{}")); });
        await client.InvokeBoundActionAsync("accounts", RowId, "WinOpportunity", Parameters(), writesEnabled: true);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/Microsoft.Dynamics.CRM.WinOpportunity", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task FunctionComposesParameterAliasSyntax()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("{}")); });
        await client.InvokeFunctionAsync("RetrieveTotalRecordCount", new Dictionary<string, string> { ["EntityNames"] = "['account']" });
        var decoded = Uri.UnescapeDataString(requested!.AbsoluteUri);
        Assert.Contains("RetrieveTotalRecordCount(EntityNames=@p1)", decoded);
        Assert.Contains("@p1=['account']", decoded);
    }

    [Fact]
    public async Task FunctionWithoutParametersComposesEmptyParentheses()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("{}")); });
        await client.InvokeFunctionAsync("WhoAmI");
        Assert.Equal("https://contoso.api.crm.dynamics.com/api/data/v9.2/WhoAmI()", requested!.AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad Name")]
    [InlineData("1Action")]
    [InlineData("Action/Name")]
    public async Task UnsafeActionNamesAreRejectedLocally(string name)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.InvokeUnboundActionAsync(name, Parameters(), writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

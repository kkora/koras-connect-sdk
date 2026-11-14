using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-019: solution and customization metadata - the documented <c>MSCRM.SolutionUniqueName</c> header
/// mechanism on create. Solution import/export actions are explicitly deferred, not implemented.</summary>
public sealed class DataverseSolutionTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");
    private static readonly Guid RowId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    private static JsonElement Fields() => JsonDocument.Parse("""{"name":"Contoso"}""").RootElement;

    [Fact]
    public async Task CreateInSolutionSendsTheSolutionHeader()
    {
        string? header = null;
        var client = Client((r, _) =>
        {
            header = r.Headers.TryGetValues("MSCRM.SolutionUniqueName", out var v) ? v.First() : null;
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);
            response.Headers.Add("OData-EntityId", $"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})");
            return Task.FromResult(response);
        });
        var id = await client.CreateRowInSolutionAsync("accounts", Fields(), "contoso_solution", writesEnabled: true);
        Assert.Equal("contoso_solution", header);
        Assert.Equal(RowId, id);
    }

    [Fact]
    public async Task CreateInSolutionWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateRowInSolutionAsync("accounts", Fields(), "contoso_solution"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task InvalidSolutionNameIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.CreateRowInSolutionAsync("accounts", Fields(), "bad\nname", writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-004: table and row CRUD - the five documented request/response shapes, the explicit write opt-in
/// gate, and 404/malformed-response handling.</summary>
public sealed class DataverseRowCrudTests
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

    private static JsonElement Fields(string json = """{"name":"Contoso"}""") => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task CreateWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateRowAsync("accounts", Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CreateRecoversGuidFromODataEntityIdHeaderOnNoContent()
    {
        var client = Client((r, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NoContent);
            response.Headers.Add("OData-EntityId", $"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})");
            return Task.FromResult(response);
        });
        var id = await client.CreateRowAsync("accounts", Fields(), writesEnabled: true);
        Assert.Equal(RowId, id);
    }

    [Fact]
    public async Task CreateRecoversGuidFromODataIdAnnotationWhenRepresentationIsReturned()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})","name":"Contoso"}""", HttpStatusCode.Created)));
        var id = await client.CreateRowAsync("accounts", Fields(), writesEnabled: true);
        Assert.Equal(RowId, id);
    }

    [Fact]
    public async Task CreateWithoutRecoverableIdSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        await Assert.ThrowsAsync<ContractException>(() => client.CreateRowAsync("accounts", Fields(), writesEnabled: true));
    }

    [Fact]
    public async Task GetRowReturnsTheRowDocument()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}","name":"Contoso"}""")); });
        using var row = await client.GetRowAsync("accounts", RowId);
        Assert.Equal("Contoso", row.RootElement.GetProperty("name").GetString());
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetRowOnMissingRowSurfacesDataverseApiException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.NotFound)));
        var ex = await Assert.ThrowsAsync<DataverseApiException>(() => client.GetRowAsync("accounts", RowId));
        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdateRowAsync("accounts", RowId, Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpdateSendsAPatchToTheRowAddress()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRowAsync("accounts", RowId, Fields(), writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task UpdatePropertyWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpdatePropertyAsync("accounts", RowId, "name", Fields(""" "Contoso" """)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UpdatePropertySendsAPutToTheDocumentedPropertyAddressWithAValueEnvelope()
    {
        HttpMethod? method = null; Uri? requested = null; string? body = null;
        var client = Client(async (r, ct) =>
        {
            method = r.Method; requested = r.RequestUri; body = await r.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        await client.UpdatePropertyAsync("accounts", RowId, "name", Fields(""" "Contoso" """), writesEnabled: true);
        Assert.Equal(HttpMethod.Put, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/name", requested!.AbsoluteUri);
        Assert.Equal("""{"value":"Contoso"}""", body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("name/other")]
    [InlineData("name(1)")]
    public async Task UpdatePropertyRejectsAnUnsafePropertyNameLocally(string property)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.UpdatePropertyAsync("accounts", RowId, property, Fields(""" "Contoso" """), writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DeleteWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteRowAsync("accounts", RowId));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DeleteSendsADeleteToTheRowAddress()
    {
        HttpMethod? method = null;
        var client = Client((r, _) => { method = r.Method; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteRowAsync("accounts", RowId, writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, method);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Accounts")]
    [InlineData("accounts;drop")]
    [InlineData("1accounts")]
    public async Task UnsafeEntitySetNamesAreRejectedLocally(string entitySet)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync(entitySet, RowId));
        Assert.Equal(0, calls);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-008: upsert and alternate keys - create-vs-update disambiguation via <c>return=representation</c>,
/// <c>UpdateOnly</c>/<c>CreateOnly</c> mode headers, and the write opt-in gate.</summary>
public sealed class DataverseUpsertTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");
    private static readonly Guid RowId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static JsonElement Fields() => JsonDocument.Parse("""{"name":"Contoso"}""").RootElement;

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task UpsertWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields()));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AtLeastOneAlternateKeyFieldIsRequired()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string>(), Fields(), writesEnabled: true));
    }

    [Fact]
    public async Task KeySegmentIsComposedWithEscapedLiterals()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})"}""", HttpStatusCode.OK)); });
        await client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["name"] = "O'Brien" }, Fields(), writesEnabled: true);
        Assert.Contains("name='O''Brien'", Uri.UnescapeDataString(requested!.AbsoluteUri));
    }

    [Fact]
    public async Task CreatedStatusReportsCreatedTrue()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})"}""", HttpStatusCode.Created)));
        var result = await client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields(), writesEnabled: true);
        Assert.True(result.Created);
        Assert.Equal(RowId, result.RowId);
    }

    [Fact]
    public async Task OkStatusReportsCreatedFalse()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})"}""", HttpStatusCode.OK)));
        var result = await client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields(), writesEnabled: true);
        Assert.False(result.Created);
    }

    [Fact]
    public async Task UpdateOnlyModeSendsIfMatchStar()
    {
        string? ifMatch = null;
        var client = Client((r, _) => { ifMatch = r.Headers.TryGetValues("If-Match", out var v) ? v.First() : null; return Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})"}""")); });
        await client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields(), DataverseUpsertMode.UpdateOnly, writesEnabled: true);
        Assert.Equal("*", ifMatch);
    }

    [Fact]
    public async Task CreateOnlyModeSendsIfNoneMatchStar()
    {
        string? ifNoneMatch = null;
        var client = Client((r, _) => { ifNoneMatch = r.Headers.TryGetValues("If-None-Match", out var v) ? v.First() : null; return Task.FromResult(Json($$"""{"@odata.id":"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({{RowId:D}})"}""", HttpStatusCode.Created)); });
        await client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields(), DataverseUpsertMode.CreateOnly, writesEnabled: true);
        Assert.Equal("*", ifNoneMatch);
    }

    [Fact]
    public async Task UpdateOnlyViolationSurfacesAsDataverseConcurrencyException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.PreconditionFailed)));
        await Assert.ThrowsAsync<DataverseConcurrencyException>(() =>
            client.UpsertByAlternateKeyAsync("accounts", new Dictionary<string, string> { ["emailaddress1"] = "a@b.com" }, Fields(), DataverseUpsertMode.UpdateOnly, writesEnabled: true));
    }
}

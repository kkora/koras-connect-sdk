using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-013: ETags and optimistic concurrency - <c>If-Match</c> on writes, <c>If-None-Match: null</c> on
/// expanded reads, and distinct classification of a <c>412</c> conflict.</summary>
public sealed class DataverseConcurrencyTests
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
    public async Task UpdateSendsIfMatchWhenSupplied()
    {
        string? ifMatch = null;
        var client = Client((r, _) => { ifMatch = r.Headers.TryGetValues("If-Match", out var v) ? v.First() : null; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRowAsync("accounts", RowId, Fields(), ifMatchETag: "\"12345\"", writesEnabled: true);
        Assert.Equal("\"12345\"", ifMatch);
    }

    [Fact]
    public async Task UpdateWithoutAnETagSendsNoIfMatchHeader()
    {
        var sawIfMatch = false;
        var client = Client((r, _) => { sawIfMatch = r.Headers.Contains("If-Match"); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRowAsync("accounts", RowId, Fields(), writesEnabled: true);
        Assert.False(sawIfMatch);
    }

    [Fact]
    public async Task DeleteSendsIfMatchWhenSupplied()
    {
        string? ifMatch = null;
        var client = Client((r, _) => { ifMatch = r.Headers.TryGetValues("If-Match", out var v) ? v.First() : null; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteRowAsync("accounts", RowId, ifMatchETag: "*", writesEnabled: true);
        Assert.Equal("*", ifMatch);
    }

    [Fact]
    public async Task UpdateConflictSurfacesDistinctConcurrencyExceptionNotGenericContractException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.PreconditionFailed)));
        var ex = await Assert.ThrowsAsync<DataverseConcurrencyException>(() => client.UpdateRowAsync("accounts", RowId, Fields(), ifMatchETag: "\"old\"", writesEnabled: true));
        Assert.IsNotType<ContractException>(ex);
    }

    [Fact]
    public async Task DeleteConflictSurfacesDistinctConcurrencyException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.PreconditionFailed)));
        await Assert.ThrowsAsync<DataverseConcurrencyException>(() => client.DeleteRowAsync("accounts", RowId, ifMatchETag: "*", writesEnabled: true));
    }

    [Fact]
    public async Task OtherWriteFailuresStillClassifyThroughTheSharedClassifier()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Forbidden)));
        var ex = await Assert.ThrowsAsync<DataverseApiException>(() => client.UpdateRowAsync("accounts", RowId, Fields(), writesEnabled: true));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task GetRowWithExpandSendsIfNoneMatchNull()
    {
        string? ifNoneMatch = null;
        var client = Client((r, _) =>
        {
            ifNoneMatch = r.Headers.TryGetValues("If-None-Match", out var v) ? v.First() : null;
            return Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}"}"""));
        });
        using var _ = await client.GetRowAsync("accounts", RowId, new DataverseQuery(Expand: "primarycontactid"));
        Assert.Equal("null", ifNoneMatch);
    }

    [Fact]
    public async Task GetRowWithoutExpandSendsNoIfNoneMatchHeader()
    {
        var sawHeader = false;
        var client = Client((r, _) => { sawHeader = r.Headers.Contains("If-None-Match"); return Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}"}""")); });
        using var _ = await client.GetRowAsync("accounts", RowId);
        Assert.False(sawHeader);
    }

    [Fact]
    public async Task EnumerationWithExpandSendsIfNoneMatchNull()
    {
        string? ifNoneMatch = null;
        var client = Client((r, _) =>
        {
            ifNoneMatch = r.Headers.TryGetValues("If-None-Match", out var v) ? v.First() : null;
            return Task.FromResult(Json("""{"value":[]}"""));
        });
        await client.EnumeratePageAsync("accounts", new DataverseQuery(Expand: "contact_customer_accounts"));
        Assert.Equal("null", ifNoneMatch);
    }
}

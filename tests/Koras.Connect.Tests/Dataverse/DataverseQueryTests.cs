using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-005: OData filtering, selection and ordering - supported query-option composition, local rejection
/// of the three unsupported standard options, and single-row read restrictions.</summary>
public sealed class DataverseQueryTests
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

    [Fact]
    public void DataverseQueryHasNoPropertyForTheThreeUnsupportedStandardOptions()
    {
        // $skip/$search/$format are explicitly unsupported by Dataverse - there must be no property through which
        // a caller could even attempt to request them (they are rejected locally by never being exposed).
        var properties = typeof(DataverseQuery).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("Skip", properties);
        Assert.DoesNotContain("Search", properties);
        Assert.DoesNotContain("Format", properties);
    }

    [Fact]
    public async Task EveryDocumentedOptionComposesIntoTheQueryString()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await client.EnumeratePageAsync("accounts", new DataverseQuery(
            Select: "name,revenue", Filter: "revenue gt 100000", OrderBy: "name asc", Expand: "primarycontactid", Apply: "groupby((name))", Top: 10, Count: true));
        var query = Uri.UnescapeDataString(requested!.Query);
        Assert.Contains("$select=name,revenue", query);
        Assert.Contains("$filter=revenue gt 100000", query);
        Assert.Contains("$orderby=name asc", query);
        Assert.Contains("$expand=primarycontactid", query);
        Assert.Contains("$apply=groupby((name))", query);
        Assert.Contains("$top=10", query);
        Assert.Contains("$count=true", query);
    }

    [Fact]
    public async Task FilterWithSpecialCharactersRoundTripsThroughEscaping()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await client.EnumeratePageAsync("accounts", new DataverseQuery(Filter: "name eq 'A & B / C?'"));
        Assert.Equal("name eq 'A & B / C?'", Uri.UnescapeDataString(requested!.Query.Split("$filter=")[1]));
    }

    [Fact]
    public async Task GetRowRejectsCollectionOnlyQueryOptions()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", RowId, new DataverseQuery(Filter: "name eq 'x'")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", RowId, new DataverseQuery(Top: 1)));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", RowId, new DataverseQuery(Count: true)));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", RowId, new DataverseQuery(OrderBy: "name asc")));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", RowId, new DataverseQuery(Apply: "groupby((name))")));
    }

    [Fact]
    public async Task GetRowHonorsSelectAndExpand()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}"}""")); });
        using var _ = await client.GetRowAsync("accounts", RowId, new DataverseQuery(Select: "name", Expand: "primarycontactid"));
        var query = Uri.UnescapeDataString(requested!.Query);
        Assert.Contains("$select=name", query);
        Assert.Contains("$expand=primarycontactid", query);
    }
}

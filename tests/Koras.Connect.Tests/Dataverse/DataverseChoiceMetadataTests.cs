using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-011: choice, status and state metadata - the formatted-value annotation mechanism and
/// <c>GlobalOptionSetDefinitions</c> enumeration.</summary>
public sealed class DataverseChoiceMetadataTests
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
    public async Task IncludeFormattedValuesSendsThePreferAnnotationHeader()
    {
        string? prefer = null;
        var client = Client((r, _) => { prefer = r.Headers.TryGetValues("Prefer", out var v) ? v.First() : null; return Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}","statuscode":1,"statuscode@OData.Community.Display.V1.FormattedValue":"Active"}""")); });
        using var row = await client.GetRowAsync("accounts", RowId, includeFormattedValues: true);
        Assert.Contains("OData.Community.Display.V1.FormattedValue", prefer);
        Assert.Equal("Active", DataverseClient.TryGetFormattedValue(row.RootElement, "statuscode"));
    }

    [Fact]
    public async Task FormattedValueIsAbsentGracefullyWhenNotRequested()
    {
        var client = Client((_, _) => Task.FromResult(Json($$"""{"accountid":"{{RowId:D}}","statuscode":1}""")));
        using var row = await client.GetRowAsync("accounts", RowId);
        Assert.Null(DataverseClient.TryGetFormattedValue(row.RootElement, "statuscode"));
    }

    [Fact]
    public void TryGetFormattedValueNeverThrowsOnANonObjectOrMissingProperty()
    {
        Assert.Null(DataverseClient.TryGetFormattedValue(JsonDocument.Parse("[1,2,3]").RootElement, "statuscode"));
        Assert.Null(DataverseClient.TryGetFormattedValue(JsonDocument.Parse("{}").RootElement, "statuscode"));
    }

    [Fact]
    public async Task EnumerateGlobalOptionSetsFiltersByName()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"Name":"incident_caseorigincode"}]}""")); });
        var items = new List<JsonElement>();
        await foreach (var item in client.EnumerateGlobalOptionSetsAsync("incident_caseorigincode")) items.Add(item);
        Assert.Single(items);
        Assert.StartsWith("https://contoso.api.crm.dynamics.com/api/data/v9.2/GlobalOptionSetDefinitions", requested!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("Name eq 'incident_caseorigincode'", Uri.UnescapeDataString(requested.Query));
    }

    [Fact]
    public async Task EnumerateGlobalOptionSetsWithoutAFilterReturnsEveryOptionSet()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[{"Name":"a"},{"Name":"b"}]}""")));
        var items = new List<JsonElement>();
        await foreach (var item in client.EnumerateGlobalOptionSetsAsync()) items.Add(item);
        Assert.Equal(2, items.Count);
    }
}

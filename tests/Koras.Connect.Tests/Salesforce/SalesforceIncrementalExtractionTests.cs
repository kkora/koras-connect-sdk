using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-021: incremental extraction and checkpoints - <c>SystemModstamp</c>-filtered SOQL composition
/// via <c>queryAll</c>, reusing SFD-005/006's pagination unchanged.</summary>
public sealed class SalesforceIncrementalExtractionTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    private sealed record Account(string Id, string? Name);

    [Fact]
    public async Task ComposesASystemModstampFilteredQueryAllRequest()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        var checkpoint = new SalesforceDeltaCheckpoint(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
        await foreach (var _ in client.EnumerateChangesAsync<Account>("Account", checkpoint, ["Id", "Name"])) { }
        var decoded = Uri.UnescapeDataString(requested!.AbsoluteUri);
        Assert.Contains("/queryAll?q=", decoded);
        Assert.Contains("SELECT Id,Name,SystemModstamp FROM Account WHERE SystemModstamp >= 2026-11-01T00:00:00.000Z ORDER BY SystemModstamp", decoded);
    }

    [Fact]
    public async Task DoesNotDuplicateSystemModstampWhenAlreadyRequested()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"totalSize":0,"done":true,"records":[]}""")); });
        var checkpoint = new SalesforceDeltaCheckpoint(DateTimeOffset.UtcNow);
        await foreach (var _ in client.EnumerateChangesAsync<Account>("Account", checkpoint, ["Id", "SystemModstamp"])) { }
        var decoded = Uri.UnescapeDataString(requested!.AbsoluteUri);
        Assert.DoesNotContain("SystemModstamp,SystemModstamp", decoded);
    }

    [Fact]
    public async Task ChangedAndSoftDeletedRowsBothSurface()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"totalSize":2,"done":true,"records":[
              {"Id":"001gL000003OpvBQAS","Name":"Acme","SystemModstamp":"2026-11-13T00:00:00.000+0000"},
              {"Id":"001gL000003OpvBQAT","Name":null,"SystemModstamp":"2026-11-13T00:00:01.000+0000"}
            ]}
            """)));
        var checkpoint = new SalesforceDeltaCheckpoint(DateTimeOffset.UtcNow);
        var rows = new List<Account>();
        await foreach (var row in client.EnumerateChangesAsync<Account>("Account", checkpoint, ["Id", "Name"])) rows.Add(row);
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task EmptyFieldListIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var checkpoint = new SalesforceDeltaCheckpoint(DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in client.EnumerateChangesAsync<Account>("Account", checkpoint, [])) { }
        });
        Assert.Equal(0, calls);
    }
}

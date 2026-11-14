using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-009: OneDrive drives and items - the app-only/delegated asymmetry on <c>/me/drive</c> and
/// <c>/users/{id}/drive</c>, and <c>/drives</c> enumeration.</summary>
public sealed class GraphDrivesTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, GraphOptions? options = null)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")), options);

    [Fact]
    public async Task GetMyDriveThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetMyDriveAsync());
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetMyDriveSucceedsWithADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"d1"}""")); }, new GraphOptions(IsDelegatedContext: true));
        await client.GetMyDriveAsync();
        Assert.Equal("https://graph.microsoft.com/v1.0/me/drive", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetUserDriveThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetUserDriveAsync("u1"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task EnumerateDrivesWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"d1"}]}""")); });
        var drives = new List<System.Text.Json.JsonDocument>();
        await foreach (var drive in client.EnumerateDrivesAsync()) drives.Add(drive);
        Assert.Single(drives);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task GetDriveWorksWithoutADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"id":"d1"}""")); });
        await client.GetDriveAsync("d1");
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateDriveItemsUsesRootChildrenWhenNoItemIdSupplied()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"i1","name":"doc.docx","size":100}]}""")); });
        var items = new List<GraphDriveItem>();
        await foreach (var item in client.EnumerateDriveItemsAsync("d1")) items.Add(item);
        Assert.Single(items);
        Assert.Equal("doc.docx", items[0].Name);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/root/children", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateDriveItemsUsesItemChildrenWhenItemIdSupplied()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); });
        await foreach (var _ in client.EnumerateDriveItemsAsync("d1", "folder1")) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/items/folder1/children", requested!.AbsoluteUri);
    }
}

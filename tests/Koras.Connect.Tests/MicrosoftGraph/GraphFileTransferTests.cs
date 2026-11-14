using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-010: file upload, download and sessions - the conservative 4MB simple-upload ceiling and
/// streamed download.</summary>
public sealed class GraphFileTransferTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")));

    [Fact]
    public async Task UploadRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadSmallFileAsync("d1", null, "hello.txt", content));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UploadRejectsFilesAtOrAboveTheConservativeCeiling()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        using var content = new MemoryStream(new byte[GraphClient.ConservativeSimpleUploadByteLimit + 1]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadSmallFileAsync("d1", null, "big.bin", content, writesEnabled: true));
        Assert.Contains("resumable upload session", ex.Message);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UploadAcceptsAFileUnderTheCeilingAndComposesTheRootColonPath()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"id":"i1"}""", HttpStatusCode.Created); });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        await client.UploadSmallFileAsync("d1", "Documents/Reports", "hello.txt", content, writesEnabled: true);
        Assert.Equal(HttpMethod.Put, request!.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/root:/Documents/Reports/hello.txt:/content", request.RequestUri!.AbsoluteUri);
        Assert.Equal("hello", body);
    }

    [Fact]
    public async Task UploadWithNoParentPathUsesFileNameDirectlyUnderRoot()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(Json("""{"id":"i1"}""", HttpStatusCode.Created)); });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        await client.UploadSmallFileAsync("d1", null, "hello.txt", content, writesEnabled: true);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/root:/hello.txt:/content", request!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task UploadRequiresASeekableStream()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        using var nonSeekable = new NonSeekableStream(Encoding.UTF8.GetBytes("hello"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.UploadSmallFileAsync("d1", null, "hello.txt", nonSeekable, writesEnabled: true));
    }

    [Fact]
    public async Task DownloadStreamsTheResponseBodyToTheDestination()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("file-bytes") }); });
        using var destination = new MemoryStream();
        await client.DownloadFileAsync("d1", "i1", destination);
        Assert.Equal("https://graph.microsoft.com/v1.0/drives/d1/items/i1/content", requested!.AbsoluteUri);
        Assert.Equal("file-bytes", Encoding.UTF8.GetString(destination.ToArray()));
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}

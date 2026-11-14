using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-016: file and image columns - single-request upload/download/delete shapes and the write opt-in gate.</summary>
public sealed class DataverseFileColumnsTests
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

    [Fact]
    public async Task UploadSendsOctetStreamWithFileNameHeader()
    {
        HttpMethod? method = null; string? contentType = null; string? fileNameHeader = null; byte[]? sentBytes = null; Uri? requested = null;
        var client = Client(async (r, ct) =>
        {
            method = r.Method; requested = r.RequestUri;
            contentType = r.Content!.Headers.ContentType?.MediaType;
            fileNameHeader = r.Headers.TryGetValues("x-ms-file-name", out var v) ? v.First() : null;
            sentBytes = await r.Content.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        await client.UploadFileColumnAsync("accounts", RowId, "entityimage", content, "photo.jpg", writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, method);
        Assert.Equal("application/octet-stream", contentType);
        Assert.Equal("photo.jpg", fileNameHeader);
        Assert.Equal("hello world", Encoding.UTF8.GetString(sentBytes!));
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/entityimage", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task UploadWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        using var content = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadFileColumnAsync("accounts", RowId, "entityimage", content, "photo.jpg"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DownloadCopiesResponseBodyToTheDestinationStream()
    {
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("file-bytes")) }));
        using var destination = new MemoryStream();
        await client.DownloadFileColumnAsync("accounts", RowId, "entityimage", destination);
        Assert.Equal("file-bytes", Encoding.UTF8.GetString(destination.ToArray()));
    }

    [Fact]
    public async Task DownloadRequestsTheValueSubResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) }); });
        using var destination = new MemoryStream();
        await client.DownloadFileColumnAsync("accounts", RowId, "entityimage", destination);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/entityimage/$value", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task DeleteWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.DeleteFileColumnAsync("accounts", RowId, "entityimage"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DeleteSendsADeleteToTheColumnUrl()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DeleteFileColumnAsync("accounts", RowId, "entityimage", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/entityimage", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task InvalidFileNameIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        using var content = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(() => client.UploadFileColumnAsync("accounts", RowId, "entityimage", content, "bad\nname", writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

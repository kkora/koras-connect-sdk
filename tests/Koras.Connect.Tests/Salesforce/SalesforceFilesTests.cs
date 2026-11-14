using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-016: Files and ContentVersion - Base64-encoded upload via sObject-create reuse, and
/// ContentDocumentLink composition.</summary>
public sealed class SalesforceFilesTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");
    private const string RecordId = "001gL000003OpvBQAS";
    private const string OtherRecordId = "069gL000003OpvBQAS";

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    [Fact]
    public async Task UploadRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.UploadContentVersionAsync("Doc", "doc.txt", content));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("", "doc.txt")]
    [InlineData("Doc", "")]
    public async Task UploadRejectsEmptyTitleOrPath(string title, string path)
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        await Assert.ThrowsAsync<ArgumentException>(() => client.UploadContentVersionAsync(title, path, content, writesEnabled: true));
    }

    [Fact]
    public async Task UploadBase64EncodesTheStreamAndPostsToContentVersion()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json($$"""{"id":"{{RecordId}}","errors":[],"success":true}""", HttpStatusCode.Created); });
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));
        var id = await client.UploadContentVersionAsync("Doc", "doc.txt", content, writesEnabled: true);
        Assert.Equal(RecordId, id);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/sobjects/ContentVersion/", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal("Doc", parsed.RootElement.GetProperty("Title").GetString());
        Assert.Equal("doc.txt", parsed.RootElement.GetProperty("PathOnClient").GetString());
        Assert.Equal("hello world", Encoding.UTF8.GetString(Convert.FromBase64String(parsed.RootElement.GetProperty("VersionData").GetString()!)));
    }

    [Fact]
    public async Task LinkRequiresWritesEnabled()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.LinkContentDocumentAsync(RecordId, OtherRecordId, "V", "AllUsers"));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task LinkPostsTheDocumentedContentDocumentLinkFields()
    {
        HttpRequestMessage? request = null;
        string? body = null;
        var client = Client(async (r, ct) => { request = r; body = await r.Content!.ReadAsStringAsync(ct); return Json("""{"id":"06Agl000003OpvBQAS","errors":[],"success":true}""", HttpStatusCode.Created); });
        await client.LinkContentDocumentAsync(RecordId, OtherRecordId, "V", "AllUsers", writesEnabled: true);
        Assert.Equal("https://contoso.my.salesforce.com/services/data/v68.0/sobjects/ContentDocumentLink/", request!.RequestUri!.AbsoluteUri);
        using var parsed = JsonDocument.Parse(body!);
        Assert.Equal(RecordId, parsed.RootElement.GetProperty("ContentDocumentId").GetString());
        Assert.Equal(OtherRecordId, parsed.RootElement.GetProperty("LinkedEntityId").GetString());
        Assert.Equal("V", parsed.RootElement.GetProperty("ShareType").GetString());
        Assert.Equal("AllUsers", parsed.RootElement.GetProperty("Visibility").GetString());
    }

    [Fact]
    public async Task LinkRejectsInvalidRecordIdsLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.LinkContentDocumentAsync("bad id", OtherRecordId, "V", "AllUsers", writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

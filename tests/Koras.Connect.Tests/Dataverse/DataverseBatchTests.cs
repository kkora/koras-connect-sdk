using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-007: batch and changeset operations - the multipart/mixed wire format, the 1,000-request limit,
/// changeset all-or-nothing rollback reporting, and <c>odata.continue-on-error</c>.</summary>
public sealed class DataverseBatchTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    private static HttpResponseMessage MultipartResponse(string boundary, string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8) };
        response.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse($"multipart/mixed; boundary={boundary}");
        return response;
    }

    private static string SinglePart(string boundary, int status, string? jsonBody)
    {
        var sb = new StringBuilder();
        sb.Append("--").Append(boundary).Append("\r\n");
        sb.Append("Content-Type: application/http\r\n\r\n");
        sb.Append("HTTP/1.1 ").Append(status).Append(" Status\r\n");
        if (jsonBody is not null) sb.Append("Content-Type: application/json\r\n\r\n").Append(jsonBody).Append("\r\n");
        else sb.Append("\r\n");
        sb.Append('\r');
        sb.Append('\n');
        return sb.ToString();
    }

    private static JsonElement Fields(string json = """{"name":"Contoso"}""") => JsonDocument.Parse(json).RootElement;

    [Fact]
    public async Task MoreThan1000RequestsAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        var requests = Enumerable.Range(0, 1001).Select(_ => new DataverseBatchRequest(HttpMethod.Get, "accounts")).ToArray();
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteBatchAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetInsideAChangesetIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        var changeset = new DataverseChangeset([new DataverseBatchRequest(HttpMethod.Get, "accounts")]);
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteBatchAsync(changesets: [changeset], writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AWriteRequestWithoutWritesEnabledThrowsBeforeAnyHttpCall()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        var requests = new[] { new DataverseBatchRequest(HttpMethod.Post, "accounts", Fields()) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.ExecuteBatchAsync(requests));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AReadOnlyBatchNeedsNoWriteOptIn()
    {
        var boundary = "batchresponse_1";
        var client = Client((_, _) => Task.FromResult(MultipartResponse(boundary, SinglePart(boundary, 200, """{"value":[]}""") + $"--{boundary}--")));
        var results = await client.ExecuteBatchAsync([new DataverseBatchRequest(HttpMethod.Get, "accounts")]);
        Assert.True(results[0].Succeeded);
    }

    [Fact]
    public async Task RequestBodyUsesCrlfLineEndingsRegardlessOfHostDefaults()
    {
        string? sentBody = null;
        var boundary = "batchresponse_1";
        var client = Client(async (r, ct) =>
        {
            sentBody = await r.Content!.ReadAsStringAsync(ct);
            return MultipartResponse(boundary, SinglePart(boundary, 204, null) + $"--{boundary}--");
        });
        await client.ExecuteBatchAsync([new DataverseBatchRequest(HttpMethod.Get, "accounts")]);
        Assert.Contains("\r\n", sentBody);
        Assert.DoesNotContain("\n\n", sentBody!.Replace("\r\n", "")); // no bare LF survives once CRLF pairs are stripped
    }

    [Fact]
    public async Task ContentIdIsIncludedForChangesetMembersEvenWhenNotSupplied()
    {
        string? sentBody = null;
        var boundary = "batchresponse_1";
        var client = Client(async (r, ct) =>
        {
            sentBody = await r.Content!.ReadAsStringAsync(ct);
            return MultipartResponse(boundary, $"--{boundary}\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n" + SinglePart("inner", 204, null) + "--inner--\r\n\r\n" + $"--{boundary}--");
        });
        var changeset = new DataverseChangeset([new DataverseBatchRequest(HttpMethod.Patch, "accounts(11111111-1111-1111-1111-111111111111)", Fields())]);
        await client.ExecuteBatchAsync(changesets: [changeset], writesEnabled: true);
        Assert.Contains("Content-ID: 1", sentBody);
    }

    [Fact]
    public async Task ExplicitContentIdIsPreservedForCrossReferencing()
    {
        string? sentBody = null;
        var boundary = "batchresponse_1";
        var client = Client(async (r, ct) =>
        {
            sentBody = await r.Content!.ReadAsStringAsync(ct);
            return MultipartResponse(boundary, $"--{boundary}\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n" + SinglePart("inner", 204, null) + "--inner--\r\n\r\n" + $"--{boundary}--");
        });
        var changeset = new DataverseChangeset([new DataverseBatchRequest(HttpMethod.Post, "accounts", Fields(), ContentId: "newaccount")]);
        await client.ExecuteBatchAsync(changesets: [changeset], writesEnabled: true);
        Assert.Contains("Content-ID: newaccount", sentBody);
        Assert.Contains("POST accounts HTTP/1.1", sentBody);
    }

    [Fact]
    public async Task ContinueOnErrorSurfacesPerItemResultsInsteadOfThrowing()
    {
        var boundary = "batchresponse_1";
        var body = SinglePart(boundary, 204, null) + SinglePart(boundary, 404, null) + $"--{boundary}--";
        string? preferHeader = null;
        var client = Client((r, _) => { preferHeader = r.Headers.TryGetValues("Prefer", out var v) ? v.First() : null; return Task.FromResult(MultipartResponse(boundary, body)); });
        var requests = new[] { new DataverseBatchRequest(HttpMethod.Delete, "accounts(1)"), new DataverseBatchRequest(HttpMethod.Delete, "accounts(2)") };
        var results = await client.ExecuteBatchAsync(requests, continueOnError: true, writesEnabled: true);
        Assert.Equal("odata.continue-on-error", preferHeader);
        Assert.True(results[0].Succeeded);
        Assert.False(results[1].Succeeded);
        Assert.Equal("http_404", results[1].ErrorCode);
    }

    [Fact]
    public async Task AChangesetFailureReportsRollbackForEveryMemberNotJustTheFailingOne()
    {
        var boundary = "batchresponse_1";
        // The changeset as a whole rolled back - only a single error response represents it, not one per member.
        var body = $"--{boundary}\r\nContent-Type: application/http\r\n\r\nHTTP/1.1 400 Bad Request\r\n\r\n\r\n--{boundary}--";
        var client = Client((_, _) => Task.FromResult(MultipartResponse(boundary, body)));
        var changeset = new DataverseChangeset([
            new DataverseBatchRequest(HttpMethod.Patch, "accounts(1)", Fields()),
            new DataverseBatchRequest(HttpMethod.Patch, "accounts(2)", Fields()),
        ]);
        var results = await client.ExecuteBatchAsync(changesets: [changeset], writesEnabled: true);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.Succeeded));
        Assert.Equal(0, results[0].Index);
        Assert.Equal(1, results[1].Index);
    }

    [Fact]
    public async Task SuccessfulChangesetReportsEveryMemberIndividually()
    {
        var boundary = "batchresponse_1";
        var inner = SinglePart("inner", 204, null) + SinglePart("inner", 204, null) + "--inner--";
        var body = $"--{boundary}\r\nContent-Type: multipart/mixed; boundary=inner\r\n\r\n{inner}\r\n\r\n--{boundary}--";
        var client = Client((_, _) => Task.FromResult(MultipartResponse(boundary, body)));
        var changeset = new DataverseChangeset([
            new DataverseBatchRequest(HttpMethod.Patch, "accounts(1)", Fields()),
            new DataverseBatchRequest(HttpMethod.Patch, "accounts(2)", Fields()),
        ]);
        var results = await client.ExecuteBatchAsync(changesets: [changeset], writesEnabled: true);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Succeeded));
    }
}

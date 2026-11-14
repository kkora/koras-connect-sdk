using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-018: Concurrency and conditional requests - the weak-validation <c>If-Unmodified-Since</c> path
/// and its distinct 412 classification.</summary>
public sealed class SalesforceConcurrencyTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");
    private const string RecordId = "001gL000003OpvBQAS";

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    private static JsonElement Fields() => JsonDocument.Parse("""{"Name":"Acme"}""").RootElement;

    [Fact]
    public async Task IfUnmodifiedSinceIsSentWhenSupplied()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRecordAsync("Account", RecordId, Fields(), ifUnmodifiedSince: "Wed, 12 Nov 2026 00:00:00 GMT", writesEnabled: true);
        Assert.Equal("Wed, 12 Nov 2026 00:00:00 GMT", request!.Headers.GetValues("If-Unmodified-Since").Single());
    }

    [Fact]
    public async Task NoIfUnmodifiedSinceHeaderIsSentWhenNotSupplied()
    {
        HttpRequestMessage? request = null;
        var client = Client((r, _) => { request = r; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.UpdateRecordAsync("Account", RecordId, Fields(), writesEnabled: true);
        Assert.False(request!.Headers.Contains("If-Unmodified-Since"));
    }

    [Fact]
    public async Task APreconditionFailedResponseClassifiesAsSalesforceConcurrencyException()
    {
        var client = Client((_, _) => Task.FromResult(Json("""[{"message":"...","errorCode":"..."}]""", HttpStatusCode.PreconditionFailed)));
        await Assert.ThrowsAsync<SalesforceConcurrencyException>(() =>
            client.UpdateRecordAsync("Account", RecordId, Fields(), ifUnmodifiedSince: "Wed, 12 Nov 2026 00:00:00 GMT", writesEnabled: true));
    }

    [Fact]
    public async Task EveryOtherFailureStillGoesThroughTheStandardClassifier()
    {
        var client = Client((_, _) => Task.FromResult(Json("[]", HttpStatusCode.Forbidden)));
        var ex = await Assert.ThrowsAsync<SalesforceApiException>(() => client.UpdateRecordAsync("Account", RecordId, Fields(), writesEnabled: true));
        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }
}

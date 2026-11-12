using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;

namespace Koras.Connect.Tests;

public sealed class WorkersTests
{
    [Fact]
    public async Task EnumeratesMultiplePagesWithoutLeakingDocumentLifetime()
    {
        var calls = 0;
        using var http = new HttpClient(new StubHandler(_ =>
        {
            calls++;
            var body = calls == 1 ? """{"workers":[{"id":"a"},{"id":"b"}]}""" : """{"workers":[{"id":"c"}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }));
        var client = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://example.test"), 2));
        var ids = new List<string>();
        await foreach (var worker in client.EnumerateWorkersAsync()) ids.Add(worker.GetProperty("id").GetString()!);
        Assert.Equal(["a", "b", "c"], ids);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ApiFailuresDoNotExposeResponseBody()
    {
        using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        { Content = new StringContent("secret employee data") }));
        var client = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://example.test")));
        var error = await Assert.ThrowsAsync<IntegrationRequestException>(() => client.GetWorkersPageAsync());
        Assert.DoesNotContain("secret employee data", error.Message);
        Assert.Equal(HttpStatusCode.Forbidden, error.ResponseStatus);
    }

    private sealed class FixedToken : IAccessTokenSource
    {
        public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("test-token");
    }
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(reply(request));
    }
}

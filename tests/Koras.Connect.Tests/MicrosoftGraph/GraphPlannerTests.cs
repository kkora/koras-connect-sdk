using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-017: Planner - the delegated-only constraint, the single clearest "do not even attempt
/// app-only" case in the catalog.</summary>
public sealed class GraphPlannerTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, GraphOptions? options = null)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")), options);

    [Fact]
    public async Task EnumerateMyPlansThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.EnumerateMyPlansAsync()) { }
        });
        Assert.Contains("delegated-only", ex.Message);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task EnumeratePlanTasksThrowsLocallyWithoutADelegatedContext()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.EnumeratePlanTasksAsync("p1")) { }
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task EnumerateMyPlansSucceedsWithADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"p1"}]}""")); }, new GraphOptions(IsDelegatedContext: true));
        await foreach (var _ in client.EnumerateMyPlansAsync()) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/me/planner/plans", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumeratePlanTasksSucceedsWithADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"id":"t1","title":"Finish report"}]}""")); }, new GraphOptions(IsDelegatedContext: true));
        var tasks = new List<GraphPlannerTask>();
        await foreach (var task in client.EnumeratePlanTasksAsync("p1")) tasks.Add(task);
        Assert.Single(tasks);
        Assert.Equal("Finish report", tasks[0].Title);
        Assert.Equal("https://graph.microsoft.com/v1.0/planner/plans/p1/tasks", requested!.AbsoluteUri);
    }
}

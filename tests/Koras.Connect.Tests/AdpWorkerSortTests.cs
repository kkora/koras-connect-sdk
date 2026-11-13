using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-005 follow-up: the typed, best-effort <see cref="WorkerSort"/> <c>$orderby</c> builder and its
/// wiring onto <see cref="AdpWorkersClient.GetWorkersPageAsync"/>/<see cref="AdpWorkersClient.SearchWorkersAsync(string,int,WorkerSort,System.Threading.CancellationToken)"/>.</summary>
public sealed class AdpWorkerSortTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private static HttpResponseMessage Reply(string body) => new(System.Net.HttpStatusCode.OK) { Content = new StringContent(body) };
    private static AdpWorkersClient ClientFor(Func<HttpRequestMessage, HttpResponseMessage> reply)
        => new(new(new HttpClient(new Handler(reply)), new Token()), new(new("https://api.test")));

    [Theory]
    [InlineData(WorkerFilterField.AssociateOid, SortDirection.Ascending, "associateOID asc")]
    [InlineData(WorkerFilterField.LegalGivenName, SortDirection.Descending, "person/legalName/givenName desc")]
    [InlineData(WorkerFilterField.WorkAssignmentJobTitle, SortDirection.Ascending, "workAssignments/jobTitle asc")]
    public void RendersTheDocumentedODataOrderByFragment(WorkerFilterField field, SortDirection direction, string expected)
        => Assert.Equal(expected, new WorkerSort(field, direction).ToODataOrderBy());

    [Fact]
    public void DefaultsToAscendingWhenNoDirectionIsSupplied()
        => Assert.Equal("associateOID asc", new WorkerSort(WorkerFilterField.AssociateOid).ToODataOrderBy());

    [Fact]
    public async Task GetWorkersPageAsyncSendsTheEncodedOrderByWhenSortIsSupplied()
    {
        string? capturedQuery = null;
        var client = ClientFor(r => { capturedQuery = r.RequestUri!.Query; return Reply("""{"workers":[]}"""); });
        using var doc = await client.GetWorkersPageAsync(sort: new WorkerSort(WorkerFilterField.WorkAssignmentJobTitle, SortDirection.Descending));
        Assert.Contains("$orderby=workAssignments%2FjobTitle%20desc", capturedQuery);
    }

    [Fact]
    public async Task GetWorkersPageAsyncOmitsOrderByWhenNoSortIsSupplied()
    {
        string? capturedQuery = null;
        var client = ClientFor(r => { capturedQuery = r.RequestUri!.Query; return Reply("""{"workers":[]}"""); });
        using var doc = await client.GetWorkersPageAsync();
        Assert.DoesNotContain("orderby", capturedQuery);
    }

    [Fact]
    public async Task SearchWorkersAsyncRawStringOverloadSendsTheEncodedOrderByAlongsideTheFilter()
    {
        string? capturedQuery = null;
        var client = ClientFor(r => { capturedQuery = r.RequestUri!.Query; return Reply("""{"workers":[]}"""); });
        using var doc = await client.SearchWorkersAsync("associateOID eq 'w1'", sort: new WorkerSort(WorkerFilterField.AssociateOid));
        Assert.Contains("$filter=", capturedQuery);
        Assert.Contains("$orderby=associateOID%20asc", capturedQuery);
    }

    [Fact]
    public async Task SearchWorkersAsyncTypedFilterOverloadForwardsTheSortParameter()
    {
        string? capturedQuery = null;
        var client = ClientFor(r => { capturedQuery = r.RequestUri!.Query; return Reply("""{"workers":[]}"""); });
        using var doc = await client.SearchWorkersAsync(new WorkerFilter(WorkerFilterField.AssociateOid, WorkerFilterOperator.Equals, "w1"),
            sort: new WorkerSort(WorkerFilterField.LegalFamilyName, SortDirection.Descending));
        Assert.Contains("$orderby=person%2FlegalName%2FfamilyName1%20desc", capturedQuery);
    }
}

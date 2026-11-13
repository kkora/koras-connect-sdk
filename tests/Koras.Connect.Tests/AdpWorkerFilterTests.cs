using System.Net;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-005: the typed <see cref="WorkerFilter"/> whitelist and explicit HTTP 202 (asynchronous worker
/// retrieval) rejection across every worker-read surface.</summary>
public sealed class AdpWorkerFilterTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }
    private static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body) };
    private static AdpWorkersClient ClientFor(Func<HttpRequestMessage, HttpResponseMessage> reply)
        => new(new(new HttpClient(new Handler(reply)), new Token()), new(new("https://api.test")));

    [Theory]
    [InlineData(WorkerFilterField.AssociateOid, WorkerFilterOperator.Equals, "w1", "associateOID eq 'w1'")]
    [InlineData(WorkerFilterField.LegalGivenName, WorkerFilterOperator.Contains, "Ann", "person/legalName/givenName co 'Ann'")]
    [InlineData(WorkerFilterField.LegalFamilyName, WorkerFilterOperator.Equals, "Smith", "person/legalName/familyName1 eq 'Smith'")]
    [InlineData(WorkerFilterField.WorkAssignmentJobTitle, WorkerFilterOperator.Equals, "Engineer", "workAssignments/jobTitle eq 'Engineer'")]
    public void RendersTheDocumentedODataFragmentForEachWhitelistedField(WorkerFilterField field, WorkerFilterOperator op, string value, string expected)
        => Assert.Equal(expected, new WorkerFilter(field, op, value).ToODataFilter());

    [Fact]
    public void EmbeddedSingleQuotesAreDoubledRatherThanBreakingTheFilterLiteral()
    {
        var filter = new WorkerFilter(WorkerFilterField.LegalGivenName, WorkerFilterOperator.Equals, "O'Brien");
        Assert.Equal("person/legalName/givenName eq 'O''Brien'", filter.ToODataFilter());
    }

    [Fact]
    public void EmptyValueIsRejectedAtConstructionNotAtRequestTime()
        => Assert.Throws<ArgumentException>(() => new WorkerFilter(WorkerFilterField.AssociateOid, WorkerFilterOperator.Equals, ""));

    [Fact]
    public async Task TypedSearchOverloadSendsTheSameEncodedFilterAsTheRawStringOverload()
    {
        string? capturedQuery = null;
        var client = ClientFor(r => { capturedQuery = r.RequestUri!.Query; return Reply("""{"workers":[]}"""); });
        using var doc = await client.SearchWorkersAsync(new WorkerFilter(WorkerFilterField.AssociateOid, WorkerFilterOperator.Equals, "w1"));
        Assert.Contains("$filter=associateOID%20eq%20%27w1%27", capturedQuery);
    }

    [Fact]
    public async Task GetWorkersPageAccepted202FailsExplicitlyRatherThanParsingAnEmptyBody()
    {
        var client = ClientFor(_ => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("") });
        var ex = await Assert.ThrowsAsync<NotSupportedException>(() => client.GetWorkersPageAsync());
        Assert.Contains("202", ex.Message);
    }

    [Fact]
    public async Task SearchWorkersAccepted202FailsExplicitly()
    {
        var client = ClientFor(_ => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("") });
        await Assert.ThrowsAsync<NotSupportedException>(() => client.SearchWorkersAsync("associateOID eq 'w1'"));
    }

    [Fact]
    public async Task GetWorkerAccepted202FailsExplicitly()
    {
        var client = ClientFor(_ => new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent("") });
        await Assert.ThrowsAsync<NotSupportedException>(() => client.GetWorkerAsync("w1"));
    }

    [Fact]
    public async Task GetWorkersPageStillSurfacesRawIntegrationRequestExceptionOnFailureAfterTheSendResponseRefactor()
    {
        // Pins the documented behavior (AdpApiExceptionTests.SubmitEventAsyncAndGetWorkersPageAsyncStillSurface...)
        // that switching the internal transport call to SendResponseAsync did not change failure-path semantics.
        var client = ClientFor(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        await Assert.ThrowsAsync<IntegrationRequestException>(() => client.GetWorkersPageAsync());
    }
}

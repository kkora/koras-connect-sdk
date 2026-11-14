using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-018: Dynamics 365 specialization - the account/contact/opportunity typed wrappers are pure
/// pass-throughs over the generic entity-set mechanism, composing the identical request a hand-written caller
/// would, with unknown fields preserved via <c>AdditionalData</c>.</summary>
public sealed class DataverseDynamics365Tests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task EnumerateAccountsComposesTheIdenticalRequestAsTheGenericPrimitive()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"accountid":"11111111-1111-1111-1111-111111111111","name":"Contoso","revenue":1000}]}""")); });
        var accounts = new List<DataverseAccount>();
        await foreach (var account in client.EnumerateAccountsAsync(new DataverseQuery(Select: "name"))) accounts.Add(account);
        Assert.Single(accounts);
        Assert.Equal("Contoso", accounts[0].Name);
        Assert.True(accounts[0].AdditionalData!.ContainsKey("revenue"));
        Assert.Equal("https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts?$select=name", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task EnumerateContactsTargetsTheContactsEntitySet()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"contactid":"11111111-1111-1111-1111-111111111111"}]}""")); });
        var contacts = new List<DataverseContact>();
        await foreach (var contact in client.EnumerateContactsAsync()) contacts.Add(contact);
        Assert.Single(contacts);
        Assert.StartsWith("https://contoso.api.crm.dynamics.com/api/data/v9.2/contacts", requested!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnumerateOpportunitiesTargetsTheOpportunitiesEntitySet()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"opportunityid":"11111111-1111-1111-1111-111111111111"}]}""")); });
        var opportunities = new List<DataverseOpportunity>();
        await foreach (var opportunity in client.EnumerateOpportunitiesAsync()) opportunities.Add(opportunity);
        Assert.Single(opportunities);
        Assert.StartsWith("https://contoso.api.crm.dynamics.com/api/data/v9.2/opportunities", requested!.AbsoluteUri, StringComparison.Ordinal);
    }
}

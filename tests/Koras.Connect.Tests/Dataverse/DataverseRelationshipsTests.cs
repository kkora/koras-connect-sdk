using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-010: lookups and relationships - the <c>@odata.bind</c>/<c>$ref</c> mutation shapes and the write
/// opt-in gate.</summary>
public sealed class DataverseRelationshipsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");
    private static readonly Guid RowId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TargetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task BindLookupSendsTheODataBindPayload()
    {
        HttpMethod? method = null; string? body = null; Uri? requested = null;
        var client = Client(async (r, ct) => { method = r.Method; requested = r.RequestUri; body = await r.Content!.ReadAsStringAsync(ct); return new HttpResponseMessage(HttpStatusCode.NoContent); });
        await client.BindLookupAsync("accounts", RowId, "primarycontactid", "contacts", TargetId, writesEnabled: true);
        Assert.Equal(HttpMethod.Patch, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})", requested!.AbsoluteUri);
        var payload = JsonDocument.Parse(body!).RootElement;
        Assert.Equal($"/contacts({TargetId:D})", payload.GetProperty("primarycontactid@odata.bind").GetString());
    }

    [Fact]
    public async Task ClearLookupSendsADeleteToTheRefPath()
    {
        HttpMethod? method = null; Uri? requested = null;
        var client = Client((r, _) => { method = r.Method; requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.ClearLookupAsync("accounts", RowId, "primarycontactid", writesEnabled: true);
        Assert.Equal(HttpMethod.Delete, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/primarycontactid/$ref", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task AssociateSendsAPostWithTheODataIdPayload()
    {
        HttpMethod? method = null; string? body = null; Uri? requested = null;
        var client = Client(async (r, ct) => { method = r.Method; requested = r.RequestUri; body = await r.Content!.ReadAsStringAsync(ct); return new HttpResponseMessage(HttpStatusCode.NoContent); });
        await client.AssociateAsync("accounts", RowId, "contact_customer_accounts", "contacts", TargetId, writesEnabled: true);
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/contact_customer_accounts/$ref", requested!.AbsoluteUri);
        var payload = JsonDocument.Parse(body!).RootElement;
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/contacts({TargetId:D})", payload.GetProperty("@odata.id").GetString());
    }

    [Fact]
    public async Task DisassociateSendsADeleteToTheMemberRefPath()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await client.DisassociateAsync("accounts", RowId, "contact_customer_accounts", TargetId, writesEnabled: true);
        Assert.Equal($"https://contoso.api.crm.dynamics.com/api/data/v9.2/accounts({RowId:D})/contact_customer_accounts({TargetId:D})/$ref", requested!.AbsoluteUri);
    }

    [Theory]
    [InlineData("BindLookup")]
    [InlineData("ClearLookup")]
    [InlineData("Associate")]
    [InlineData("Disassociate")]
    public async Task EveryOperationRequiresWritesEnabledBeforeAnyHttpCall(string operation)
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        Func<Task> act = operation switch
        {
            "BindLookup" => () => client.BindLookupAsync("accounts", RowId, "primarycontactid", "contacts", TargetId),
            "ClearLookup" => () => client.ClearLookupAsync("accounts", RowId, "primarycontactid"),
            "Associate" => () => client.AssociateAsync("accounts", RowId, "contact_customer_accounts", "contacts", TargetId),
            _ => () => client.DisassociateAsync("accounts", RowId, "contact_customer_accounts", TargetId),
        };
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task UnsafeNavigationPropertyNamesAreRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)); });
        await Assert.ThrowsAsync<ArgumentException>(() => client.ClearLookupAsync("accounts", RowId, "bad/prop", writesEnabled: true));
        Assert.Equal(0, calls);
    }
}

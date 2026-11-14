using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-009: table and column metadata - CSDL parsing independent of EDM namespace version, the size cap,
/// hash-cached reparse, and the <c>EntityDefinitions</c> metadata-entity enumerator.</summary>
public sealed class DataverseMetadataTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static readonly Uri EnvironmentRoot = new("https://contoso.api.crm.dynamics.com/");

    private const string Csdl = """
        <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm" Namespace="Microsoft.Dynamics.CRM">
        <EntityType Name="account"><Key><PropertyRef Name="accountid"/></Key><Property Name="accountid" Type="Edm.Guid"/><Property Name="name" Type="Edm.String"/></EntityType>
        </Schema></edmx:DataServices></edmx:Edmx>
        """;

    private static DataverseClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseClient(new IntegrationTransport(http, new StaticToken("token")), new DataverseOptions(EnvironmentRoot));
    }

    [Fact]
    public async Task MetadataParsesEntityKeysAndProperties()
    {
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Csdl, Encoding.UTF8, "application/xml") }));
        var document = await client.GetMetadataAsync();
        var descriptors = DataverseClient.ParseEntityDescriptors(document);
        var account = Assert.Single(descriptors);
        Assert.Equal("account", account.LogicalName);
        Assert.Equal(["accountid"], account.Keys);
        Assert.Contains("name", account.Properties);
    }

    [Fact]
    public async Task UnchangedContentIsNotReparsedUnlessForced()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Csdl, Encoding.UTF8, "application/xml") }); });
        var first = await client.GetMetadataAsync();
        var second = await client.GetMetadataAsync();
        Assert.Same(first, second); // reparse skipped - same cached document instance
        Assert.Equal(2, calls); // the network call itself always happens
        var refreshed = await client.RefreshMetadataAsync();
        Assert.NotSame(first, refreshed);
    }

    [Fact]
    public async Task OversizedDocumentIsRejectedBeforeParsing()
    {
        var huge = new string('x', DataverseClient.MaxMetadataCharacters + 1);
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(huge, Encoding.UTF8, "application/xml") }));
        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetMetadataAsync());
    }

    [Fact]
    public async Task XxePayloadIsRejected()
    {
        const string malicious = """<!DOCTYPE edmx [<!ENTITY xxe SYSTEM "file:///etc/passwd">]><edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx">&xxe;</edmx:Edmx>""";
        var client = Client((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(malicious, Encoding.UTF8, "application/xml") }));
        await Assert.ThrowsAnyAsync<Exception>(() => client.GetMetadataAsync());
    }

    [Fact]
    public async Task EntityDefinitionsEnumeratesWithoutLowercaseValidation()
    {
        Uri? requested = null;
        var client = Client((r, _) =>
        {
            requested = r.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":[{"LogicalName":"account"}]}""", Encoding.UTF8, "application/json") });
        });
        var items = new List<System.Text.Json.JsonElement>();
        await foreach (var item in client.EnumerateEntityDefinitionsAsync(new DataverseQuery(Filter: "ChangeTrackingEnabled eq true"))) items.Add(item);
        Assert.Single(items);
        Assert.StartsWith("https://contoso.api.crm.dynamics.com/api/data/v9.2/EntityDefinitions", requested!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("ChangeTrackingEnabled", Uri.UnescapeDataString(requested.Query));
    }
}

using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Salesforce;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SFD-007: describe and metadata - object/global describe parsing, the 304 "unchanged" outcome, and
/// unknown-field tolerance.</summary>
public sealed class SalesforceDescribeTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    private static readonly Uri InstanceRoot = new("https://contoso.my.salesforce.com/");

    private static SalesforceClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new SalesforceClient(new IntegrationTransport(http, new StaticToken("token")), new SalesforceOptions(InstanceRoot));
    }

    private const string ObjectDescribeBody = """
        {"name":"Account","label":"Account","createable":true,"updateable":true,"deletable":false,"queryable":true,"keyPrefix":"001",
         "fields":[{"name":"Name","type":"string","createable":true,"updateable":true,"nillable":false,"unique":false,"externalId":false},
                   {"name":"ParentId","type":"reference","createable":true,"updateable":true,"nillable":true,"unique":false,"externalId":false,"referenceTo":["Account"],"relationshipName":"Parent"},
                   {"name":"FutureField__c","type":"string","createable":true,"updateable":true,"nillable":true,"unique":false,"externalId":false,"somethingUnexpected":true}]}
        """;

    [Fact]
    public async Task DescribeObjectParsesTheDocumentedFieldList()
    {
        var client = Client((_, _) => Task.FromResult(Json(ObjectDescribeBody)));
        var descriptor = await client.DescribeObjectAsync("Account");
        Assert.NotNull(descriptor);
        Assert.Equal("001", descriptor!.KeyPrefix);
        Assert.Equal(3, descriptor.Fields.Count);
        var parent = descriptor.Fields.Single(f => f.Name == "ParentId");
        Assert.Equal(["Account"], parent.ReferenceTo);
        Assert.Equal("Parent", parent.RelationshipName);
    }

    [Fact]
    public async Task UnknownFieldInAFutureResponseIsToleratedNotDropped()
    {
        // The "somethingUnexpected" property on FutureField__c must not cause a parse failure - the connector
        // reads only the documented keys it needs and ignores the rest.
        var client = Client((_, _) => Task.FromResult(Json(ObjectDescribeBody)));
        var descriptor = await client.DescribeObjectAsync("Account");
        Assert.Contains(descriptor!.Fields, f => f.Name == "FutureField__c");
    }

    [Fact]
    public async Task NotModifiedIsHandledAsUnchangedNotAParseFailure()
    {
        string? header = null;
        var client = Client((r, _) => { header = r.Headers.TryGetValues("If-Modified-Since", out var v) ? v.First() : null; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotModified)); });
        var descriptor = await client.DescribeObjectAsync("Account", DateTimeOffset.UtcNow.AddHours(-1));
        Assert.Null(descriptor);
        Assert.NotNull(header);
    }

    [Fact]
    public async Task DescribeGlobalParsesTheSummaryShapeWithoutFields()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"encoding":"UTF-8","maxBatchSize":200,"sobjects":[
              {"name":"Account","label":"Account","createable":true,"updateable":true,"deletable":true,"queryable":true,"keyPrefix":"001"},
              {"name":"Contact","label":"Contact","createable":true,"updateable":true,"deletable":true,"queryable":true,"keyPrefix":"003"}
            ]}
            """)));
        var descriptors = await client.DescribeGlobalAsync();
        Assert.Equal(2, descriptors.Count);
        Assert.All(descriptors, d => Assert.Empty(d.Fields));
    }

    [Fact]
    public async Task MalformedGlobalDescribeSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"not":"the expected shape"}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.DescribeGlobalAsync());
    }
}

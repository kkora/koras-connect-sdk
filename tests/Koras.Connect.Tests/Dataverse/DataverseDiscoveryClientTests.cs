using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-002: Global Discovery Service instance listing - zero/one/multiple instances, and malformed
/// responses surfacing a contract error rather than a crash.</summary>
public sealed class DataverseDiscoveryClientTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static DataverseDiscoveryClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var http = new HttpClient(new Handler(reply));
        return new DataverseDiscoveryClient(new IntegrationTransport(http, new StaticToken("token")));
    }

    private const string OneInstance = """
        {"value":[{"ApiUrl":"https://contoso.api.crm.dynamics.com","Id":"00000000-0000-0000-0000-000000000001",
        "EnvironmentId":"env-1","UniqueName":"contoso","FriendlyName":"Contoso","Region":"NorthAmerica","Version":"9.2.1.1","TenantId":"tenant-1"}]}
        """;

    [Fact]
    public async Task SingleInstanceIsParsed()
    {
        var client = Client((_, _) => Task.FromResult(Json(OneInstance)));
        var instances = new List<DataverseInstance>();
        await foreach (var instance in client.ListInstancesAsync()) instances.Add(instance);
        Assert.Single(instances);
        Assert.Equal("contoso", instances[0].UniqueName);
        Assert.Equal("tenant-1", instances[0].TenantId);
    }

    [Fact]
    public async Task EmptyInstanceListIsAValidNonErrorOutcome()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"value":[]}""")));
        var instances = new List<DataverseInstance>();
        await foreach (var instance in client.ListInstancesAsync()) instances.Add(instance);
        Assert.Empty(instances);
    }

    [Fact]
    public async Task MultipleInstancesAreAllReturnedWithoutAutoSelection()
    {
        var client = Client((_, _) => Task.FromResult(Json("""
            {"value":[
              {"ApiUrl":"https://a.api.crm.dynamics.com","Id":"1","EnvironmentId":"e1","UniqueName":"a","FriendlyName":"A","Region":"NA","Version":"9.2","TenantId":"t"},
              {"ApiUrl":"https://b.api.crm.dynamics.com","Id":"2","EnvironmentId":"e2","UniqueName":"b","FriendlyName":"B","Region":"EU","Version":"9.2","TenantId":"t"}
            ]}
            """)));
        var instances = new List<DataverseInstance>();
        await foreach (var instance in client.ListInstancesAsync()) instances.Add(instance);
        Assert.Equal(2, instances.Count);
    }

    [Theory]
    [InlineData("""{"notValue":[]}""")]
    [InlineData("""{"value":"not-an-array"}""")]
    [InlineData("""{"value":[{"ApiUrl":"https://a"}]}""")]
    public async Task MalformedResponseSurfacesAContractErrorNotACrash(string malformed)
    {
        var client = Client((_, _) => Task.FromResult(Json(malformed)));
        await Assert.ThrowsAsync<ContractException>(async () =>
        {
            await foreach (var _ in client.ListInstancesAsync()) { }
        });
    }

    [Fact]
    public async Task NonSuccessResponseClassifiesThroughDataverseApiException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)));
        var ex = await Assert.ThrowsAsync<DataverseApiException>(async () =>
        {
            await foreach (var _ in client.ListInstancesAsync()) { }
        });
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.IsType<AuthenticationException>(ex.InnerException);
    }

    [Fact]
    public void DefaultDiscoveryRootIsTheCommercialCloudHost()
    {
        Assert.Equal("https://globaldisco.crm.dynamics.com/", DataverseDiscoveryClient.CommercialGlobalDiscoveryRoot.AbsoluteUri);
        Assert.Equal("https://globaldisco.crm.dynamics.cn/", DataverseDiscoveryClient.ChinaGlobalDiscoveryRoot.AbsoluteUri);
    }

    [Fact]
    public async Task SovereignCloudRootMustBeSuppliedExplicitlyRatherThanInferred()
    {
        var requested = (Uri?)null;
        var http = new HttpClient(new Handler((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); }));
        var client = new DataverseDiscoveryClient(new IntegrationTransport(http, new StaticToken("token")), DataverseDiscoveryClient.GccHighGlobalDiscoveryRoot);
        await foreach (var _ in client.ListInstancesAsync()) { }
        Assert.StartsWith("https://globaldisco.crm.microsoftdynamics.us/", requested!.AbsoluteUri, StringComparison.Ordinal);
    }
}

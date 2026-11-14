using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Dataverse;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>DVT-003: client foundation (base URL composition, required headers, options validation) and DVT-002's
/// WhoAmI-based connectivity probe / <see cref="IConnector.DiagnoseAsync"/> wiring.</summary>
public sealed class DataverseClientTests
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

    [Theory]
    [InlineData("http://contoso.api.crm.dynamics.com/")]
    [InlineData("https://user:pass@contoso.api.crm.dynamics.com/")]
    public void NonHttpsOrCredentialBearingRootIsRejectedAtConstruction(string root)
    {
        Assert.ThrowsAny<Exception>(() => new DataverseOptions(new Uri(root)).Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5001)]
    public void PageSizeOutsideBoundsIsRejected(int pageSize)
        => Assert.Throws<OptionsValidationException>(() => new DataverseOptions(EnvironmentRoot, pageSize).Validate());

    [Fact]
    public void ValidateOptionsMatchesConstructorValidation()
        => Assert.Throws<OptionsValidationException>(() => DataverseClient.ValidateOptions(new DataverseOptions(new Uri("http://contoso/"))));

    [Fact]
    public async Task EveryRequestCarriesTheRequiredODataHeaders()
    {
        IReadOnlyList<string>? maxVersion = null, version = null, accept = null;
        var client = Client((r, _) =>
        {
            maxVersion = r.Headers.TryGetValues("OData-MaxVersion", out var mv) ? mv.ToList() : null;
            version = r.Headers.TryGetValues("OData-Version", out var v) ? v.ToList() : null;
            accept = r.Headers.Accept.Select(a => a.MediaType!).ToList();
            return Task.FromResult(Json("""{"UserId":"00000000-0000-0000-0000-000000000001","BusinessUnitId":"00000000-0000-0000-0000-000000000002","OrganizationId":"00000000-0000-0000-0000-000000000003"}"""));
        });
        await client.WhoAmIAsync();
        Assert.Equal(["4.0"], maxVersion);
        Assert.Equal(["4.0"], version);
        Assert.Contains("application/json", accept!);
    }

    [Fact]
    public async Task BaseUrlIsComposedWithTheFixedApiVersion()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"UserId":"00000000-0000-0000-0000-000000000001","BusinessUnitId":"00000000-0000-0000-0000-000000000002","OrganizationId":"00000000-0000-0000-0000-000000000003"}""")); });
        await client.WhoAmIAsync();
        Assert.Equal("https://contoso.api.crm.dynamics.com/api/data/v9.2/WhoAmI", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task WhoAmIParsesAllThreeIdentifiers()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"UserId":"11111111-1111-1111-1111-111111111111","BusinessUnitId":"22222222-2222-2222-2222-222222222222","OrganizationId":"33333333-3333-3333-3333-333333333333"}""")));
        var who = await client.WhoAmIAsync();
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), who.UserId);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), who.BusinessUnitId);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), who.OrganizationId);
    }

    [Fact]
    public async Task WhoAmIMissingFieldSurfacesAContractError()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"UserId":"11111111-1111-1111-1111-111111111111"}""")));
        await Assert.ThrowsAsync<ContractException>(() => client.WhoAmIAsync());
    }

    [Fact]
    public async Task UnauthorizedWhoAmIClassifiesAsAuthenticationException()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)));
        var ex = await Assert.ThrowsAsync<DataverseApiException>(() => client.WhoAmIAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.IsType<AuthenticationException>(ex.InnerException);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsAllThreeStagesPassedOnSuccess()
    {
        var client = Client((_, _) => Task.FromResult(Json("""{"UserId":"11111111-1111-1111-1111-111111111111","BusinessUnitId":"22222222-2222-2222-2222-222222222222","OrganizationId":"33333333-3333-3333-3333-333333333333"}""")));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(3, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticOutcome.Passed, d.Outcome));
    }

    [Fact]
    public async Task DiagnoseAsyncReportsAuthenticationFailedAndSkipsConnectivityOn401()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.Unauthorized)));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Passed, diagnostics.Single(d => d.Stage == DiagnosticStage.Configuration).Outcome);
        Assert.Equal(DiagnosticOutcome.Failed, diagnostics.Single(d => d.Stage == DiagnosticStage.Authentication).Outcome);
        Assert.Equal(DiagnosticOutcome.Skipped, diagnostics.Single(d => d.Stage == DiagnosticStage.Connectivity).Outcome);
    }

    [Fact]
    public async Task DiagnoseAsyncReportsConnectivityFailedOnNonAuthenticationFailure()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}", HttpStatusCode.ServiceUnavailable)));
        var diagnostics = await client.DiagnoseAsync();
        Assert.Equal(DiagnosticOutcome.Passed, diagnostics.Single(d => d.Stage == DiagnosticStage.Authentication).Outcome);
        Assert.Equal(DiagnosticOutcome.Failed, diagnostics.Single(d => d.Stage == DiagnosticStage.Connectivity).Outcome);
    }

    [Fact]
    public async Task AnUrlExceedingTheDocumented32KiBBoundIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var hugeSelect = string.Join(",", Enumerable.Repeat("a", 20_000));
        await Assert.ThrowsAsync<ArgumentException>(() => client.GetRowAsync("accounts", Guid.NewGuid(), new DataverseQuery(Select: hugeSelect)));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task APathSegmentExceedingTheDocumented260CharacterBoundIsRejectedLocally()
    {
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("{}")); });
        var hugeProperty = new string('a', 300);
        using var value = System.Text.Json.JsonDocument.Parse(""" "x" """);
        await Assert.ThrowsAsync<ArgumentException>(() => client.UpdatePropertyAsync("accounts", Guid.NewGuid(), hugeProperty, value.RootElement, writesEnabled: true));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void DescriptorIdentifiesMicrosoftDataverseAtTheFixedApiVersion()
    {
        var client = Client((_, _) => Task.FromResult(Json("{}")));
        Assert.Equal("Microsoft", client.Descriptor.Provider);
        Assert.Equal("Dataverse", client.Descriptor.Product);
        Assert.Equal("v9.2", client.Descriptor.ApiVersion);
    }
}

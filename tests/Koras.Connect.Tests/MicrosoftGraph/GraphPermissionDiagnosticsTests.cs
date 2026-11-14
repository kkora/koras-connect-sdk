using System.Net;
using System.Text;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-024: permission and consent diagnostics - grant listing and the admin-consent URL builder's
/// <c>common</c>-never-valid rule.</summary>
public sealed class GraphPermissionDiagnosticsTests
{
    private sealed class StaticToken(string token) : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult(token); }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => reply(r, ct); }
    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static GraphClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply, GraphOptions? options = null)
        => new(new IntegrationTransport(new HttpClient(new Handler(reply)), new StaticToken("token")), options);

    [Fact]
    public async Task ListGrantsForCurrentAppHitsTheTenantWideResource()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[{"clientId":"c1","consentType":"AllPrincipals","resourceId":"r1","scope":"User.Read"}]}""")); });
        var grants = new List<GraphPermissionGrant>();
        await foreach (var grant in client.ListGrantsForCurrentAppAsync()) grants.Add(grant);
        Assert.Single(grants);
        Assert.Equal("User.Read", grants[0].Scope);
        Assert.Equal("https://graph.microsoft.com/v1.0/oauth2PermissionGrants", requested!.AbsoluteUri);
    }

    [Fact]
    public async Task ListMyGrantsThrowsLocallyWithoutADelegatedContext()
    {
        // Found missing by Phase 2 review pass 2 - every other /me-shortcut in this connector throws locally
        // before any HTTP call unless IsDelegatedContext is explicitly set.
        var calls = 0;
        var client = Client((_, _) => { calls++; return Task.FromResult(Json("""{"value":[]}""")); });
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in client.ListMyGrantsAsync()) { }
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task ListMyGrantsHitsThePerUserResourceWithADelegatedContext()
    {
        Uri? requested = null;
        var client = Client((r, _) => { requested = r.RequestUri; return Task.FromResult(Json("""{"value":[]}""")); }, new GraphOptions(IsDelegatedContext: true));
        await foreach (var _ in client.ListMyGrantsAsync()) { }
        Assert.Equal("https://graph.microsoft.com/v1.0/me/oauth2PermissionGrants", requested!.AbsoluteUri);
    }

    [Fact]
    public void BuildAdminConsentUrlRejectsCommonTenant()
    {
        var ex = Assert.Throws<ArgumentException>(() => GraphClient.BuildAdminConsentUrl("common", "client-1", ["User.Read"], new Uri("https://example.com/callback")));
        Assert.Contains("common", ex.Message);
    }

    [Fact]
    public void BuildAdminConsentUrlComposesTheDocumentedQueryShape()
    {
        var url = GraphClient.BuildAdminConsentUrl("contoso-tenant", "client-1", ["User.Read", "Mail.Read"], new Uri("https://example.com/callback"), state: "xyz");
        Assert.Equal("https", url.Scheme);
        Assert.Equal("login.microsoftonline.com", url.Host);
        Assert.Equal("/contoso-tenant/v2.0/adminconsent", url.AbsolutePath);
        var decoded = Uri.UnescapeDataString(url.Query);
        Assert.Contains("client_id=client-1", decoded);
        Assert.Contains("scope=User.Read Mail.Read", decoded);
        Assert.Contains("redirect_uri=https://example.com/callback", decoded);
        Assert.Contains("state=xyz", decoded);
    }

    [Fact]
    public void BuildAdminConsentUrlOmitsStateWhenNotSupplied()
    {
        var url = GraphClient.BuildAdminConsentUrl("contoso-tenant", "client-1", ["User.Read"], new Uri("https://example.com/callback"));
        Assert.DoesNotContain("state=", url.Query);
    }
}

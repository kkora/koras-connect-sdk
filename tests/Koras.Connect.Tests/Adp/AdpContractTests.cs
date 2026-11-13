using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Events;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Koras.Connect.Tests;

public sealed class AdpContractTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => Task.FromResult(reply(r)); }
    [Theory]
    [InlineData(AdpOperation.Hire,"/events/hr/v1/worker.hire")]
    [InlineData(AdpOperation.Rehire,"/events/hr/v1/worker.rehire")]
    [InlineData(AdpOperation.TerminateAssignment,"/events/hr/v1/worker.work-assignment.terminate")]
    [InlineData(AdpOperation.ChangeManager,"/events/hr/v1/worker.reports-to.modify")]
    [InlineData(AdpOperation.ChangePersonalEmail,"/events/hr/v1/worker.personal-communication.email.change")]
    [InlineData(AdpOperation.ModifyDepartmentAllocations,"/events/hr/v1/worker.work-assignment.assigned-organizational-units.modify")]
    [InlineData(AdpOperation.ChangePayDataInput,"/events/payroll/v1/pay-data-input.modify")]
    public async Task DocumentedEventRoutesAreExplicitAndNeverRetried(AdpOperation op,string path)
    {
        var calls=0;
        using var http=new HttpClient(new Handler(r=>{calls++;Assert.Equal(HttpMethod.Post,r.Method);Assert.Equal(path,r.RequestUri!.AbsolutePath);return new(HttpStatusCode.ServiceUnavailable);}));
        var c=new AdpOperationsClient(new(http,new Token()),new("https://api.test"),[op]);
        using var payload=JsonDocument.Parse("""{"events":[{"data":{"transform":{}}}]}""");
        await Assert.ThrowsAsync<IntegrationRequestException>(()=>c.SubmitEventAsync(op,payload.RootElement));
        Assert.Equal(1,calls);
    }
    [Theory]
    [InlineData(AdpOperation.PayrollInstructions,"/payroll/v1/workers/synthetic/payroll-instructions")]
    [InlineData(AdpOperation.PayDistributions,"/payroll/v2/workers/synthetic/pay-distributions")]
    [InlineData(AdpOperation.TimeOffBalances,"/time/v2/workers/synthetic/time-off-details/time-off-balances")]
    [InlineData(AdpOperation.TimeOffRequests,"/time/v2/workers/synthetic/time-off-details/time-off-requests")]
    public async Task DocumentedWorkerReadsUseExpectedRoutes(AdpOperation op,string path)
    {
        using var http=new HttpClient(new Handler(r=>{Assert.Equal(path,r.RequestUri!.AbsolutePath);return new(HttpStatusCode.OK){Content=new StringContent("{}")};}));
        var c=new AdpOperationsClient(new(http,new Token()),new("https://api.test"),[op]);
        using var result=await c.ReadAsync(op,"synthetic");
        await Assert.ThrowsAsync<ArgumentException>(()=>c.ReadAsync(op,"../bad"));
    }
    [Fact] public async Task BenefitsRequireEffectiveDateAndPreserveDocumentedFilter()
    {
        using var http=new HttpClient(new Handler(r=>{Assert.Equal("/benefits/v1/associates/synthetic/spending-account-enrollments",r.RequestUri!.AbsolutePath);Assert.Contains("validFrom ge '2026-01-01'",Uri.UnescapeDataString(r.RequestUri.Query));return new(HttpStatusCode.OK){Content=new StringContent("{}")};}));
        var c=new AdpOperationsClient(new(http,new Token()),new("https://api.test"),[AdpOperation.SpendingAccountEnrollments]);
        using var result=await c.ReadSpendingAccountEnrollmentsAsync("synthetic",new(2026,1,1));
        await Assert.ThrowsAsync<ArgumentException>(()=>c.ReadAsync(AdpOperation.SpendingAccountEnrollments,"synthetic"));
    }
    [Fact] public async Task PollingPreservesMessageIdAndAcknowledgesOnlyExplicitly()
    {
        var calls=0;
        using var http=new HttpClient(new Handler(r=>{calls++;var response=new HttpResponseMessage(HttpStatusCode.NoContent);if(r.Method==HttpMethod.Get)response.Headers.Add("adp-msg-msgid","synthetic-message");else {Assert.Equal(HttpMethod.Delete,r.Method);Assert.EndsWith("/synthetic-message",r.RequestUri!.AbsolutePath);}return response;}));
        var c=new AdpOperationsClient(new(http,new Token()),new("https://api.test"),[AdpOperation.EventNotifications]);
        using var result=await c.ReadNotificationAsync();Assert.Equal("synthetic-message",result.Headers["adp-msg-msgid"][0]);Assert.Equal(1,calls);
        using var ack=await c.AcknowledgeNotificationAsync("synthetic-message");Assert.Equal(2,calls);
    }
    [Fact] public async Task MissingEntitlementsFailBeforeNetwork()
    {
        using var http=new HttpClient(new Handler(_=>throw new Exception("No network expected")));
        var c=new AdpOperationsClient(new(http,new Token()),new("https://api.test"),[]);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>c.ReadAsync(AdpOperation.EventNotifications));
    }
    [Fact] public void PublisherHeaderUsesConstantTimeComparisonAndDeliveryKeysAreUnambiguous()
    {
        const string id="synthetic-client",secret="synthetic-secret";
        var expected=Convert.ToHexString(HMACSHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret),System.Text.Encoding.UTF8.GetBytes(id)));
        Assert.True(AdpWebhookAuthentication.VerifyPublisherHeader(expected,id,secret));
        Assert.False(AdpWebhookAuthentication.VerifyPublisherHeader(new string('0',64),id,secret));
        Assert.False(AdpWebhookAuthentication.VerifyPublisherHeader("invalid",id,secret));
        using var d=JsonDocument.Parse("""{"meta":{"orgID":"synthetic-org","messageId":"synthetic-message"}}""");
        Assert.Equal("[\"synthetic-org\",\"synthetic-message\"]",AdpWebhookAuthentication.GetDeliveryKey(d.RootElement));
    }
    [Fact] public void CertificateAndDIContractsResolveWithoutExternalInfrastructure()
    {
        using var rsa=RSA.Create(2048);
        var request=new CertificateRequest("CN=synthetic",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var cert=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(1));
        using var client=ClientCertificateTransport.Create(cert);Assert.Equal(TimeSpan.FromSeconds(100),client.Timeout);
        var services=new ServiceCollection();services.AddKorasAdp(new(new("https://auth.test"),"id","synthetic"),new(new("https://api.test")),cert);
        using var provider=services.BuildServiceProvider();Assert.NotNull(provider.GetRequiredService<AdpWorkersClient>());
        Assert.Throws<InvalidOperationException>(()=>services.AddKorasAdp(new(new("https://auth.test"),"id","synthetic"),new(new("https://api.test")),cert));
    }
    [Fact] public async Task RepeatedPagesStopInsteadOfLooping()
    {
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent("""{"workers":[{"associateOID":"synthetic"}]}""")}));
        var c=new AdpWorkersClient(new(http,new Token()),new(new("https://api.test"),1));
        await Assert.ThrowsAsync<InvalidDataException>(async()=>{await foreach(var w in c.EnumerateWorkersAsync()) { }});
    }
}

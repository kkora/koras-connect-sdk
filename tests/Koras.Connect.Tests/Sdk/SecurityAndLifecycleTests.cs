using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;
public sealed class SecurityAndLifecycleTests
{
    private sealed class Clock : TimeProvider
    { public DateTimeOffset Now = new(2026,1,1,0,0,0,TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) => Task.FromResult(reply(r)); }
    private sealed class SlowHandler : HttpMessageHandler
    { protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct) { await Task.Delay(Timeout.InfiniteTimeSpan,ct); return new(HttpStatusCode.OK); } }
    private sealed class Token : IAccessTokenSource
    { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic-secret-token"); }
    [Fact] public async Task TokenRenewsBeforeExpiryAndCanceledCallsCannotUseCache()
    {
        var calls=0; var clock=new Clock();
        using var h=new HttpClient(new Handler(_=>{calls++;return new(HttpStatusCode.OK){Content=new StringContent("""{"access_token":"synthetic","expires_in":100}""")};}));
        using var source=new ClientCredentialsTokenSource(h,new(new("https://auth.test"),"id","secret"),clock);
        await source.GetTokenAsync(); clock.Now=clock.Now.AddSeconds(89); await source.GetTokenAsync(); Assert.Equal(1,calls);
        clock.Now=clock.Now.AddSeconds(2); await source.GetTokenAsync(); Assert.Equal(2,calls);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>source.GetTokenAsync(new(true)).AsTask());
    }
    [Fact] public async Task DeadlineCancelsInFlightResponse()
    {
        using var h=new HttpClient(new SlowHandler());
        var t=new IntegrationTransport(h,new Token(),new(){RequestTimeout=TimeSpan.FromMilliseconds(20)});
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>t.ReadJsonAsync(new("https://api.test/private-worker")));
    }
    [Fact] public async Task SdkActivityDoesNotEmitWorkerOrCredentialData()
    {
        using var parent = new Activity("synthetic-telemetry-test").Start();
        var activities=new List<Activity>();
        using var listener=new ActivityListener { ShouldListenTo=s=>s.Name==IntegrationTransport.InstrumentationName,
            Sample=(ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped=a=>{ if(a.TraceId==parent.TraceId) activities.Add(a); } };
        ActivitySource.AddActivityListener(listener);
        using var h=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent("{}")}));
        using var body=await new IntegrationTransport(h,new Token()).ReadJsonAsync(new("https://api.test/private-worker?private=1"));
        var activity=Assert.Single(activities);
        var tags=string.Join(" ",activity.TagObjects.Select(t=>$"{t.Key}:{t.Value}"));
        Assert.DoesNotContain("private-worker",tags); Assert.DoesNotContain("synthetic-secret",tags); Assert.DoesNotContain("private=1",tags);
        Assert.Contains("GET",tags);
    }
    [Fact] public void CertificateLoadersRoundTripPemAndPkcs12WithoutPersistingRepositorySecrets()
    {
        using var rsa=RSA.Create(2048);
        var request=new CertificateRequest("CN=synthetic",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var certificate=request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(1));
        var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var certPath=Path.Combine(folder,"synthetic.crt"); var keyPath=Path.Combine(folder,"synthetic.key"); var pfxPath=Path.Combine(folder,"synthetic.pfx");
            File.WriteAllText(certPath,certificate.ExportCertificatePem()); File.WriteAllText(keyPath,rsa.ExportPkcs8PrivateKeyPem()); File.WriteAllBytes(pfxPath,certificate.Export(X509ContentType.Pkcs12,"synthetic"));
            using var pem=ClientCertificateTransport.LoadPem(certPath,keyPath);using var pfx=ClientCertificateTransport.LoadPkcs12(pfxPath,"synthetic");
            Assert.True(pem.HasPrivateKey);Assert.True(pfx.HasPrivateKey);
            using var publicOnly=X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
            Assert.Throws<ArgumentException>(()=>ClientCertificateTransport.Create(publicOnly));
        }
        finally { Directory.Delete(folder,true); }
    }
}

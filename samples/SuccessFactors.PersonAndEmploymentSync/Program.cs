using System.Net;
using System.Text;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;

// Offline demonstration of the SAP SuccessFactors OData V2 connector: SAML2-bearer token exchange -> enumerate
// persons/employment/jobs -> resolve a manager chain -> read $metadata -> upsert a person record (opt-in write).
// Every HTTP response below is synthetic; this sample never contacts a live SAP SuccessFactors tenant and
// requires no company ID, API host or SAML assertion provider to run.
Console.WriteLine("Koras.Connect.Sap.SuccessFactors offline sample: auth -> enumerate persons/employment/jobs -> manager chain -> metadata -> upsert.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

using var oauthHttp = new HttpClient(new SyntheticHandler((request, _) =>
    Reply("""{"access_token":"sample-access-token","token_type":"Bearer","expires_in":86399}""")));
using var tokens = new SamlBearerTokenSource(oauthHttp, new Uri("https://sample.successfactors.test/oauth/token"),
    "sample-company", "sample-client", _ => ValueTask.FromResult("sample-saml-assertion"));

using var apiHttp = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (path.EndsWith("/PerPerson", StringComparison.Ordinal) && request.Method == HttpMethod.Get)
        return Reply("""{"d":{"results":[{"personIdExternal":"sample-1","dateOfBirth":"/Date(631152000000)/"}]}}""");
    if (path.EndsWith("/EmpEmployment", StringComparison.Ordinal))
        return Reply("""{"d":{"results":[{"userId":"sample-1","personIdExternal":"sample-1","startDate":"/Date(1600000000000)/"}]}}""");
    if (path.EndsWith("/EmpJob", StringComparison.Ordinal))
        return Reply("""{"d":{"results":[{"userId":"sample-1","seqNumber":1,"managerId":null,"startDate":"/Date(1600000000000)/","jobTitle":"Engineer"}]}}""");
    if (path.EndsWith("/$metadata", StringComparison.Ordinal))
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""
            <edmx:Edmx xmlns:edmx="http://schemas.microsoft.com/ado/2007/06/edmx"><edmx:DataServices><Schema xmlns="http://schemas.microsoft.com/ado/2008/09/edm">
            <EntityType Name="PerPerson"><Key><PropertyRef Name="personIdExternal"/></Key><Property Name="personIdExternal" Type="Edm.String"/></EntityType>
            </Schema></edmx:DataServices></edmx:Edmx>
            """, Encoding.UTF8, "application/xml") };
    if (path.EndsWith("/upsert", StringComparison.Ordinal))
        return Reply("""{"d":[{"key":"sample-1","status":"OK","editStatus":"UPDATED","httpCode":204,"index":0,"message":null}]}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SuccessFactorsClient(new IntegrationTransport(apiHttp, tokens), new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));

await foreach (var person in client.EnumeratePersonsAsync())
    Console.WriteLine($"Person: {person.PersonIdExternal} (dateOfBirth raw={person.DateOfBirth})");

await foreach (var employment in client.EnumerateEmploymentAsync())
    Console.WriteLine($"Employment: {employment.UserId} started {employment.StartDate}");

await foreach (var job in client.EnumerateJobsAsync())
    Console.WriteLine($"Job: {job.UserId} seq={job.SequenceNumber} title={job.JobTitle}");

var managerChain = await client.ResolveManagerChainAsync("sample-1");
Console.WriteLine($"Manager chain for sample-1 has {managerChain.Count} link(s); top of chain manager={managerChain[^1].ManagerId ?? "(none)"}.");

var metadata = await client.GetMetadataAsync();
var descriptors = SuccessFactorsClient.ParseEntityDescriptors(metadata);
Console.WriteLine($"Metadata describes {descriptors.Count} entity type(s): {string.Join(", ", descriptors.Select(d => d.Name))}.");

// Writes require explicit opt-in (writesEnabled: true) only after the host has verified entity upsert support
// and RBP permissions against its own tenant - this sample's "verification" is simply the synthetic handler above.
using var updatedPerson = System.Text.Json.JsonDocument.Parse("""{"personIdExternal":"sample-1"}""");
var upsertResults = await client.UpsertAsync("PerPerson", [updatedPerson.RootElement], writesEnabled: true);
Console.WriteLine($"Upsert row 0: editStatus={upsertResults[0].EditStatus}, succeeded={upsertResults[0].Succeeded}.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live SAP SuccessFactors tenant.");

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

using System.Net;
using System.Text;
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Transport;

// Offline demonstration of the Employee Central foundation-object surface added under SAP-010: enumerate
// departments/divisions/locations, then walk a department's parent-reference chain to its root. The
// FODepartment/FODivision/FOLocation entity-set names and their externalCode+effectiveStartDate business keys
// are confirmed against the official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), sections 6.4.2
// and 6.7 - see the remarks at the top of FoundationObjects.cs for exactly what remains unverified (the complete
// field list beyond the confirmed key fields, and whether a given tenant has actually enabled these entities).
// Every HTTP response below is synthetic; this sample never contacts a live SAP SuccessFactors tenant.
Console.WriteLine("Koras.Connect.Sap.SuccessFactors offline sample: foundation objects (departments, divisions, locations) and parent-chain resolution.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    var query = request.RequestUri.Query;
    if (path.EndsWith("/FODivision", StringComparison.Ordinal))
        return Reply("""{"d":{"results":[{"externalCode":"DIV-SALES","effectiveStartDate":"/Date(1600000000000)/","status":"A"}]}}""");
    if (path.EndsWith("/FOLocation", StringComparison.Ordinal))
        return Reply("""{"d":{"results":[{"externalCode":"LOC-ATL","effectiveStartDate":"/Date(1600000000000)/","status":"A"}]}}""");
    if (path.EndsWith("/FODepartment", StringComparison.Ordinal) && query.Contains("DEPT-ROOT"))
        return Reply("""{"d":{"results":[{"externalCode":"DEPT-ROOT","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":null}]}}""");
    if (path.EndsWith("/FODepartment", StringComparison.Ordinal) && query.Contains("DEPT-ENGINEERING"))
        return Reply("""{"d":{"results":[{"externalCode":"DEPT-ENGINEERING","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":"DEPT-ROOT"}]}}""");
    if (path.EndsWith("/FODepartment", StringComparison.Ordinal))
        return Reply("""{"d":{"results":[{"externalCode":"DEPT-ENGINEERING","effectiveStartDate":"/Date(1600000000000)/","status":"A","parentExternalCode":"DEPT-ROOT"},{"externalCode":"DEPT-RETIRED","effectiveStartDate":"/Date(1500000000000)/","status":"T","parentExternalCode":null}]}}""");
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var client = new SuccessFactorsClient(new IntegrationTransport(http, new StaticToken("sample-access-token")), new SuccessFactorsOptions(new Uri("https://sample.successfactors.test/odata/v2/")));

await foreach (var department in client.EnumerateFoundationObjectAsync<FODepartment>("FODepartment"))
    Console.WriteLine($"Department: {department.ExternalCode} (status={department.Status}, parent={department.ParentExternalCode ?? "(none)"})");

await foreach (var division in client.EnumerateFoundationObjectAsync<FODivision>("FODivision"))
    Console.WriteLine($"Division: {division.ExternalCode} (status={division.Status})");

await foreach (var location in client.EnumerateFoundationObjectAsync<FOLocation>("FOLocation"))
    Console.WriteLine($"Location: {location.ExternalCode} (status={location.Status})");

// ResolveDepartmentParentChainAsync reuses the same visited-set cycle guard as ResolveManagerChainAsync; a
// revisited externalCode throws InvalidDataException rather than looping indefinitely.
var chain = await client.ResolveDepartmentParentChainAsync("DEPT-ENGINEERING");
Console.WriteLine();
Console.WriteLine("Department parent chain for DEPT-ENGINEERING:");
foreach (var link in chain)
    Console.WriteLine($"  {link.ExternalCode} -> parent: {link.ParentExternalCode ?? "(root)"}");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live SAP SuccessFactors tenant.");

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class StaticToken(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

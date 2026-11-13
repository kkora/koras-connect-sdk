using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;

// Offline demonstration of ADP-004/005/013/016/017/019: the read surface - single-worker read, "retrieve all" via
// automatic paging, typed filter+sort search, resumable paging via a persisted ContinuationToken, typed catalog
// reads (organization departments, time-off balances, spending-account enrollments), worker metadata, and the
// explicit HTTP 202 safety rejection. Every HTTP response below is synthetic; this sample never contacts a live
// ADP tenant.
Console.WriteLine("Koras.Connect.Adp offline sample: the worker-read surface - retrieve all, filter, sort, paging.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

Console.WriteLine("Scenario 1: read a single worker, found and not found (ADP-004, GetWorkerAsync).");
await RunSingleWorkerReadAsync();

Console.WriteLine();
Console.WriteLine("Scenario 2: retrieve the whole population via automatic paging (ADP-005, EnumerateTypedWorkersAsync).");
await RunRetrieveAllAsync();

Console.WriteLine();
Console.WriteLine("Scenario 3: a typed, whitelisted filter combined with a typed sort (ADP-005, WorkerFilter + WorkerSort).");
await RunFilteredAndSortedSearchAsync();

Console.WriteLine();
Console.WriteLine("Scenario 4: resumable paging - persist a ContinuationToken, then resume from it (ADP-005, GetWorkersPageAsPageAsync).");
await RunResumablePaginationAsync();

Console.WriteLine();
Console.WriteLine("Scenario 5: typed catalog reads beyond workers (ADP-013/016/017/019, departments, time-off and benefits).");
await RunTypedCatalogReadsAsync();

Console.WriteLine();
Console.WriteLine("Scenario 6: worker metadata and the explicit HTTP 202 safety rejection (ADP-005/019).");
await RunMetadataAndSafetyChecksAsync();

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static async Task RunSingleWorkerReadAsync()
{
    using var http = new HttpClient(new SyntheticHandler(request =>
    {
        var oid = request.RequestUri!.AbsolutePath.Split('/')[^1];
        return oid == "sample-worker-1"
            ? Reply("""{"workers":[{"associateOID":"sample-worker-1","person":{"legalName":{"givenName":"Ada","familyName1":"Lovelace"}}}]}""")
            : Reply("""{"workers":[]}""");
    }));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));

    var found = await workers.GetWorkerAsync("sample-worker-1");
    Console.WriteLine($"  Found: associateOID={found!.AssociateOid}, name={found.Person!.LegalName!.GivenName} {found.Person.LegalName.FamilyName}.");

    var missing = await workers.GetWorkerAsync("unknown-worker");
    Console.WriteLine($"  Not found: GetWorkerAsync returned {(missing is null ? "null" : "an unexpected non-null result")} for an unauthorized/unknown associate OID.");
}

static async Task RunRetrieveAllAsync()
{
    const string page1 = """{"workers":[{"associateOID":"w1"},{"associateOID":"w2"}]}""";
    const string emptyPage = """{"workers":[]}""";
    // Each fresh enumeration restarts paging at $skip=0, so the handler keys its reply off that query parameter.
    using var http = new HttpClient(new SyntheticHandler(request =>
        Reply(request.RequestUri!.Query.Contains("$skip=0", StringComparison.Ordinal) ? page1 : emptyPage)));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));

    var all = new List<string>();
    await foreach (var worker in workers.EnumerateTypedWorkersAsync()) all.Add(worker.AssociateOid!);
    Console.WriteLine($"  Retrieved {all.Count} worker(s) across automatic paging: {string.Join(", ", all)}.");
}

static async Task RunFilteredAndSortedSearchAsync()
{
    string? capturedQuery = null;
    using var http = new HttpClient(new SyntheticHandler(request =>
    {
        capturedQuery = Uri.UnescapeDataString(request.RequestUri!.Query);
        return Reply("""{"workers":[{"associateOID":"w2"},{"associateOID":"w1"}]}""");
    }));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));

    var filter = new WorkerFilter(WorkerFilterField.WorkAssignmentJobTitle, WorkerFilterOperator.Equals, "Engineer");
    // Sort is a best-effort addition: ADP's own documented contract for this endpoint never cites an $orderby
    // parameter, so a tenant may honor, ignore, or reject it. See WorkerSort's own remarks for the full caveat.
    var sort = new WorkerSort(WorkerFilterField.AssociateOid, SortDirection.Descending);
    using var result = await workers.SearchWorkersAsync(filter, sort: sort);

    Console.WriteLine($"  Query sent: {capturedQuery}");
    Console.WriteLine($"  Matched {result.RootElement.GetProperty("workers").GetArrayLength()} worker(s).");
}

static async Task RunResumablePaginationAsync()
{
    const string page1 = """{"workers":[{"associateOID":"w1"},{"associateOID":"w2"}]}""";
    const string page2 = """{"workers":[{"associateOID":"w3"}]}""";
    const string emptyPage = """{"workers":[]}""";
    using var http = new HttpClient(new SyntheticHandler(request =>
    {
        var query = request.RequestUri!.Query;
        if (query.Contains("$skip=0", StringComparison.Ordinal)) return Reply(page1);
        if (query.Contains("$skip=2", StringComparison.Ordinal)) return Reply(page2);
        return Reply(emptyPage);
    }));
    var workers = new AdpWorkersClient(new IntegrationTransport(http, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));

    var firstPage = await workers.GetWorkersPageAsPageAsync(take: 2);
    Console.WriteLine($"  Page 1: {string.Join(", ", firstPage.Items.Select(w => w.AssociateOid))}.");

    // A host persists only this opaque token (for example alongside a checkpoint record), not an in-memory
    // iterator, and can resume minutes or days later without replaying from the start.
    var token = firstPage.Next!;
    var secondPage = await workers.GetWorkersPageAsPageAsync(token);
    Console.WriteLine($"  Page 2 (resumed from the persisted token): {string.Join(", ", secondPage.Items.Select(w => w.AssociateOid))}.");

    // A page with items is never itself proof it was the last one - the terminal signal is an empty next page.
    var thirdPage = await workers.GetWorkersPageAsPageAsync(secondPage.Next!);
    Console.WriteLine($"  Page 3: {thirdPage.Items.Count} worker(s); Next={(thirdPage.Next is null ? "null (end of population reached)" : "non-null")}.");
}

static async Task RunTypedCatalogReadsAsync()
{
    using var http = new HttpClient(new SyntheticHandler(request => request.RequestUri!.AbsolutePath switch
    {
        "/hr/v1/organization-departments" => Reply("""{"organizationDepartments":[{"itemID":"D1","nameCode":{"codeValue":"ENG","shortName":"Engineering"}}]}"""),
        var path when path.EndsWith("/time-off-details/time-off-balances", StringComparison.Ordinal) =>
            Reply("""{"workers":[{"associateOID":"sample-worker-1","timeOffBalances":[{"accrualCode":{"codeValue":"VAC"},"balanceAmount":{"quantityValue":12.5,"unitCode":{"codeValue":"DAYS"}}}]}]}"""),
        var path when path.EndsWith("/spending-account-enrollments", StringComparison.Ordinal) =>
            // The account number is deliberately shown masked here, matching how ADP returns sensitive benefit
            // fields by default; this SDK neither unmasks it nor strips it, just preserves whatever was sent.
            Reply("""{"spendingAccountEnrollments":[{"itemID":"E1","accountBasedCoverage":{"planName":"FSA","accountNumber":"***-**-1234"}}]}"""),
        var other => throw new InvalidOperationException($"Unexpected request path: {other}"),
    }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"),
        [AdpOperation.OrganizationDepartments, AdpOperation.TimeOffBalances, AdpOperation.SpendingAccountEnrollments]);

    using var departmentsDoc = await client.ReadAsync(AdpOperation.OrganizationDepartments);
    var departments = departmentsDoc.RootElement.Deserialize<OrganizationDepartmentsResponse>()!;
    Console.WriteLine($"  Organization departments: {string.Join(", ", departments.Departments.Select(d => $"{d.ItemId} ({d.NameCode?.ShortName})"))}.");

    using var balancesDoc = await client.ReadAsync(AdpOperation.TimeOffBalances, "sample-worker-1");
    var balances = balancesDoc.RootElement.Deserialize<TimeOffBalancesResponse>()!;
    var balance = balances.Workers[0].Balances[0];
    Console.WriteLine($"  Time-off balance for sample-worker-1: {balance.BalanceAmount!.Value} {balance.BalanceAmount.UnitCode?.Code} ({balance.AccrualCode?.Code}).");

    // Spending-account enrollment reads require a mandatory effective-date filter (ADP-017); the SDK still
    // returns a raw JsonDocument here, so deserializing to the typed model is the caller's own choice.
    using var enrollmentsDoc = await client.ReadSpendingAccountEnrollmentsAsync("sample-worker-1", new DateOnly(2026, 1, 1));
    var enrollments = enrollmentsDoc.RootElement.Deserialize<SpendingAccountEnrollmentsResponse>()!;
    var accountNumber = enrollments.Enrollments[0].AccountBasedCoverage!.Value.GetProperty("accountNumber").GetString();
    Console.WriteLine($"  Spending-account enrollment for sample-worker-1: plan={enrollments.Enrollments[0].AccountBasedCoverage!.Value.GetProperty("planName").GetString()}, accountNumber={accountNumber} (masked by the tenant, preserved as-is).");
}

static async Task RunMetadataAndSafetyChecksAsync()
{
    using var metadataHttp = new HttpClient(new SyntheticHandler(_ => Reply("""{"workers":{"schema":{"associateOID":{"type":"string"}}}}""")));
    var workersForMetadata = new AdpWorkersClient(new IntegrationTransport(metadataHttp, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    using var metadataDoc = await workersForMetadata.GetMetadataAsync();
    Console.WriteLine($"  Worker metadata retrieved ({metadataDoc.RootElement.GetRawText().Length} characters of synthetic schema JSON).");

    using var acceptedHttp = new HttpClient(new SyntheticHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted)));
    var workersForAsyncProbe = new AdpWorkersClient(new IntegrationTransport(acceptedHttp, new FixedToken()), new AdpWorkersOptions(new Uri("https://api.sample-tenant.adp.test")));
    try
    {
        await workersForAsyncProbe.GetWorkersPageAsync();
        Console.WriteLine("  Unexpected: a 202 Accepted response was not rejected.");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"  HTTP 202 Accepted correctly rejected rather than misparsed: {ex.Message}");
    }
}

static HttpResponseMessage Reply(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class FixedToken : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("sample-token");
}

sealed class SyntheticHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request));
}

using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

// Offline demonstration of ADP-013/014/015/021: the typed write-command builders (ChangeManagerCommand,
// ModifyDepartmentAllocationsCommand, TerminateAssignmentCommand, ChangePayDataInputCommand) and the two safety
// gates around them - an explicit payroll opt-in and entitlement-tracker deny-before-network. Every HTTP response
// below is synthetic; this sample never contacts a live ADP tenant and never performs a real write.
Console.WriteLine("Koras.Connect.Adp offline sample: typed write-command builders and their safety gates.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs and no live ADP tenant is contacted.");
Console.WriteLine();

Console.WriteLine("Scenario 1: change a worker's manager (ADP-013, ChangeManagerCommand).");
await RunChangeManagerAsync();

Console.WriteLine();
Console.WriteLine("Scenario 2: reassign a worker's department allocations (ADP-013, ModifyDepartmentAllocationsCommand).");
await RunModifyDepartmentAllocationsAsync();

Console.WriteLine();
Console.WriteLine("Scenario 3: terminate a work assignment (ADP-014, TerminateAssignmentCommand).");
await RunTerminateAssignmentAsync();

Console.WriteLine();
Console.WriteLine("Scenario 4: change payroll input - the explicit opt-in gate (ADP-015, ChangePayDataInputCommand).");
await RunChangePayDataInputAsync();

Console.WriteLine();
Console.WriteLine("Scenario 5: a tenant-denied operation is rejected before any network call (ADP-021, AdpEntitlementTracker).");
await RunDeniedEntitlementAsync();

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live ADP tenant.");

static async Task RunChangeManagerAsync()
{
    string? sentEnvelope = null;
    using var http = new HttpClient(new SyntheticHandler(request => { sentEnvelope = ReadBody(request); return Reply("{}"); }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.ChangeManager]);

    var manager = new ManagerRelationship { WorkerId = new Identifier { Value = "MGR-42" }, ReportsToWorkerName = "Grace Hopper" };
    using var result = await client.SubmitChangeManagerAsync("sample-worker-1", manager);
    Console.WriteLine($"  Envelope sent: {sentEnvelope}");
    Console.WriteLine("  Submission accepted by the mocked endpoint.");
}

static async Task RunModifyDepartmentAllocationsAsync()
{
    string? sentEnvelope = null;
    using var http = new HttpClient(new SyntheticHandler(request => { sentEnvelope = ReadBody(request); return Reply("{}"); }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.ModifyDepartmentAllocations]);

    var departments = new[] { new OrganizationDepartment { ItemId = "DEPT-ENG", NameCode = new CodeValue { Code = "ENG" } } };
    using var result = await client.SubmitModifyDepartmentAllocationsAsync("sample-worker-1", departments);
    Console.WriteLine($"  Envelope sent: {sentEnvelope}");
    Console.WriteLine("  Submission accepted by the mocked endpoint.");
}

static async Task RunTerminateAssignmentAsync()
{
    string? sentEnvelope = null;
    using var http = new HttpClient(new SyntheticHandler(request => { sentEnvelope = ReadBody(request); return Reply("{}"); }));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.TerminateAssignment]);

    using var result = await client.SubmitTerminateAssignmentAsync("sample-worker-1", "WA-1", new DateOnly(2026, 12, 31), new CodeValue { Code = "RIF" });
    Console.WriteLine($"  Envelope sent: {sentEnvelope}");
    Console.WriteLine("  Submission accepted by the mocked endpoint.");
}

static async Task RunChangePayDataInputAsync()
{
    using var payDataInput = JsonDocument.Parse("""{"earningsCode":{"codeValue":"REG"},"amount":1500.00}""");

    Console.WriteLine("  Attempting without explicitPayrollWriteOptIn (expected to fail locally; the handler throws if ever called)...");
    using var refusingHttp = new HttpClient(new SyntheticHandler(_ => throw new InvalidOperationException("Should not be called: payroll writes require explicit opt-in.")));
    var refusingClient = new AdpOperationsClient(new IntegrationTransport(refusingHttp, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.ChangePayDataInput]);
    try
    {
        await refusingClient.SubmitChangePayDataInputAsync("sample-worker-1", "PG-1", payDataInput.RootElement, explicitPayrollWriteOptIn: false);
        Console.WriteLine("  Unexpected: the write was not rejected.");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Rejected locally as expected: {ex.Message}");
    }

    Console.WriteLine("  Retrying with explicitPayrollWriteOptIn: true...");
    string? sentEnvelope = null;
    using var acceptingHttp = new HttpClient(new SyntheticHandler(request => { sentEnvelope = ReadBody(request); return Reply("{}"); }));
    var acceptingClient = new AdpOperationsClient(new IntegrationTransport(acceptingHttp, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.ChangePayDataInput]);
    using var result = await acceptingClient.SubmitChangePayDataInputAsync("sample-worker-1", "PG-1", payDataInput.RootElement, explicitPayrollWriteOptIn: true);
    Console.WriteLine($"  Envelope sent: {sentEnvelope}");
    Console.WriteLine("  Submission accepted by the mocked endpoint.");
}

static async Task RunDeniedEntitlementAsync()
{
    var tracker = new AdpEntitlementTracker();
    tracker.Record(AdpOperation.TerminateAssignment, EntitlementState.Denied, "sample: tenant denied this operation in a prior authorized call");

    using var http = new HttpClient(new SyntheticHandler(_ => throw new InvalidOperationException("Should not be called: the entitlement tracker already recorded this operation as Denied.")));
    var client = new AdpOperationsClient(new IntegrationTransport(http, new FixedToken()), new Uri("https://api.sample-tenant.adp.test"), [AdpOperation.TerminateAssignment], tracker);

    try
    {
        await client.SubmitTerminateAssignmentAsync("sample-worker-1", "WA-1", new DateOnly(2026, 12, 31));
        Console.WriteLine("  Unexpected: the write was not rejected.");
    }
    catch (AuthorizationException ex)
    {
        Console.WriteLine($"  Rejected before any network call: {ex.Message}");
    }
}

static string ReadBody(HttpRequestMessage request) => request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();

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

using System.Net;
using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-013: typed write-command builders that are the inverse projection of the already-modeled
/// <see cref="ManagerRelationship"/>/<see cref="OrganizationDepartment"/> read shapes, for
/// <see cref="AdpOperation.ChangeManager"/> and <see cref="AdpOperation.ModifyDepartmentAllocations"/>. Covers the
/// exact documented envelope shape produced, local rejection of missing identity before any HTTP call, and wiring
/// through <see cref="AdpOperationsClient"/>'s convenience submission methods.</summary>
public sealed class AdpWriteCommandTests
{
    private sealed class Token : IAccessTokenSource { public ValueTask<string> GetTokenAsync(CancellationToken ct = default) => ValueTask.FromResult("synthetic"); }
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(reply(r)); }

    [Fact]
    public void ChangeManagerCommandBuildProducesTheExactDocumentedEnvelopeShape()
    {
        var manager = new ManagerRelationship
        {
            WorkerId = new Identifier { Value = "MGR1" },
            ReportsToWorkerName = "Ada Lovelace",
            ReportingRelationshipCode = new CodeValue { Code = "line" }
        };

        var envelope = ChangeManagerCommand.Build("W1", manager);

        var evt = envelope.GetProperty("events")[0].GetProperty("data");
        Assert.Equal("W1", evt.GetProperty("eventContext").GetProperty("worker").GetProperty("associateOID").GetString());
        var reportsTo = evt.GetProperty("transform").GetProperty("worker").GetProperty("reportsTo")[0];
        Assert.Equal("MGR1", reportsTo.GetProperty("workerID").GetProperty("idValue").GetString());
        Assert.Equal("Ada Lovelace", reportsTo.GetProperty("reportsToWorkerName").GetString());
        Assert.Equal("line", reportsTo.GetProperty("reportingRelationshipCode").GetProperty("codeValue").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeManagerCommandBuildRejectsAMissingAssociateOidLocally(string? associateOid)
    {
        var manager = new ManagerRelationship { WorkerId = new Identifier { Value = "MGR1" } };
        Assert.Throws<ArgumentException>(() => ChangeManagerCommand.Build(associateOid!, manager));
    }

    [Fact]
    public void ChangeManagerCommandBuildRejectsAMissingTargetManagerWorkerIdLocally()
    {
        Assert.Throws<ArgumentException>(() => ChangeManagerCommand.Build("W1", new ManagerRelationship()));
        Assert.Throws<ArgumentException>(() => ChangeManagerCommand.Build("W1", new ManagerRelationship { WorkerId = new Identifier() }));
    }

    [Fact]
    public void ModifyDepartmentAllocationsCommandBuildProducesTheExactDocumentedEnvelopeShape()
    {
        var departments = new[] { new OrganizationDepartment { ItemId = "D1", NameCode = new CodeValue { Code = "ENG" } } };

        var envelope = ModifyDepartmentAllocationsCommand.Build("W1", departments);

        var evt = envelope.GetProperty("events")[0].GetProperty("data");
        Assert.Equal("W1", evt.GetProperty("eventContext").GetProperty("worker").GetProperty("associateOID").GetString());
        var unit = evt.GetProperty("transform").GetProperty("worker").GetProperty("workAssignment").GetProperty("assignedOrganizationalUnits")[0];
        Assert.Equal("D1", unit.GetProperty("itemID").GetString());
        Assert.Equal("ENG", unit.GetProperty("nameCode").GetProperty("codeValue").GetString());
    }

    [Fact]
    public void ModifyDepartmentAllocationsCommandBuildRejectsAnEmptyOrMissingItemIdLocally()
    {
        Assert.Throws<ArgumentException>(() => ModifyDepartmentAllocationsCommand.Build("", [new OrganizationDepartment { ItemId = "D1" }]));
        Assert.Throws<ArgumentException>(() => ModifyDepartmentAllocationsCommand.Build("W1", Array.Empty<OrganizationDepartment>()));
        Assert.Throws<ArgumentException>(() => ModifyDepartmentAllocationsCommand.Build("W1", [new OrganizationDepartment()]));
    }

    [Fact]
    public void TerminateAssignmentCommandBuildProducesTheExactDocumentedEnvelopeShape()
    {
        var envelope = TerminateAssignmentCommand.Build("W1", "WA1", new DateOnly(2026, 6, 30), new CodeValue { Code = "RIF" });

        var context = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("eventContext").GetProperty("worker");
        Assert.Equal("W1", context.GetProperty("associateOID").GetString());
        Assert.Equal("WA1", context.GetProperty("workAssignment").GetProperty("itemID").GetString());

        var transform = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("worker").GetProperty("workAssignment");
        Assert.Equal("WA1", transform.GetProperty("itemID").GetString());
        Assert.Equal("2026-06-30", transform.GetProperty("terminationDate").GetString());
        Assert.Equal("RIF", transform.GetProperty("terminationReasonCode").GetProperty("codeValue").GetString());
    }

    [Fact]
    public void TerminateAssignmentCommandBuildOmitsTheReasonCodeWhenNotSupplied()
    {
        var envelope = TerminateAssignmentCommand.Build("W1", "WA1", new DateOnly(2026, 6, 30));
        var transform = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("worker").GetProperty("workAssignment");
        Assert.False(transform.TryGetProperty("terminationReasonCode", out _));
    }

    [Theory]
    [InlineData(null, "WA1")]
    [InlineData("", "WA1")]
    [InlineData("W1", null)]
    [InlineData("W1", "")]
    public void TerminateAssignmentCommandBuildRejectsAMissingIdentityLocally(string? associateOid, string? workAssignmentItemId)
        => Assert.Throws<ArgumentException>(() => TerminateAssignmentCommand.Build(associateOid!, workAssignmentItemId!, new DateOnly(2026, 6, 30)));

    [Fact]
    public async Task SubmitTerminateAssignmentAsyncWiresTheBuiltEnvelopeThroughSubmitEventAsync()
    {
        JsonElement? sentBody = null;
        using var http = new HttpClient(new Handler(r =>
        {
            sentBody = JsonDocument.Parse(r.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.TerminateAssignment]);

        using var result = await client.SubmitTerminateAssignmentAsync("W1", "WA1", new DateOnly(2026, 6, 30));

        Assert.NotNull(sentBody);
        Assert.Equal("WA1", sentBody!.Value.GetProperty("events")[0].GetProperty("data").GetProperty("eventContext").GetProperty("worker").GetProperty("workAssignment").GetProperty("itemID").GetString());
    }

    [Fact]
    public void ChangePayDataInputCommandBuildProducesTheExactDocumentedEnvelopeShape()
    {
        using var payDataInput = JsonDocument.Parse("""{"earningsCode":{"codeValue":"REG"},"amount":1234.56}""");

        var envelope = ChangePayDataInputCommand.Build("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: true);

        var context = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("eventContext");
        Assert.Equal("W1", context.GetProperty("worker").GetProperty("associateOID").GetString());
        Assert.Equal("PG1", context.GetProperty("payGroupID").GetString());
        var transform = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("payDataInput");
        Assert.Equal("REG", transform.GetProperty("earningsCode").GetProperty("codeValue").GetString());
    }

    [Fact]
    public void ChangePayDataInputCommandBuildPreservesExactDecimalPrecisionRatherThanRoundTrippingThroughADouble()
    {
        // 1234.5 and 1234.50 are numerically equal as a double, but this command must never reparse payDataInput
        // as a number - the exact textual representation the caller supplied must survive untouched.
        using var payDataInput = JsonDocument.Parse("""{"amount":1234.500}""");
        var envelope = ChangePayDataInputCommand.Build("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: true);
        var transform = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("payDataInput");
        Assert.Equal("1234.500", transform.GetProperty("amount").GetRawText());
    }

    [Fact]
    public void ChangePayDataInputCommandBuildPreservesExplicitNullFieldsInsidePayDataInput()
    {
        using var payDataInput = JsonDocument.Parse("""{"amount":100,"overrideReasonCode":null}""");
        var envelope = ChangePayDataInputCommand.Build("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: true);
        var transform = envelope.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("payDataInput");
        Assert.True(transform.TryGetProperty("overrideReasonCode", out var reason));
        Assert.Equal(JsonValueKind.Null, reason.ValueKind);
    }

    [Fact]
    public void ChangePayDataInputCommandBuildRequiresExplicitPayrollWriteOptIn()
    {
        using var payDataInput = JsonDocument.Parse("""{"amount":100}""");
        Assert.Throws<InvalidOperationException>(() => ChangePayDataInputCommand.Build("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: false));
    }

    [Theory]
    [InlineData(null, "PG1")]
    [InlineData("", "PG1")]
    [InlineData("W1", null)]
    [InlineData("W1", "")]
    public void ChangePayDataInputCommandBuildRejectsAMissingIdentityLocally(string? associateOid, string? payGroupId)
    {
        using var payDataInput = JsonDocument.Parse("""{"amount":100}""");
        Assert.Throws<ArgumentException>(() => ChangePayDataInputCommand.Build(associateOid!, payGroupId!, payDataInput.RootElement, explicitPayrollWriteOptIn: true));
    }

    [Fact]
    public void ChangePayDataInputCommandBuildRejectsNonObjectPayDataInput()
    {
        using var payDataInput = JsonDocument.Parse("123");
        Assert.Throws<ArgumentException>(() => ChangePayDataInputCommand.Build("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: true));
    }

    [Fact]
    public async Task SubmitChangePayDataInputAsyncWiresTheBuiltEnvelopeThroughSubmitEventAsync()
    {
        JsonElement? sentBody = null;
        using var http = new HttpClient(new Handler(r =>
        {
            sentBody = JsonDocument.Parse(r.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.ChangePayDataInput]);
        using var payDataInput = JsonDocument.Parse("""{"amount":100}""");

        using var result = await client.SubmitChangePayDataInputAsync("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: true);

        Assert.NotNull(sentBody);
        Assert.Equal("PG1", sentBody!.Value.GetProperty("events")[0].GetProperty("data").GetProperty("eventContext").GetProperty("payGroupID").GetString());
    }

    [Fact]
    public async Task LocalValidationFailuresNeverReachTheNetwork()
    {
        using var http = new HttpClient(new Handler(_ => throw new InvalidOperationException("The transport must never be invoked for a locally-rejected command.")));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"),
            [AdpOperation.ChangeManager, AdpOperation.ModifyDepartmentAllocations, AdpOperation.TerminateAssignment, AdpOperation.ChangePayDataInput]);

        await Assert.ThrowsAsync<ArgumentException>(() => client.SubmitChangeManagerAsync("", new ManagerRelationship { WorkerId = new Identifier { Value = "M1" } }));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SubmitModifyDepartmentAllocationsAsync("W1", Array.Empty<OrganizationDepartment>()));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SubmitTerminateAssignmentAsync("W1", "", new DateOnly(2026, 6, 30)));
        using var payDataInput = JsonDocument.Parse("""{"amount":100}""");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SubmitChangePayDataInputAsync("W1", "PG1", payDataInput.RootElement, explicitPayrollWriteOptIn: false));
    }

    [Fact]
    public async Task SubmitChangeManagerAsyncWiresTheBuiltEnvelopeThroughSubmitEventAsync()
    {
        JsonElement? sentBody = null;
        using var http = new HttpClient(new Handler(r =>
        {
            sentBody = JsonDocument.Parse(r.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.ChangeManager]);

        using var result = await client.SubmitChangeManagerAsync("W1", new ManagerRelationship { WorkerId = new Identifier { Value = "M1" } });

        Assert.NotNull(sentBody);
        Assert.Equal("W1", sentBody!.Value.GetProperty("events")[0].GetProperty("data").GetProperty("eventContext").GetProperty("worker").GetProperty("associateOID").GetString());
    }

    [Fact]
    public async Task SubmitModifyDepartmentAllocationsAsyncWiresTheBuiltEnvelopeThroughSubmitEventAsync()
    {
        JsonElement? sentBody = null;
        using var http = new HttpClient(new Handler(r =>
        {
            sentBody = JsonDocument.Parse(r.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }));
        var client = new AdpOperationsClient(new(http, new Token()), new("https://api.test"), [AdpOperation.ModifyDepartmentAllocations]);

        using var result = await client.SubmitModifyDepartmentAllocationsAsync("W1", [new OrganizationDepartment { ItemId = "D1" }]);

        Assert.NotNull(sentBody);
        var unit = sentBody!.Value.GetProperty("events")[0].GetProperty("data").GetProperty("transform").GetProperty("worker").GetProperty("workAssignment").GetProperty("assignedOrganizationalUnits")[0];
        Assert.Equal("D1", unit.GetProperty("itemID").GetString());
    }
}

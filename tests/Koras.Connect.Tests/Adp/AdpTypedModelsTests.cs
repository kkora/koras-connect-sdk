using System.Text.Json;
using Koras.Connect.Adp.Events;
using Koras.Connect.Adp.Workers;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-013 (typed organization/manager DTOs) and ADP-019 (typed response models for event notifications,
/// time-off and spending-account data): golden round-trip tests proving unknown fields survive and null/missing
/// collections never throw, all deserialized from the existing documented response shapes without any new endpoint.</summary>
public sealed class AdpTypedModelsTests
{
    [Fact]
    public void OrganizationDepartmentsResponseRoundTripsUnknownFieldsAndTolerantlyHandlesAMissingCollection()
    {
        var withData = JsonSerializer.Deserialize<OrganizationDepartmentsResponse>("""
            {"organizationDepartments":[{"itemID":"D1","nameCode":{"codeValue":"ENG","shortName":"Engineering"},"futureField":"x"}],"futureTopLevel":1}
            """)!;
        Assert.Single(withData.Departments);
        Assert.Equal("ENG", withData.Departments[0].NameCode!.Code);
        Assert.True(withData.Departments[0].AdditionalData!.ContainsKey("futureField"));
        Assert.True(withData.AdditionalData!.ContainsKey("futureTopLevel"));
        var roundTripped = JsonSerializer.Serialize(withData);
        Assert.Contains("futureField", roundTripped);
        Assert.Contains("futureTopLevel", roundTripped);

        var missingCollection = JsonSerializer.Deserialize<OrganizationDepartmentsResponse>("{}")!;
        Assert.Empty(missingCollection.Departments);
    }

    [Fact]
    public void ManagerRelationshipDeserializesFromAWorkAssignmentsReportsToEntryAndPreservesUnknownFields()
    {
        var assignment = JsonSerializer.Deserialize<WorkAssignment>("""
            {"itemID":"WA1","reportsTo":[{"workerID":{"idValue":"M1"},"reportsToWorkerName":"Mgr One","futureField":true}]}
            """)!;
        var relationships = assignment.GetManagerRelationships();
        Assert.Single(relationships);
        Assert.Equal("M1", relationships[0].WorkerId!.Value);
        Assert.Equal("Mgr One", relationships[0].ReportsToWorkerName);
        Assert.True(relationships[0].AdditionalData!.ContainsKey("futureField"));
    }

    [Fact]
    public void ManagerRelationshipsAreEmptyWhenReportsToIsAbsentRatherThanThrowing()
    {
        var assignment = JsonSerializer.Deserialize<WorkAssignment>("""{"itemID":"WA1"}""")!;
        Assert.Empty(assignment.GetManagerRelationships());
    }

    [Fact]
    public void NotificationEnvelopeFromWebhookDeliveryRoundTripsTheOriginalPayload()
    {
        using var envelope = JsonDocument.Parse("""{"meta":{"orgID":"org-1","messageId":"msg-1"},"data":{"transform":{"future":true}}}""");
        var typed = NotificationEnvelope.FromWebhookDelivery(envelope.RootElement);
        Assert.Equal("msg-1", typed.MessageId);
        Assert.True(typed.Payload.GetProperty("data").GetProperty("transform").GetProperty("future").GetBoolean());
    }

    [Fact]
    public void NotificationEnvelopeFromPolledMessageUsesTheHeaderSourcedIdNotAnUnauthenticatedPayloadField()
    {
        using var payload = JsonDocument.Parse("""{"events":[{"data":{}}]}""");
        var typed = NotificationEnvelope.FromPolledMessage("msg-from-header", payload.RootElement);
        Assert.Equal("msg-from-header", typed.MessageId);
    }

    [Fact]
    public void TimeOffBalancesResponseRoundTripsUnknownFieldsAndTolerantlyHandlesMissingCollections()
    {
        var withData = JsonSerializer.Deserialize<TimeOffBalancesResponse>("""
            {"workers":[{"associateOID":"w1","timeOffBalances":[{"accrualCode":{"codeValue":"VAC"},"balanceAmount":{"quantityValue":12.5,"unitCode":{"codeValue":"DAYS"}},"futureField":"x"}]}]}
            """)!;
        Assert.Single(withData.Workers);
        Assert.Equal(12.5, withData.Workers[0].Balances[0].BalanceAmount!.Value);
        Assert.True(withData.Workers[0].Balances[0].AdditionalData!.ContainsKey("futureField"));

        var missing = JsonSerializer.Deserialize<TimeOffBalancesResponse>("{}")!;
        Assert.Empty(missing.Workers);
    }

    [Fact]
    public void TimeOffRequestsResponseRoundTripsUnknownFieldsAndTolerantlyHandlesMissingCollections()
    {
        var withData = JsonSerializer.Deserialize<TimeOffRequestsResponse>("""
            {"timeOffRequests":[{"itemID":"R1","statusCode":{"codeValue":"APPROVED"},"futureField":1}]}
            """)!;
        Assert.Single(withData.Requests);
        Assert.Equal("APPROVED", withData.Requests[0].StatusCode!.Code);
        Assert.True(withData.Requests[0].AdditionalData!.ContainsKey("futureField"));
        Assert.Empty(withData.Requests[0].TimeOffPeriods);

        var missing = JsonSerializer.Deserialize<TimeOffRequestsResponse>("{}")!;
        Assert.Empty(missing.Requests);
    }

    [Fact]
    public void SpendingAccountEnrollmentsResponseRoundTripsUnknownFieldsAndPreservesVariableCoverageShape()
    {
        var withData = JsonSerializer.Deserialize<SpendingAccountEnrollmentsResponse>("""
            {"spendingAccountEnrollments":[{"itemID":"E1","accountBasedCoverage":{"planName":"FSA","anything":[1,2,3]},"futureField":"x"}]}
            """)!;
        Assert.Single(withData.Enrollments);
        Assert.Equal("FSA", withData.Enrollments[0].AccountBasedCoverage!.Value.GetProperty("planName").GetString());
        Assert.True(withData.Enrollments[0].AdditionalData!.ContainsKey("futureField"));

        var missing = JsonSerializer.Deserialize<SpendingAccountEnrollmentsResponse>("{}")!;
        Assert.Empty(missing.Enrollments);
    }
}

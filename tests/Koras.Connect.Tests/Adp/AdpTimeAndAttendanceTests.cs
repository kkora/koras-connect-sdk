using System.Text.Json;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-16: the explicit attendance/timecard scope statement required by this feature's own acceptance
/// criteria, plus the date/unit/timezone edge cases from its unit-test-requirement list that were not previously
/// exercised for the existing typed time-off models.</summary>
public sealed class AdpTimeAndAttendanceTests
{
    [Fact]
    public void CapabilityStatementExplicitlyDeclaresAttendanceAndTimecardUnimplemented()
    {
        Assert.True(AdpTimeAndAttendanceCapabilityStatement.TimeOffReadsImplemented);
        Assert.False(AdpTimeAndAttendanceCapabilityStatement.AttendanceOperationsImplemented);
        Assert.False(AdpTimeAndAttendanceCapabilityStatement.TimecardOperationsImplemented);
        Assert.Contains("NOT implemented", AdpTimeAndAttendanceCapabilityStatement.Statement);
        Assert.Contains("ADP-016", AdpTimeAndAttendanceCapabilityStatement.Statement);
    }

    [Theory]
    [InlineData("2026-03-10T08:00:00Z")]
    [InlineData("2026-03-10T08:00:00-05:00")]
    [InlineData("2026-03-10T08:00:00+09:30")]
    public void TimeOffPeriodTimestampsWithVariousTimezoneOffsetsSurviveRoundTripExactly(string timestamp)
    {
        var json = "{\"timeOffRequests\":[{\"itemID\":\"R1\",\"timeOffPeriods\":[{\"startDateTime\":\"" + timestamp + "\"}]}]}";
        var requests = JsonSerializer.Deserialize<TimeOffRequestsResponse>(json)!;
        // This SDK does not reinterpret or normalize period timestamps - the raw string must survive untouched,
        // since this package does not claim to know which of ADP's time zone conventions a given tenant uses.
        Assert.Equal(timestamp, requests.Requests[0].TimeOffPeriods[0].GetProperty("startDateTime").GetString());
    }

    [Theory]
    [InlineData("DAYS")]
    [InlineData("HOURS")]
    public void BalanceAmountUnitCodeVariationsAreBothPreservedRatherThanAssumingADefaultUnit(string unit)
    {
        var json = "{\"workers\":[{\"associateOID\":\"w1\",\"timeOffBalances\":[{\"accrualCode\":{\"codeValue\":\"VAC\"},"
            + "\"balanceAmount\":{\"quantityValue\":4,\"unitCode\":{\"codeValue\":\"" + unit + "\"}}}]}]}";
        var balances = JsonSerializer.Deserialize<TimeOffBalancesResponse>(json)!;
        Assert.Equal(unit, balances.Workers[0].Balances[0].BalanceAmount!.UnitCode!.Code);
    }

    [Fact]
    public void MissingUnitCodeOnABalanceAmountIsNullRatherThanAFabricatedDefault()
    {
        var balances = JsonSerializer.Deserialize<TimeOffBalancesResponse>("""
            {"workers":[{"associateOID":"w1","timeOffBalances":[{"balanceAmount":{"quantityValue":4}}]}]}
            """)!;
        Assert.Null(balances.Workers[0].Balances[0].BalanceAmount!.UnitCode);
    }

    [Fact]
    public void AZeroAndANegativeBalanceAmountBothRoundTripExactly()
    {
        var balances = JsonSerializer.Deserialize<TimeOffBalancesResponse>("""
            {"workers":[{"associateOID":"w1","timeOffBalances":[
                {"accrualCode":{"codeValue":"VAC"},"balanceAmount":{"quantityValue":0}},
                {"accrualCode":{"codeValue":"SICK"},"balanceAmount":{"quantityValue":-2.5}}
            ]}]}
            """)!;
        Assert.Equal(0, balances.Workers[0].Balances[0].BalanceAmount!.Value);
        Assert.Equal(-2.5, balances.Workers[0].Balances[1].BalanceAmount!.Value);
    }

    [Fact]
    public void AWorkerAbsentFromTheResponseIsSimplyNotInTheCollectionRatherThanThrowing()
    {
        var balances = JsonSerializer.Deserialize<TimeOffBalancesResponse>("""{"workers":[]}""")!;
        Assert.Empty(balances.Workers);
    }
}

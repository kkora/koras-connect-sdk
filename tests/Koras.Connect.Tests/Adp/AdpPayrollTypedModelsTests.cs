using System.Text.Json;
using Koras.Connect.Adp.Workers;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-019: best-effort typed response models for the two remaining GET-only operations that previously
/// had no typed wrapper, <see cref="Koras.Connect.Adp.AdpOperation.PayrollInstructions"/> and
/// <see cref="Koras.Connect.Adp.AdpOperation.PayDistributions"/>. Golden round-trip tests proving unknown fields
/// survive and null/missing collections never throw, matching the pattern already used for every other typed model
/// in this package.</summary>
public sealed class AdpPayrollTypedModelsTests
{
    [Fact]
    public void PayrollInstructionsResponseRoundTripsUnknownFieldsAndTolerantlyHandlesAMissingCollection()
    {
        var withData = JsonSerializer.Deserialize<PayrollInstructionsResponse>("""
            {"payrollInstructions":[{"itemID":"PI1","effectiveDate":"2026-01-01","accountDetail":{"anything":[1,2]},"futureField":"x"}],"futureTopLevel":1}
            """)!;
        Assert.Single(withData.Instructions);
        Assert.Equal("PI1", withData.Instructions[0].ItemId);
        Assert.Equal("2026-01-01", withData.Instructions[0].EffectiveDate);
        Assert.Equal(JsonValueKind.Array, withData.Instructions[0].AccountDetail!.Value.GetProperty("anything").ValueKind);
        Assert.True(withData.Instructions[0].AdditionalData!.ContainsKey("futureField"));
        Assert.True(withData.AdditionalData!.ContainsKey("futureTopLevel"));

        var roundTripped = JsonSerializer.Serialize(withData);
        Assert.Contains("futureField", roundTripped);
        Assert.Contains("futureTopLevel", roundTripped);

        var missing = JsonSerializer.Deserialize<PayrollInstructionsResponse>("{}")!;
        Assert.Empty(missing.Instructions);
    }

    [Fact]
    public void PayrollInstructionHandlesNullAndMissingFieldsWithoutThrowing()
    {
        var instruction = JsonSerializer.Deserialize<PayrollInstruction>("""{"itemID":null,"effectiveDate":null}""")!;
        Assert.Null(instruction.ItemId);
        Assert.Null(instruction.EffectiveDate);
        Assert.Null(instruction.AccountDetail);
    }

    [Fact]
    public void PayDistributionsResponseRoundTripsUnknownFieldsAndTolerantlyHandlesAMissingCollection()
    {
        var withData = JsonSerializer.Deserialize<PayDistributionsResponse>("""
            {"payDistributions":[{"itemID":"PD1","distributionType":{"codeValue":"DIRECT_DEPOSIT","shortName":"Direct Deposit"},"futureField":"x"}],"futureTopLevel":1}
            """)!;
        Assert.Single(withData.Distributions);
        Assert.Equal("PD1", withData.Distributions[0].ItemId);
        Assert.Equal("DIRECT_DEPOSIT", withData.Distributions[0].DistributionType!.Code);
        Assert.True(withData.Distributions[0].AdditionalData!.ContainsKey("futureField"));
        Assert.True(withData.AdditionalData!.ContainsKey("futureTopLevel"));

        var roundTripped = JsonSerializer.Serialize(withData);
        Assert.Contains("futureField", roundTripped);

        var missing = JsonSerializer.Deserialize<PayDistributionsResponse>("{}")!;
        Assert.Empty(missing.Distributions);
    }

    [Fact]
    public void PayDistributionHandlesNullAndMissingFieldsWithoutThrowing()
    {
        var distribution = JsonSerializer.Deserialize<PayDistribution>("""{"itemID":"PD2"}""")!;
        Assert.Equal("PD2", distribution.ItemId);
        Assert.Null(distribution.DistributionType);
        Assert.Null(distribution.AccountDetail);
    }
}

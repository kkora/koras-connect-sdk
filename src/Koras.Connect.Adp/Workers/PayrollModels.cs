using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

// ADP-019 scaffolding only, following the same caveat posture as src/Koras.Connect.Sap.SuccessFactors/FoundationObjects.cs:
// the field names below are a best-effort hypothesis based on the "itemID" + "codeValue" conventions already used
// and round-trip-tested elsewhere in this package (Worker.cs, OrganizationDepartment.cs, TimeOff.cs), NOT an
// independently verified official schema for the AdpOperation.PayrollInstructions / PayDistributions GET responses.
// Treat these as a best-effort typed wrapper over the existing raw operation, not a confirmed current tenant
// contract - confirm the actual field set via GetMetadataAsync()/tenant documentation before relying on it in
// production. No bank account, routing number or other distribution-account identifying detail is exposed as a
// typed property; that content is deliberately retained only inside the raw AdditionalData/AccountDetail bag (the
// same posture already used for SpendingAccountEnrollment.AccountBasedCoverage) so this file never invents a
// sensitive field it has not verified.

/// <summary>Best-effort typed view of one entry in the existing <c>GET /payroll/v1/workers/{worker}/payroll-instructions</c>
/// response (<see cref="AdpOperation.PayrollInstructions"/>). No new endpoint is introduced. See the remarks at the
/// top of this file - the field set is not independently schema-verified.</summary>
public sealed class PayrollInstruction
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("effectiveDate")] public string? EffectiveDate { get; set; }

    /// <summary>Raw account/payment-method detail, deliberately left untyped - see the remarks at the top of this file.</summary>
    [JsonPropertyName("accountDetail")] public JsonElement? AccountDetail { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Best-effort typed envelope for the full payroll-instructions response. See the remarks at the top of
/// this file - the field set is not independently schema-verified.</summary>
public sealed class PayrollInstructionsResponse
{
    [JsonPropertyName("payrollInstructions")] public List<PayrollInstruction> Instructions { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Best-effort typed view of one entry in the existing <c>GET /payroll/v2/workers/{worker}/pay-distributions</c>
/// response (<see cref="AdpOperation.PayDistributions"/>). No new endpoint is introduced. See the remarks at the
/// top of this file - the field set is not independently schema-verified.</summary>
public sealed class PayDistribution
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("distributionType")] public CodeValue? DistributionType { get; set; }

    /// <summary>Raw account/amount detail, deliberately left untyped - see the remarks at the top of this file.</summary>
    [JsonPropertyName("accountDetail")] public JsonElement? AccountDetail { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Best-effort typed envelope for the full pay-distributions response. See the remarks at the top of this
/// file - the field set is not independently schema-verified.</summary>
public sealed class PayDistributionsResponse
{
    [JsonPropertyName("payDistributions")] public List<PayDistribution> Distributions { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

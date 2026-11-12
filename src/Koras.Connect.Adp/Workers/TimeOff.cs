using System.Text.Json;
using System.Text.Json.Serialization;
namespace Koras.Connect.Adp.Workers;

/// <summary>Typed view of one entry in the existing <c>GET /time/v2/workers/{worker}/time-off-details/time-off-balances</c>
/// response (<see cref="AdpOperation.TimeOffBalances"/>). No new endpoint is introduced. Unknown vendor fields are
/// preserved, matching the pattern used by <see cref="Worker"/>.</summary>
public sealed class TimeOffBalanceEntry
{
    [JsonPropertyName("accrualCode")] public CodeValue? AccrualCode { get; set; }
    [JsonPropertyName("balanceAmount")] public QuantityValue? BalanceAmount { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed class TimeOffBalanceWorker
{
    [JsonPropertyName("associateOID")] public string? AssociateOid { get; set; }
    [JsonPropertyName("timeOffBalances")] public List<TimeOffBalanceEntry> Balances { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed class TimeOffBalancesResponse
{
    [JsonPropertyName("workers")] public List<TimeOffBalanceWorker> Workers { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Typed view of one entry in the existing <c>GET /time/v2/workers/{worker}/time-off-details/time-off-requests</c>
/// response (<see cref="AdpOperation.TimeOffRequests"/>). No new endpoint is introduced.</summary>
public sealed class TimeOffRequest
{
    [JsonPropertyName("itemID")] public string? ItemId { get; set; }
    [JsonPropertyName("statusCode")] public CodeValue? StatusCode { get; set; }
    [JsonPropertyName("timeOffPeriods")] public List<JsonElement> TimeOffPeriods { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed class TimeOffRequestsResponse
{
    [JsonPropertyName("timeOffRequests")] public List<TimeOffRequest> Requests { get; set; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

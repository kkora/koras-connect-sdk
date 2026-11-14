using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Dataverse;

/// <summary>Thin typed convenience wrapper over the generic entity-set mechanism (DVT-004/005) for the Dynamics
/// 365 Sales <c>account</c> table (DVT-018) - there is no separate "Dynamics 365 API"; first-party app tables are
/// reached identically to any custom table. Only <c>Name</c> is hardcoded, matching this feature's own documented
/// illustrative example; every other field remains in <see cref="AdditionalData"/> until DVT-009's live metadata
/// discovery independently confirms it, per this feature's explicit "no invented field" requirement.</summary>
public sealed class DataverseAccount
{
    [JsonPropertyName("accountid")] public Guid? AccountId { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Thin typed convenience wrapper for the <c>contact</c> table (DVT-018). No business field beyond the
/// primary key is hardcoded - the documented example set for this feature covers only <c>account</c>'s <c>Name</c>
/// field; a <c>contact</c>-specific field (e.g. a full-name column) is not independently confirmed this session.</summary>
public sealed class DataverseContact
{
    [JsonPropertyName("contactid")] public Guid? ContactId { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>Thin typed convenience wrapper for the <c>opportunity</c> table (DVT-018). No business field beyond
/// the primary key is hardcoded, for the same reason as <see cref="DataverseContact"/>.</summary>
public sealed class DataverseOpportunity
{
    [JsonPropertyName("opportunityid")] public Guid? OpportunityId { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class DataverseClient
{
    /// <summary>Enumerates the <c>account</c> table - a pure pass-through over <see cref="EnumerateAsync"/> with
    /// the entity set and target type fixed (DVT-018). Composes the identical request a hand-written caller
    /// invoking <c>EnumerateAsync("accounts", query, maxPageSize)</c> would.</summary>
    public async IAsyncEnumerable<DataverseAccount> EnumerateAccountsAsync(DataverseQuery? query = null, int? maxPageSize = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in EnumerateAsync("accounts", query, maxPageSize, cancellationToken: cancellationToken))
            yield return item.Deserialize<DataverseAccount>() ?? throw new JsonException("Null account row.");
    }

    /// <summary>Enumerates the <c>contact</c> table - a pure pass-through over <see cref="EnumerateAsync"/> (DVT-018).</summary>
    public async IAsyncEnumerable<DataverseContact> EnumerateContactsAsync(DataverseQuery? query = null, int? maxPageSize = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in EnumerateAsync("contacts", query, maxPageSize, cancellationToken: cancellationToken))
            yield return item.Deserialize<DataverseContact>() ?? throw new JsonException("Null contact row.");
    }

    /// <summary>Enumerates the <c>opportunity</c> table - a pure pass-through over <see cref="EnumerateAsync"/> (DVT-018).</summary>
    public async IAsyncEnumerable<DataverseOpportunity> EnumerateOpportunitiesAsync(DataverseQuery? query = null, int? maxPageSize = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in EnumerateAsync("opportunities", query, maxPageSize, cancellationToken: cancellationToken))
            yield return item.Deserialize<DataverseOpportunity>() ?? throw new JsonException("Null opportunity row.");
    }
}

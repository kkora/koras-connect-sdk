using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Tenant-level organization metadata (MGR-008). Every field besides <see cref="Id"/> is nullable - a
/// caller holding only <c>User.Read</c> sees a documented, restricted response where every field beyond
/// <c>id</c>/<c>displayName</c>/<c>verifiedDomains</c> comes back <c>null</c> rather than omitted, and this is
/// the intended least-privilege shape, not a contract violation. <see cref="VerifiedDomains"/> simplifies the
/// actual <c>verifiedDomains</c> array (each entry also carries <c>capability</c>/<c>isDefault</c>/<c>type</c>)
/// down to just the domain names, matching this feature's own proposed contract.</summary>
public sealed record GraphOrganization(string Id, string? DisplayName, IReadOnlyList<string>? VerifiedDomains);

public sealed partial class GraphClient
{
    /// <summary>Reads the tenant's organization record via <c>GET /organization</c> (MGR-008). Graph models
    /// <c>organization</c> as a single-element collection even though exactly one applies to the current tenant,
    /// so the response is parsed as a <c>value</c> array and only its first element is used.</summary>
    public async Task<GraphOrganization> GetOrganizationAsync(CancellationToken cancellationToken = default)
    {
        var address = new Uri(_root, "organization");
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
                throw new ContractException("Organization response did not contain a value array.", new ErrorDetails("graph_organization_contract"));
            var organization = value[0];
            var id = RequireString(organization, "id");
            var displayName = organization.TryGetProperty("displayName", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
            IReadOnlyList<string>? verifiedDomains = null;
            if (organization.TryGetProperty("verifiedDomains", out var domains) && domains.ValueKind == JsonValueKind.Array)
                verifiedDomains = domains.EnumerateArray()
                    .Where(d => d.ValueKind == JsonValueKind.Object && d.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
                    .Select(d => d.GetProperty("name").GetString()!).ToArray();
            return new GraphOrganization(id, displayName, verifiedDomains);
        }
    }
}

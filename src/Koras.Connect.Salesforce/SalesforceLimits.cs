using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>One org-level API/storage allocation entry (SFD-017), shaped <c>{"Max":N,"Remaining":N}</c>.</summary>
public sealed record SalesforceLimit(int Max, int Remaining);

public sealed partial class SalesforceClient
{
    /// <summary>Reads the org's current API/storage allocations via <c>GET .../limits/</c> (SFD-017). Every key
    /// present in the response is preserved verbatim - this SDK does not maintain its own allowlist of known
    /// limit names, since Salesforce documents roughly 20+ and adds more over time.</summary>
    public async Task<IReadOnlyDictionary<string, SalesforceLimit>> GetLimitsAsync(CancellationToken cancellationToken = default)
    {
        var address = new Uri(_root, "limits/");
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new ContractException("Limits response was not a JSON object.", new ErrorDetails("limits_contract"));
            var limits = new Dictionary<string, SalesforceLimit>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object) continue;
                if (!property.Value.TryGetProperty("Max", out var max) || max.ValueKind != JsonValueKind.Number) continue;
                if (!property.Value.TryGetProperty("Remaining", out var remaining) || remaining.ValueKind != JsonValueKind.Number) continue;
                limits[property.Name] = new SalesforceLimit(max.GetInt32(), remaining.GetInt32());
            }
            return limits;
        }
    }
}

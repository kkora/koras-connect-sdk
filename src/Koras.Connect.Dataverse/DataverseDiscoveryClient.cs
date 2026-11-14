using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>A Dataverse environment the authenticated identity can access, as returned by the Global Discovery
/// Service (DVT-002). Field shapes confirmed against the documented GDS <c>Instance</c> response [D5].</summary>
public sealed record DataverseInstance(string ApiUrl, string Id, string EnvironmentId, string UniqueName,
    string FriendlyName, string Region, string Version, string TenantId);

public interface IDataverseDiscoveryClient
{
    IAsyncEnumerable<DataverseInstance> ListInstancesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Global Discovery Service (GDS) client (DVT-002). The GDS host varies by sovereign cloud - this type
/// never hardcodes the commercial-cloud default silently; callers requiring a sovereign cloud must pass its root
/// explicitly. An empty instance list is a valid outcome (the identity has no accessible environment), not an
/// error. The SDK never auto-selects an instance when more than one is returned - the caller must disambiguate.</summary>
public sealed class DataverseDiscoveryClient : IDataverseDiscoveryClient
{
    public static readonly Uri CommercialGlobalDiscoveryRoot = new("https://globaldisco.crm.dynamics.com/");
    public static readonly Uri GccGlobalDiscoveryRoot = new("https://globaldisco.crm9.dynamics.com/");
    public static readonly Uri GccHighGlobalDiscoveryRoot = new("https://globaldisco.crm.microsoftdynamics.us/");
    public static readonly Uri DodGlobalDiscoveryRoot = new("https://globaldisco.crm.appsplatform.us/");
    public static readonly Uri ChinaGlobalDiscoveryRoot = new("https://globaldisco.crm.dynamics.cn/");

    private readonly IntegrationTransport _transport;
    private readonly Uri _root;

    public DataverseDiscoveryClient(IntegrationTransport transport, Uri? discoveryRoot = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _root = EndpointSafety.ValidateRoot(discoveryRoot ?? CommercialGlobalDiscoveryRoot);
    }

    public async IAsyncEnumerable<DataverseInstance> ListInstancesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(new Uri(_root, "api/discovery/v2.0/Instances"), cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw DataverseApiException.FromTransportFailure(ex); }
        using (document)
        {
            if (!document.RootElement.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                throw new ContractException("Global Discovery Service response did not contain a value array.", new ErrorDetails("gds_contract"));
            foreach (var item in value.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new DataverseInstance(
                    RequireString(item, "ApiUrl"), RequireString(item, "Id"), RequireString(item, "EnvironmentId"),
                    RequireString(item, "UniqueName"), RequireString(item, "FriendlyName"),
                    RequireString(item, "Region"), RequireString(item, "Version"), RequireString(item, "TenantId"));
            }
        }
    }

    private static string RequireString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ContractException($"Global Discovery Service instance missing required field '{property}'.", new ErrorDetails("gds_contract"));
        return value.GetString()!;
    }
}

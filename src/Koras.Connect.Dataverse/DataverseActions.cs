using System.Text.Json;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

public sealed partial class DataverseClient
{
    private const string CrmNamespace = "Microsoft.Dynamics.CRM";

    private static readonly System.Text.RegularExpressions.Regex ActionNamePattern =
        new("^[A-Za-z][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void ValidateActionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !ActionNamePattern.IsMatch(name))
            throw new ArgumentException("Action/function name must be a simple identifier; never dynamically forward an unvalidated caller-supplied name.", nameof(name));
    }

    /// <summary>Invokes an unbound action (DVT-015), for example the documented <c>Merge</c> example: <c>POST
    /// {environment}/{ActionName}</c>. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c> -
    /// an action is server-side business logic and is never assumed to be side-effect-free.</summary>
    public async Task<JsonDocument> InvokeUnboundActionAsync(string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the action's effect and the caller's privilege to invoke it.");
        ValidateActionName(actionName);
        var address = new Uri(_root, actionName);
        ValidateRequestUri(address);
        try { return await _transport.SendJsonAsync(HttpMethod.Post, address, parameters, RequiredHeaders, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Invokes an action bound to a specific row (DVT-015): <c>POST {entityset}(id)/Microsoft.Dynamics.CRM.{ActionName}</c>
    /// - always fully qualified with the CRM namespace, per the documented requirement. Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<JsonDocument> InvokeBoundActionAsync(string entitySet, Guid id, string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the action's effect and the caller's privilege to invoke it.");
        ValidateEntitySet(entitySet);
        ValidateActionName(actionName);
        var address = new Uri(_root, $"{entitySet}({id:D})/{CrmNamespace}.{actionName}");
        ValidateRequestUri(address);
        try { return await _transport.SendJsonAsync(HttpMethod.Post, address, parameters, RequiredHeaders, cancellationToken); }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Invokes a function via <c>GET</c> using parameter-alias syntax (<c>Fn(Param=@p1)?@p1=...</c>) to
    /// stay under the 260-character URL-segment limit (DVT-003) (DVT-015). Every value in <paramref name="parameters"/>
    /// is forwarded as the literal OData query value exactly as supplied - a string parameter must already include
    /// its own surrounding quotes (for example <c>"'Active'"</c>), matching the raw OData literal syntax for its
    /// declared type; the SDK does not infer or add quoting.</summary>
    public async Task<JsonDocument> InvokeFunctionAsync(string functionName, IReadOnlyDictionary<string, string>? parameters = null, CancellationToken cancellationToken = default)
    {
        ValidateActionName(functionName);
        parameters ??= new Dictionary<string, string>();
        var aliasAssignments = new List<string>();
        var queryParts = new List<string>();
        var index = 0;
        foreach (var (name, value) in parameters)
        {
            ValidateActionName(name);
            var alias = $"@p{++index}";
            aliasAssignments.Add($"{name}={alias}");
            queryParts.Add($"{alias}={Uri.EscapeDataString(value)}");
        }
        var relative = $"{functionName}({string.Join(",", aliasAssignments)})" + (queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "");
        var address = new Uri(_root, relative);
        ValidateRequestUri(address);
        try { return await _transport.ReadJsonAsync(address, RequiredHeaders, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }
}

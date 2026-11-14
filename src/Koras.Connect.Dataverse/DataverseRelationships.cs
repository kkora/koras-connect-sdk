using System.Text.Json;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

public sealed partial class DataverseClient
{
    private static readonly System.Text.RegularExpressions.Regex NavigationPropertyPattern =
        new("^[A-Za-z][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void ValidateNavigationProperty(string navigationProperty)
    {
        if (string.IsNullOrWhiteSpace(navigationProperty) || !NavigationPropertyPattern.IsMatch(navigationProperty))
            throw new ArgumentException("Navigation property must be a simple identifier.", nameof(navigationProperty));
    }

    /// <summary>Sets a single-valued lookup on an existing row via <c>"{navprop}@odata.bind"</c> (DVT-010). To set
    /// a lookup at create time instead, include the same <c>"{navprop}@odata.bind"</c> property directly in the
    /// <c>fields</c> passed to <see cref="CreateRowAsync"/> - no separate API is needed for that case.</summary>
    public async Task BindLookupAsync(string entitySet, Guid id, string navigationProperty, string targetEntitySet, Guid targetId,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(targetEntitySet);
        ValidateNavigationProperty(navigationProperty);
        var address = RowAddress(entitySet, id);
        ValidateRequestUri(address);
        var body = JsonSerializer.SerializeToElement(new Dictionary<string, string> { [$"{navigationProperty}@odata.bind"] = $"/{targetEntitySet}({targetId:D})" });
        try { using (await _transport.SendResponseAsync(HttpMethod.Patch, address, body, RequiredHeaders, cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Clears a single-valued lookup via <c>DELETE {entityset}(id)/{navprop}/$ref</c> (DVT-010).</summary>
    public async Task ClearLookupAsync(string entitySet, Guid id, string navigationProperty, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        ValidateNavigationProperty(navigationProperty);
        var address = new Uri(_root, $"{entitySet}({id:D})/{navigationProperty}/$ref");
        ValidateRequestUri(address);
        try { using (await _transport.SendResponseAsync(HttpMethod.Delete, address, null, RequiredHeaders, cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Adds a member to a collection-valued (1:N/N:N) relationship via
    /// <c>POST {entityset}(id)/{collectionNavProp}/$ref</c> (DVT-010). Requires Append (on <paramref name="entitySet"/>'s
    /// row) and AppendTo (on the target row) privilege, in addition to Write on the primary row - a 4xx denial is a
    /// genuine entitlement outcome the connector classifies, not a silent no-op.</summary>
    public async Task AssociateAsync(string entitySet, Guid id, string collectionNavigationProperty, string targetEntitySet, Guid targetId,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        ValidateEntitySet(targetEntitySet);
        ValidateNavigationProperty(collectionNavigationProperty);
        var address = new Uri(_root, $"{entitySet}({id:D})/{collectionNavigationProperty}/$ref");
        ValidateRequestUri(address);
        var body = JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["@odata.id"] = new Uri(_root, $"{targetEntitySet}({targetId:D})").AbsoluteUri });
        try { using (await _transport.SendResponseAsync(HttpMethod.Post, address, body, RequiredHeaders, cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Removes one member from a collection-valued relationship via
    /// <c>DELETE {entityset}(id)/{collectionNavProp}(targetId)/$ref</c> (DVT-010).</summary>
    public async Task DisassociateAsync(string entitySet, Guid id, string collectionNavigationProperty, Guid targetId,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        ValidateNavigationProperty(collectionNavigationProperty);
        var address = new Uri(_root, $"{entitySet}({id:D})/{collectionNavigationProperty}({targetId:D})/$ref");
        ValidateRequestUri(address);
        try { using (await _transport.SendResponseAsync(HttpMethod.Delete, address, null, RequiredHeaders, cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }
}

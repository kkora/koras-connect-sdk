using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A SharePoint list item (MGR-012). <see cref="Fields"/> is the nested <c>fields</c> sub-resource -
/// every list has caller/tenant-defined columns, so this is never a fixed, hardcoded shape.</summary>
public sealed record GraphListItem([property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fields")] IReadOnlyDictionary<string, JsonElement> Fields);

public sealed partial class GraphClient
{
    /// <summary>Enumerates a site's lists via <c>GET /sites/{siteId}/lists</c> (MGR-012/019). Lists are reached
    /// only through their owning site - there is no top-level <c>/lists/{id}</c> resource, so every method in
    /// this feature requires a <paramref name="siteId"/>.</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateListsAsync(string siteId, CancellationToken cancellationToken = default)
    {
        ValidateSiteId(siteId);
        return EnumerateAsync<JsonDocument>($"sites/{Uri.EscapeDataString(siteId)}/lists", cancellationToken: cancellationToken);
    }

    /// <summary>Enumerates a list's items via <c>GET /sites/{siteId}/lists/{listId}/items</c> (MGR-012/019).</summary>
    public IAsyncEnumerable<GraphListItem> EnumerateListItemsAsync(string siteId, string listId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        ValidateSiteId(siteId);
        ValidateListId(listId);
        return EnumerateAsync<GraphListItem>($"sites/{Uri.EscapeDataString(siteId)}/lists/{Uri.EscapeDataString(listId)}/items", query, cancellationToken);
    }

    /// <summary>Creates a list item via <c>POST /sites/{siteId}/lists/{listId}/items</c> (MGR-012). Disabled
    /// unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> CreateListItemAsync(string siteId, string listId, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the list-write privilege and business impact.");
        ValidateSiteId(siteId);
        ValidateListId(listId);
        var address = new Uri(_root, $"sites/{Uri.EscapeDataString(siteId)}/lists/{Uri.EscapeDataString(listId)}/items");
        var payload = JsonSerializer.SerializeToElement(new { fields });
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Updates a list item's fields via <c>PATCH /sites/{siteId}/lists/{listId}/items/{itemId}/fields</c>
    /// (MGR-012). Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdateListItemFieldsAsync(string siteId, string listId, string itemId, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the list-write privilege and business impact.");
        ValidateSiteId(siteId);
        ValidateListId(listId);
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("An item id is required.", nameof(itemId));
        var address = new Uri(_root, $"sites/{Uri.EscapeDataString(siteId)}/lists/{Uri.EscapeDataString(listId)}/items/{Uri.EscapeDataString(itemId)}/fields");
        try { await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static void ValidateSiteId(string siteId)
    {
        if (string.IsNullOrWhiteSpace(siteId)) throw new ArgumentException("A site id is required.", nameof(siteId));
    }

    private static void ValidateListId(string listId)
    {
        if (string.IsNullOrWhiteSpace(listId)) throw new ArgumentException("A list id is required.", nameof(listId));
    }
}

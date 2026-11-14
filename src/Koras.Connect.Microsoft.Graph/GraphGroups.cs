using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Directory group (MGR-005).</summary>
public sealed class GraphGroup
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates <c>GET /groups</c> (MGR-005/019).</summary>
    public IAsyncEnumerable<GraphGroup> EnumerateGroupsAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphGroup>("groups", query, cancellationToken);

    /// <summary>Enumerates a group's members via <c>GET /groups/{groupId}/members</c> (MGR-005/019). Members are
    /// users, per this feature's proposed contract - a non-user member (for example a nested group) still
    /// deserializes, with user-specific fields left absent, since every member shares the same directory-object
    /// id/displayName shape at this level of detail.</summary>
    public IAsyncEnumerable<GraphUser> EnumerateGroupMembersAsync(string groupId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        ValidateDirectoryObjectId(groupId);
        return EnumerateAsync<GraphUser>($"groups/{Uri.EscapeDataString(groupId)}/members", query, cancellationToken);
    }

    /// <summary>Adds a member to a group via <c>POST /groups/{groupId}/members/$ref</c> (MGR-005), posting the
    /// documented <c>{"@odata.id":...}</c> body pointing at the member's <c>directoryObjects</c> resource.
    /// Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>. Not supported for personal
    /// Microsoft accounts (MSA) - tenant/organizational accounts only, per Graph's documented restriction; this
    /// SDK cannot detect the account type locally and relies on Graph's own rejection.</summary>
    public async Task AddGroupMemberAsync(string groupId, string memberId, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the directory-write privilege and business impact.");
        ValidateDirectoryObjectId(groupId);
        ValidateDirectoryObjectId(memberId);
        var address = new Uri(_root, $"groups/{Uri.EscapeDataString(groupId)}/members/$ref");
        using var body = JsonDocument.Parse($$"""{"@odata.id":"{{new Uri(_root, $"directoryObjects/{Uri.EscapeDataString(memberId)}").AbsoluteUri}}"}""");
        try { await _transport.SendJsonAsync(HttpMethod.Post, address, body.RootElement, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Removes a member from a group via <c>DELETE /groups/{groupId}/members/{memberId}/$ref</c> (MGR-005).
    /// Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task RemoveGroupMemberAsync(string groupId, string memberId, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the directory-write privilege and business impact.");
        ValidateDirectoryObjectId(groupId);
        ValidateDirectoryObjectId(memberId);
        var address = new Uri(_root, $"groups/{Uri.EscapeDataString(groupId)}/members/{Uri.EscapeDataString(memberId)}/$ref");
        try { await _transport.SendResponseAsync(HttpMethod.Delete, address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static void ValidateDirectoryObjectId(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A directory object id is required.", nameof(id));
    }
}

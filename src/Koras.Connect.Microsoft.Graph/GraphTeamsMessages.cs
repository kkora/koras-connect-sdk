using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Teams channel or chat message (MGR-016). <see cref="Body"/> is flattened from the nested
/// <c>body.content</c> field, mirroring <see cref="GraphMessage"/>'s pattern for Outlook mail.</summary>
public sealed class GraphChatMessage
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("body")] public GraphMessageBody? BodyDetail { get; set; }
    [JsonIgnore] public string? Body => BodyDetail?.Content;
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates a channel's messages via <c>GET /teams/{teamId}/channels/{channelId}/messages</c>
    /// (MGR-016/019). Works under either Resource-Specific Consent (per-channel app install) or tenant-wide
    /// application permissions - the HTTP call itself is identical either way; which permission tier backs the
    /// token is purely a configuration choice (MGR-002), not something this method branches on.</summary>
    public IAsyncEnumerable<GraphChatMessage> EnumerateChannelMessagesAsync(string teamId, string channelId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        ValidateTeamAndChannel(teamId, channelId);
        return EnumerateAsync<GraphChatMessage>($"teams/{Uri.EscapeDataString(teamId)}/channels/{Uri.EscapeDataString(channelId)}/messages", query, cancellationToken);
    }

    /// <summary>Sends a channel message via <c>POST /teams/{teamId}/channels/{channelId}/messages</c> (MGR-016).
    /// Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> SendChannelMessageAsync(string teamId, string channelId, string body, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the Teams-write privilege and business impact.");
        ValidateTeamAndChannel(teamId, channelId);
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("A message body is required.", nameof(body));
        var address = new Uri(_root, $"teams/{Uri.EscapeDataString(teamId)}/channels/{Uri.EscapeDataString(channelId)}/messages");
        var payload = JsonSerializer.SerializeToElement(new { body = new { content = body } });
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Enumerates a chat's messages via <c>GET /chats/{chatId}/messages</c> (MGR-016/019).</summary>
    public IAsyncEnumerable<GraphChatMessage> EnumerateChatMessagesAsync(string chatId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        ValidateChatId(chatId);
        return EnumerateAsync<GraphChatMessage>($"chats/{Uri.EscapeDataString(chatId)}/messages", query, cancellationToken);
    }

    /// <summary>Sends a chat message via <c>POST /chats/{chatId}/messages</c> (MGR-016). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> SendChatMessageAsync(string chatId, string body, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the Teams-write privilege and business impact.");
        ValidateChatId(chatId);
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("A message body is required.", nameof(body));
        var address = new Uri(_root, $"chats/{Uri.EscapeDataString(chatId)}/messages");
        var payload = JsonSerializer.SerializeToElement(new { body = new { content = body } });
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    private static void ValidateTeamAndChannel(string teamId, string channelId)
    {
        if (string.IsNullOrWhiteSpace(teamId)) throw new ArgumentException("A team id is required.", nameof(teamId));
        if (string.IsNullOrWhiteSpace(channelId)) throw new ArgumentException("A channel id is required.", nameof(channelId));
    }

    private static void ValidateChatId(string chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId)) throw new ArgumentException("A chat id is required.", nameof(chatId));
    }
}

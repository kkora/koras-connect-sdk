using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A message body's content and declared type (MGR-013). Message bodies are documented as HTML-only in
/// the returned shape - no separate plain-text content type is returned alongside.</summary>
public sealed record GraphMessageBody([property: JsonPropertyName("contentType")] string? ContentType, [property: JsonPropertyName("content")] string? Content);

/// <summary>An Outlook mail message (MGR-013).</summary>
public sealed class GraphMessage
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("subject")] public string? Subject { get; set; }
    [JsonPropertyName("body")] public GraphMessageBody? Body { get; set; }
    [JsonIgnore] public string? BodyHtml => Body?.Content;
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>The documented maximum <c>$top</c> for <c>/messages</c> (MGR-013) - a caller-supplied value above
    /// this is clamped down rather than sent as-is and rejected server-side.</summary>
    public const int MaxMessagesTop = 1000;

    private static GraphQuery ClampMessagesTop(GraphQuery? query)
        => query is { Top: > MaxMessagesTop } ? query with { Top = MaxMessagesTop } : query ?? new GraphQuery();

    /// <summary>Enumerates the signed-in user's mail via <c>GET /me/messages</c> (MGR-013/019) - documented
    /// default page size <c>10</c>, far smaller than most other Graph collections, so callers should not assume
    /// a larger implicit batch. <c>/me</c> is delegated-only, matching MGR-004's general rule for every <c>/me</c>
    /// alias; this throws locally, before any HTTP call, unless <see cref="GraphOptions.IsDelegatedContext"/> was
    /// explicitly set.</summary>
    public IAsyncEnumerable<GraphMessage> EnumerateMyMessagesAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/me/messages requires a delegated (signed-in-user) context; application permissions must use EnumerateUserMessagesAsync against /users/{id}/messages instead.");
        return EnumerateAsync<GraphMessage>("me/messages", ClampMessagesTop(query), cancellationToken);
    }

    /// <summary>Enumerates a user's mail via <c>GET /users/{userId}/messages</c> (MGR-013/019) - the app-only
    /// path, since application permissions do not support <c>/me</c>.</summary>
    public IAsyncEnumerable<GraphMessage> EnumerateUserMessagesAsync(string userId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(userId));
        return EnumerateAsync<GraphMessage>($"users/{Uri.EscapeDataString(userId)}/messages", ClampMessagesTop(query), cancellationToken);
    }

    /// <summary>Sends mail via <c>POST /me/sendMail</c> (MGR-013). The documented success response is
    /// <c>202 Accepted</c> with no body - this is the success shape, not a missing confirmation, and
    /// <see cref="IntegrationTransport.SendResponseAsync"/> already treats <c>202</c> as success without
    /// attempting to parse a body. Delegated-only, like every other <c>/me</c> alias; disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task SendMailAsync(string subject, string bodyHtml, IReadOnlyList<string> toRecipients, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the mail-send privilege and business impact.");
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/me/sendMail requires a delegated (signed-in-user) context.");
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("A subject is required.", nameof(subject));
        if (toRecipients is null || toRecipients.Count == 0) throw new ArgumentException("At least one recipient is required.", nameof(toRecipients));
        var payload = JsonSerializer.SerializeToElement(new
        {
            message = new
            {
                subject,
                body = new { contentType = "HTML", content = bodyHtml },
                toRecipients = toRecipients.Select(address => new { emailAddress = new { address } }).ToArray(),
            },
        });
        var address = new Uri(_root, "me/sendMail");
        try { await _transport.SendResponseAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }
}

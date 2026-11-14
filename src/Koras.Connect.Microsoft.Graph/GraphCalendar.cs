using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Graph event date/time: a local wall-clock time plus a separate named time zone (MGR-014). The
/// proposed contract's literal <c>DateTimeOffset?</c> was adapted to this nested shape - Graph's actual wire
/// format carries broken-down local time and an IANA/Windows time zone name, not a UTC offset, and converting
/// that pair to a precise <see cref="DateTimeOffset"/> would require a time zone database dependency this SDK
/// does not take. Callers needing an absolute instant must resolve <see cref="TimeZone"/> themselves.</summary>
public sealed record GraphEventDateTime([property: JsonPropertyName("dateTime")] string? DateTime, [property: JsonPropertyName("timeZone")] string? TimeZone);

/// <summary>An Outlook calendar event (MGR-014).</summary>
public sealed class GraphEvent
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("subject")] public string? Subject { get; set; }
    [JsonPropertyName("start")] public GraphEventDateTime? Start { get; set; }
    [JsonPropertyName("end")] public GraphEventDateTime? End { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates a user's calendar events via <c>GET /users/{userId}/events</c> (MGR-014/019).
    /// Calendar application permissions are fully symmetric with delegated ones (unlike Teams/Planner), so no
    /// <c>/me</c>-only restriction is imposed here - matching the vendor's own documented behavior rather than
    /// an invented one.</summary>
    public IAsyncEnumerable<GraphEvent> EnumerateEventsAsync(string userId, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        return EnumerateAsync<GraphEvent>($"users/{Uri.EscapeDataString(userId)}/events", query, cancellationToken);
    }

    /// <summary>Enumerates the signed-in user's own calendar events via <c>GET /me/events</c> (MGR-014/019) -
    /// found missing by Phase 2 review pass 1 despite being a documented functional requirement alongside
    /// <see cref="EnumerateEventsAsync"/>. Unlike <see cref="EnumerateEventsAsync"/>, <c>/me</c> itself is
    /// delegated-only - application permissions have no signed-in-user context - so this throws locally, before
    /// any HTTP call, unless <see cref="GraphOptions.IsDelegatedContext"/> was explicitly set, matching every
    /// other <c>/me</c>-shortcut method in this connector (MGR-004/009/013/017).</summary>
    public IAsyncEnumerable<GraphEvent> EnumerateMyEventsAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/me/events requires a delegated (signed-in-user) context; application permissions must use EnumerateEventsAsync against /users/{id}/events instead.");
        return EnumerateAsync<GraphEvent>("me/events", query, cancellationToken);
    }

    /// <summary>Creates an event via <c>POST /users/{userId}/events</c> (MGR-014). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task<string> CreateEventAsync(string userId, GraphEvent @event, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the calendar-write privilege and business impact.");
        ValidateUserId(userId);
        ArgumentNullException.ThrowIfNull(@event);
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(userId)}/events");
        var payload = JsonSerializer.SerializeToElement(@event);
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Updates an event via <c>PATCH /users/{userId}/events/{eventId}</c> (MGR-014). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdateEventAsync(string userId, string eventId, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the calendar-write privilege and business impact.");
        ValidateUserId(userId);
        if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("An event id is required.", nameof(eventId));
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(userId)}/events/{Uri.EscapeDataString(eventId)}");
        try { using (await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Deletes an event via <c>DELETE /users/{userId}/events/{eventId}</c> (MGR-014). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task DeleteEventAsync(string userId, string eventId, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the calendar-write privilege and business impact.");
        ValidateUserId(userId);
        if (string.IsNullOrWhiteSpace(eventId)) throw new ArgumentException("An event id is required.", nameof(eventId));
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(userId)}/events/{Uri.EscapeDataString(eventId)}");
        try { using (await _transport.SendResponseAsync(HttpMethod.Delete, address, cancellationToken: cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(userId));
    }
}

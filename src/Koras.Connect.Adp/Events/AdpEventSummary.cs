using System.Text.Json;
namespace Koras.Connect.Adp.Events;

/// <summary>Parsed, safe view over one entry inside an ADP event-notification message's documented
/// <c>events[].data.eventContext</c> shape (ADP-018) &#8212; the same envelope structure used both for polled/webhook
/// event notifications and for the write-event dispatch payloads built by <c>AdpWriteCommands</c>. Extracts only
/// the routing identity fields this package already treats as safe to surface elsewhere (a worker's associate OID,
/// and the event's own item id); never the full business payload, which stays in
/// <see cref="NotificationEnvelope.Payload"/> for the host to interpret against its own authorized tenant
/// metadata. Either field is <c>null</c> when the envelope does not carry it &#8212; never guessed.</summary>
public sealed record AdpEventSummary(string? EventItemId, string? WorkerAssociateOid);

/// <summary>Safe, tolerant parser for an event-notification envelope's <c>events</c> array.</summary>
public static class AdpEventEnvelopeParser
{
    /// <summary>Extracts an <see cref="AdpEventSummary"/> per entry in <paramref name="envelope"/>'s <c>events</c>
    /// array. Returns an empty sequence &#8212; never throws &#8212; when the array is missing or not the documented
    /// shape, since this SDK does not claim every tenant's event payload matches the matrix's documented shape
    /// exactly; a host that needs the raw structure still has <see cref="NotificationEnvelope.Payload"/>.</summary>
    public static IReadOnlyList<AdpEventSummary> ParseEvents(JsonElement envelope)
    {
        if (!envelope.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array) return [];
        var summaries = new List<AdpEventSummary>();
        foreach (var entry in events.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object) continue;
            string? itemId = null, associateOid = null;
            if (entry.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("eventContext", out var context) && context.ValueKind == JsonValueKind.Object)
            {
                if (context.TryGetProperty("itemID", out var item) && item.ValueKind == JsonValueKind.String) itemId = item.GetString();
                if (context.TryGetProperty("worker", out var worker) && worker.ValueKind == JsonValueKind.Object
                    && worker.TryGetProperty("associateOID", out var oid) && oid.ValueKind == JsonValueKind.String) associateOid = oid.GetString();
            }
            summaries.Add(new AdpEventSummary(itemId, associateOid));
        }
        return summaries;
    }
}

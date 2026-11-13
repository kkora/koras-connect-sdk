using System.Text.Json;
namespace Koras.Connect.Adp.Events;

/// <summary>Minimal typed envelope around one ADP event-notification message, whether delivered via webhook or
/// retrieved by polling <see cref="AdpOperation.EventNotifications"/>. The full original message is preserved as
/// <see cref="Payload"/> so downstream processing stays schema-tolerant to vendor event shapes this SDK does not
/// model. Schema source: "Event polling"/"Event acknowledgement" rows (S2) and the webhook delivery envelope (S6)
/// in <c>docs/features/ADP-FEATURE-MATRIX.md</c>; only the <c>meta.messageId</c> field needed to identify a
/// delivery is modeled, matching S6's documented envelope shape.</summary>
public sealed class NotificationEnvelope
{
    public required string MessageId { get; init; }
    public required JsonElement Payload { get; init; }

    /// <summary>Builds an envelope from a webhook delivery body shaped like the documented
    /// <c>{"meta":{"orgID":...,"messageId":...}}</c> envelope used by <see cref="AdpWebhookAuthentication.GetDeliveryKey"/>.</summary>
    public static NotificationEnvelope FromWebhookDelivery(JsonElement envelope)
    {
        var meta = envelope.GetProperty("meta");
        var id = meta.GetProperty("messageId").GetString();
        if (string.IsNullOrWhiteSpace(id)) throw new JsonException("Webhook delivery is missing a message id.");
        return new NotificationEnvelope { MessageId = id, Payload = envelope.Clone() };
    }

    /// <summary>Builds an envelope from one polled <see cref="AdpOperation.EventNotifications"/> message, using the
    /// message id already captured from the allowlisted adp-msg-msgid response header rather than trusting an
    /// unauthenticated payload field.</summary>
    public static NotificationEnvelope FromPolledMessage(string messageId, JsonElement payload)
    {
        if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentException("A message id is required.", nameof(messageId));
        return new NotificationEnvelope { MessageId = messageId, Payload = payload.Clone() };
    }
}

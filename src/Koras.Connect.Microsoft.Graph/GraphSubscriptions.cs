using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A Graph change-notification subscription (MGR-020). This SDK is a subscription client/manager
/// only - hosting the webhook receiver endpoint itself (including echoing the validation token) is the
/// caller's own responsibility.</summary>
public sealed record GraphSubscription(string Id, string Resource, string ChangeType, DateTimeOffset ExpirationDateTime, string NotificationUrl);

public sealed partial class GraphClient
{
    /// <summary>Creates a change-notification subscription via <c>POST /subscriptions</c> (MGR-020).
    /// <paramref name="includeResourceData"/> is a deliberately higher-sensitivity, shorter-max-expiration
    /// opt-in (notification payloads carry resource content, not just identifiers) and is surfaced here as its
    /// own explicit parameter rather than folded silently into another option. Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Maximum allowed expiration varies sharply by
    /// resource type (as short as 60 minutes for <c>presence</c>, up to ~30 days for several Teams/security
    /// types) - this SDK does not locally enforce the per-resource-type ceiling, since the full table was only
    /// partially captured in research; an excessive expiration is rejected by Graph itself.</summary>
    public async Task<GraphSubscription> CreateSubscriptionAsync(string resource, string changeType, Uri notificationUrl,
        DateTimeOffset expirationDateTime, bool includeResourceData = false, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the subscription-management privilege and business impact.");
        if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentException("A resource path is required.", nameof(resource));
        if (string.IsNullOrWhiteSpace(changeType)) throw new ArgumentException("A change type is required.", nameof(changeType));
        ArgumentNullException.ThrowIfNull(notificationUrl);
        var payload = JsonSerializer.SerializeToElement(new
        {
            resource,
            changeType,
            notificationUrl = notificationUrl.AbsoluteUri,
            expirationDateTime,
            includeResourceData,
        });
        var address = new Uri(_root, "subscriptions");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return ParseSubscription(document.RootElement); }
    }

    /// <summary>Renews a subscription's expiration via <c>PATCH /subscriptions/{subscriptionId}</c> (MGR-020).
    /// Renewal must be scheduled proactively given how short some resource types' expiration windows are (as
    /// little as 60 minutes) - this method does not itself schedule anything; it only performs one renewal
    /// call. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task RenewSubscriptionAsync(string subscriptionId, DateTimeOffset newExpirationDateTime, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the subscription-management privilege and business impact.");
        if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException("A subscription id is required.", nameof(subscriptionId));
        var address = new Uri(_root, $"subscriptions/{Uri.EscapeDataString(subscriptionId)}");
        var payload = JsonSerializer.SerializeToElement(new { expirationDateTime = newExpirationDateTime });
        try { await _transport.SendResponseAsync(HttpMethod.Patch, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Deletes a subscription via <c>DELETE /subscriptions/{subscriptionId}</c> (MGR-020), for example
    /// after receiving a <c>subscriptionRemoved</c> lifecycle notification and deciding not to recreate it, or
    /// for explicit unsubscription. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task DeleteSubscriptionAsync(string subscriptionId, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the subscription-management privilege and business impact.");
        if (string.IsNullOrWhiteSpace(subscriptionId)) throw new ArgumentException("A subscription id is required.", nameof(subscriptionId));
        var address = new Uri(_root, $"subscriptions/{Uri.EscapeDataString(subscriptionId)}");
        try { await _transport.SendResponseAsync(HttpMethod.Delete, address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    private static GraphSubscription ParseSubscription(JsonElement root)
    {
        if (!root.TryGetProperty("expirationDateTime", out var expiration) || expiration.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(expiration.GetString(), out var expirationValue))
            throw new ContractException("Subscription response missing a valid expirationDateTime.", new ErrorDetails("graph_subscription_contract"));
        return new GraphSubscription(RequireString(root, "id"), RequireString(root, "resource"), RequireString(root, "changeType"),
            expirationValue, RequireString(root, "notificationUrl"));
    }
}

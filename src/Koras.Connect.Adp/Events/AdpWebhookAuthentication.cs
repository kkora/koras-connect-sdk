using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Koras.Connect.Adp.Events;

/// <summary>ADP header verification per the official Webhooks guide. This authenticates publisher credentials,
/// not payload integrity or freshness. Require independent endpoint authentication and TLS at the host.</summary>
public static class AdpWebhookAuthentication
{
    public static bool VerifyPublisherHeader(string? header, string clientId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException("Publisher credentials required.");
        if (header is null || header.Length != 64) return false;
        byte[] actual;
        try { actual = Convert.FromHexString(header); } catch (FormatException) { return false; }
        var expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(clientSecret), Encoding.UTF8.GetBytes(clientId));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
    /// <summary>Stable delivery identity for a host-owned durable inbox. Never mark processed before downstream success.</summary>
    public static string GetDeliveryKey(JsonElement envelope)
    {
        var meta = envelope.GetProperty("meta");
        var org = meta.GetProperty("orgID").GetString();
        var id = meta.GetProperty("messageId").GetString();
        if (string.IsNullOrWhiteSpace(org) || string.IsNullOrWhiteSpace(id)) throw new JsonException("Webhook identity is required.");
        return JsonSerializer.Serialize(new[] { org, id });
    }
}

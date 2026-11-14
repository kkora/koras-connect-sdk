using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>A parsed Change Data Capture event's <c>ChangeEventHeader</c> fields (SFD-012).</summary>
public sealed record SalesforceChangeEvent(string EntityName, string ChangeType, string? ChangeOrigin, string TransactionKey,
    DateTimeOffset CommitTimestamp, IReadOnlyList<string> RecordIds, IReadOnlyList<string>? ChangedFields, IReadOnlyList<string>? NulledFields);

public sealed partial class SalesforceClient
{
    private static readonly Regex ChangeChannelPattern =
        new(@"^/data/(ChangeEvents|[A-Za-z][A-Za-z0-9_]*ChangeEvent|[A-Za-z][A-Za-z0-9_]*__chn)$", RegexOptions.Compiled);
    private static readonly Regex PlatformEventPattern = new(@"^[A-Za-z][A-Za-z0-9_]*__e$", RegexOptions.Compiled);

    private static void ValidateChangeChannel(string channel)
    {
        if (string.IsNullOrWhiteSpace(channel) || !ChangeChannelPattern.IsMatch(channel))
            throw new ArgumentException("Channel must be a documented Change Data Capture channel (/data/ChangeEvents, {Object}ChangeEvent, or a custom __chn channel).", nameof(channel));
    }

    private static void ValidatePlatformEventName(string eventApiName)
    {
        if (string.IsNullOrWhiteSpace(eventApiName) || !PlatformEventPattern.IsMatch(eventApiName))
            throw new ArgumentException("A Platform Event API name must end with the documented '__e' suffix.", nameof(eventApiName));
    }

    /// <summary>Subscribes to a Change Data Capture channel (SFD-012) over the CometD/Bayeux long-polling
    /// transport, parsing each event's documented <c>ChangeEventHeader</c> fields. Implements the public Bayeux
    /// handshake/subscribe/connect handshake only - the Salesforce-specific replay-ID resume extension is not
    /// implemented, since its exact wire mechanics were not independently confirmed this pass; every subscription
    /// starts fresh (no replay) and never reconnects after a dropped long poll.</summary>
    public async IAsyncEnumerable<SalesforceChangeEvent> SubscribeToChangeEventsAsync(string channel, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidateChangeChannel(channel);
        await foreach (var data in CometDSubscribeLoopAsync(channel, cancellationToken))
            yield return ParseChangeEvent(data);
    }

    private static SalesforceChangeEvent ParseChangeEvent(JsonElement data)
    {
        if (!data.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
            throw new ContractException("Change event is missing its payload.", new ErrorDetails("change_event_contract"));
        if (!payload.TryGetProperty("ChangeEventHeader", out var header) || header.ValueKind != JsonValueKind.Object)
            throw new ContractException("Change event payload is missing ChangeEventHeader.", new ErrorDetails("change_event_contract"));
        return new SalesforceChangeEvent(RequireString(header, "entityName"), RequireString(header, "changeType"), OptionalString(header, "changeOrigin"),
            RequireString(header, "transactionKey"), DateTimeOffset.FromUnixTimeMilliseconds(RequireLong(header, "commitTimestamp")),
            ReadStringArray(header, "recordIds") ?? [], ReadStringArray(header, "changedFields"), ReadStringArray(header, "nulledFields"));
    }

    private static long RequireLong(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var result))
            throw new ContractException($"Change event header missing required field '{property}'.", new ErrorDetails("change_event_contract"));
        return result;
    }

    private static IReadOnlyList<string>? ReadStringArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array) return null;
        return value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!).ToArray();
    }

    /// <summary>Publishes a custom Platform Event (SFD-013) by reusing the sObject-create mechanism against the
    /// event's own <c>__e</c>-suffixed API name - publishing a Platform Event is, per the documented design,
    /// literally an sObject create. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public Task<string> PublishPlatformEventAsync(string eventApiName, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        ValidatePlatformEventName(eventApiName);
        return CreateRecordAsync(eventApiName, fields, writesEnabled, cancellationToken);
    }

    /// <summary>Subscribes to a custom Platform Event's CometD channel <c>/event/{eventApiName}</c> (SFD-013),
    /// sharing SFD-012's Bayeux handshake/subscribe/connect loop and the same no-replay limitation.</summary>
    public async IAsyncEnumerable<JsonDocument> SubscribeToPlatformEventAsync(string eventApiName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ValidatePlatformEventName(eventApiName);
        await foreach (var data in CometDSubscribeLoopAsync($"/event/{eventApiName}", cancellationToken))
            yield return JsonDocument.Parse(data.GetRawText());
    }

    /// <summary>Runs the public Bayeux handshake/subscribe/connect sequence against the org's CometD endpoint
    /// (<c>{instance}/cometd/{numeric-version}/</c>) and yields each subsequent long-poll response's <c>data</c>
    /// payload for messages on <paramref name="channel"/>. Shared by SFD-012 and SFD-013, the two Salesforce
    /// features that subscribe over this transport.</summary>
    private async IAsyncEnumerable<JsonElement> CometDSubscribeLoopAsync(string channel, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var clientId = await CometDHandshakeAsync(cancellationToken);
        await CometDSubscribeAsync(clientId, channel, cancellationToken);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var response = await CometDConnectAsync(clientId, cancellationToken);
            foreach (var message in response.RootElement.EnumerateArray())
                if (message.TryGetProperty("channel", out var ch) && ch.GetString() == channel && message.TryGetProperty("data", out var data))
                    yield return data.Clone();
        }
    }

    private async Task<string> CometDHandshakeAsync(CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new object[] { new { channel = "/meta/handshake", version = "1.0", minimumVersion = "1.0", supportedConnectionTypes = new[] { "long-polling" } } });
        using var response = await SendCometDAsync(body, cancellationToken);
        var message = FindMessage(response, "/meta/handshake");
        if (!RequireBool(message, "successful")) throw new ContractException("CometD handshake was not successful.", new ErrorDetails("cometd_handshake_failed"));
        return RequireString(message, "clientId");
    }

    private async Task CometDSubscribeAsync(string clientId, string channel, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new object[] { new { channel = "/meta/subscribe", clientId, subscription = channel } });
        using var response = await SendCometDAsync(body, cancellationToken);
        var message = FindMessage(response, "/meta/subscribe");
        if (!RequireBool(message, "successful")) throw new ContractException("CometD channel subscription was not successful.", new ErrorDetails("cometd_subscribe_failed"));
    }

    private Task<JsonDocument> CometDConnectAsync(string clientId, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new object[] { new { channel = "/meta/connect", clientId, connectionType = "long-polling" } });
        return SendCometDAsync(body, cancellationToken);
    }

    private async Task<JsonDocument> SendCometDAsync(string body, CancellationToken cancellationToken)
    {
        IntegrationRawResponse response;
        try { response = await _transport.SendRawAsync(HttpMethod.Post, _cometdRoot, body, "application/json", cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        var document = JsonDocument.Parse(response.Body);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        { document.Dispose(); throw new ContractException("CometD response was not a JSON array.", new ErrorDetails("cometd_contract")); }
        return document;
    }

    private static JsonElement FindMessage(JsonDocument response, string channel)
    {
        foreach (var message in response.RootElement.EnumerateArray())
            if (message.TryGetProperty("channel", out var ch) && ch.GetString() == channel) return message;
        throw new ContractException($"CometD response did not include a '{channel}' message.", new ErrorDetails("cometd_contract"));
    }
}

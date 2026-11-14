using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;

namespace Koras.Connect.Qualtrics;

/// <summary>Qualtrics Event Subscription record. Endpoint shape based on the publicly documented Qualtrics API v3
/// Event Subscriptions contract (<c>GET/POST /eventsubscriptions</c>, <c>DELETE /eventsubscriptions/{subscriptionId}</c>);
/// NOT verified against a live tenant. <see cref="Topics"/> is a single string per the documented contract (not a
/// JSON array); multiple topics are joined into one comma-separated value. This client manages subscription
/// registrations only - it is not a receiver: verifying delivery authenticity/freshness and running the endpoint
/// that actually accepts an inbound webhook call remain entirely host-owned, the same division of responsibility
/// already documented for ADP-018's webhook support.</summary>
public sealed class QualtricsEventSubscription
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("topics")] public string? Topics { get; set; }
    [JsonPropertyName("publicationUrl")] public string? PublicationUrl { get; set; }
    [JsonPropertyName("encrypt")] public bool? Encrypt { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class QualtricsClient
{
    /// <summary>Lists every event subscription registered for this account. Endpoint shape based on the publicly
    /// documented Qualtrics API v3 contract (<c>GET /eventsubscriptions</c>); NOT verified against a live tenant.
    /// Read-only and enabled by default; reuses the same bounded, cycle-guarded pagination as
    /// <see cref="EnumerateSurveysAsync"/>.</summary>
    public IAsyncEnumerable<QualtricsEventSubscription> ListEventSubscriptionsAsync(CancellationToken cancellationToken = default)
        => QualtricsPaging.EnumerateAsync<QualtricsEventSubscription>(_root, EventSubscriptionsAddress(), _maxPages, _transport, cancellationToken);

    /// <summary>Fetches exactly one page of event subscriptions; pass the previous call's <see cref="Page{T}.Next"/>
    /// as <paramref name="resumeFrom"/> to pause and resume. Endpoint shape based on the publicly documented
    /// Qualtrics API v3 contract; NOT verified against a live tenant.</summary>
    public Task<Page<QualtricsEventSubscription>> ListEventSubscriptionsPageAsync(ContinuationToken? resumeFrom = null, CancellationToken cancellationToken = default)
    {
        const string fingerprint = "eventsubscriptions";
        var address = resumeFrom is null ? EventSubscriptionsAddress() : QualtricsPaging.ResolveResume(_root, fingerprint, resumeFrom);
        return QualtricsPaging.FetchPageAsync<QualtricsEventSubscription>(fingerprint, _root, address, _transport, cancellationToken);
    }

    /// <summary>Registers an event subscription. Explicit opt-in write; <paramref name="writesEnabled"/> must be set
    /// to <c>true</c> by the caller after verifying Event Subscriptions create support and permissions. Only
    /// registers the subscription with Qualtrics - it does not stand up a receiver endpoint, verify delivery
    /// authenticity/freshness, or deduplicate deliveries; all of that remains entirely host-owned. Endpoint shape
    /// based on the publicly documented Qualtrics API v3 contract (<c>POST /eventsubscriptions</c>); NOT verified
    /// against a live tenant.</summary>
    public async Task<string> CreateEventSubscriptionAsync(IReadOnlyList<string> topics, Uri publicationUrl, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying Event Subscriptions create support and permissions.");
        ArgumentNullException.ThrowIfNull(topics);
        if (topics.Count == 0 || topics.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("At least one non-blank topic is required before any request is sent.", nameof(topics));
        if (topics.Any(t => t.Contains(',')))
            throw new ArgumentException("A topic must not itself contain a comma - topics are joined with ',' before being sent, and an embedded comma would silently split one topic into two.", nameof(topics));
        ArgumentNullException.ThrowIfNull(publicationUrl);
        if (!publicationUrl.IsAbsoluteUri || publicationUrl.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("The publication URL must be an absolute https address.", nameof(publicationUrl));
        var body = JsonSerializer.SerializeToElement(new { topics = string.Join(",", topics), publicationUrl = publicationUrl.AbsoluteUri });
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Post, EventSubscriptionsAddress(), body, cancellationToken: cancellationToken);
            var id = doc.RootElement.GetProperty("result").GetProperty("id").GetString();
            if (string.IsNullOrWhiteSpace(id)) throw new JsonException("Missing created event subscription ID.");
            return id;
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    /// <summary>Deletes an event subscription. Explicit opt-in write; <paramref name="writesEnabled"/> must be set
    /// to <c>true</c> by the caller after verifying Event Subscriptions delete support and permissions. Endpoint
    /// shape based on the publicly documented Qualtrics API v3 contract (<c>DELETE /eventsubscriptions/{subscriptionId}</c>);
    /// NOT verified against a live tenant.</summary>
    public async Task DeleteEventSubscriptionAsync(string subscriptionId, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying Event Subscriptions delete support and permissions.");
        try
        {
            using var doc = await _transport.SendJsonAsync(HttpMethod.Delete, EventSubscriptionsAddress("/" + EndpointSafety.Segment(subscriptionId)), cancellationToken: cancellationToken);
        }
        catch (IntegrationRequestException failure) { throw QualtricsApiException.FromTransportFailure(failure); }
    }

    private Uri EventSubscriptionsAddress(string suffix = "") => new(_root, "eventsubscriptions" + suffix);
}

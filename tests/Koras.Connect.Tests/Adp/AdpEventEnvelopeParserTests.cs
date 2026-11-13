using System.Text.Json;
using Koras.Connect.Adp.Events;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-018: the parsed, safe event-envelope abstraction (<see cref="AdpEventEnvelopeParser"/>,
/// surfaced on <see cref="NotificationEnvelope.Events"/>) over the raw notification JSON.</summary>
public sealed class AdpEventEnvelopeParserTests
{
    [Fact]
    public void ExtractsEventItemIdAndWorkerAssociateOidFromADocumentedEnvelope()
    {
        using var envelope = JsonDocument.Parse("""
            {"events":[{"data":{"eventContext":{"itemID":"evt-1","worker":{"associateOID":"w1"}}}}]}
            """);
        var summaries = AdpEventEnvelopeParser.ParseEvents(envelope.RootElement);
        Assert.Single(summaries);
        Assert.Equal("evt-1", summaries[0].EventItemId);
        Assert.Equal("w1", summaries[0].WorkerAssociateOid);
    }

    [Fact]
    public void MultipleEventsInOneEnvelopeAreAllParsedInOrder()
    {
        using var envelope = JsonDocument.Parse("""
            {"events":[
                {"data":{"eventContext":{"itemID":"evt-1","worker":{"associateOID":"w1"}}}},
                {"data":{"eventContext":{"itemID":"evt-2","worker":{"associateOID":"w2"}}}}
            ]}
            """);
        var summaries = AdpEventEnvelopeParser.ParseEvents(envelope.RootElement);
        Assert.Equal(["evt-1", "evt-2"], summaries.Select(s => s.EventItemId));
        Assert.Equal(["w1", "w2"], summaries.Select(s => s.WorkerAssociateOid));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"events":null}""")]
    [InlineData("""{"events":"not-an-array"}""")]
    public void MissingOrMalformedEventsArrayYieldsAnEmptySequenceRatherThanThrowing(string json)
    {
        using var envelope = JsonDocument.Parse(json);
        Assert.Empty(AdpEventEnvelopeParser.ParseEvents(envelope.RootElement));
    }

    [Fact]
    public void AnEventEntryMissingEventContextYieldsNullFieldsRatherThanThrowing()
    {
        using var envelope = JsonDocument.Parse("""{"events":[{"data":{}}]}""");
        var summaries = AdpEventEnvelopeParser.ParseEvents(envelope.RootElement);
        Assert.Single(summaries);
        Assert.Null(summaries[0].EventItemId);
        Assert.Null(summaries[0].WorkerAssociateOid);
    }

    [Fact]
    public void ANonObjectEventEntryIsSkippedRatherThanThrowing()
    {
        using var envelope = JsonDocument.Parse("""{"events":["not-an-object",{"data":{"eventContext":{"itemID":"evt-1"}}}]}""");
        var summaries = AdpEventEnvelopeParser.ParseEvents(envelope.RootElement);
        Assert.Single(summaries);
        Assert.Equal("evt-1", summaries[0].EventItemId);
    }

    [Fact]
    public void NotificationEnvelopeFromWebhookDeliverySurfacesParsedEventsAlongsideTheRawPayload()
    {
        using var envelope = JsonDocument.Parse("""
            {"meta":{"orgID":"org-1","messageId":"msg-1"},"events":[{"data":{"eventContext":{"worker":{"associateOID":"w1"}}}}]}
            """);
        var typed = NotificationEnvelope.FromWebhookDelivery(envelope.RootElement);
        Assert.Equal("w1", typed.Events[0].WorkerAssociateOid);
        Assert.Equal("org-1", typed.Payload.GetProperty("meta").GetProperty("orgID").GetString()); // raw payload still intact
    }

    [Fact]
    public void NotificationEnvelopeFromPolledMessageSurfacesParsedEvents()
    {
        using var payload = JsonDocument.Parse("""{"events":[{"data":{"eventContext":{"itemID":"evt-1"}}}]}""");
        var typed = NotificationEnvelope.FromPolledMessage("msg-1", payload.RootElement);
        Assert.Equal("evt-1", typed.Events[0].EventItemId);
    }
}

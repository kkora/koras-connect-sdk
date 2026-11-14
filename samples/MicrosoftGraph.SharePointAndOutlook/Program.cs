using System.Net;
using System.Text;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Koras.Connect.Transport;

// Offline demonstration of the Microsoft Graph connector's SharePoint and Outlook surface: site resolution,
// document libraries, list items, and mail/calendar - none of which is touched by
// samples/MicrosoftGraph.OfflineWalkthrough (auth/diagnostics/users/groups/OneDrive/delta) or
// samples/MicrosoftGraph.TeamsAndPlannerCollaboration (Teams/Planner/To Do). Every HTTP response below is
// synthetic; this sample never contacts a live Entra tenant. Everything here works under application
// permissions (no delegated-only /me restriction applies to any of these calls), so the client uses the
// default app-only GraphOptions throughout.
Console.WriteLine("Koras.Connect.Microsoft.Graph offline sample: SharePoint sites/lists -> Outlook mail/calendar.");
Console.WriteLine("All HTTP responses below are synthetic; no network access occurs.");
Console.WriteLine();

const string siteId = "site-marketing";
const string listId = "list-1";
const string userId = "user-1@contoso.com";

using var http = new HttpClient(new SyntheticHandler((request, _) =>
{
    var path = request.RequestUri!.AbsolutePath;
    if (request.Method == HttpMethod.Get && path == "/v1.0/sites/root")
        return Reply("""{"id":"site-root","displayName":"Contoso","webUrl":"https://contoso.sharepoint.com"}""");
    if (request.Method == HttpMethod.Get && path == "/v1.0/sites/contoso.sharepoint.com:/sites/Marketing")
        return Reply($$"""{"id":"{{siteId}}","displayName":"Marketing","webUrl":"https://contoso.sharepoint.com/sites/Marketing"}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/sites/{siteId}/drives")
        return Reply("""{"value":[{"id":"drive-1","name":"Documents"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/sites/{siteId}/lists")
        return Reply($$"""{"value":[{"id":"{{listId}}","displayName":"Campaigns"}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/sites/{siteId}/lists/{listId}/items")
        return Reply("""{"value":[{"id":"item-1","fields":{"Title":"Spring Launch"}}]}""");
    if (request.Method == HttpMethod.Post && path == $"/v1.0/sites/{siteId}/lists/{listId}/items")
        return Reply("""{"id":"item-2"}""", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Patch && path == $"/v1.0/sites/{siteId}/lists/{listId}/items/item-2/fields")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Get && path == $"/v1.0/users/{Uri.EscapeDataString(userId)}/messages")
        return Reply("""{"value":[{"id":"msg-1","subject":"Q3 budget review","body":{"contentType":"html","content":"<p>See attached.</p>"}}]}""");
    if (request.Method == HttpMethod.Get && path == $"/v1.0/users/{Uri.EscapeDataString(userId)}/events")
        return Reply("""{"value":[{"id":"event-1","subject":"Launch standup","start":{"dateTime":"2026-11-20T09:00:00","timeZone":"Pacific Standard Time"},"end":{"dateTime":"2026-11-20T09:30:00","timeZone":"Pacific Standard Time"}}]}""");
    if (request.Method == HttpMethod.Post && path == $"/v1.0/users/{Uri.EscapeDataString(userId)}/events")
        return Reply("""{"id":"event-2"}""", HttpStatusCode.Created);
    if (request.Method == HttpMethod.Patch && path == $"/v1.0/users/{Uri.EscapeDataString(userId)}/events/event-2")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    if (request.Method == HttpMethod.Delete && path == $"/v1.0/users/{Uri.EscapeDataString(userId)}/events/event-2")
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    return new HttpResponseMessage(HttpStatusCode.NotFound);
}));

var tokens = new StaticTokenSource("sample-access-token");
var client = new GraphClient(new IntegrationTransport(http, tokens));

var rootSite = await client.GetRootSiteAsync();
Console.WriteLine($"Root site: {rootSite.DisplayName} ({rootSite.WebUrl})");

var marketingSite = await client.GetSiteByPathAsync("contoso.sharepoint.com", "sites/Marketing");
Console.WriteLine($"Resolved site by path: {marketingSite.DisplayName}");

Console.WriteLine("Enumerating the site's document libraries:");
await foreach (var drive in client.EnumerateSiteDrivesAsync(marketingSite.Id))
    Console.WriteLine($"  Drive: {drive.RootElement.GetProperty("name").GetString()}");

Console.WriteLine("Enumerating lists and list items:");
await foreach (var list in client.EnumerateListsAsync(marketingSite.Id))
    Console.WriteLine($"  List: {list.RootElement.GetProperty("displayName").GetString()}");
await foreach (var item in client.EnumerateListItemsAsync(marketingSite.Id, listId))
    Console.WriteLine($"  List item: {item.Fields!["Title"].GetString()}");

using var newItemFields = JsonDocument.Parse("""{"Title":"Fall Refresh"}""");
var newItemId = await client.CreateListItemAsync(marketingSite.Id, listId, newItemFields.RootElement, writesEnabled: true);
Console.WriteLine($"Created list item {newItemId}.");
using var updatedItemFields = JsonDocument.Parse("""{"Title":"Fall Refresh (Updated)"}""");
await client.UpdateListItemFieldsAsync(marketingSite.Id, listId, newItemId, updatedItemFields.RootElement, writesEnabled: true);
Console.WriteLine("Updated list item fields.");

Console.WriteLine("Enumerating a user's mail:");
await foreach (var message in client.EnumerateUserMessagesAsync(userId))
    Console.WriteLine($"  Message: {message.Subject} - {message.BodyHtml}");

Console.WriteLine("Enumerating, creating, updating and deleting calendar events (app-only, fully symmetric with delegated):");
await foreach (var ev in client.EnumerateEventsAsync(userId))
    Console.WriteLine($"  Event: {ev.Subject} at {ev.Start!.DateTime} ({ev.Start.TimeZone})");

var newEvent = new GraphEvent
{
    Subject = "Retro",
    Start = new GraphEventDateTime("2026-11-21T15:00:00", "Pacific Standard Time"),
    End = new GraphEventDateTime("2026-11-21T15:30:00", "Pacific Standard Time"),
};
var newEventId = await client.CreateEventAsync(userId, newEvent, writesEnabled: true);
Console.WriteLine($"Created event {newEventId}.");
using var updatedEventFields = JsonDocument.Parse("""{"subject":"Retro (rescheduled)"}""");
await client.UpdateEventAsync(userId, newEventId, updatedEventFields.RootElement, writesEnabled: true);
Console.WriteLine("Updated event subject.");
await client.DeleteEventAsync(userId, newEventId, writesEnabled: true);
Console.WriteLine("Deleted event.");

Console.WriteLine();
Console.WriteLine("Offline sample completed without contacting any live Entra tenant.");

static HttpResponseMessage Reply(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

sealed class SyntheticHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(reply(request, cancellationToken));
}

sealed class StaticTokenSource(string token) : IAccessTokenSource
{
    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(token);
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>A OneDrive drive item - a file or folder (MGR-009).</summary>
public sealed class GraphDriveItem
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("size")] public long? Size { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Reads the signed-in user's drive via <c>GET /me/drive</c> (MGR-009). Application permissions do
    /// not support this shortcut - this throws locally, before any HTTP call, unless
    /// <see cref="GraphOptions.IsDelegatedContext"/> was explicitly set, pointing the caller at
    /// <see cref="EnumerateDrivesAsync"/>/<see cref="GetDriveAsync"/> instead.</summary>
    public Task<JsonDocument> GetMyDriveAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/me/drive requires a delegated (signed-in-user) context; application permissions must use EnumerateDrivesAsync/GetDriveAsync against /drives instead.");
        return ReadResourceAsync<JsonDocument>(new Uri(_root, "me/drive"), cancellationToken);
    }

    /// <summary>Reads a user's drive via <c>GET /users/{userId}/drive</c> (MGR-009). Application permissions do
    /// not support this shortcut either - this throws locally for the same reason as <see cref="GetMyDriveAsync"/>.</summary>
    public Task<JsonDocument> GetUserDriveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/users/{id}/drive requires a delegated (signed-in-user) context; application permissions must use EnumerateDrivesAsync/GetDriveAsync against /drives instead.");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(userId));
        return ReadResourceAsync<JsonDocument>(new Uri(_root, $"users/{Uri.EscapeDataString(userId)}/drive"), cancellationToken);
    }

    /// <summary>Enumerates every drive via <c>GET /drives</c> (MGR-009) - the only drive-listing path supported
    /// for application permissions.</summary>
    public IAsyncEnumerable<JsonDocument> EnumerateDrivesAsync(CancellationToken cancellationToken = default)
        => EnumerateAsync<JsonDocument>("drives", cancellationToken: cancellationToken);

    /// <summary>Reads a single drive via <c>GET /drives/{driveId}</c> (MGR-009). Works under both application and
    /// delegated permissions.</summary>
    public Task<JsonDocument> GetDriveAsync(string driveId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(driveId)) throw new ArgumentException("A drive id is required.", nameof(driveId));
        return ReadResourceAsync<JsonDocument>(new Uri(_root, $"drives/{Uri.EscapeDataString(driveId)}"), cancellationToken);
    }

    /// <summary>Enumerates a drive's children via <c>GET /drives/{driveId}/root/children</c>, or
    /// <c>GET /drives/{driveId}/items/{itemId}/children</c> when <paramref name="itemId"/> is supplied (MGR-009/019).</summary>
    public IAsyncEnumerable<GraphDriveItem> EnumerateDriveItemsAsync(string driveId, string? itemId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(driveId)) throw new ArgumentException("A drive id is required.", nameof(driveId));
        var resourcePath = string.IsNullOrWhiteSpace(itemId)
            ? $"drives/{Uri.EscapeDataString(driveId)}/root/children"
            : $"drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}/children";
        return EnumerateAsync<GraphDriveItem>(resourcePath, cancellationToken: cancellationToken);
    }
}

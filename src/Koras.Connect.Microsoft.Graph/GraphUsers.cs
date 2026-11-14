using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Microsoft.Graph;

/// <summary>Directory user profile (MGR-004). Only the three most commonly-needed fields are hardcoded; every
/// other field (and every tenant-specific extension attribute) remains in <see cref="AdditionalData"/>, matching
/// every existing Phase 1/2 connector's extension-data-preserving DTO discipline.</summary>
public sealed class GraphUser
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
    [JsonPropertyName("userPrincipalName")] public string? UserPrincipalName { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

public sealed partial class GraphClient
{
    /// <summary>Enumerates <c>GET /users</c>, transparently paginating via <see cref="EnumerateAsync{T}"/> (MGR-004/019).</summary>
    public IAsyncEnumerable<GraphUser> EnumerateUsersAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<GraphUser>("users", query, cancellationToken);

    /// <summary>Reads a single user via <c>GET /users/{idOrUserPrincipalName}</c> (MGR-004).</summary>
    public Task<GraphUser> GetUserAsync(string idOrUserPrincipalName, GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idOrUserPrincipalName)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(idOrUserPrincipalName));
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(idOrUserPrincipalName)}" + (query?.ToQueryString() ?? ""));
        return ReadResourceAsync<GraphUser>(address, cancellationToken);
    }

    /// <summary>Reads the signed-in user via <c>GET /me</c> (MGR-004). <c>/me</c> is delegated-only - application
    /// permissions have no signed-in-user context, so this throws locally, before any HTTP call, unless
    /// <see cref="GraphOptions.IsDelegatedContext"/> was explicitly set on this client.</summary>
    public Task<GraphUser> GetMeAsync(GraphQuery? query = null, CancellationToken cancellationToken = default)
    {
        if (!_options.IsDelegatedContext)
            throw new InvalidOperationException("/me requires a delegated (signed-in-user) context; application permissions do not support /me. Construct GraphClient with GraphOptions.IsDelegatedContext set to true when using a delegated token source.");
        var address = new Uri(_root, "me" + (query?.ToQueryString() ?? ""));
        return ReadResourceAsync<GraphUser>(address, cancellationToken);
    }

    /// <summary>Creates a user via <c>POST /users</c> (MGR-004). Disabled unless <paramref name="writesEnabled"/>
    /// is explicitly <c>true</c>.</summary>
    public async Task<string> CreateUserAsync(JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the directory-write privilege and business impact.");
        var address = new Uri(_root, "users");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, fields, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "id"); }
    }

    /// <summary>Updates a user via <c>PATCH /users/{idOrUserPrincipalName}</c> (MGR-004). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdateUserAsync(string idOrUserPrincipalName, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the directory-write privilege and business impact.");
        if (string.IsNullOrWhiteSpace(idOrUserPrincipalName)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(idOrUserPrincipalName));
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(idOrUserPrincipalName)}");
        try { using (await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, cancellationToken: cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Deletes a user via <c>DELETE /users/{idOrUserPrincipalName}</c> (MGR-004). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>.</summary>
    public async Task DeleteUserAsync(string idOrUserPrincipalName, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the directory-write privilege and business impact.");
        if (string.IsNullOrWhiteSpace(idOrUserPrincipalName)) throw new ArgumentException("A user id or userPrincipalName is required.", nameof(idOrUserPrincipalName));
        var address = new Uri(_root, $"users/{Uri.EscapeDataString(idOrUserPrincipalName)}");
        try { using (await _transport.SendResponseAsync(HttpMethod.Delete, address, cancellationToken: cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }
}

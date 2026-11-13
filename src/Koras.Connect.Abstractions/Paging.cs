namespace Koras.Connect.Abstractions;

/// <summary>Opaque, provider/query-bound continuation. Never contains raw credentials.</summary>
public sealed record ContinuationToken(string Provider, string QueryFingerprint, string OpaqueValue, DateTimeOffset? ExpiresAt = null);

/// <summary>A single page of results plus an optional continuation for the next page.</summary>
public sealed record Page<T>(IReadOnlyList<T> Items, ContinuationToken? Next);

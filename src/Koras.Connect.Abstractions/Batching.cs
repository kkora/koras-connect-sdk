namespace Koras.Connect.Abstractions;

/// <summary>Outcome of one item within a multi-item batch write. A shared shape convention, not a new interface -
/// each connector still defines its own batch-request type matching its vendor's wire format (Dataverse's
/// multipart/mixed changesets, Salesforce's Composite JSON array, Microsoft Graph's $batch JSON array); this only
/// standardizes how a mixed success/failure result array is reported back, matching the "distinguish success from
/// partial failure without silently dropping information" discipline <see cref="MappingResult{TTarget}"/> already
/// applies to mapping. Carries a sanitized error code/correlation id rather than <c>Koras.Connect.Exceptions.ErrorDetails</c>
/// directly: this project has no dependency on <c>Koras.Connect.Core</c> (the reviewed package-dependency graph),
/// and <c>ErrorDetails</c> lives there - connectors map their classified exception's <c>Details</c> onto these two
/// fields at the boundary. <see cref="Value"/> is only meaningful when <see cref="Succeeded"/> is <c>true</c> -
/// deliberately typed as plain <typeparamref name="T"/> rather than <c>T?</c>, since an unconstrained <c>T?</c>
/// does not become <c>Nullable&lt;T&gt;</c> for a value-type <typeparamref name="T"/> (for example
/// <see cref="System.Text.Json.JsonElement"/>) without a <c>struct</c> constraint this type intentionally avoids
/// to stay usable with reference-typed results too.</summary>
public sealed record BatchItemResult<T>(int Index, bool Succeeded, T Value, string? ErrorCode = null, string? ErrorCorrelationId = null);

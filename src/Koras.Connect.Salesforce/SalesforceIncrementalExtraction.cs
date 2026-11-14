using System.Globalization;

namespace Koras.Connect.Salesforce;

/// <summary>A caller-owned checkpoint for SFD-021's incremental extraction - a filterable <c>SystemModstamp</c>
/// value, not an opaque server-issued cursor (unlike Dataverse's <c>$deltatoken</c>), so it is a plain timestamp
/// rather than a <c>ContinuationToken</c>.</summary>
public sealed record SalesforceDeltaCheckpoint(DateTimeOffset SinceSystemModstamp);

public sealed partial class SalesforceClient
{
    /// <summary>Extracts every row changed (including soft-deleted, via <c>queryAll</c>) since
    /// <paramref name="checkpoint"/>'s <c>SystemModstamp</c> value (SFD-021), reusing SFD-005/006's SOQL
    /// pagination unchanged. <c>SystemModstamp</c> is added to <paramref name="fields"/> automatically if not
    /// already present, so callers can always read the row's own modstamp to advance their next checkpoint.
    /// The legacy SOAP-only <c>getUpdated</c>/<c>getDeleted</c> calls are a documented alternative vendor
    /// mechanism, not implemented here - this connector is REST/JSON-only.</summary>
    public IAsyncEnumerable<T> EnumerateChangesAsync<T>(string sObject, SalesforceDeltaCheckpoint checkpoint, IReadOnlyList<string> fields, CancellationToken cancellationToken = default)
    {
        ValidateSObject(sObject);
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (fields is null || fields.Count == 0) throw new ArgumentException("At least one field is required.", nameof(fields));
        var selectFields = fields.Contains("SystemModstamp", StringComparer.OrdinalIgnoreCase) ? fields : [.. fields, "SystemModstamp"];
        var timestamp = checkpoint.SinceSystemModstamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var soql = $"SELECT {string.Join(",", selectFields)} FROM {sObject} WHERE SystemModstamp >= {timestamp} ORDER BY SystemModstamp";
        return EnumerateQueryAsync<T>(soql, includeDeleted: true, cancellationToken: cancellationToken);
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Koras.Connect.Abstractions;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Dataverse;

/// <summary>Table and row CRUD (DVT-004), OData filtering/selection/ordering (DVT-005) and pagination (DVT-006).</summary>
public sealed partial class DataverseClient
{
    private static readonly System.Text.RegularExpressions.Regex EntitySetPattern =
        new("^[a-z][a-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static void ValidateEntitySet(string entitySet)
    {
        if (string.IsNullOrWhiteSpace(entitySet) || !EntitySetPattern.IsMatch(entitySet))
            throw new ArgumentException("Entity set must be a lowercase logical-collection name.", nameof(entitySet));
    }

    /// <summary>Creates a row and returns its new <see cref="Guid"/>, recovered from either the documented
    /// <c>OData-EntityId</c> response header (no <c>Prefer</c> header sent - the default, minimal round-trip) or
    /// an <c>@odata.id</c> annotation in the response body, when present. Disabled unless <paramref name="writesEnabled"/>
    /// is explicitly <c>true</c>, matching every existing Phase 1 write operation's convention.</summary>
    public Task<Guid> CreateRowAsync(string entitySet, JsonElement fields, bool writesEnabled = false, CancellationToken cancellationToken = default)
        => CreateRowCoreAsync(entitySet, fields, RequiredHeaders, writesEnabled, cancellationToken);

    /// <summary>Shared create implementation behind <see cref="CreateRowAsync"/> and <c>CreateRowInSolutionAsync</c>
    /// (DVT-019), which differs only in the extra <c>MSCRM.SolutionUniqueName</c> header it supplies.</summary>
    private async Task<Guid> CreateRowCoreAsync(string entitySet, JsonElement fields, IReadOnlyDictionary<string, string> headers, bool writesEnabled, CancellationToken cancellationToken)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        ValidateEntitySet(entitySet);
        var address = new Uri(_root, entitySet);
        ValidateRequestUri(address);
        IntegrationJsonResponse response;
        try { response = await _transport.SendResponseAsync(HttpMethod.Post, address, fields, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (response)
        {
            if (response.Body.RootElement.ValueKind == JsonValueKind.Object
                && response.Body.RootElement.TryGetProperty("@odata.id", out var odataId) && odataId.ValueKind == JsonValueKind.String)
                return ExtractGuidFromODataId(odataId.GetString()!);
            if (response.Headers.TryGetValue("odata-entityid", out var values) && values.Length > 0)
                return ExtractGuidFromODataId(values[0]);
            throw new ContractException("Create response did not include an OData-EntityId header or an @odata.id annotation.", new ErrorDetails("create_contract"));
        }
    }

    /// <summary>Reads a single row. <paramref name="query"/> may only set <see cref="DataverseQuery.Select"/>/
    /// <see cref="DataverseQuery.Expand"/> - the other options apply only to a collection read (DVT-005) and are
    /// rejected locally rather than silently ignored. When <see cref="DataverseQuery.Expand"/> is set, sends
    /// <c>If-None-Match: null</c> to defeat Dataverse's documented caching of expanded collection-valued
    /// navigation properties (DVT-013) - a Dataverse-specific quirk, not a generic OData convention.
    /// <paramref name="includeFormattedValues"/>, when <c>true</c>, requests the <c>OData.Community.Display.V1.FormattedValue</c>
    /// annotation (DVT-011) so choice/status/state columns carry a readable label sibling property - read it back
    /// with <see cref="TryGetFormattedValue"/>.</summary>
    public async Task<JsonDocument> GetRowAsync(string entitySet, Guid id, DataverseQuery? query = null, bool includeFormattedValues = false, CancellationToken cancellationToken = default)
    {
        var address = BuildRowAddress(entitySet, id, query);
        ValidateRequestUri(address);
        try { return await _transport.ReadJsonAsync(address, BuildReadHeaders(query, includeFormattedValues: includeFormattedValues), cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Updates a row via <c>PATCH</c>. Disabled unless <paramref name="writesEnabled"/> is explicitly
    /// <c>true</c>. Never retried, matching every existing connector's "never replay an uncertain write" policy.
    /// <paramref name="ifMatchETag"/>, when supplied, enforces optimistic concurrency (DVT-013): a mismatch
    /// surfaces as <see cref="DataverseConcurrencyException"/>, distinguishable from every other 4xx failure.</summary>
    public async Task UpdateRowAsync(string entitySet, Guid id, JsonElement fields, string? ifMatchETag = null, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        var address = RowAddress(entitySet, id);
        ValidateRequestUri(address);
        try { using (await _transport.SendResponseAsync(HttpMethod.Patch, address, fields, WithIfMatch(ifMatchETag), cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Deletes a row. Disabled unless <paramref name="writesEnabled"/> is explicitly <c>true</c>.
    /// <paramref name="ifMatchETag"/>, when supplied, enforces optimistic concurrency (DVT-013).</summary>
    public async Task DeleteRowAsync(string entitySet, Guid id, string? ifMatchETag = null, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        var address = RowAddress(entitySet, id);
        ValidateRequestUri(address);
        try { using (await _transport.SendResponseAsync(HttpMethod.Delete, address, null, WithIfMatch(ifMatchETag), cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Updates a single property via <c>PUT {entityset}(guid)/{property}</c> with a <c>{"value": ...}</c>
    /// body (DVT-004) - distinct from <see cref="UpdateRowAsync"/>'s full-row <c>PATCH</c>. Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c>. Never retried.</summary>
    public async Task UpdatePropertyAsync(string entitySet, Guid id, string property, JsonElement value, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the table privilege and business impact.");
        if (string.IsNullOrWhiteSpace(property) || property.Contains('/') || property.Contains('(') || property.Contains(')'))
            throw new ArgumentException("A single, unencoded property name is required.", nameof(property));
        ValidateEntitySet(entitySet);
        var address = new Uri(_root, $"{entitySet}({id:D})/{Uri.EscapeDataString(property)}");
        ValidateRequestUri(address);
        using var body = JsonDocument.Parse($$"""{"value":{{value.GetRawText()}}}""");
        try { using (await _transport.SendResponseAsync(HttpMethod.Put, address, body.RootElement, RequiredHeaders, cancellationToken)) { } }
        catch (IntegrationRequestException ex) { throw ClassifyWrite(ex); }
    }

    /// <summary>Enumerates a whole entity set, transparently following <c>@odata.nextLink</c> (DVT-006) - the
    /// opaque <c>$skiptoken</c> it carries is never decoded or reconstructed, only forwarded via
    /// <see cref="EndpointSafety.Continuation"/>.</summary>
    public async IAsyncEnumerable<JsonElement> EnumerateAsync(string entitySet, DataverseQuery? query = null,
        int? maxPageSize = null, bool includeFormattedValues = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ContinuationToken? next = null;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= _options.MaxPages) throw new InvalidDataException("Dataverse pagination limit exceeded.");
            var result = await EnumeratePageAsync(entitySet, query, next, maxPageSize, includeFormattedValues, cancellationToken);
            foreach (var item in result.Items) { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
            if (result.Next is null) yield break;
            if (!visited.Add(result.Next.OpaqueValue)) throw new InvalidDataException("Dataverse pagination cycle detected.");
            next = result.Next;
        }
    }

    /// <summary>Reads a single page, either the first page of a query (<paramref name="resumeFrom"/> null) or a
    /// continuation of a prior page. <paramref name="maxPageSize"/> and <see cref="DataverseQuery.Top"/> are
    /// mutually exclusive per Dataverse's own documented precedence - rejected locally rather than silently
    /// deferring to whichever the server happens to honor. <paramref name="includeFormattedValues"/> requests the
    /// formatted-value annotation for every choice/status/state column (DVT-011).</summary>
    public async Task<Page<JsonElement>> EnumeratePageAsync(string entitySet, DataverseQuery? query = null,
        ContinuationToken? resumeFrom = null, int? maxPageSize = null, bool includeFormattedValues = false, CancellationToken cancellationToken = default)
    {
        ValidateEntitySet(entitySet);
        var effectiveQuery = query ?? new DataverseQuery();
        if (effectiveQuery.Top is not null && maxPageSize is not null)
            throw new ArgumentException("$top and odata.maxpagesize are mutually exclusive - the server ignores $top when both are supplied.", nameof(maxPageSize));
        if (effectiveQuery.Top is <= 0) throw new ArgumentException("$top must be positive.", nameof(query));
        if (maxPageSize is <= 0) throw new ArgumentOutOfRangeException(nameof(maxPageSize), "odata.maxpagesize must be positive.");

        var headers = BuildReadHeaders(effectiveQuery, maxPageSize, includeFormattedValues);

        var address = resumeFrom is not null ? EndpointSafety.Continuation(_root, resumeFrom.OpaqueValue) : BuildListAddress(entitySet, effectiveQuery);
        ValidateRequestUri(address);
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, headers, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document)
        {
            var root = document.RootElement;
            if (!root.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
                throw new ContractException("Dataverse list response did not contain a value array.", new ErrorDetails("list_contract"));
            var items = value.EnumerateArray().Select(e => e.Clone()).ToArray();
            ContinuationToken? next = null;
            if (root.TryGetProperty("@odata.nextLink", out var nextLink) && nextLink.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(nextLink.GetString()))
                next = new ContinuationToken("Dataverse", QueryFingerprint(entitySet, effectiveQuery), nextLink.GetString()!);
            return new Page<JsonElement>(items, next);
        }
    }

    private static string QueryFingerprint(string entitySet, DataverseQuery query)
        => string.Join('|', entitySet, query.Select, query.Filter, query.OrderBy, query.Expand, query.Apply, query.Top, query.Count);

    private Uri RowAddress(string entitySet, Guid id)
    {
        ValidateEntitySet(entitySet);
        return new Uri(_root, $"{entitySet}({id:D})");
    }

    private Uri BuildRowAddress(string entitySet, Guid id, DataverseQuery? query)
    {
        ValidateEntitySet(entitySet);
        var parts = new List<string>();
        if (query is not null)
        {
            if (query.Filter is not null || query.OrderBy is not null || query.Apply is not null || query.Top is not null || query.Count)
                throw new ArgumentException("A single-row read supports only $select/$expand.", nameof(query));
            if (!string.IsNullOrWhiteSpace(query.Select)) parts.Add("$select=" + Uri.EscapeDataString(query.Select));
            if (!string.IsNullOrWhiteSpace(query.Expand)) parts.Add("$expand=" + Uri.EscapeDataString(query.Expand));
        }
        var relative = $"{entitySet}({id:D})" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        return new Uri(_root, relative);
    }

    private Uri BuildListAddress(string entitySet, DataverseQuery query)
    {
        var parts = new List<string>();
        void Add(string name, string? value) { if (!string.IsNullOrWhiteSpace(value)) parts.Add(name + "=" + Uri.EscapeDataString(value)); }
        Add("$select", query.Select); Add("$filter", query.Filter); Add("$orderby", query.OrderBy);
        Add("$expand", query.Expand); Add("$apply", query.Apply);
        if (query.Top is not null) parts.Add("$top=" + query.Top.Value.ToString(CultureInfo.InvariantCulture));
        if (query.Count) parts.Add("$count=true");
        var relative = entitySet + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        return new Uri(_root, relative);
    }

    private static Guid ExtractGuidFromODataId(string value)
    {
        var open = value.LastIndexOf('(');
        var close = value.LastIndexOf(')');
        if (open < 0 || close <= open || !Guid.TryParse(value[(open + 1)..close], out var id))
            throw new ContractException("Could not parse an entity id from the create response.", new ErrorDetails("create_contract"));
        return id;
    }

    /// <summary>Adds the <c>If-Match</c> header when a concurrency token is supplied (DVT-013). Returns the shared
    /// <see cref="RequiredHeaders"/> instance unchanged when none is supplied, avoiding an allocation per call.</summary>
    private static IReadOnlyDictionary<string, string> WithIfMatch(string? ifMatchETag)
        => ifMatchETag is null ? RequiredHeaders : new Dictionary<string, string>(RequiredHeaders) { ["If-Match"] = ifMatchETag };

    /// <summary>Composes the read-request headers: <c>If-None-Match: null</c> when the query expands a navigation
    /// property (defeating Dataverse's documented caching quirk for expanded collection-valued properties,
    /// DVT-013), and a combined <c>Prefer</c> value for <paramref name="maxPageSize"/> (DVT-006) and/or the
    /// formatted-value annotation (DVT-011) when requested.</summary>
    private static IReadOnlyDictionary<string, string> BuildReadHeaders(DataverseQuery? query, int? maxPageSize = null, bool includeFormattedValues = false)
    {
        var preferences = new List<string>();
        if (maxPageSize is not null) preferences.Add($"odata.maxpagesize={maxPageSize}");
        if (includeFormattedValues) preferences.Add($"odata.include-annotations=\"{FormattedValueAnnotation}\"");
        var hasExpand = !string.IsNullOrWhiteSpace(query?.Expand);
        if (preferences.Count == 0 && !hasExpand) return RequiredHeaders;
        var headers = new Dictionary<string, string>(RequiredHeaders);
        if (preferences.Count > 0) headers["Prefer"] = string.Join(",", preferences);
        if (hasExpand) headers["If-None-Match"] = "null";
        return headers;
    }

    /// <summary>Classifies a write failure, distinguishing a <c>412</c> optimistic-concurrency conflict
    /// (<see cref="DataverseConcurrencyException"/>) from every other failure, which still goes through the
    /// shared <see cref="Classify"/> path.</summary>
    private static Koras.Connect.Exceptions.IntegrationException ClassifyWrite(IntegrationRequestException ex)
        => ex.ResponseStatus == System.Net.HttpStatusCode.PreconditionFailed
            ? new DataverseConcurrencyException("The row was modified since it was last read (HTTP 412).", new ErrorDetails("dataverse_concurrency_conflict"))
            : Classify(ex);
}

/// <summary>A <c>412 Precondition Failed</c> optimistic-concurrency conflict (DVT-013) - Dataverse documents
/// <c>ConcurrencyVersionMismatch</c>/<c>DuplicateRecord</c> codes for this status. The shared
/// <see cref="IntegrationExceptionClassifier"/> has no dedicated mapping for <c>412</c> (its default arm returns
/// <c>ContractException</c>, which is <c>sealed</c> and so cannot be subclassed); this type derives from
/// <see cref="IntegrationException"/> directly instead, giving callers a type distinct from every other 4xx
/// failure so they can implement their own re-read-and-retry policy rather than treating this as a generic
/// contract error. Kept Dataverse-local per the master specification's "no abstraction creep for one vendor"
/// principle (Section 8.1) - 412-as-concurrency-conflict is not yet proven needed by Salesforce or Graph.</summary>
public sealed class DataverseConcurrencyException(string message, ErrorDetails details) : IntegrationException(message, details);

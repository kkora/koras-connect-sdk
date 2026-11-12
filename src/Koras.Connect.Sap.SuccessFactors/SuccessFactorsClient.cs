using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Transport;
namespace Koras.Connect.Sap.SuccessFactors;

public sealed record SuccessFactorsOptions(Uri ODataRoot, int PageSize = 100, int MaxPages = 10000);
public sealed record ODataQuery(string? Filter = null, string? Select = null, string? Expand = null,
    DateOnly? AsOfDate = null, DateOnly? FromDate = null, DateOnly? ToDate = null, string? Paging = null)
{
    public override string ToString() => "ODataQuery { values=[REDACTED] }";
}
public sealed class PerPerson
{
    [JsonPropertyName("personIdExternal")] public string? PersonIdExternal { get; set; }
    [JsonPropertyName("personId")] public string? PersonId { get; set; }
    [JsonPropertyName("dateOfBirth")] public JsonElement? DateOfBirth { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class UpsertResult
{
    [JsonPropertyName("key")] public string? Key { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("httpCode")] public int HttpCode { get; set; }
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonIgnore] public bool Succeeded => HttpCode is >= 200 and < 300 && Status == "OK";
    public override string ToString() => $"UpsertResult {{ httpCode={HttpCode}, succeeded={Succeeded}, details=[REDACTED] }}";
}
/// <summary>Explicit tenant OData V2 root and host-owned valid OAuth/OIDC token source. No UI-host or API-host inference.</summary>
public sealed partial class SuccessFactorsClient
{
    private readonly IntegrationTransport _transport;
    private readonly SuccessFactorsOptions _options;
    private readonly Uri _root;
    public SuccessFactorsClient(IntegrationTransport transport, SuccessFactorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);
        _transport = transport; _options = options; _root = EndpointSafety.ValidateRoot(options.ODataRoot);
        ValidateBounds(options, _root);
    }
    /// <summary>Validates a <see cref="SuccessFactorsOptions"/> instance using the same rule the constructor
    /// enforces, without requiring an <see cref="IntegrationTransport"/>. Intended for fail-fast DI registration
    /// (see <c>AddKorasSuccessFactors</c>) so a bad root is rejected before any HttpClient/handler is created.</summary>
    public static void ValidateOptions(SuccessFactorsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateBounds(options, EndpointSafety.ValidateRoot(options.ODataRoot));
    }
    private static void ValidateBounds(SuccessFactorsOptions options, Uri root)
    {
        if (!root.AbsolutePath.EndsWith("/odata/v2/", StringComparison.Ordinal) || options.PageSize is < 1 or > 1000 || options.MaxPages is < 1 or > 100000)
            throw new ArgumentException("An OData V2 root and bounded page configuration are required.", nameof(options));
    }
    /// <summary>Classifies a raw transport failure into the structured <see cref="Koras.Connect.Exceptions.IntegrationException"/>
    /// hierarchy so callers can distinguish authentication (401), RBP/authorization denial (403), throttling (429)
    /// and transient transport (5xx) failures. Applied to read paths (enumeration, metadata); <see cref="UpsertAsync"/>
    /// intentionally continues to surface the raw <see cref="IntegrationRequestException"/> so existing row-level
    /// upsert diagnostics are unaffected.</summary>
    private static Koras.Connect.Exceptions.IntegrationException Classify(IntegrationRequestException ex)
        => Koras.Connect.Exceptions.IntegrationExceptionClassifier.Classify(ex.ResponseStatus, ex.Attempts, correlationId: null);
    public IAsyncEnumerable<PerPerson> EnumeratePersonsAsync(ODataQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<PerPerson>("PerPerson", query, cancellationToken);
    /// <summary>Convenience wrapper over <see cref="EnumerateAsync{T}"/> for the EmpEmployment entity set.</summary>
    public IAsyncEnumerable<EmpEmployment> EnumerateEmploymentAsync(ODataQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<EmpEmployment>("EmpEmployment", query, cancellationToken);
    /// <summary>Convenience wrapper over <see cref="EnumerateAsync{T}"/> for the EmpJob entity set.</summary>
    public IAsyncEnumerable<EmpJob> EnumerateJobsAsync(ODataQuery? query = null, CancellationToken cancellationToken = default)
        => EnumerateAsync<EmpJob>("EmpJob", query, cancellationToken);
    public async IAsyncEnumerable<T> EnumerateAsync<T>(string entity, ODataQuery? query = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var next = QueryAddress(entity, query ?? new());
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var page = 0; ; page++)
        {
            if (page >= _options.MaxPages || !visited.Add(next.AbsoluteUri)) throw new InvalidDataException("OData pagination limit or cycle detected.");
            JsonDocument document;
            try { document = await _transport.ReadJsonAsync(next, cancellationToken: cancellationToken); }
            catch (IntegrationRequestException ex) { throw Classify(ex); }
            using (document)
            {
                var d = document.RootElement.GetProperty("d");
                foreach (var item in d.GetProperty("results").EnumerateArray())
                { cancellationToken.ThrowIfCancellationRequested(); yield return item.Deserialize<T>() ?? throw new JsonException("Null OData entity."); }
                if (!d.TryGetProperty("__next", out var link) || string.IsNullOrWhiteSpace(link.GetString())) yield break;
                next = EndpointSafety.Continuation(_root, link.GetString()!);
            }
        }
    }
    /// <summary>Explicitly enabled upsert of a single configured entity. RBP/target-population checks remain enforced by SAP.
    /// Results can contain row failures even when HTTP status is 200; inspect every Succeeded property.</summary>
    public async Task<IReadOnlyList<UpsertResult>> UpsertAsync(string entity, IEnumerable<JsonElement> records, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying entity upsert support and RBP permissions.");
        ValidateEntity(entity);
        var rows = records.Take(1001).ToArray();
        if (rows.Length is < 1 or > 1000) throw new ArgumentException("Upsert requires 1–1000 records.", nameof(records));
        var payloads = new List<Dictionary<string, JsonElement>>();
        foreach (var row in rows)
        {
            if (row.ValueKind != JsonValueKind.Object || row.TryGetProperty("__metadata", out _)) throw new ArgumentException("Supply entity fields without __metadata.", nameof(records));
            var values = row.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
            values["__metadata"] = JsonSerializer.SerializeToElement(new { uri = entity }); payloads.Add(values);
        }
        using var doc = await _transport.SendJsonAsync(HttpMethod.Post, new Uri(_root, "upsert?$format=json"), JsonSerializer.SerializeToElement(payloads), cancellationToken: cancellationToken);
        var d = doc.RootElement.GetProperty("d");
        var results = d.ValueKind == JsonValueKind.Array ? d : d.GetProperty("results");
        return results.Deserialize<List<UpsertResult>>() ?? throw new JsonException("Missing upsert results.");
    }
    private Uri QueryAddress(string entity, ODataQuery query)
    {
        ValidateEntity(entity);
        if (query.AsOfDate is not null && (query.FromDate is not null || query.ToDate is not null) || query.FromDate > query.ToDate)
            throw new ArgumentException("Use asOfDate or a valid effective-date range.", nameof(query));
        if (query.Paging is not null && query.Paging is not ("snapshot" or "cursor")) throw new ArgumentException("Unsupported paging mode.", nameof(query));
        var parts = new List<string> { "$format=json", $"customPageSize={_options.PageSize}" };
        void Add(string name, string? value) { if (value is not null) parts.Add(name + "=" + Uri.EscapeDataString(value)); }
        Add("$filter", query.Filter); Add("$select", query.Select); Add("$expand", query.Expand); Add("paging", query.Paging);
        Add("asOfDate", query.AsOfDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Add("fromDate", query.FromDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        Add("toDate", query.ToDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        return new Uri(_root, entity + "?" + string.Join("&", parts));
    }
    private static void ValidateEntity(string entity)
    { if (string.IsNullOrWhiteSpace(entity) || !entity.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') || !char.IsAsciiLetter(entity[0])) throw new ArgumentException("Entity must be a metadata entity-set name.", nameof(entity)); }
}

/// <summary>Employment fields with SAP date values retained in their original OData representation.</summary>
public sealed class EmpEmployment
{
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("personIdExternal")] public string? PersonIdExternal { get; set; }
    [JsonPropertyName("startDate")] public JsonElement? StartDate { get; set; }
    [JsonPropertyName("endDate")] public JsonElement? EndDate { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed class EmpJob
{
    [JsonPropertyName("userId")] public string? UserId { get; set; }
    [JsonPropertyName("seqNumber"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] public long? SequenceNumber { get; set; }
    [JsonPropertyName("startDate")] public JsonElement? StartDate { get; set; }
    [JsonPropertyName("managerId")] public string? ManagerId { get; set; }
    [JsonPropertyName("department")] public string? Department { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }
    [JsonPropertyName("jobTitle")] public string? JobTitle { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

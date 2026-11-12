using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Koras.Connect.Abstractions;
using Koras.Connect.Transport;
namespace Koras.Connect.Qualtrics;

/// <summary>Tenant API v3 root and bounded pagination configuration. <see cref="Validate"/> mirrors the checks the
/// <see cref="QualtricsClient"/> constructor already performs so dependency-injection registration can reject an
/// unsafe root or an out-of-range page count before any <see cref="HttpClient"/> is built.</summary>
public sealed record QualtricsOptions(Uri ApiRoot, int MaxPages = 10000) : IValidatableOptions
{
    /// <summary>Throws <see cref="OptionsValidationException"/> describing every violated field. Never performs I/O.</summary>
    public void Validate()
    {
        var failures = new List<string>();
        Uri? safeRoot = null;
        try { safeRoot = EndpointSafety.ValidateRoot(ApiRoot); }
        catch (ArgumentException) { failures.Add("ApiRoot must be an absolute HTTPS tenant URI without embedded credentials, query or fragment."); }
        if (safeRoot is not null && !safeRoot.AbsolutePath.EndsWith("/API/v3/", StringComparison.Ordinal))
            failures.Add("ApiRoot must point at the tenant's /API/v3/ root.");
        if (MaxPages is < 1 or > 100000) failures.Add("MaxPages must be between 1 and 100000.");
        if (failures.Count > 0) throw new OptionsValidationException(nameof(QualtricsOptions), failures);
    }
}
public sealed class SurveySummary
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("ownerId")] public string? OwnerId { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; }
    [JsonPropertyName("lastModified")] public DateTimeOffset? LastModified { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
public sealed record ResponseExportOptions(string Format = "csv", bool Compress = true, string? FilterId = null,
    DateTimeOffset? StartDate = null, DateTimeOffset? EndDate = null, string? ContinuationToken = null)
{
    public override string ToString() => "ResponseExportOptions { values=[REDACTED] }";
}
public sealed class ExportProgress
{
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("percentComplete")] public double PercentComplete { get; set; }
    [JsonPropertyName("fileId")] public string? FileId { get; set; }
    [JsonPropertyName("continuationToken")] public string? ContinuationToken { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
/// <summary>Qualtrics API v3. Use Create to configure X-API-TOKEN authentication and a redirect-disabled HTTP client.</summary>
public sealed partial class QualtricsClient
{
    private readonly IntegrationTransport _transport;
    private readonly Uri _root;
    private readonly int _maxPages;
    public QualtricsClient(IntegrationTransport transport, QualtricsOptions options)
    {
        _transport = transport; _root = EndpointSafety.ValidateRoot(options.ApiRoot); _maxPages = options.MaxPages;
        if (!_root.AbsolutePath.EndsWith("/API/v3/", StringComparison.Ordinal) || _maxPages is < 1 or > 100000)
            throw new ArgumentException("Configure an explicit tenant API v3 root and bounded page count.", nameof(options));
    }
    public static QualtricsClient Create(HttpClient http, IAccessTokenSource apiTokens, QualtricsOptions options, TransportOptions? transport = null)
        => new(new IntegrationTransport(http, new HeaderTokenAuthenticator("X-API-TOKEN", apiTokens), transport), options);
    public IAsyncEnumerable<SurveySummary> EnumerateSurveysAsync(CancellationToken cancellationToken = default)
        => QualtricsPaging.EnumerateAsync<SurveySummary>(_root, new Uri(_root, "surveys"), _maxPages, _transport, cancellationToken);
    /// <summary>Fetches exactly one page of surveys. Pass the <see cref="Page{T}.Next"/> continuation from a previous call
    /// as <paramref name="resumeFrom"/> to pause and resume later rather than re-iterating <see cref="EnumerateSurveysAsync"/>
    /// from the beginning.</summary>
    public Task<Page<SurveySummary>> ListSurveysPageAsync(ContinuationToken? resumeFrom = null, CancellationToken cancellationToken = default)
    {
        var address = resumeFrom is null ? new Uri(_root, "surveys") : QualtricsPaging.ResolveResume(_root, "surveys", resumeFrom);
        return QualtricsPaging.FetchPageAsync<SurveySummary>("surveys", _root, address, _transport, cancellationToken);
    }
    public Task<JsonDocument> GetSurveyAsync(string surveyId, CancellationToken cancellationToken = default)
        => _transport.ReadJsonAsync(SurveyAddress(surveyId), cancellationToken: cancellationToken);
    /// <summary>Starts an asynchronous export. POST is never automatically replayed.</summary>
    public async Task<string> StartResponseExportAsync(string surveyId, ResponseExportOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        if (options.Format is not ("csv" or "tsv" or "json" or "ndjson" or "spss" or "xml") || options.StartDate > options.EndDate)
            throw new ArgumentException("Unsupported export format or invalid date range.", nameof(options));
        var payload = new Dictionary<string, object?> { ["format"] = options.Format, ["compress"] = options.Compress };
        if (options.FilterId is not null) payload["filterId"] = options.FilterId;
        if (options.StartDate is not null) payload["startDate"] = options.StartDate.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
        if (options.EndDate is not null) payload["endDate"] = options.EndDate.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
        if (options.ContinuationToken is not null) { payload["continuationToken"] = options.ContinuationToken; payload["allowContinuation"] = true; }
        using var doc = await _transport.SendJsonAsync(HttpMethod.Post, SurveyAddress(surveyId, "/export-responses"), JsonSerializer.SerializeToElement(payload), cancellationToken: cancellationToken);
        var id = doc.RootElement.GetProperty("result").GetProperty("progressId").GetString();
        if (string.IsNullOrWhiteSpace(id)) throw new JsonException("Missing export progress ID."); return id;
    }
    public async Task<ExportProgress> GetExportProgressAsync(string surveyId, string progressId, CancellationToken cancellationToken = default)
    {
        using var doc = await _transport.ReadJsonAsync(SurveyAddress(surveyId, "/export-responses/" + EndpointSafety.Segment(progressId)), cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("result").Deserialize<ExportProgress>() ?? throw new JsonException("Missing export progress.");
    }
    /// <summary>Bounded polling without persistence or background infrastructure. Caller can resume using GetExportProgressAsync.</summary>
    public async Task<ExportProgress> WaitForExportAsync(string surveyId, string progressId, TimeSpan? pollingInterval = null,
        int maxPolls = 120, CancellationToken cancellationToken = default)
    {
        var delay = pollingInterval ?? TimeSpan.FromSeconds(2);
        if (delay < TimeSpan.Zero || delay > TimeSpan.FromMinutes(5) || maxPolls is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(maxPolls));
        for (var poll = 0; poll < maxPolls; poll++)
        {
            var result = await GetExportProgressAsync(surveyId, progressId, cancellationToken);
            if (result.Status == "complete")
            { if (string.IsNullOrWhiteSpace(result.FileId)) throw new JsonException("Completed export has no file ID."); return result; }
            if (result.Status == "failed") throw new InvalidOperationException("Qualtrics export failed. Inspect provider diagnostics securely.");
            if (result.Status != "inProgress") throw new JsonException("Unknown export state.");
            if (poll + 1 < maxPolls) await Task.Delay(delay, cancellationToken);
        }
        throw new TimeoutException("Export polling limit reached; resume with the existing progress ID rather than starting another job.");
    }
    /// <summary>Streams raw compressed/uncompressed export bytes; does not extract archives or close the destination.</summary>
    public Task DownloadExportAsync(string surveyId, string fileId, Stream output, CancellationToken cancellationToken = default)
        => _transport.CopyToAsync(SurveyAddress(surveyId, "/export-responses/" + EndpointSafety.Segment(fileId) + "/file"), output, cancellationToken);
    private Uri SurveyAddress(string id, string suffix = "") => new(_root, "surveys/" + EndpointSafety.Segment(id) + suffix);
}

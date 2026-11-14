using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

/// <summary>Bulk ingest operation (SFD-010). <c>Upsert</c> requires <c>externalIdFieldName</c> on job creation.</summary>
public enum SalesforceBulkIngestOperation { Insert, Update, Upsert, Delete, HardDelete }

/// <summary>A created Bulk API 2.0 job (ingest or query, SFD-010/011). <see cref="Operation"/> is <c>null</c> for
/// a query job - reused across both job kinds per the proposed contract rather than introducing a second,
/// near-identical handle type.</summary>
public sealed record SalesforceBulkJobHandle(string JobId, SalesforceBulkIngestOperation? Operation = null);

public sealed partial class SalesforceClient
{
    private static void ValidateJobId(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId) || !RecordIdPattern.IsMatch(jobId))
            throw new ArgumentException("A Salesforce job id must be 15 or 18 alphanumeric characters.", nameof(jobId));
    }

    private static string IngestOperationName(SalesforceBulkIngestOperation operation) => operation switch
    {
        SalesforceBulkIngestOperation.Insert => "insert",
        SalesforceBulkIngestOperation.Update => "update",
        SalesforceBulkIngestOperation.Upsert => "upsert",
        SalesforceBulkIngestOperation.Delete => "delete",
        SalesforceBulkIngestOperation.HardDelete => "hardDelete",
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    /// <summary>Creates a Bulk API 2.0 ingest job: <c>POST .../jobs/ingest</c> (SFD-010). Disabled unless
    /// <paramref name="writesEnabled"/> is explicitly <c>true</c> - every ingest operation, including a bulk
    /// insert, is a write.</summary>
    public async Task<SalesforceBulkJobHandle> CreateIngestJobAsync(string sObject, SalesforceBulkIngestOperation operation,
        string? externalIdFieldName = null, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ValidateSObject(sObject);
        if (operation == SalesforceBulkIngestOperation.Upsert && string.IsNullOrWhiteSpace(externalIdFieldName))
            throw new ArgumentException("externalIdFieldName is required for an upsert ingest job.", nameof(externalIdFieldName));
        var body = new Dictionary<string, object?> { ["object"] = sObject, ["operation"] = IngestOperationName(operation), ["contentType"] = "CSV" };
        if (!string.IsNullOrWhiteSpace(externalIdFieldName)) body["externalIdFieldName"] = externalIdFieldName;
        var payload = JsonSerializer.SerializeToElement(body);
        var address = new Uri(_root, "jobs/ingest");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return new SalesforceBulkJobHandle(RequireString(document.RootElement, "id"), operation); }
    }

    /// <summary>Uploads a batch of CSV rows to an open ingest job: <c>PUT .../jobs/ingest/{jobId}/batches</c>
    /// (SFD-010), streamed via <see cref="IntegrationTransport.UploadAsync"/> without whole-file buffering.</summary>
    public async Task UploadIngestBatchAsync(SalesforceBulkJobHandle job, Stream csv, bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var address = new Uri(_root, $"jobs/ingest/{job.JobId}/batches");
        try { await _transport.UploadAsync(HttpMethod.Put, address, csv, "text/csv", cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Closes an ingest job for processing: <c>PATCH .../jobs/ingest/{jobId}</c> with
    /// <c>{"state":"UploadComplete"}</c> (SFD-010).</summary>
    public Task CloseIngestJobAsync(SalesforceBulkJobHandle job, bool writesEnabled = false, CancellationToken cancellationToken = default)
        => PatchIngestJobStateAsync(job, "UploadComplete", writesEnabled, cancellationToken);

    /// <summary>Aborts an open or in-progress ingest job: <c>PATCH .../jobs/ingest/{jobId}</c> with
    /// <c>{"state":"Aborted"}</c> (SFD-010).</summary>
    public Task AbortIngestJobAsync(SalesforceBulkJobHandle job, bool writesEnabled = false, CancellationToken cancellationToken = default)
        => PatchIngestJobStateAsync(job, "Aborted", writesEnabled, cancellationToken);

    private async Task PatchIngestJobStateAsync(SalesforceBulkJobHandle job, string state, bool writesEnabled, CancellationToken cancellationToken)
    {
        if (!writesEnabled) throw new InvalidOperationException("Enable writes only after verifying the object privilege and business impact.");
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var address = new Uri(_root, $"jobs/ingest/{job.JobId}");
        var payload = JsonSerializer.SerializeToElement(new { state });
        try { await _transport.SendJsonAsync(HttpMethod.Patch, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }

    /// <summary>Reads the current job state (<c>Open</c>/<c>UploadComplete</c>/<c>InProgress</c>/<c>JobComplete</c>/
    /// <c>Failed</c>/<c>Aborted</c>) via <c>GET .../jobs/ingest/{jobId}</c> (SFD-010). A <c>JobComplete</c> state
    /// does not by itself mean every record succeeded - inspect <see cref="DownloadFailedResultsAsync"/> too.</summary>
    public async Task<string> GetIngestJobStateAsync(SalesforceBulkJobHandle job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var address = new Uri(_root, $"jobs/ingest/{job.JobId}");
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "state"); }
    }

    /// <summary>Streams the successfully processed rows (CSV) via <c>GET .../jobs/ingest/{jobId}/successfulResults/</c> (SFD-010).</summary>
    public Task DownloadSuccessfulResultsAsync(SalesforceBulkJobHandle job, Stream destination, CancellationToken cancellationToken = default)
        => DownloadIngestResultsAsync(job, "successfulResults", destination, cancellationToken);

    /// <summary>Streams the rows that failed processing (CSV) via <c>GET .../jobs/ingest/{jobId}/failedResults/</c>
    /// (SFD-010). Must be checked even when <see cref="GetIngestJobStateAsync"/> reports <c>JobComplete</c>.</summary>
    public Task DownloadFailedResultsAsync(SalesforceBulkJobHandle job, Stream destination, CancellationToken cancellationToken = default)
        => DownloadIngestResultsAsync(job, "failedResults", destination, cancellationToken);

    /// <summary>Streams rows never attempted (for example after an abort), via
    /// <c>GET .../jobs/ingest/{jobId}/unprocessedrecords/</c> (SFD-010).</summary>
    public Task DownloadUnprocessedRecordsAsync(SalesforceBulkJobHandle job, Stream destination, CancellationToken cancellationToken = default)
        => DownloadIngestResultsAsync(job, "unprocessedrecords", destination, cancellationToken);

    private async Task DownloadIngestResultsAsync(SalesforceBulkJobHandle job, string resultPath, Stream destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var address = new Uri(_root, $"jobs/ingest/{job.JobId}/{resultPath}/");
        try { await _transport.CopyToAsync(address, destination, cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
    }
}

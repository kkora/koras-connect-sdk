using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Koras.Connect.Exceptions;
using Koras.Connect.Transport;

namespace Koras.Connect.Salesforce;

public sealed partial class SalesforceClient
{
    /// <summary>Creates a Bulk API 2.0 query job: <c>POST .../jobs/query</c> with <c>{"operation":"query"|"queryAll","query":soql}</c>
    /// (SFD-011). <paramref name="includeDeleted"/> selects <c>queryAll</c>, which also returns archived/deleted rows. Read-only;
    /// no <c>writesEnabled</c> opt-in required.</summary>
    public async Task<SalesforceBulkJobHandle> CreateQueryJobAsync(string soql, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(soql)) throw new ArgumentException("A SOQL query is required.", nameof(soql));
        var payload = JsonSerializer.SerializeToElement(new { operation = includeDeleted ? "queryAll" : "query", query = soql });
        var address = new Uri(_root, "jobs/query");
        JsonDocument document;
        try { document = await _transport.SendJsonAsync(HttpMethod.Post, address, payload, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return new SalesforceBulkJobHandle(RequireString(document.RootElement, "id")); }
    }

    /// <summary>Reads the current query-job state (<c>UploadComplete</c>/<c>InProgress</c>/<c>JobComplete</c>/<c>Failed</c>/
    /// <c>Aborted</c>) via <c>GET .../jobs/query/{jobId}</c> (SFD-011).</summary>
    public async Task<string> GetQueryJobStateAsync(SalesforceBulkJobHandle job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var address = new Uri(_root, $"jobs/query/{job.JobId}");
        JsonDocument document;
        try { document = await _transport.ReadJsonAsync(address, cancellationToken: cancellationToken); }
        catch (IntegrationRequestException ex) { throw Classify(ex); }
        using (document) { return RequireString(document.RootElement, "state"); }
    }

    /// <summary>Streams every CSV results page for a completed query job via <c>GET .../jobs/query/{jobId}/results</c>
    /// (SFD-011), following the <c>Sforce-Locator</c> response header as an opaque cursor. Termination is the literal
    /// response-header value <c>"null"</c> - a genuinely absent header is treated as a contract violation, not as the
    /// end of the result set, per this feature's documented off-by-one hazard. <paramref name="maxRecords"/> is passed
    /// through verbatim when supplied; this SDK does not assume or assert an unconfirmed server-side default.</summary>
    public async IAsyncEnumerable<Stream> EnumerateQueryResultsAsync(SalesforceBulkJobHandle job, int? maxRecords = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(job.JobId);
        var seenLocators = new HashSet<string>(StringComparer.Ordinal);
        string? locator = null;
        while (true)
        {
            var query = new StringBuilder();
            if (maxRecords is { } limit) query.Append(query.Length == 0 ? '?' : '&').Append("maxRecords=").Append(limit);
            if (locator is not null) query.Append(query.Length == 0 ? '?' : '&').Append("locator=").Append(Uri.EscapeDataString(locator));
            var address = new Uri(_root, $"jobs/query/{job.JobId}/results{query}");
            IntegrationRawResponse response;
            try { response = await _transport.SendRawAsync(HttpMethod.Get, address, body: null, contentType: null,
                headers: new Dictionary<string, string> { ["Accept"] = "text/csv" }, cancellationToken: cancellationToken); }
            catch (IntegrationRequestException ex) { throw Classify(ex); }
            yield return new MemoryStream(Encoding.UTF8.GetBytes(response.Body));
            if (!response.Headers.TryGetValue("sforce-locator", out var values) || values.Length == 0)
                throw new ContractException("Response did not carry the required Sforce-Locator header.", new ErrorDetails("bulk_query_results_contract"));
            var next = values[0];
            if (next == "null") yield break;
            if (!seenLocators.Add(next))
                throw new ContractException("Sforce-Locator repeated a previously seen cursor; refusing to loop forever.", new ErrorDetails("bulk_query_results_cycle"));
            locator = next;
        }
    }
}

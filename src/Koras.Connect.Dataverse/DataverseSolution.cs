namespace Koras.Connect.Dataverse;

public sealed partial class DataverseClient
{
    /// <summary>Creates a row attached to a specific unmanaged solution, via the documented
    /// <c>MSCRM.SolutionUniqueName</c> header, instead of the tenant's default solution (DVT-019). Solution-component
    /// operations typically require a System Customizer/System Administrator-level security role, a materially
    /// higher entitlement bar than any other write in this connector - callers should expect a 403 here more often
    /// than on ordinary table writes. <c>ImportSolution</c>/<c>ExportSolution</c> action signatures are not
    /// implemented - their shapes were not independently re-fetched this session (documented gap, Section 28).</summary>
    public Task<Guid> CreateRowInSolutionAsync(string entitySet, System.Text.Json.JsonElement fields, string solutionUniqueName,
        bool writesEnabled = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionUniqueName) || solutionUniqueName.Any(c => c is '\r' or '\n'))
            throw new ArgumentException("A single-line solution unique name is required.", nameof(solutionUniqueName));
        var headers = new Dictionary<string, string>(RequiredHeaders) { ["MSCRM.SolutionUniqueName"] = solutionUniqueName };
        return CreateRowCoreAsync(entitySet, fields, headers, writesEnabled, cancellationToken);
    }
}

// DVT-020 (virtual table capability discovery) is intentionally not implemented in this pass. The feature's own
// specification is explicit that the metadata field distinguishing a virtual table from a native one was not
// independently fetched/confirmed this session, and directs: "do not implement a guessed field name" and "cannot
// be marked beyond Not started/Planned until the specific metadata field ... is independently verified." Writing
// GetVirtualTableInfoAsync against an invented EntityDefinitions field name would violate that directive outright,
// so this feature stays blocked - matching the SAP-007 precedent elsewhere in this SDK's history - rather than
// shipping a plausible-looking but unverified contract.

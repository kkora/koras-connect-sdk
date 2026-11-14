---
id: "SFD-010"
title: "Bulk API 2.0 ingest"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-010 - Bulk API 2.0 ingest

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-010 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-010.
2. **Feature name:** Bulk API 2.0 ingest.
3. **Description:** Insert, update, upsert, or delete very large record volumes efficiently via CSV-based asynchronous jobs, instead of one-record-at-a-time REST calls.
4. **Business value:** Insert, update, upsert, or delete very large record volumes efficiently via CSV-based asynchronous jobs, instead of one-record-at-a-time REST calls.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Phase-1-reusable; this is a fundamentally different (async job + CSV) protocol from every existing connector's synchronous-request model.
10. **Functional requirements:** Create job: `POST /services/data/vXX.X/jobs/ingest` with `object`, `operation` (`insert`/`update`/`upsert`/`delete`/`hardDelete`), optional `externalIdFieldName` (required for upsert), `contentType`, `lineEnding`, `columnDelimiter` [snippet]. Upload: `PUT /services/data/vXX.X/jobs/ingest/{jobId}/batches`, `Content-Type: text/csv`, raw CSV body [snippet]. Close: `PATCH /services/data/vXX.X/jobs/ingest/{jobId}` with `{"state":"UploadComplete"}` (or `"Aborted"` to cancel) [snippet]. Job states: `Open → UploadComplete → InProgress → JobComplete/Failed/Aborted` [snippet]. Results: `GET .../jobs/ingest/{jobId}/successfulResults|failedResults|unprocessedrecords` (CSV) [snippet]. Limits, with one item independently corroborated via a help.salesforce.com article (id 000382576): **15,000 batches per rolling 24 hours** (shared with legacy Bulk API 1.0) **[corroborated]**; **150 MB file size cap per job**, **max 10,000 records per batch**, theoretical **150,000,000 records/24h** ceiling **[snippet, not independently corroborated]**.
11. **Nonfunctional requirements:** Streamed CSV upload (no whole-file buffering); cancellation-aware polling for job completion; explicit write opt-in gate.
12. **Public API contracts or proposed contracts:** Official API: All endpoint shapes above are snippet-sourced from `developer.salesforce.com/docs/platform/api-asynch/guide/{close-job,bulk-api-2-job-states,bulk-api-2-0-ingest}.html`; the 15,000-batches/24h figure is independently corroborated via a help.salesforce.com article, the remaining numeric limits are not. Proposed C# contracts:

```csharp
public enum SalesforceBulkIngestOperation { Insert, Update, Upsert, Delete, HardDelete }
public sealed record SalesforceBulkJobHandle(string JobId, SalesforceBulkIngestOperation Operation);
public async Task<SalesforceBulkJobHandle> CreateIngestJobAsync(string sObject, SalesforceBulkIngestOperation operation,
    string? externalIdFieldName = null, bool writesEnabled = false, CancellationToken ct = default);
public async Task UploadIngestBatchAsync(SalesforceBulkJobHandle job, Stream csv, bool writesEnabled = false, CancellationToken ct = default);
public async Task CloseIngestJobAsync(SalesforceBulkJobHandle job, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** SFD-003, SFD-007 (to validate field names before an expensive bulk job).
14. **Security considerations:** Authentication and authorization: Same bearer token as REST; object-level Create/Update/Delete permission per the chosen operation. Required permissions/scopes/licenses: Bulk API must be enabled on the org edition (SFD-022); standard object permissions for the chosen operation. Data privacy and security: CSV payloads carry business data in bulk; never logged by default; streamed, not buffered, to bound memory.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Job-state polling, not request pagination; failed/unprocessed result retrieval is itself a CSV stream, not a JSON page. Error handling and concurrency: A job in `Failed` state surfaces distinctly from `JobComplete`-with-some-failed-records (the latter requires checking `failedResults` even though the job itself "completed").
16. **Unit-test requirements:** Job creation/upload/close URL and body composition match the documented shapes; state-machine transitions handled correctly (including `Aborted`); a job reporting `JobComplete` with nonzero `failedResults` is not treated as fully successful.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for the full job lifecycle, explicitly labeled snippet-confidence pending direct re-fetch. Integration-test requirements: Authorized sandbox org with Bulk API enabled; not available this pass.
18. **Acceptance criteria:** The full create→upload→close→poll→results lifecycle round-trips against synthetic fixtures.
19. **Definition of Done:** Standard template; exact batch-count-vs-filesize interaction (whether a single `/batches` PUT is itself capped at 10,000 rows independent of the 150MB cap) remains an explicit open gap (Section 28).
20. **External API entitlement or documentation blockers:** Most of this feature's numeric limits and field names are snippet-only; re-verify before marking `verified`.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [ ] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; no Phase 2 implementation exists yet |
| Validation commands / results | Not recorded; no Phase 2 implementation exists yet |
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Most of this feature's numeric limits and field names are snippet-only; re-verify before marking `verified`. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

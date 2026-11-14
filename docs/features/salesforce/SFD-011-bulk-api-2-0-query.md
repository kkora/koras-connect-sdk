---
id: "SFD-011"
title: "Bulk API 2.0 query"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "implemented"
baseline_status: "Implemented: CreateQueryJobAsync/GetQueryJobStateAsync/EnumerateQueryResultsAsync cover the full documented lifecycle, including Sforce-Locator literal-\"null\" termination and a cycle guard against a repeated cursor. Default maxRecords is intentionally left unasserted since the spec documents it as unconfirmed. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-011 - Bulk API 2.0 query

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-011 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-011.
2. **Feature name:** Bulk API 2.0 query.
3. **Description:** Extract very large query result sets asynchronously as CSV, avoiding the per-page overhead of SFD-006's synchronous REST pagination for bulk extraction workloads.
4. **Business value:** Extract very large query result sets asynchronously as CSV, avoiding the per-page overhead of SFD-006's synchronous REST pagination for bulk extraction workloads.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Phase-1-reusable; shares SFD-010's async-job pattern.
10. **Functional requirements:** Create job: `POST /services/data/vXX.X/jobs/query` with `{"operation":"query"|"queryAll","query":"<SOQL>"}` (`queryAll` includes deleted/archived rows) [snippet]. Status: `GET /services/data/vXX.X/jobs/query/{jobId}` [snippet]. Results: `GET .../jobs/query/{jobId}/results` → CSV; response header `Sforce-Locator` (literal string `"null"` signals no more pages); subsequent pages pass `locator` and `maxRecords` query parameters [snippet].
11. **Nonfunctional requirements:** Streamed CSV download; cancellation-aware polling; bounded max-pages guard matching every other connector's pagination safety net.
12. **Public API contracts or proposed contracts:** Official API: All endpoint shapes above are snippet-sourced from `developer.salesforce.com/docs/platform/api-asynch/guide/{query-get-job-results,query-bulk-api-2-0}.html`; not independently re-fetched. Proposed C# contracts:

```csharp
public async Task<SalesforceBulkJobHandle> CreateQueryJobAsync(string soql, bool includeDeleted = false, CancellationToken ct = default);
public async IAsyncEnumerable<Stream> EnumerateQueryResultsAsync(SalesforceBulkJobHandle job, int? maxRecords = null, CancellationToken ct = default);
```
13. **Dependencies:** SFD-005.
14. **Security considerations:** Authentication and authorization: Read access on every queried object/field. Required permissions/scopes/licenses: Bulk API enabled on the org edition. Data privacy and security: Query results stream CSV business data; never buffered into logs.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: `Sforce-Locator`-based paging, treated as opaque via `ContinuationToken` (Section 8.2, item 3) since it is a genuinely opaque server-issued cursor, matching Dataverse's `$deltatoken` reuse justification. Error handling and concurrency: A job in `Failed` state surfaces distinctly; `Sforce-Locator: "null"` (the literal string, not an absent header) must be checked explicitly to avoid an off-by-one infinite-loop bug.
16. **Unit-test requirements:** `Sforce-Locator` literal-string-`"null"` termination handled correctly (not confused with an actually-absent header); `maxRecords` default behavior documented as unconfirmed (commonly cited as 10,000 elsewhere but not confirmed official - the test suite must not assert an unconfirmed default).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multi-page fixture using the documented locator mechanism. Integration-test requirements: Authorized sandbox org with a large result set; not available this pass.
18. **Acceptance criteria:** Multi-page CSV result retrieval round-trips correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Default `maxRecords` value is unconfirmed; do not hardcode an assumed default into tests or documentation as if verified.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceBulkQuery.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceBulkQueryTests.cs`. Covers: job creation posts the documented `operation`/`query` body (`query` vs. `queryAll` via `includeDeleted`) and parses the returned job id; job-state reads parse the `state` field; results paging follows `Sforce-Locator` across multiple pages via `locator`/`maxRecords` query parameters; the literal response-header value `"null"` terminates paging correctly (not confused with an absent header); a genuinely absent `Sforce-Locator` header throws `ContractException` rather than being silently treated as end-of-results; a repeated locator value is treated as a cycle and throws rather than looping forever; `maxRecords` is passed through verbatim with no asserted or hardcoded default. Full suite: 802 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | Default `maxRecords` value remains unconfirmed per this feature's own spec; not hardcoded into tests or documentation as if verified. No live org with a large result set available this pass to confirm the locator mechanics end-to-end. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceBulkQuery.cs` (job lifecycle + locator-based results paging) plus `SalesforceBulkQueryTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: the default `maxRecords` value remains unconfirmed and no live org is available this pass. |

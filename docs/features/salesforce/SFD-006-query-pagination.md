---
id: "SFD-006"
title: "Query pagination"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-006 - Query pagination

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-006 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-006.
2. **Feature name:** Query pagination.
3. **Description:** Traverse a SOQL result set larger than one page without the caller re-implementing Salesforce's specific `nextRecordsUrl` mechanics.
4. **Business value:** Traverse a SOQL result set larger than one page without the caller re-implementing Salesforce's specific `nextRecordsUrl` mechanics.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `Page<T>`/`ContinuationToken`/`EndpointSafety.Continuation` reused unchanged (Section 8.1).
10. **Functional requirements:** Query response fields: `totalSize`, `done`, `records[]`, and `nextRecordsUrl` (present only when `done=false`), with the pattern `/services/data/vXX.X/query/{queryLocator}` [PDF, directly confirmed]. Default batch size **2,000** records; the `Sforce-Query-Options: batchSize=N` request header overrides it, confirmed bounds **min 200 / max 2,000**, with the documentation explicitly stating "no guarantee the requested size is the actual size" [PDF, directly confirmed]. The Global Describe response separately reports an org-level `maxBatchSize` (sample value 200 seen in documentation) whose relationship to the per-request `Sforce-Query-Options` override was **not reconciled in any fetched text** - this is an open, explicitly flagged gap, not resolved by assumption.
11. **Nonfunctional requirements:** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
12. **Public API contracts or proposed contracts:** Official API: `nextRecordsUrl`/`queryLocator`, `Sforce-Query-Options` header and bounds - directly confirmed [PDF]. Proposed C# contracts:

```csharp
public IAsyncEnumerable<T> EnumerateQueryAsync<T>(string soql, int? batchSize = null, CancellationToken ct = default);
```
13. **Dependencies:** SFD-005.
14. **Security considerations:** Authentication and authorization: Unchanged from SFD-005. Required permissions/scopes/licenses: None beyond SFD-005's. Data privacy and security: `queryLocator` treated as a sensitive opaque value, never logged.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Each page is a GET, reusing the shared GET-retry policy; `queryLocator` is treated as opaque and forwarded unmodified via `EndpointSafety.Continuation`. Error handling and concurrency: Cycle/max-page-count guard throws `InvalidDataException`; a `batchSize` outside [200, 2000] is rejected locally before send.
16. **Unit-test requirements:** `batchSize` outside documented bounds rejected locally; multi-page enumeration follows `nextRecordsUrl` correctly; an empty page with `done=false` is still followed (matching every existing connector's "empty page with continuation" test pattern).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multi-page fixture. Integration-test requirements: Authorized sandbox org with more than 2,000 matching records; not available this pass.
18. **Acceptance criteria:** A caller can enumerate an arbitrarily large query result via `IAsyncEnumerable<T>` without manually handling `nextRecordsUrl`.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The relationship between the org-level `maxBatchSize` (from Global Describe) and the per-request `Sforce-Query-Options` override was not reconciled in any fetched official text (Section 28 open question).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. The relationship between the org-level `maxBatchSize` (from Global Describe) and the per-request `Sforce-Query-Options` override was not reconciled in any fetched official text (Section 28 open question). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

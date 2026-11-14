---
id: "SFD-009"
title: "Composite API"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "partial"
baseline_status: "Implemented: ExecuteCompositeAsync covers /composite only (the one variant with a proposed concrete C# contract), with the documented 25-subrequest cap enforced locally and mixed per-subrequest outcomes reported via BatchItemResult<T>. /composite/batch and /composite/graph are not implemented - their field names are snippet-only and no concrete contract was proposed for them. Unit-tested; field-name re-verification against a live page remains an explicit release blocker per this feature's own spec."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-009 - Composite API

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-009 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-009.
2. **Feature name:** Composite API.
3. **Description:** Submit multiple dependent or independent operations in one HTTP round trip, trading off cross-referencing capability against batch size depending on which Composite variant is used.
4. **Business value:** Submit multiple dependent or independent operations in one HTTP round trip, trading off cross-referencing capability against batch size depending on which Composite variant is used.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `BatchItemResult<T>` (Section 8.2, item 2) reused for the mixed-outcome `/composite/batch` response shape; the multi-request envelope itself is new, connector-owned code.
10. **Functional requirements:** [snippet-only for exact field names below - not independently read from a live page; flagged accordingly, not treated as fully verified.] `POST /composite`: body `{allOrNone, compositeRequest:[{method,url,referenceId,body}]}`, response `{compositeResponse:[{body,httpHeaders,httpStatusCode,referenceId}]}`, supporting `@{referenceId.field}` cross-referencing between subrequests, **max 25 subrequests**, counted as **1 API call** [snippet]. `POST /composite/batch`: `{batchRequests:[{method,url,richInput}]}` → `{hasErrors, results:[{statusCode,result}]}`, **no cross-referencing** between subrequests, also max 25 subrequests [snippet, field names not independently re-verified]. `POST /composite/graph`: `{graphs:[{graphId, compositeRequest:[...]}]}`, with limits **max 75 graphs/payload, max 500 total nodes/payload, max 500 nodes/single graph, max depth 15, max 15 distinct node types/payload, stops after 14 graph failures** [snippet].
11. **Nonfunctional requirements:** Bounded subrequest-count validation locally (reject >25 for `/composite` and `/composite/batch`, >75 graphs for `/composite/graph`) before send, even though the exact field names carry only snippet-level confidence - the numeric limits are consistently repeated across independent queries and are treated as higher-confidence than the field-name spelling.
12. **Public API contracts or proposed contracts:** Official API: All three Composite variants and their numeric limits are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/{requests-composite,using-composite-resources,resources-composite-graph,resources-composite-graph-limits}.html` - a direct re-fetch is required before this feature ships as `verified`. Proposed C# contracts:

```csharp
public sealed record SalesforceCompositeSubrequest(HttpMethod Method, string Url, string ReferenceId, JsonElement? Body = null);
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteCompositeAsync(
    IReadOnlyList<SalesforceCompositeSubrequest> requests, bool allOrNone = false, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** SFD-003, SFD-004.
14. **Security considerations:** Authentication and authorization: Each subrequest is evaluated against the same object/field-level security as if sent standalone. Required permissions/scopes/licenses: Union of whatever the contained subrequests individually require. Data privacy and security: Composite bodies may contain business data across many records at once; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: The whole Composite call counts as a single API call against the daily limit (SFD-017); never auto-retried as a whole (it may contain writes). Error handling and concurrency: `/composite` with `allOrNone=true` rolls back every subrequest on any failure; `/composite/batch` always reports partial results via `hasErrors`/per-item `statusCode`, with the outer HTTP status remaining 200 regardless (SFD-019).
16. **Unit-test requirements:** >25 subrequests rejected locally for `/composite`/`/composite/batch`; `@{referenceId.field}` cross-reference substitution round-trips for `/composite`; `allOrNone` rollback vs. `/composite/batch`'s always-partial behavior are both exercised.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for all three Composite variants, explicitly labeled as built from snippet-confidence field names pending a direct re-fetch. Integration-test requirements: Authorized sandbox org; not available this pass.
18. **Acceptance criteria:** The three Composite shapes compose correctly against synthetic fixtures; a human re-verifies the exact field spelling against a live page or direct fetch before this feature is marked `verified` rather than `partial`.
19. **Definition of Done:** Standard template; field-name re-verification is an explicit release blocker (Section 28).
20. **External API entitlement or documentation blockers:** Exact JSON field names for all three Composite variants are snippet-only, not independently read from the official page this pass - the single largest confidence gap in the Salesforce catalog's P0 feature set.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (the `/composite` shape only - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (field-name re-verification is an explicit, unresolved release blocker per this feature's own spec).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceComposite.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceCompositeTests.cs`. Covers: >25 subrequests rejected locally; a write subrequest without `writesEnabled` throws before any HTTP call; a read-only composite needs no opt-in; the request payload carries `allOrNone` and the documented per-subrequest fields; mixed per-subrequest outcomes are reported individually via `BatchItemResult<T>`, never collapsed into one failure; unsafe `referenceId` values rejected locally; a malformed response surfaces `ContractException`. Full suite: 781 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | `/composite/batch` and `/composite/graph` are not implemented - their field names are snippet-only and this feature's own specification proposed a concrete typed C# contract only for `/composite`, so implementing the other two would mean guessing an unverified shape. All field names used (`compositeRequest`/`compositeResponse`/`httpStatusCode`/etc.) remain snippet-confidence pending a direct page re-fetch (Section 28), the stated release blocker for `verified`. No live org available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `ExecuteCompositeAsync`/`SalesforceCompositeSubrequest` for `/composite` plus `SalesforceCompositeTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the implemented scope; held at `partial` because `/composite/batch`/`/composite/graph` remain unbuilt and the snippet-sourced field names are unverified. |

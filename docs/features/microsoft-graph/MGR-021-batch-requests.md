---
id: "MGR-021"
title: "Batch requests"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: ExecuteBatchAsync enforces the 20-subrequest cap locally, composes the documented requests/dependsOn envelope, and maps each response back to its originating request by id (not response array position, since Graph does not guarantee response order), reporting mixed outcomes via BatchItemResult<T>. Per-sub-request 429 is surfaced via that item's ErrorCode for the caller to retry individually (MGR-022) - the whole $batch POST itself is never auto-retried. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-021 - Batch requests

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-021 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-021.
2. **Feature name:** Batch requests.
3. **Description:** Submit multiple independent or dependent Graph operations in a single HTTP round trip.
4. **Business value:** Submit multiple independent or dependent Graph operations in a single HTTP round trip.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `BatchItemResult<T>` reused for the mixed-outcome response shape (Section 8.2, item 2).
10. **Functional requirements:** `POST /$batch` [G7]. **Confirmed exactly, verified twice in the fetched documentation text**: maximum **20 requests per batch**. `dependsOn` array on a sub-request enforces sequential ordering relative to named sibling requests; a broken dependency chain (a request depending on one that failed) returns **`424 Failed Dependency`** for the dependent request [G7]. Each sub-request is **independently throttle-evaluated** - a batch is not a way to evade per-request rate limits, each contained call counts individually against the caller's throttling budget (MGR-022) [G7].
11. **Nonfunctional requirements:** Bounded batch size (reject >20 locally before send); explicit write opt-in gate on any batch containing a write.
12. **Public API contracts or proposed contracts:** Official API: `/$batch`, the 20-request max (confirmed twice), `dependsOn`/`424` semantics, and independent per-sub-request throttling - all per [G7]. Proposed C# contracts:

```csharp
public sealed record GraphBatchRequest(string Id, HttpMethod Method, string Url, JsonElement? Body = null, IReadOnlyList<string>? DependsOn = null);
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
    IReadOnlyList<GraphBatchRequest> requests, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** MGR-003.
14. **Security considerations:** Authentication and authorization: One bearer token covers the whole batch; individual sub-requests are still evaluated against the same permission requirements as if sent standalone. Required permissions/scopes/licenses: Union of whatever the contained sub-requests individually require. Data privacy and security: Batch bodies may contain business data across many resources at once; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Each sub-request's 429 is independent and must be retried individually using its own `Retry-After`, not at the batch level (MGR-022). Error handling and concurrency: A `dependsOn` chain broken by an earlier failure surfaces `424` for every downstream dependent request, distinguished from the original failure itself.
16. **Unit-test requirements:** >20 requests rejected locally; `dependsOn` ordering respected; a broken dependency chain surfaces `424` for dependents, not a generic failure; per-sub-request 429 handled independently, not batch-wide.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multi-request fixture including a `dependsOn` chain and a broken-dependency case. Integration-test requirements: Authorized tenant; not available this pass.
18. **Acceptance criteria:** The 20-request cap, `dependsOn` ordering, and `424` propagation all behave correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-tenant caveat.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphBatch.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphBatchTests.cs`. Covers: >20 requests and duplicate ids are both rejected locally before any HTTP call; a write sub-request without `writesEnabled` throws before any HTTP call; a read-only batch needs no opt-in; the request payload carries the documented `id`/`method`/`url`/`dependsOn` fields; results are mapped back to the caller's original request order by matching each response's `id` - not by response array position, since Graph does not guarantee response ordering - with a `424` surfacing as `http_424` for that item specifically; an unrecognized response id surfaces `ContractException`; a `429` sub-request is reported as `http_429` without the `/$batch` POST itself being retried (review pass 1 found this was documented as implemented but never actually tested - added `AFailedSubrequestWith429IsReportedAsHttp429NotRetriedLocally`); a sub-request failure with a parsable Graph error body now extracts the real `errorCode`/correlation id (`request-id`) via `GraphApiException.TryExtractErrorCode`/`TryExtractCorrelationId` (the latter added in review pass 1 - previously `ErrorCorrelationId` was always `null`). |
| Remaining work / blockers | No live tenant available this pass to confirm the documented `dependsOn`/`424` behavior end to end. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphBatch.cs` plus `GraphBatchTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 added the missing 429 sub-request test and wired real correlation-id extraction into per-sub-request failures (previously always `null`). No functional regression; closes two test-coverage/completeness gaps. |

---
id: "DVT-006"
title: "Pagination and continuation links"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-006 - Pagination and continuation links

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-006 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-006.
2. **Feature name:** Pagination and continuation links.
3. **Description:** Traverse a result set larger than one page without the caller re-implementing Dataverse's specific paging mechanics.
4. **Business value:** Traverse a result set larger than one page without the caller re-implementing Dataverse's specific paging mechanics.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `Page<T>`/`ContinuationToken`/`EndpointSafety.Continuation` reused unchanged (Section 8.1), matching the exact pattern `QualtricsPaging`/SAP SuccessFactors's `EnumerateAsync<T>` already implement for their own `nextPage`/`__next` links.
10. **Functional requirements:** Honor `Prefer: odata.maxpagesize=N` to request a specific page size, maximum **5,000** for both standard and elastic tables (larger values are documented as ignored by the server, not an error) [D7]. Parse `@odata.nextLink`, which embeds a Dataverse-proprietary, opaque, URL-encoded XML `$skiptoken=<cookie>` - treat it as fully opaque and forward it unmodified via `EndpointSafety.Continuation`, never attempt to decode or reconstruct it [D7]. `$top` and `Prefer: odata.maxpagesize` are mutually exclusive - if both are present, `$top` is documented as ignored by the server [D7]; the connector should reject supplying both locally rather than relying on server-side silent precedence. No `$skip`-based paging exists at all [D6, D7].
11. **Nonfunctional requirements:** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
12. **Public API contracts or proposed contracts:** Official API: `Prefer: odata.maxpagesize`, `@odata.nextLink`/`$skiptoken` per [D7]. Proposed C# contracts:

```csharp
public IAsyncEnumerable<T> EnumerateAsync<T>(string entitySet, DataverseQuery? query = null, int? maxPageSize = null, CancellationToken ct = default);
public Task<Page<T>> EnumeratePageAsync<T>(string entitySet, DataverseQuery? query = null, ContinuationToken? resumeFrom = null, int? maxPageSize = null, CancellationToken ct = default);
```
13. **Dependencies:** DVT-003, DVT-005.
14. **Security considerations:** Authentication and authorization: Unchanged. Required permissions/scopes/licenses: None beyond the underlying table read privilege. Data privacy and security: The `$skiptoken` is treated as a sensitive opaque value, matching `ContinuationToken`'s existing design intent - never logged.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: GET pages reuse the shared GET-retry policy; a `$skiptoken` is never retried across a different page boundary than the one it was issued for. Error handling and concurrency: Cycle/max-page-count guard throws `InvalidDataException`, matching every existing connector; an off-origin or malformed `@odata.nextLink` is rejected by `EndpointSafety.Continuation` unchanged.
16. **Unit-test requirements:** `Prefer: odata.maxpagesize` honored up to 5,000, values above 5,000 still function (server-ignored, not client-rejected, since the documented behavior is server-side silent capping not an error); simultaneous `$top`+`odata.maxpagesize` rejected locally; off-origin/malformed `@odata.nextLink` rejected; empty page with a non-null `@odata.nextLink` still followed (matching the "empty page with continuation" pattern already tested for SAP/Qualtrics).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multi-page fixture with a realistic opaque `$skiptoken` value. Integration-test requirements: Authorized sandbox table with more than 5,000 rows, to observe real pagination - not available this pass.
18. **Acceptance criteria:** A caller can enumerate an arbitrarily large table via `IAsyncEnumerable<T>` without manually handling `@odata.nextLink`.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact per-tenant/per-table row-count thresholds that trigger multi-page results are not independently verifiable without a live large table.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Exact per-tenant/per-table row-count thresholds that trigger multi-page results are not independently verifiable without a live large table. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

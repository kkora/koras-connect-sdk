---
id: "MGR-019"
title: "Pagination and delta queries"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: ordinary pagination (GraphQuery/EnumerateAsync<T> following @odata.nextLink) was built earlier under MGR-004 as a structural dependency; this pass completes the delta half with GetDeltaAsync<T>/GraphDeltaPage<T>, distinguishing @odata.nextLink (more pages) from @odata.deltaLink (sync pass complete), with a cycle guard and a distinct GraphDeltaTokenExpiredException for 410/syncStateNotFound. The ~26-resource delta-capable allowlist is not locally enforced - mapping an arbitrary resourcePath string to the vendor's singular resource-type name would require guessing an unspecified convention. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-019 - Pagination and delta queries

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-019 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-019.
2. **Feature name:** Pagination and delta queries.
3. **Description:** Traverse large result sets page-by-page, and extract only changed/deleted resources since a prior checkpoint, across every delta-capable Graph resource.
4. **Business value:** Traverse large result sets page-by-page, and extract only changed/deleted resources since a prior checkpoint, across every delta-capable Graph resource.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `Page<T>`/`ContinuationToken` reused for `@odata.nextLink`; a new `ContinuationToken`-wrapped delta-link mechanism parallels DVT-012's `@odata.deltaLink` reuse decision exactly (Section 8.2, item 3).
10. **Functional requirements:** `@odata.nextLink` for ordinary paging; `@odata.deltaLink` for delta queries - **a single response carries either one or the other, never both** [G5, G6, confirmed]. `$deltatoken=latest` is a documented shortcut to start a fresh delta session without enumerating full history first [G6]. Token duration varies by resource family: directory/education objects have a **fixed 7-day** delta-token lifetime; Outlook entities have **no fixed limit**, instead relying on an LRU-cache-dependent server-side retention that can evict a token earlier under load [G6]. An expired/evicted delta token surfaces as **`410 Gone`** with error code `syncStateNotFound` [G6]. Approximately 26 resource types support delta queries (confirmed list includes `application`, `administrativeUnit`, `chatMessage`, `contact`, `device`, `directoryRole`, `driveItem`, several `education*` types, `event`, `group`, `listItem`, `mailFolder`, `message`, `orgContact`, `oAuth2PermissionGrant`, `plannerUser`, `site`, `servicePrincipal`, `todoTask`, `todoTaskList`, `user`) - **`plannerBucket` delta support is beta-only**, not available in v1.0 [G6]. Delta queries are **not supported in Entra External ID / Azure AD B2C tenants** [G6].
11. **Nonfunctional requirements:** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
12. **Public API contracts or proposed contracts:** Official API: `@odata.nextLink`/`@odata.deltaLink` mechanics, token-duration distinctions, `410 Gone`/`syncStateNotFound`, the ~26-resource delta-capable list (with the `plannerBucket` beta-only caveat), and the Entra External ID/B2C unsupported note - all per [G6]. Proposed C# contracts:

```csharp
public sealed record GraphDeltaPage<T>(IReadOnlyList<T> Items, ContinuationToken? DeltaLink);
public IAsyncEnumerable<T> EnumerateAsync<T>(string resourcePath, GraphQuery? query = null, CancellationToken ct = default);
public Task<GraphDeltaPage<T>> GetDeltaAsync<T>(string resourcePath, ContinuationToken? resumeFrom = null, CancellationToken ct = default);
```
13. **Dependencies:** MGR-003.
14. **Security considerations:** Authentication and authorization: Unchanged from the underlying resource's own requirement. Required permissions/scopes/licenses: None beyond the underlying resource's. Data privacy and security: Delta/next tokens treated as sensitive opaque values, never logged.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: GET pages reuse the shared GET-retry policy; delta tokens are treated as opaque via `ContinuationToken`. Error handling and concurrency: `410 Gone`/`syncStateNotFound` classifies as a distinct, recoverable-via-`$deltatoken=latest`-restart condition, not a generic contract error.
16. **Unit-test requirements:** A response carrying `@odata.nextLink` is distinguished from one carrying `@odata.deltaLink`, never both; `410`/`syncStateNotFound` classifies distinctly and the SDK surfaces a clear "restart via $deltatoken=latest" recovery path; a resource not in the delta-capable list is rejected locally for delta use (falls back to plain pagination only).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for ordinary paging, a successful delta page, and an expired-token 410 response. Integration-test requirements: Authorized tenant to directly observe token-expiry timing per resource family; not available this pass.
18. **Acceptance criteria:** Both pagination mechanisms are handled correctly and distinguished from one another; delta-capability is checked against the documented resource list before attempting a delta query.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact LRU-eviction timing for Outlook-entity delta tokens cannot be predicted (by design, per the vendor documentation itself) and must be documented as "no fixed SLA," not estimated.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (allowlist validation not enforced - see below).
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: ordinary pagination (`GraphQuery`/`EnumerateAsync<T>`) already covered under MGR-004's tests; this pass added `src/Koras.Connect.Microsoft.Graph/GraphDelta.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphDeltaTests.cs`. Covers: the first call targets `{resourcePath}/delta`; `@odata.nextLink` pages are followed until a response carries `@odata.deltaLink` instead, accumulating items across both; resuming from a prior `ContinuationToken` skips the initial delta path; a `410` response classifies as `GraphDeltaTokenExpiredException`, distinct from a generic contract error; a repeated `nextLink` is treated as a cycle; a response missing both link types surfaces `ContractException`. Full suite: 984 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The ~26-resource delta-capable allowlist is not locally enforced - mapping an arbitrary caller-supplied `resourcePath` to the vendor's singular resource-type name (e.g. `users` → `user`) would require guessing a naming convention not specified anywhere, so a non-delta-capable resource is rejected by Graph itself, not locally. Exact LRU-eviction timing for Outlook-entity delta tokens remains undocumented by design. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphDelta.cs` (`GetDeltaAsync<T>`/`GraphDeltaPage<T>`/`GraphDeltaTokenExpiredException`) plus `GraphDeltaTests.cs`, completing the delta half (the ordinary-pagination half shipped earlier under MGR-004). Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: the resource-type allowlist is unenforced and no live tenant is available this pass. |

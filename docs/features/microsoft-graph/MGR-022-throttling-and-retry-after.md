---
id: "MGR-022"
title: "Throttling and Retry-After"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-022 - Throttling and Retry-After

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-022 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-022.
2. **Feature name:** Throttling and Retry-After.
3. **Description:** Respect Graph's documented rate limits and recover automatically from 429 responses without the caller writing custom retry logic.
4. **Business value:** Respect Graph's documented rate limits and recover automatically from 429 responses without the caller writing custom retry logic.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException(RetryAfter)` mapping and `TransportOptions`'s bounded-backoff GET-retry policy reused unchanged.
10. **Functional requirements:** `429 Too Many Requests` + `Retry-After` (seconds) [G3]. **Confirmed global default**: **130,000 requests per 10 seconds per app, across all tenants** [G3] - a tenant-agnostic, app-wide ceiling distinct from any per-tenant/per-resource limit. Per-service limits exist beyond the global default (sample figures captured for Bookings, Cloud Communications, Call Records, Excel, and Identity Protection) but the full per-service table was **only partially captured** in the underlying research and is **not exhaustive** - must not be treated as a complete reference. **Critical, explicitly confirmed caveat**: automatic SDK-level retry logic does **not** apply inside a batch (MGR-021) - each sub-request's 429 must be retried individually using its own `Retry-After`, since the outer batch response's own status does not itself carry a single aggregate retry delay.
11. **Nonfunctional requirements:** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged; writes are never auto-retried on 429.
12. **Public API contracts or proposed contracts:** Official API: 429+`Retry-After`, the 130,000/10s global default, and the batch-retry caveat - all per [G3]. Proposed C# contracts:

None new - fully covered by the existing `ThrottleException`/`TransportOptions`.
13. **Dependencies:** MGR-003.
14. **Security considerations:** Authentication and authorization: Not applicable. Required permissions/scopes/licenses: None. Data privacy and security: Not applicable.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: This feature *is* the throttling/retry behavior; see Functional requirements. Error handling and concurrency: 429 classifies via the unchanged shared classifier; a batch sub-request's individual 429 is retried at the sub-request level, not the batch level, which requires the connector's batch-execution code to re-issue only the failed sub-request(s), not the whole batch.
16. **Unit-test requirements:** 429+`Retry-After` classifies as `ThrottleException`; a batch containing one throttled sub-request retries only that sub-request, not the entire batch; writes are never retried on 429.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic 429 fixtures for both a standalone request and a batch sub-request. Integration-test requirements: Deliberately triggering a real 130,000-req/10s breach is infeasible for a documentation-only pass; not attempted.
18. **Acceptance criteria:** 429 responses are classified and retried (GET, standalone or per-sub-request) or surfaced without retry (writes) correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The full per-service throttling-limit table is not exhaustive in the underlying research; only a representative sample of services was captured.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. The full per-service throttling-limit table is not exhaustive in the underlying research; only a representative sample of services was captured. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

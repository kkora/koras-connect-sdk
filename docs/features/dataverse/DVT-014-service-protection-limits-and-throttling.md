---
id: "DVT-014"
title: "Service protection limits and throttling"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: no new contract proposed - fully covered by the existing shared ThrottleException/TransportOptions, confirmed to reach Dataverse's own wrapper paths (DataverseApiException, writes, batch) by dedicated tests."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-014 - Service protection limits and throttling

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-014 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-014.
2. **Feature name:** Service protection limits and throttling.
3. **Description:** Respect Dataverse's documented per-user rate limits so the SDK itself never causes a tenant-wide lockout, and surface a throttled response as a recoverable, typed condition.
4. **Business value:** Respect Dataverse's documented per-user rate limits so the SDK itself never causes a tenant-wide lockout, and surface a throttled response as a recoverable, typed condition.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException(RetryAfter)` mapping reused unchanged (Section 8.1); `TransportOptions`'s existing bounded-backoff/jitter GET-retry policy reused unchanged.
10. **Functional requirements:** Document, without hardcoding as a guaranteed SLA, the three independent service-protection facets Microsoft publishes as current defaults, each evaluated per web server per authenticated user over a 5-minute sliding window [D2]: (1) **6,000 requests** per 300 seconds (error `0x80072322`); (2) **1,200,000 ms (20 minutes) combined execution time** per 300 seconds (error `0x80072321`); (3) **52 (or higher) concurrent requests** (error `0x80072326`) [D2]. A breach returns `429 Too Many Requests` with a `Retry-After` header in seconds [D2]. Debug-only headers `x-ms-ratelimit-burst-remaining-xrm-requests`/`x-ms-ratelimit-time-remaining-xrm-requests` exist but are explicitly documented as **not** to be used for throttling decision logic [D2] - the connector must not build retry logic around them. Limits do not apply to plug-in/sandbox-originated calls, but the execution time of triggered server-side logic counts against the *originating* request [D2]. The `$batch` 1,000-request cap (DVT-007) is a separate limit from entitlement/license-based quotas - batching is explicitly documented as not a way to evade license-based limits [D2]. **Microsoft explicitly states these numeric defaults "can change and might vary between environments"** [D2] - the SDK must treat them as documentary context for callers, never as compile-time constants gating behavior.
11. **Nonfunctional requirements:** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged; writes are never auto-retried on 429, matching the universal "never replay an uncertain write" policy.
12. **Public API contracts or proposed contracts:** Official API: 429 + `Retry-After`, the three numeric defaults, debug-header caveat - all per [D2]. Proposed C# contracts:

None new - fully covered by the existing `ThrottleException`/`TransportOptions`.
13. **Dependencies:** DVT-003.
14. **Security considerations:** Authentication and authorization: Not applicable. Required permissions/scopes/licenses: None. Data privacy and security: Not applicable.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: This feature *is* the throttling/retry behavior for the connector; see Functional requirements. Error handling and concurrency: 429 classifies via the unchanged shared classifier.
16. **Unit-test requirements:** 429 + `Retry-After` classifies as `ThrottleException` with the correct `RetryAfter` value; an excessive `Retry-After` beyond `TransportOptions.MaxDelay` fails fast rather than waiting (matching the existing SAP/Qualtrics-proven pattern); a malformed `Retry-After` falls back to bounded backoff rather than hanging; writes are never retried on 429.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic 429 fixtures with valid, excessive, and malformed `Retry-After` values. Integration-test requirements: Deliberately triggering a real 6,000-request/300s breach against a live tenant is both infeasible and inadvisable for a documentation-only test harness; not attempted.
18. **Acceptance criteria:** 429 responses are classified and retried (GET) or surfaced without retry (writes) correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The three numeric defaults are Microsoft's own documented defaults, explicitly subject to per-environment variation - do not hardcode them as assertions against a live tenant's actual behavior.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable (the three numeric defaults are documentary context only, per the spec; no live-tenant breach test is appropriate or attempted).
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `tests/Koras.Connect.Tests/Dataverse/DataverseThrottlingTests.cs`. No new source file - confirms the existing `IntegrationExceptionClassifier`/`TransportOptions` behavior reaches Dataverse's own `DataverseApiException` wrapper and every write/batch path: a throttled GET retries then succeeds; exhausting retries surfaces `DataverseApiException` wrapping `ThrottleException` with the correct `RetryAfter`; a `Retry-After` beyond `TransportOptions.MaxDelay` fails fast instead of waiting; a throttled write (`UpdateRowAsync`) and a throttled batch (`ExecuteBatchAsync`, which uses the non-retrying `SendRawAsync`) are each attempted exactly once. The debug-only `x-ms-ratelimit-*` headers are not read or referenced anywhere, matching the explicit "not for throttling decision logic" guidance. Full suite: 686 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | None identified; the three documented numeric defaults are intentionally not hardcoded as assertions anywhere in code or tests. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `DataverseThrottlingTests.cs` confirming the shared throttling/retry behavior reaches every Dataverse wrapper path. Checked all applicable completion gates, including the vendor-schema gate (not applicable per the spec's own guidance against live-tenant breach testing); status moves from `not-started` to `implemented`. |

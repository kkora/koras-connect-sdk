---
id: "SDK-008"
title: "Retry and resilience policies"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-14"
implementation_commit: null
---

# SDK-008 - Retry and resilience policies

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-008.
2. **Feature name:** Retry and resilience policies.
3. **Description:** Recover safe transient reads while avoiding duplicate writes.
4. **Business value:** 429/selected 5xx retry only within budget reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT bounded GET backoff/jitter/Retry-After; E-TEST TransientReadsRetry and WritesAreNeverRetried. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Retain GET-only retry defaults, total deadlines and server minimum delay; expose deterministic clock/delay testing, optional concurrency/circuit policies without stacked retries.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing TransportOptions and IntegrationTransport; proposed immutable operation-safety policy.
13. **Dependencies:** SDK-010, SDK-009; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise 429/date and delta Retry-After, network failures, budget exhaustion, jitter bounds, circuit cancellation and no POST/DELETE replay. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** 429/date and delta Retry-After, network failures, budget exhaustion, jitter bounds, circuit cancellation and no POST/DELETE replay.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** 429/selected 5xx retry only within budget; excessive Retry-After fails without early retry; unsafe writes are sent at most once automatically.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor global rate number assumed; advanced policy library is optional and requires dependency review.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 297, Skipped 3 (live-sandbox, unrelated), Failed 0. `CoreTests.cs` already covered status-code transient retry (429/500/503), write-never-retried (POST only), and delta-form Retry-After exceeding budget. New `ResiliencePolicyTests.cs` (10 tests) closes every remaining named requirement: a GET retries after a connection-level `HttpRequestException`, not only an HTTP status code ("network failures"); a persistently-failing endpoint exhausts the retry budget and throws with the correct final attempt count ("budget exhaustion"); backoff timing stays within the documented `[1.0, 1.2)` jitter multiplier and respects `MaxDelay` at a later attempt ("jitter bounds", 2 tests); cancelling during an in-progress backoff wait stops in well under the configured delay rather than waiting it out ("circuit cancellation"); the date form of `Retry-After` is honored, not only the delta/`TimeSpan` form; and all four unsafe HTTP methods (POST/PUT/DELETE/PATCH), not only POST, are sent at most once on a transient failure ("no POST/DELETE replay"). |
| Remaining work / blockers | The jitter-bounds and MaxDelay-capping tests measure real wall-clock elapsed time with generous tolerance bands rather than injecting a controllable `TimeProvider`, since `IntegrationTransport`'s own jitter uses `Random.Shared` directly (not seeded/injectable) — these are timing-sensitive but use large enough margins (tens of ms against a 500ms+ ceiling) to be stable on typical CI hardware; flagging in case they ever need loosening for a slower runner. No functional gap found for this ticket. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Closed every remaining named unit-test requirement with `ResiliencePolicyTests.cs`: network-level failure retry, budget exhaustion, jitter bounds, circuit cancellation during backoff, date-form Retry-After, and no-replay across all unsafe HTTP methods. Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |
| 2026-11-14 | `implemented` | Fixed a genuine bug found incidentally during Phase 2 Salesforce connector review (not this ticket's own review pass): once a GET's connection-level `HttpRequestException` retry budget was exhausted, or on any non-GET method (never retried at all), the exception escaped `IntegrationTransport.ExecuteAsync` as a raw `HttpRequestException` instead of the dedicated `TransportException` the shared exception hierarchy already defines for exactly this case - no connector's `catch (IntegrationRequestException ex)` pattern could ever observe it, since a plain `HttpRequestException` is not an `IntegrationRequestException`. Wrapped the connection-failure catch at the `_http.SendAsync` call site; added `APersistentConnectionFailureThatExhaustsTheGetRetryBudgetWrapsIntoTransportException` and `AConnectionFailureOnAWriteMethodWrapsIntoTransportExceptionImmediatelyWithNoRetry` to `ResiliencePolicyTests.cs`. |
| 2026-11-14 | `implemented` | Phase 2 review pass 2 (Salesforce) found the prior fix only covered `ExecuteAsync`'s own `_http.SendAsync` call site - three sibling methods that bypass `ExecuteAsync` entirely (`SendJsonAllowingAnyStatusAsync`, `UploadAsync`, `SendRawAsync`) had no such catch at all, so a connection failure on any call routed through them (Salesforce's upsert/bulk-ingest/bulk-query/CometD streaming, and any other connector using the same primitives) still escaped raw. Added the identical catch to all three, plus `AConnectionFailureOnSendJsonAllowingAnyStatusWrapsIntoTransportException`/`AConnectionFailureOnUploadAsyncWrapsIntoTransportException`/`AConnectionFailureOnSendRawAsyncWrapsIntoTransportException` to `ResiliencePolicyTests.cs`. |

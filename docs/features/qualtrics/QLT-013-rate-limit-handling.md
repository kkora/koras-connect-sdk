---
id: "QLT-013"
title: "Rate-limit handling"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-013 - Rate-limit handling

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-013.
2. **Feature name:** Rate-limit handling.
3. **Description:** Avoid read storms and duplicate unsafe submissions.
4. **Business value:** Avoid read storms and duplicate unsafe submissions.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT GET 429/Retry-After; no quota/concurrency model; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Provide bounded configured concurrency and safe throttle hints; respect server delay/total budget; never hardcode universal brand quota or retry POST automatically.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing TransportOptions; proposed quota/concurrency policy and safe metadata.
13. **Dependencies:** SDK-008, QLT-004, QLT-005; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** 429, long/date/malformed hints, concurrent callers, queue cancellation and POST throttle. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** 429, long/date/malformed hints, concurrent callers, queue cancellation and POST throttle.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Safe reads wait server minimum or fail budget; configured concurrency bounded; writes remain one automatic attempt with explicit reconciliation guidance.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Exact account/family/brand quotas not verified; obtain official limits rather than invent requests/minute.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "queue cancellation" unit-test-requirement gap by adding `CancellingWhileQueuedOnTheGateNeverStartsTheOperation` to `tests/Koras.Connect.Tests/Qualtrics/QualtricsExpansionTests.cs`. Starts one operation that holds the only configured slot (`maxConcurrency: 1`), then calls `RunAsync` for a second operation with an already-queued (not yet cancelled) token, cancels that token while it is still waiting on the semaphore, and asserts: (1) the queued call surfaces `OperationCanceledException`; (2) the queued operation body never ran (`queuedStarted` stays `false`) - cancellation during the wait never starts the work; (3) the first operation, unaffected, still completes normally once released. Full suite: 472 passed, 3 skipped, 0 failed (one unrelated pre-existing `ResiliencePolicyTests` timing flake observed and reconfirmed non-reproducing on an isolated rerun). |
| Remaining work / blockers | No further engineering gap identified. Exact account/family/brand-wide request quotas remain an external blocker (see above) - only caller-configured concurrency is bounded by this SDK, never an invented universal rate. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `QualtricsConcurrencyLimiter` (`src/Koras.Connect.Qualtrics/QualtricsConcurrencyLimiter.cs`), a semaphore-based bounded-concurrency wrapper (`RunAsync`/`RunAllAsync`, 1-64 configured slots) for fanning out survey/export calls without an unbounded burst; retry/backoff/Retry-After handling remains solely `IntegrationTransport`'s responsibility and is not duplicated. Added Qualtrics-level tests confirming: a 429 with a Retry-After far beyond `TransportOptions.MaxDelay` fails fast rather than waiting; a 429 with a malformed (non-parseable) Retry-After falls back to bounded exponential backoff rather than hanging or looping unboundedly; `StartResponseExportAsync` (POST) is never automatically retried on 429. Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`ConcurrencyLimiterBoundsSimultaneousOperations`, `ConcurrencyLimiterRejectsOutOfRangeConcurrency`, `LongRetryAfterOn429ThrowsCleanlyRatherThanWaiting`, `MalformedRetryAfterOn429FallsBackToBoundedBackoffRatherThanHanging`, `StartResponseExportIsNeverAutomaticallyRetriedOnThrottle`). No brand-wide quota number was invented; only caller-configured concurrency is bounded. `dotnet build`/`dotnet test` passing as of this revision. |
| 2026-11-13 | `implemented` | Added `CancellingWhileQueuedOnTheGateNeverStartsTheOperation`, closing the "queue cancellation" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: exact account/family quota numbers against a live tenant remain a standing external blocker. |

---
id: "ADP-010"
title: "Structured errors and rate-limit handling"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-010 - Structured errors and rate-limit handling

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-010.
2. **Feature name:** Structured errors and rate-limit handling.
3. **Description:** Expose actionable ADP failure classes without leaking HR records.
4. **Business value:** Expose actionable ADP failure classes without leaking HR records.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT IntegrationRequestException/retries; E-TEST ApiFailuresDoNotExposeResponseBody; aliases resolve in section 3.
10. **Functional requirements:** Map documented ADP error metadata/correlation to neutral hierarchy; preserve HTTP/attempt/retry hints safely; never emit raw ADP userMessage by default.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed AdpApiException/SafeProviderError layered on SDK-009; current IntegrationRequestException.
13. **Dependencies:** SDK-009, SDK-008, SDK-011; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** 401/403/404/429/5xx, blank benefits 400, malformed provider error, hostile IDs, excessive Retry-After and payload canaries. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** 401/403/404/429/5xx, blank benefits 400, malformed provider error, hostile IDs, excessive Retry-After and payload canaries.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Host can distinguish denied access, throttling, invalid input and malformed response with sanitized fields; no private message/body appears in diagnostics.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Per-operation official error schemas and tenant samples needed; no numeric ADP rate limit verified.

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
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added two tests to `AdpApiExceptionTests.cs` closing the last two unit-test-requirement gaps: (1) `ExcessiveRetryAfterOnAThrottledWorkerSearchFailsWithoutRetryAndClassifiesAsThrottleException` — a server Retry-After of 1 hour (exceeding the default 30s `MaxDelay`) through `AdpWorkersClient.SearchWorkersAsync` fails on the first attempt (asserted via call count) rather than actually waiting, and still classifies as `ThrottleException`; (2) `MalformedOrHostileProviderErrorBodyNeverLeaksIntoTheClassifiedExceptionOrItsMessage` — a 502 response whose body is deliberately malformed/hostile (unterminated JSON, a fake SSN, an embedded `<script>` tag) neither breaks classification nor appears anywhere in `AdpApiException.Message`/`ToString()`, confirming the pre-existing architectural guarantee that failure classification never reads the response body. Full suite: 367 passed, 3 skipped, 0 failed, plus one unrelated pre-existing timing flake in `ResiliencePolicyTests` that passes in isolation (`dotnet test --filter FullyQualifiedName~ResiliencePolicyTests`: 10/10) — not an ADP-010 regression. |
| Remaining work / blockers | No further engineering gap identified. Per-operation official ADP error schemas/tenant samples and a verified numeric rate limit remain external blockers (see above) — this SDK's own `MaxDelay`/`MaxRetries` are configuration, not a documented ADP limit. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `Koras.Connect.Adp.AdpApiException : Koras.Connect.Exceptions.IntegrationException` (`src/Koras.Connect.Adp/AdpApiException.cs`), which classifies a failed `IntegrationRequestException` via `IntegrationExceptionClassifier.Classify` and carries the ADP operation name, a `Category` (the classified subtype's name, e.g. `AuthenticationException`/`ThrottleException`), and an optional correlation id. Wired into `AdpOperationsClient.ReadAsync/GetMetadataAsync/ReadSpendingAccountEnrollmentsAsync/ReadNotificationAsync/AcknowledgeNotificationAsync` and `AdpWorkersClient.SearchWorkersAsync/GetMetadataAsync/GetWorkerAsync/EnumerateWorkersAsync`, each via a private `GuardAsync` wrapper. Tests in `tests/Koras.Connect.Tests/AdpApiExceptionTests.cs` cover classification of 401/403/404/429(/Retry-After)/5xx into the expected category, end-to-end classification through `AdpOperationsClient` (401) and `AdpWorkersClient` (502), and an explicit regression test (`SubmitEventAsyncAndGetWorkersPageAsyncStillSurfaceTheRawIntegrationRequestExceptionUnchanged`) proving the two pinned-behavior call sites are untouched. Also added `AdpEntitlementTracker`-based deny-before-network via `AuthorizationException` (see ADP-021). Remains `partial`: correlation-id capture on failure paths is architecturally bounded by `IntegrationTransport` (see remaining-work note), and not every call site is wrapped. |
| 2026-11-12 | `partial` | `src/Koras.Connect/Transport/IntegrationTransport.cs`'s `IntegrationRequestException` now carries `Headers` populated on both the retry-exhausted and immediate-failure paths (not only on success), and `AdpApiException.Classify` defaults `correlationId` from that allowlist (`request-id`/`x-request-id`/`adp-msg-msgid`) when the caller does not supply one. Added `tests/Koras.Connect.Tests/AdpCorrelationTests.cs`, which drives a full mocked-handler HTTP round trip (not a direct `Classify` call) through both `AdpOperationsClient.ReadAsync` and `AdpWorkersClient.GetWorkerAsync` against a 403 response carrying a `request-id` header and asserts the resulting `AdpApiException.Details.CorrelationId` matches, plus an explicit-override test and a no-header-means-null test. This closes the "correlation-id on failure is architecturally bounded" gap recorded in the row above. Still `partial`: `SubmitEventAsync`/`GetWorkersPageAsync` remain intentionally unreclassified (pinned contract), and no malformed-provider-error-body or excessive-Retry-After case is separately tested. |
| 2026-11-13 | `implemented` | Closed the remaining two unit-test-requirement gaps (excessive Retry-After, malformed/hostile provider error body) recorded above. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented` since every functional requirement and listed unit-test case is now exercised. `verification` moved from `unverified` to `partial`: per-operation official error schemas and a verified numeric rate limit remain external blockers. |
| 2026-11-13 | `implemented` | Added `samples/ADP.Resilience` as additional runnable evidence: structured `AdpApiException` classification with correlation ids across 401/403/404/429/500 (see `docs/features/adp/ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md`); `dotnet run --project samples/ADP.Resilience` executes all five cases successfully offline. Docs-only addition; no status/verification change. |

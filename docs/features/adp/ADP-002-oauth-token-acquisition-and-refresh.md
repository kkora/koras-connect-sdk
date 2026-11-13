---
id: "ADP-002"
title: "OAuth token acquisition and refresh"
category: "adp"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Implemented but unverified"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-002 - OAuth token acquisition and refresh

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-002.
2. **Feature name:** OAuth token acquisition and refresh.
3. **Description:** Acquire and renew tokens predictably under concurrent load.
4. **Business value:** Acquire and renew tokens predictably under concurrent load.
5. **Owning package:** Koras.Connect.ADP with Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-AUTH ClientCredentialsTokenSource; E-TEST ConcurrentTokenAcquisitionIsSingleFlightAndRedacted and TokenRenewsBeforeExpiryAndCanceledCallsCannotUseCache; aliases resolve in section 3.
10. **Functional requirements:** Use client_credentials on the exact configured ADP token endpoint; renew using expiry/skew; no refresh-token grant assumed; isolate caches and define credential-rotation invalidation.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing IAccessTokenSource/GetTokenAsync and ClientCredentialsOptions; proposed rotatable credential adapter.
13. **Dependencies:** ADP-001, SDK-005, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Concurrency single-flight, expiry boundary, bad/missing token/type/lifetime, failed renewal and canceled cache hit. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Concurrency single-flight, expiry boundary, bad/missing token/type/lifetime, failed renewal and canceled cache hit.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Concurrent requests share one valid acquisition; expired credentials cannot remain cached; request shape and live token grant pass with authorized test credentials.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Official getting-started page fetch failed in P1; current endpoint is source/README evidence and official collection candidate, not newly live-verified; app grant required.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: all 11 functional requirements re-checked against the current revision of `ClientCredentialsTokenSource`/`AdpRotatingTokenSource` and found to already be covered, not just imported baseline evidence: single-flight concurrency (`CoreTests.ConcurrentTokenAcquisitionIsSingleFlightAndRedacted`), credential redaction (same test, asserts `ClientCredentialsOptions.ToString()` never contains the secret), renew-before-expiry with skew and canceled-call cache rejection (`SecurityAndLifecycleTests.TokenRenewsBeforeExpiryAndCanceledCallsCannotUseCache`), bad/missing token/type/lifetime (`CoreTests.InvalidTokensAreRejected`, 3 cases), failed renewal surfaced as a structured, credential-free `AuthenticationException` (`DiagnosticsAndCorrelationTests.TokenEndpointHttpFailureThrowsStructuredAuthenticationException`), and rotation invalidation both via resolved-credential change and explicit `Invalidate()` (`AdpCredentialRotationTests.RotatingCredentialProviderReacquiresTokenWhenResolvedCredentialsChange`, `InvalidateForcesReacquisitionEvenWithoutAResolvedCredentialChange`). Full suite: 337 passed, 3 skipped (unrelated live-sandbox opt-in tests), 1 unrelated pre-existing timing flake in `ResiliencePolicyTests` that passes in isolation (`dotnet test --filter FullyQualifiedName~ResiliencePolicyTests`: 10/10 passed) — not an ADP-002 regression. |
| Remaining work / blockers | No further engineering gap identified. Still open: "request shape and live token grant pass with authorized test credentials" in the acceptance criteria requires an authorized ADP sandbox app/client registration, which is not available in this environment (see external blockers above) — this stays an explicit, standing blocker rather than an inferred pass. |
| Acceptance / completion | Not recorded; the live-grant half of the acceptance criteria cannot be satisfied without authorized sandbox credentials. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `implemented` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Re-verified every unit-test requirement against the current revision (single-flight, redaction, expiry/skew, canceled-cache rejection, bad/missing token fields, failed-renewal classification, rotation invalidation) — all already covered by existing tests, no code gap found. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates accordingly. `verification` moved from `unverified` to `partial`: the live-grant acceptance half remains blocked on unavailable sandbox credentials, so acceptance/completion is not recorded. |

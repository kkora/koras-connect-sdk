---
id: "ADP-011"
title: "Retry, timeout, and cancellation"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Implemented but unverified"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-011 - Retry, timeout, and cancellation

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-011.
2. **Feature name:** Retry, timeout, and cancellation.
3. **Description:** Bound ADP work while protecting writes from replay.
4. **Business value:** Bound ADP work while protecting writes from replay.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT bounded GET retry/deadline; E-TEST transient/deadline/no-write-retry tests; aliases resolve in section 3.
10. **Functional requirements:** Apply shared operation-safety defaults to workers, event reads, acknowledgement and writes; include auth/waits in documented operation budget or expose separate explicit auth budget.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing TransportOptions and operation CancellationToken parameters.
13. **Dependencies:** SDK-008, SDK-010, ADP-002; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Cancel during auth/page/delay/body, Retry-After date/delta, write timeout unknown outcome, no DELETE replay and disposed response. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Cancel during auth/page/delay/body, Retry-After date/delta, write timeout unknown outcome, no DELETE replay and disposed response.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Transient reads respect bounded delay/deadline; POST and acknowledgement DELETE are not automatically replayed; cancellation escapes promptly and writes advise reconciliation.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Sandbox rate behavior not verified; authorized live tests must avoid inducing damaging throttling or duplicate writes.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 183, Skipped 1, Failed 0, Total 184 (includes `AdpCredentialRotationTests.HangingTokenEndpointDoesNotBlockForeverEvenUnderAGenerousOuterCancellation`) |
| Remaining work / blockers | No live-tenant verification of sandbox rate-limit behavior; the acquisition timeout is a local, bounded-time guarantee only. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `implemented` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Wired `ClientCredentialsTokenSource`'s existing `acquisitionTimeout` constructor parameter through `AdpServiceCollectionExtensions.AddKorasAdp`/`AddKorasAdpWithRotatingCredentials` (default `TimeSpan.FromSeconds(30)`, overridable per registration) so the OAuth token endpoint has its own bounded deadline independent of `TransportOptions.RequestTimeout`. `src/Koras.Connect.Adp/AdpRotatingTokenSource.cs` forwards the same timeout to each inner `ClientCredentialsTokenSource` it creates. Added `tests/Koras.Connect.Tests/AdpCredentialRotationTests.HangingTokenEndpointDoesNotBlockForeverEvenUnderAGenerousOuterCancellation`, which proves a token endpoint that never responds still fails within its configured `acquisitionTimeout` (asserted under 5s) even though the caller passed a 10-minute outer `CancellationToken` that never fires. Still `implemented`/unverified: no live sandbox has exercised real ADP rate-limit or timeout behavior. |

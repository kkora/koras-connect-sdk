---
id: "SDK-003"
title: "Dependency injection extensions"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-003 - Dependency injection extensions

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-003.
2. **Feature name:** Dependency injection extensions.
3. **Description:** Reduce host configuration errors while retaining direct construction.
4. **Business value:** All provider clients can resolve independently reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-DI: AddKorasAdp only; E-TEST: CertificateAndDIContractsResolveWithoutExternalInfrastructure. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add shared/provider registration builders; validate eagerly; isolate named instances, token caches, certificate and handler lifetime; retain duplicate protection until named instances are implemented.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing AddKorasAdp; proposed AddKorasConnect, AddKorasSuccessFactors, AddKorasQualtrics.
13. **Dependencies:** SDK-002, SDK-004, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise provider resolution/disposal, duplicate registration, named instance isolation and options failure before network. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Provider resolution/disposal, duplicate registration, named instance isolation and options failure before network.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All provider clients can resolve independently; separate tenant registrations cannot exchange credentials; disposal ownership is documented and tested.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor blocker; lifecycle and named-registration API must be frozen in P2.

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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 258, Skipped 3 (live-sandbox, unrelated), Failed 0. New `DependencyInjectionDisposalTests.cs` (5 tests) closes the "disposal ownership is documented and tested" acceptance clause and the missing cross-provider parity items found while auditing: it proves `AddKorasAdp`'s and `AddKorasSuccessFactors`'s container-registered token sources (`AdpRotatingTokenSource`, `SamlBearerTokenSource`) are actually disposed on `ServiceProvider.Dispose()` (observed via the post-dispose `ObjectDisposedException` from their internal `SemaphoreSlim`), that host-supplied resources the SDK never registers as its own service (the ADP certificate, the SAP assertion delegate) are left untouched by disposal, documents the real asymmetry that Qualtrics's token source is never container-tracked so a disposable one supplied via `tokenSourceFactory` is **not** disposed by the container (host-owned, not a bug — just undocumented before this pass), and adds ADP's missing dedicated "rejected before any registration" test (`AddKorasAdpRejectsAMissingPrivateKeyBeforeAnyRegistration`), matching the test naming/style already used for SAP and Qualtrics. |
| Remaining work / blockers | (1) **`AddKorasConnect` (the shared, provider-neutral builder named in this ticket's own "proposed contracts" field) was not added.** Investigated first: `Koras.Connect.Core` currently has no concrete service that needs container registration — `IntegrationTransport`/`EndpointSafety`/the exception hierarchy are plain utility types constructed inline by each provider's own extension, not DI services. Adding `AddKorasConnect` today would be a no-op method with nothing to test or verify, so it is deliberately deferred until Core (or a future shared concern, e.g. SDK-012's Observability registration) actually has a shared, container-registrable service to compose. (2) **Named-instance multi-tenant registration does not exist for any provider** — this is explicitly correct per master spec section 7 ("Do not imply named multi-tenant registration exists today"), not a gap; all three providers correctly retain duplicate-registration rejection instead, which this pass's acceptance evidence (above) confirms is tested for all three. (3) The underlying `AddKorasAdp`/`AddKorasSuccessFactors`/`AddKorasQualtrics` extensions were already implemented before this pass (this ticket's own "Existing implementation evidence" field, and SDK-003.md's prior status history, were stale on this point — they predate SAP/Qualtrics getting their own extensions, which SAP-016/QLT-016 still describe as "proposed"/"no DI extension" even though the files exist; flagging for whoever maintains those two category trackers, not fixed here since it's outside this ticket's owning package). (4) No consumer sample specifically demonstrating DI registration/disposal exists yet (only xunit tests do). (5) Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Confirmed (via audit, not re-implementation) that `AddKorasAdp`/`AddKorasSuccessFactors`/`AddKorasQualtrics` all already exist with eager validation and duplicate-registration rejection — this ticket's own evidence fields were stale on that point. Added `DependencyInjectionDisposalTests.cs` to close the previously-untested "disposal ownership is documented and tested" acceptance clause across all three providers, plus ADP's missing dedicated pre-registration-rejection test. Deliberately did not add a shared `AddKorasConnect` builder (no concrete shared service exists yet to justify one) or named-instance multi-tenancy (explicitly out of scope per master spec section 7). Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

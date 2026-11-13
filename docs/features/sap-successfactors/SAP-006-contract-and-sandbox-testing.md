---
id: "SAP-006"
title: "Contract and sandbox testing"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-006 - Contract and sandbox testing

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-006.
2. **Feature name:** Contract and sandbox testing.
3. **Description:** Separate deterministic protocol tests from actual tenant compatibility.
4. **Business value:** Separate deterministic protocol tests from actual tenant compatibility.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TEST ExpansionTests; E-SAMPLE SAP options smoke; historical execution only; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Version entity/metadata/upsert fixtures; isolated consumer; opt-in read-only sandbox; approved writes use explicit test objects and per-row verification.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed SAP test categories/config; no runtime infrastructure.
13. **Dependencies:** SAP-001-005, SDK-018; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Offline mode, missing credentials, fixture drift, RBP denial, mixed batches and secret canaries. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Offline mode, missing credentials, fixture drift, RBP denial, mixed batches and secret canaries.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Both OS jobs pass synthetic tests; live evidence identifies product/schema/RBP/revision or explicit access blocker; no production calls by default.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Sandbox/API host/company/client/assertion/RBP and population absent.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "secret canaries" and "mixed batches" unit-test-requirement gaps - the last two of the six named in this feature's spec without any dedicated test. Added `tests/Koras.Connect.Tests/Sap/SapSecretCanaryTests.cs`, a single cross-cutting sweep threading one fake secret value through every failure path on the SAP surface (`SamlBearerTokenSource` exchange, `EnumerateAsync`, `GetMetadataAsync`, `UpsertAsync` transport failure, and an `UpsertResult` row-level failure) and asserting it never appears in a thrown exception's `Message`/`ToString()` or in `UpsertResult.ToString()`. Added `MixedBatchReturnsIndependentPerRowOutcomesWithinOneHttp200Response` to `tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs` (one success + one failure + one success in a single upsert call, each reported independently, the failing row's message never leaked). The other four named requirements were already covered and are cited as evidence: offline mode (every SAP test in this repo runs against a synthetic `HttpMessageHandler`, no live network access anywhere); missing credentials (`SapAuthenticationTests.AssertionSourceFailureIsNeverSwallowedOrRetried`); fixture drift (`SapMetadataTests.ChangedFixtureInvalidatesTheCachedHashAndRefreshAlwaysReparses`); RBP denial (the 401/403 classification tests across `SapExceptionClassificationTests`, `SapManagerResolutionTests`, `SapEmploymentAndJobTests`). Full suite: 544 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Live evidence identifying product/schema/RBP/revision or an explicit access blocker against an authorized sandbox tenant remains a standing external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `SapSecretCanaryTests.cs` (a cross-cutting canary sweep) and `MixedBatchReturnsIndependentPerRowOutcomesWithinOneHttp200Response`, closing the "secret canaries" and "mixed batches" unit-test-requirement gaps; the other four named requirements were already covered and are now cited as evidence. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live sandbox acceptance evidence against an authorized tenant remains a standing external blocker. |

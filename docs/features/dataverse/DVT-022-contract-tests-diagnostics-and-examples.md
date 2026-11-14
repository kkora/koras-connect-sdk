---
id: "DVT-022"
title: "Contract tests, diagnostics and examples"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-022 - Contract tests, diagnostics and examples

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-022 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-022.
2. **Feature name:** Contract tests, diagnostics and examples.
3. **Description:** Prove the Dataverse connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every Phase 1 connector's established testing/sample discipline exactly.
4. **Business value:** Prove the Dataverse connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every Phase 1 connector's established testing/sample discipline exactly.
5. **Owning package:** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2 (unit/contract tests) / P2-D6 (sample, package validation).
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline, credential-free console-sample convention exactly; also requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Dataverse.snapshot.txt` entry wired into the existing `ApiSnapshotTests.cs` reflection-based baseline (SDK-017).
10. **Functional requirements:** Offline unit/contract tests for every DVT-001 through DVT-021 P0/P1 feature, using a synthetic `HttpMessageHandler`, no live network access, running on both Linux and Windows via the existing CI matrix; at least one standalone `samples/Dataverse.*` console project demonstrating authentication → CRUD → query → pagination → metadata, entirely offline, matching `samples/SuccessFactors.PersonAndEmploymentSync`'s structure; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity stages with explicit Unknown/Skipped/Failed/Passed outcomes, never issuing a mutating request, matching every existing connector's diagnostic precedent exactly.
11. **Nonfunctional requirements:** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
12. **Public API contracts or proposed contracts:** Official API: `DiagnoseAsync`'s connectivity stage uses DVT-002's `WhoAmI` function as its one permitted minimal, non-mutating probe call. Proposed C# contracts:

`DataverseConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what DVT-001 through DVT-021 already define.
13. **Dependencies:** Every other DVT-0NN feature this covers tests for.
14. **Security considerations:** Authentication and authorization: `DiagnoseAsync`'s Authentication stage issues exactly one minimal read (`WhoAmI`), never a mutating request, matching the Qualtrics/ADP connector-diagnostic precedent exactly. Required permissions/scopes/licenses: None beyond whatever the underlying probed feature already requires. Data privacy and security: Diagnostic output contains no secrets, tokens, or row payloads, matching `ConnectionDiagnostic`'s existing design intent.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to this feature itself. Error handling and concurrency: `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary, matching the shared `ConnectionDiagnostic` contract exactly.
16. **Unit-test requirements:** Every unit-test scenario enumerated in DVT-001 through DVT-021 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed (matching the Qualtrics precedent: `DiagnoseAsyncSkipsAuthenticationStageWhenConfigurationIsInvalid`-style behavior).
17. **Integration-test requirements:** Contract-test scenarios: Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus per the Phase 1 master's existing testing-strategy template. Integration-test requirements: Every feature's integration-test requirement above remains individually deferred/blocked pending a live sandbox; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live tenant.
18. **Acceptance criteria:** The sample project builds and runs on a clean checkout with `dotnet build`/`dotnet run` and performs zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed.
19. **Definition of Done:** Matches the Phase 1 master's Section 26 Definition of Done template exactly: implemented public contract, numbered acceptance cases, deterministic unit/contract tests, applicable security/cancellation/resource checks, package consumer example, current official schema citation for every external operation claimed, required sandbox outcomes recorded where available (none yet), revision-linked CI artifacts, reviewed docs/limitations.
20. **External API entitlement or documentation blockers:** No live Dataverse sandbox credentials are available in this documentation pass; every integration-test requirement listed in DVT-001 through DVT-021 remains explicitly blocked, not silently skipped, pending an authorized tenant (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live Dataverse sandbox credentials are available in this documentation pass; every integration-test requirement listed in DVT-001 through DVT-021 remains explicitly blocked, not silently skipped, pending an authorized tenant (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

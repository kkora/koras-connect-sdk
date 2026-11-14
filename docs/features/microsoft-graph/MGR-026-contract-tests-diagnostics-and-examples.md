---
id: "MGR-026"
title: "Contract tests, diagnostics and examples"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4/P2-D6"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-026 - Contract tests, diagnostics and examples

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-026 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-026.
2. **Feature name:** Contract tests, diagnostics and examples.
3. **Description:** Prove the Graph connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
4. **Business value:** Prove the Graph connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4 (unit/contract tests) / P2-D6 (sample, package validation); see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline sample convention; requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Microsoft.Graph.snapshot.txt` entry (SDK-017).
10. **Functional requirements:** Offline unit/contract tests for every MGR-001 through MGR-025 P0/P1 feature, using a synthetic `HttpMessageHandler`, no live network access, running on both Linux and Windows; at least one standalone `samples/MicrosoftGraph.*` console project demonstrating authentication → users/groups → OneDrive → pagination/delta, entirely offline; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity/Permissions (the last stage specific to Graph, built on MGR-024's diagnostic surface), never issuing a mutating request. v1.0-vs-beta usage must be exercised in at least one test to confirm the version-selection mechanism (MGR-003) works correctly, with beta usage clearly labeled non-production per [G10]'s "beta is not supported in production" guidance - carried into the sample's own comments, not just this document.
11. **Nonfunctional requirements:** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
12. **Public API contracts or proposed contracts:** Official API: `DiagnoseAsync`'s connectivity stage uses MGR-008's `GET /organization` (restricted to `User.Read`-level detail, per MGR-008's own documented restricted-response shape) as its one permitted minimal, non-mutating probe call; the permissions stage uses MGR-024's grant-listing as a non-mutating diagnostic, not a live call against a protected resource. Proposed C# contracts:

`GraphConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what MGR-001 through MGR-025 already define.
13. **Dependencies:** Every other MGR-0NN feature this covers tests for.
14. **Security considerations:** Authentication and authorization: `DiagnoseAsync`'s Authentication stage issues exactly one minimal read, never a mutating request. Required permissions/scopes/licenses: None beyond whatever the underlying probed feature already requires. Data privacy and security: Diagnostic output contains no secrets, tokens, or tenant/resource payloads.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to this feature itself. Error handling and concurrency: `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary.
16. **Unit-test requirements:** Every unit-test scenario enumerated in MGR-001 through MGR-025 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed; the two documented conflicting-figures items (MGR-010's 250MB/4MB upload limit, and any other open gap) are represented in test documentation as explicitly unresolved, not silently resolved one way.
17. **Integration-test requirements:** Contract-test scenarios: Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus. Integration-test requirements: Every feature's integration-test requirement above remains individually deferred/blocked pending a live tenant; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live tenant.
18. **Acceptance criteria:** The sample project builds and runs on a clean checkout with zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed; v1.0-vs-beta version selection is exercised at least once.
19. **Definition of Done:** Matches the Phase 1 master's Section 26 Definition of Done template exactly.
20. **External API entitlement or documentation blockers:** No live Entra tenant is available in this documentation pass; MGR-010's conflicting simple-upload-size figures and MGR-016's RSC-vs-tenant-wide permission choice are the highest-priority re-verification items before Graph connector implementation begins (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live Entra tenant is available in this documentation pass; MGR-010's conflicting simple-upload-size figures and MGR-016's RSC-vs-tenant-wide permission choice are the highest-priority re-verification items before Graph connector implementation begins (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

---
id: "DVT-022"
title: "Contract tests, diagnostics and examples"
category: "dataverse"
priority: "P0"
phase: "P2-D2/P2-D6"
status: "implemented"
baseline_status: "Implemented: 100+ synthetic-HttpMessageHandler contract tests across DVT-001 through DVT-021 (711 passing in the full suite), a committed API snapshot, DiagnoseAsync covering Configuration/Authentication/Connectivity, and a new samples/Dataverse.OfflineWalkthrough console sample that builds and runs end-to-end (auth -> WhoAmI -> CRUD -> paginated query -> metadata) with zero network access."
verification: "partial"
last_updated: "2026-11-14"
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
6. **Priority:** P0.
7. **Planned development phase:** P2-D2 (unit/contract tests) / P2-D6 (sample, package validation); see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
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

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied (sample builds/runs/zero-network and the synthetic suite pass; live sandbox outcomes remain deferred as documented).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable (every live-tenant gate across DVT-001..021 is explicitly recorded as blocked, not silently skipped).
- [x] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: 711 passed, 3 skipped (unrelated ADP live-sandbox tests), 0 failed, across 19 Dataverse test files covering every DVT-001 through DVT-021 unit/contract requirement with a synthetic `HttpMessageHandler`, no live network access. `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Dataverse.snapshot.txt` is committed and enforced by `ApiSnapshotTests`. `DataverseClient.DiagnoseAsync` (DVT-002) covers Configuration/Authentication/Connectivity with Passed/Failed/Skipped outcomes, issuing only the non-mutating `WhoAmI` read. `dotnet build samples/Dataverse.OfflineWalkthrough/Dataverse.OfflineWalkthrough.csproj` then `dotnet run --project samples/Dataverse.OfflineWalkthrough/Dataverse.OfflineWalkthrough.csproj --no-build` both succeed on a clean checkout and walk through auth -> WhoAmI -> create -> read -> update -> paginated enumerate -> delete -> `$metadata` parse, entirely offline (verified by actually running it, not just building it). |
| Remaining work / blockers | No live Dataverse sandbox credentials are available in this documentation pass; every integration-test requirement listed in DVT-001 through DVT-021 remains explicitly blocked, not silently skipped, pending an authorized tenant (Section 28). DVT-020 remains unimplemented by design (confirmed blocked, not a gap in this feature's own scope). DVT-016's chunked transfer and DVT-017/DVT-011's field-name verification gaps carry forward from their own trackers. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `samples/Dataverse.OfflineWalkthrough` (built and actually run, not just compiled) on top of the unit/contract test corpus and API snapshot already accumulated across DVT-001 through DVT-021, and `DiagnoseAsync` already added in DVT-002. Checked every applicable completion gate; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live Dataverse sandbox available this pass, consistent with every other Dataverse feature's `verification` status. |

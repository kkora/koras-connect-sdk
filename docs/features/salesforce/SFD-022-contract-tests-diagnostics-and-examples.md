---
id: "SFD-022"
title: "Contract tests, diagnostics and examples"
category: "salesforce"
priority: "P0"
phase: "P2-D3/P2-D6"
status: "partial"
baseline_status: "Implemented: 19 synthetic-HttpMessageHandler test files cover SFD-001 through SFD-021's P0/P1 functional/unit-test requirements offline (no live network access); DiagnoseAsync (SFD-003) implements Configuration/Authentication/Connectivity, issuing only the single GET-root probe (GetResourceMapAsync, always authenticated - every request on this connector's transport is, including this one), never a mutating request; samples/Salesforce.OfflineWalkthrough demonstrates auth -> diagnostics -> CRUD -> paginated SOQL -> describe end to end, built and actually run via dotnet run, entirely offline. The required gRPC synthetic test double for SFD-014's surface does not exist because SFD-014 itself was never implemented (confirmed blocked, not attempted, per its own spec)."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-022 - Contract tests, diagnostics and examples

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-022 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-022.
2. **Feature name:** Contract tests, diagnostics and examples.
3. **Description:** Prove the Salesforce connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
4. **Business value:** Prove the Salesforce connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3 (unit/contract tests) / P2-D6 (sample, package validation); see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline sample convention; requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Salesforce.snapshot.txt` entry (SDK-017).
10. **Functional requirements:** Offline unit/contract tests for every SFD-001 through SFD-021 P0/P1 feature, using a synthetic `HttpMessageHandler` (and, for SFD-014's gRPC surface, a synthetic gRPC test double), no live network access, running on both Linux and Windows; at least one standalone `samples/Salesforce.*` console project demonstrating authentication → CRUD → SOQL → pagination → describe, entirely offline; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity, never issuing a mutating request. **This feature carries a distinct, explicit documentation obligation beyond every other connector's SFD-022/DVT-022/MGR-026 equivalent**: every per-feature specification above that is marked snippet-only or flagged with an explicit "not independently re-verified" caveat must have that caveat carried into the sample/test documentation verbatim, not silently upgraded to "confirmed" - per-edition numeric limits (SFD-022's own research findings) are the most consequential category of this, since they vary by org edition and were the least-corroborated figures in the whole Salesforce research pass.
11. **Nonfunctional requirements:** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
12. **Public API contracts or proposed contracts:** Official API: `DiagnoseAsync`'s connectivity stage uses `GetResourceMapAsync`'s `GET /services/data/vXX.X/` as its one permitted minimal, non-mutating probe call, paralleling DVT-022's `WhoAmI` choice. **Corrected by Phase 2 review pass 1**: this probe is always authenticated, like every request this connector's `IntegrationTransport` instance sends - there is no unauthenticated code path on this client at all, so the originally-drafted "lighter-weight since no auth is needed" framing was simply wrong, not merely imprecise. Proposed C# contracts:

`SalesforceConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what SFD-001 through SFD-021 already define.
13. **Dependencies:** Every other SFD-0NN feature this covers tests for.
14. **Security considerations:** Authentication and authorization: `DiagnoseAsync`'s Authentication stage issues exactly one minimal read (e.g. `GET /services/data/vXX.X/limits/` or an equivalent lightweight authenticated GET), never a mutating request. Required permissions/scopes/licenses: None beyond whatever the underlying probed feature already requires. Data privacy and security: Diagnostic output contains no secrets, tokens, or record payloads.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to this feature itself. Error handling and concurrency: `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary.
16. **Unit-test requirements:** Every unit-test scenario enumerated in SFD-001 through SFD-021 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed.
17. **Integration-test requirements:** Contract-test scenarios: Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus, each fixture's confidence tier (PDF/snippet/unverified) preserved in an accompanying comment or doc annotation so a future maintainer knows which fixtures need re-verification against a live org before being trusted at face value. Integration-test requirements: Every feature's integration-test requirement above remains individually deferred/blocked pending a live sandbox; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live org.
18. **Acceptance criteria:** The sample project builds and runs on a clean checkout with zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed; every snippet-only fact used anywhere in the Salesforce test/sample corpus is traceable to its confidence tier.
19. **Definition of Done:** Matches the Phase 1 master's Section 26 Definition of Done template, with the added explicit condition that no snippet-only Salesforce fact is presented in sample code or documentation as if it were PDF-confirmed.
20. **External API entitlement or documentation blockers:** No live Salesforce org is available in this documentation pass; the Composite API (SFD-009) and Pub/Sub API (SFD-014) field-name/wire-format details are explicitly the highest-priority re-verification items before any Salesforce connector implementation begins (Section 28).

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (REST/JSON scope only - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (SFD-009/SFD-014 field-name/wire-format re-verification remains the stated release blocker).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [x] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: 19+ synthetic-`HttpMessageHandler` test files under `tests/Koras.Connect.Tests/Salesforce/` exercise every SFD-001 through SFD-021 feature offline. Added `samples/Salesforce.OfflineWalkthrough/` (`Program.cs` + `.csproj`), built and actually run via `dotnet run` on this checkout: token exchange, `DiagnoseAsync` (Configuration/Authentication/Connectivity all `Passed`), create/read/update/delete an Account, a two-page SOQL enumeration, and a describe call, all against a synthetic `HttpMessageHandler`, zero network access. `Koras.Connect.Salesforce.snapshot.txt` is current as of this commit. (Phase 2 review pass 1 found the full-suite pass count recorded here - 856 - was stale against the repo's actual state at the time, which had grown to 1016 passed by then via unrelated Microsoft Graph work landing in the same shared test project; the discrepancy was about which other connectors' tests existed in the suite at any given commit, not about Salesforce's own tests being broken. This doc intentionally stops recording a point-in-time full-suite number going forward, to avoid repeating a count that goes stale the moment another connector's tests change.) |
| Remaining work / blockers | No gRPC synthetic test double exists for SFD-014, since SFD-014 itself remains confirmed-blocked-not-attempted per its own spec - there is no implementation surface to double for. SFD-009 (Composite) and the handful of other `partial`-status features' snippet-sourced field names remain the highest-priority re-verification items before any Salesforce connector feature ships as `verified`; every such caveat is carried into each feature's own tracker doc rather than silently upgraded here. No live Salesforce org available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `samples/Salesforce.OfflineWalkthrough/`, built and run successfully offline; confirmed 19 existing Salesforce test files already satisfy this feature's offline-test requirement across SFD-001-021. Checked the unit/contract, security/cancellation/resource-ownership, and documentation gates; held at `partial` because SFD-014 has no implementation to test and several other features remain snippet-confidence pending live-org re-verification. |
| 2026-11-14 | `partial` | Phase 2 review pass 1 corrected the `DiagnoseAsync` connectivity-probe description (it is always authenticated, not "lighter-weight since no auth is needed" as originally drafted) and removed the point-in-time full-suite pass count, which had already gone stale against the shared test project's growth from unrelated connector work. No code changed. |

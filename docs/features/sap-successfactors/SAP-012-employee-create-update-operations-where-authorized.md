---
id: "SAP-012"
title: "Employee create/update operations where authorized"
category: "sap-successfactors"
priority: "P1"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-012 - Employee create/update operations where authorized

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-012.
2. **Feature name:** Employee create/update operations where authorized.
3. **Description:** Support intentional employee mutations with real entity permissions.
4. **Business value:** Support intentional employee mutations with real entity permissions.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP generic opt-in UpsertAsync; no employee command validation; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add operation-specific commands using supported create/update/upsert semantics; keys/date validation, explicit write opt-in and target-population enforcement by SAP.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed typed commands atop UpsertAsync where supported; no invented universal PATCH/MERGE API.
13. **Dependencies:** SAP-003, SAP-007, SAP-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Read-only entity, missing key, invalid date, row rejection and uncertain write outcome. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Read-only entity, missing key, invalid date, row rejection and uncertain write outcome.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Only verified authorized operations are exposed; default mode sends no writes; fixtures and disposable sandbox cases demonstrate documented changes.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity-specific operations/RBP and separate sandbox write authorization required [S2].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `UpsertEmploymentAsync`/`UpsertJobAsync` (`src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.WriteCommands.cs`), thin typed wrappers over the already-verified `UpsertAsync` primitive (SAP-003/005) for the `EmpEmployment`/`EmpJob` entities specifically - no invented universal PATCH/MERGE API. Each validates its entity's documented composite key locally, before any HTTP call: `userId`+`startDate` for `EmpEmployment` (a missing or unparseable `startDate` is rejected via the same `SapDates.ParseUtc` policy used elsewhere), `userId`+`seqNumber` for `EmpJob` (accepting a numeric string the same way `EmpJob.SequenceNumber` itself does). The same explicit `writesEnabled` opt-in gate is checked first, before any row validation, matching every other write path's convention. New tests in `tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs`: `UpsertEmploymentAsyncValidatesTheCompositeKeyLocallyBeforeAnyHttpCall` (missing key, missing/invalid date, missing opt-in, valid), `UpsertEmploymentAsyncStillSurfacesARowLevelRejectionForAReadOnlyEntity` ("read-only entity"), `UpsertJobAsyncValidatesTheCompositeKeyLocallyBeforeAnyHttpCall`, `UpsertEmploymentAsyncNeverAutomaticallyRetriesOnATransportFailureLeavingTheOutcomeUncertain` ("uncertain write outcome"). Regenerated the committed `Koras.Connect.Sap.SuccessFactors.snapshot.txt` API baseline for the new public methods. Full suite: 557 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Entity-specific operations/RBP and separate sandbox write authorization against a live, authorized tenant [S2] remain standing external blockers - whether a given tenant's `EmpEmployment`/`EmpJob` actually support upsert, and what fields are required beyond the documented key, is still unverified. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `UpsertEmploymentAsync`/`UpsertJobAsync`, typed wrappers over `UpsertAsync` with local composite-key validation, closing the "read-only entity", "missing key", "invalid date" and "uncertain write outcome" unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: entity-specific operations/RBP and sandbox write authorization against a live, authorized tenant [S2] remain standing external blockers. |

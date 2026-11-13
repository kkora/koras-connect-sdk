---
id: "SAP-003"
title: "Upsert operations"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-003 - Upsert operations

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-003.
2. **Feature name:** Upsert operations.
3. **Description:** Submit authorized insert/update batches with per-row outcomes.
4. **Business value:** Submit authorized insert/update batches with per-row outcomes.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP UpsertAsync/UpsertResult; E-TEST HTTP200 row failures and no-retry tests; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Keep writesEnabled=false default and SDK 1-1000 bound; inject validated entity metadata, inspect every status/httpCode/index, never infer atomic success from HTTP200.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing UpsertAsync; POST /odata/v2/upsert?$format=json.
13. **Dependencies:** SAP-001, SAP-014; primitive precedes SAP-012 typed employee commands; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Mixed row outcomes, metadata injection, count boundaries, malformed results and unknown POST outcome. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Mixed row outcomes, metadata injection, count boundaries, malformed results and unknown POST outcome.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Every row has attributable result; default call sends nothing; explicit batch is sent once; row failures remain visible without logging their private messages.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity upsert support, required keys/fields, RBP and disposable sandbox objects required [S2].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "count boundaries" and "malformed results" unit-test-requirement gaps, and added the `editStatus`/`inlineResults` fields confirmed against the **official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), section 6.3.6.4.1 "HTTP Response Status Codes for Upsert"** - `editStatus` distinguishes an insert ("INSERTED") from an update ("UPDATED") within the same upsert call, information `httpCode` alone cannot always convey. Added to `UpsertResult` in `src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.cs`. New tests in `tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs`: `UpsertAsyncEnforcesTheOneToOneThousandRecordBoundaryExactly` (0/1000/1001-record theory, exact boundary), `UpsertAsyncThrowsRatherThanSilentlyReturningNoResultsOnAMalformedBody` (missing `d`, `d` not array/object), `UpsertResultPreservesEditStatusDistinguishingInsertFromUpdate`. Regenerated the committed `Koras.Connect.Sap.SuccessFactors.snapshot.txt` API baseline for the new `UpsertResult` fields. Full suite: 520 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Entity upsert support, required keys/fields, RBP and disposable sandbox objects against a live, authorized tenant [S2] remain standing external blockers (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Closed the "count boundaries" and "malformed results" unit-test-requirement gaps, and added the vendor-confirmed `editStatus`/`inlineResults` fields. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: entity upsert support, RBP and disposable sandbox objects against a live, authorized tenant [S2] remain standing external blockers. |

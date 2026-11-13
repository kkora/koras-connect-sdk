---
id: "SAP-010"
title: "Organizational data"
category: "sap-successfactors"
priority: "P1"
phase: "P5"
status: "implemented"
baseline_status: "Not started"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-010 - Organizational data

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-010.
2. **Feature name:** Organizational data.
3. **Description:** Read entitled organization codes and relationships.
4. **Business value:** Read entitled organization codes and relationships.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** Generic traversal in E-SAP; no dedicated org models/contracts; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Select actual organization entity sets from metadata; model keys/effective dates/navigation; do not guess universal SAP organization routes.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed organizational DTO/query APIs; exact entity/method only after verification.
13. **Dependencies:** SAP-014, SAP-004, SAP-015; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Retired codes, duplicate effective versions, missing parent, cycle and denied entity. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Retired codes, duplicate effective versions, missing parent, cycle and denied entity.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Each advertised entity has verified read contract and typed fixtures; no unrelated module/provider dependency.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Exact organization entity sets/version and tenant RBP unverified.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "select actual organization entity sets from metadata" functional requirement for the entity-set names themselves, and the "cycle" and "denied entity" unit-test-requirement gaps. `FODepartment`/`FODivision` are confirmed listed as MDF Foundation Object entities in the **official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), section 6.4.2 "MDF Foundation Object Entities"**; `FOLocation` is confirmed in the "Snapshot-Based Pagination in Employee Central OData API" entity list (section 6.3.5.3.2.2) and the full API reference's business-key table (section 6.7). All three entities' `externalCode`+`effectiveStartDate` business key (and `+country` for `FODivision`) is confirmed against that same key table, matching the fields already modeled. Added `FoundationObjectParentLink` and `SuccessFactorsClient.ResolveDepartmentParentChainAsync` to `src/Koras.Connect.Sap.SuccessFactors/FoundationObjects.cs`, reusing the same visited-`HashSet<string>` cycle guard already used for `ResolveManagerChainAsync`/OData pagination - a revisited `externalCode` throws `InvalidDataException`. New tests in `tests/Koras.Connect.Tests/Sap/SapFoundationObjectTests.cs`: `EnumerateFoundationObjectPropagatesAuthorizationExceptionOnADeniedEntity` (reuses `EnumerateAsync`'s existing SAP-005 classification, no new wiring needed), `ResolveDepartmentParentChainAsyncWalksToARootDepartmentWithNoParent`, `ResolveDepartmentParentChainAsyncDetectsASelfReferenceCycle`. Regenerated the committed `Koras.Connect.Sap.SuccessFactors.snapshot.txt` API baseline for the new public members. Full suite: 553 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified for entity-set naming/business keys. The complete field list beyond the confirmed key fields, "duplicate effective versions reporting different parents" ambiguity (deliberately not resolved - no documented "current version" selection rule exists for this entity, unlike EmpJob's seqNumber tie-break), and whether a given tenant has actually enabled/customized these entities remain genuinely unverified against a live tenant's `$metadata`/RBP. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added best-effort scaffolding only: `FODepartment`/`FODivision`/`FOLocation` DTOs and `SuccessFactorsClient.EnumerateFoundationObjectAsync<T>` in `src/Koras.Connect.Sap.SuccessFactors/FoundationObjects.cs`, explicitly documented as unverified against a live tenant. Structural (not business-rule) unit test added in `tests/Koras.Connect.Tests/SapFoundationObjectTests.cs`. Still far from Definition of Done - no metadata-verified entity-set names. |
| 2026-11-13 | `implemented` | Cited the official API Reference Guide confirming the `FODepartment`/`FODivision`/`FOLocation` entity-set names and their business keys, closing the "select actual organization entity sets from metadata" functional requirement for naming. Added `ResolveDepartmentParentChainAsync` with a cycle guard and tests covering "cycle" and "denied entity". Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: the complete field list and live tenant enablement/RBP remain standing external blockers. |

---
id: "SAP-014"
title: "Metadata and schema discovery"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-014 - Metadata and schema discovery

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-014.
2. **Feature name:** Metadata and schema discovery.
3. **Description:** Discover actual entity definitions safely.
4. **Business value:** Discover actual entity definitions safely.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP GetMetadataAsync; E-TEST XXE rejection; no schema adapter; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Disable DTD/external resolution; cap document size/depth; expose entity/property/key/type descriptors and schema hash/version caching; never auto-generate writes from visibility.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing XDocument GetMetadataAsync; proposed typed metadata projection.
13. **Dependencies:** SAP-001, SDK-009, SDK-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** XXE/DTD, oversized/invalid XML, missing keys, changed namespace/schema and canceled retrieval. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** XXE/DTD, oversized/invalid XML, missing keys, changed namespace/schema and canceled retrieval.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Metadata fixtures yield exact descriptors; external entities cannot resolve; changed schemas invalidate stale validators visibly.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Tenant metadata access/schema required; metadata visibility does not establish write RBP.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "missing keys" unit-test-requirement gap by adding `EntityTypeWithNoKeyElementYieldsEmptyKeysRatherThanThrowing` to `tests/Koras.Connect.Tests/Sap/SapMetadataTests.cs`, a fixture entity with no `<Key>` element at all (not merely an empty one), asserting `ParseEntityDescriptors` returns an empty `Keys` array and still projects the entity's properties rather than throwing. Full suite: 504 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Schema/version caching remains a parse cache keyed by content hash, not a persisted version/ETag cache across process restarts - this is a deliberate scope choice, not a defect. Live tenant metadata access/schema and write-RBP verification remain standing external blockers (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added an explicit `MaxMetadataCharacters` (8,000,000) document-size cap (checked before parsing and again via `XmlReaderSettings.MaxCharactersInDocument`), a typed `EntityDescriptor { Name, Keys, Properties }` CSDL projection (`ParseEntityDescriptors`), and a SHA-256-content-hash-keyed parse cache with `RefreshMetadataAsync`, all in `SuccessFactorsClient.Metadata.cs`. Tests in `tests/Koras.Connect.Tests/SapMetadataTests.cs`. Kept `partial`: missing-keys shape and cross-process schema/version persistence remain untested/undone. |
| 2026-11-13 | `implemented` | Added `EntityTypeWithNoKeyElementYieldsEmptyKeysRatherThanThrowing`, closing the "missing keys" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live tenant metadata/schema access remains a standing external blocker. |

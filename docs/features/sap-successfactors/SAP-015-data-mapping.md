---
id: "SAP-015"
title: "Data mapping"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-015 - Data mapping

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-015.
2. **Feature name:** Data mapping.
3. **Description:** Keep date, key and code meanings explicit in neutral outputs.
4. **Business value:** Keep date, key and code meanings explicit in neutral outputs.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP extension-preserving DTOs; no mapper interface; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add versioned person/employment/job projections, raw-date conversion policy and source-key provenance; no ADP-to-SAP workflow.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<SapEntity,T>/MappingResult<T>.
13. **Dependencies:** SDK-002, SAP-008, SAP-009; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** SAP date representations/timezones, composite keys, masked/custom fields, invariant culture and unknown codes. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** SAP date representations/timezones, composite keys, masked/custom fields, invariant culture and unknown codes.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Golden mappings are deterministic with explicit date policy; omitted/error fields are visible and original source remains intact.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity schema and business profiles require review; customer-specific rules remain host-configured.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "composite keys" unit-test-requirement gap by adding `EmpEmploymentMapper`/`EmpJobMapper` (`IRecordMapper<EmpEmployment,EmploymentRecord>`/`IRecordMapper<EmpJob,JobRecord>`) to `src/Koras.Connect.Sap.SuccessFactors/SapMapping.cs`, following `PerPersonMapper`'s omission/field-error pattern: a record missing `userId` entirely is omitted (no identifier at all); a record with `userId` but no parseable `startDate` (EmpEmployment) or no `seqNumber` (EmpJob) - the field that completes its documented composite key - fails with a field-level `FieldError` rather than inventing a partial key. `EmpJob.StartDate` is deliberately *not* part of its key (unlike EmpEmployment's) and tolerates an unparseable value as `null`, matching `ToManagerAssignment`'s existing policy for the same field. New tests in `tests/Koras.Connect.Tests/Sap/SapMappingTests.cs`: `EmpEmploymentMapperOmitsARecordWithNoUserIdAtAll`, `EmpEmploymentMapperFailsWithAFieldErrorWhenStartDateIsMissingBecauseTheCompositeKeyCannotForm`, `EmpEmploymentMapperBuildsTheCompositeKeyFromUserIdAndStartDateAndPreservesOpenEndedEndDate`, `EmpJobMapperOmitsARecordWithNoUserIdAtAll`, `EmpJobMapperFailsWithAFieldErrorWhenSeqNumberIsMissingBecauseTheCompositeKeyCannotForm`, `EmpJobMapperBuildsTheCompositeKeyFromUserIdAndSeqNumberAndToleratesAnUnparseableStartDate`. Regenerated the committed `Koras.Connect.Sap.SuccessFactors.snapshot.txt` API baseline for the new public types. Full suite: 550 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Masked/custom-field and unknown-code handling beyond `AdditionalData` passthrough, and timezone-specific handling beyond `SapDates`'s UTC-normalization policy, remain out of scope - customer-specific rules stay host-configured, not invented by this SDK. Entity schema/business-profile review against a live tenant remains a standing external blocker (see above). |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Implemented `Koras.Connect.Abstractions.IRecordMapper<PerPerson,PersonProfile>` via `PerPersonMapper`, plus a documented `SapDates` date-parsing policy (always UTC `DateTimeOffset`; throws `FormatException` on an unrecognized-but-present value rather than returning null) in `src/Koras.Connect.Sap.SuccessFactors/SapMapping.cs`/`SapDates.cs`. Golden-fixture tests in `tests/Koras.Connect.Tests/SapMappingTests.cs`. Kept `partial`: `EmpEmployment`/`EmpJob` mappers and composite-key/masked-field handling remain outstanding. |
| 2026-11-13 | `implemented` | Added `EmpEmploymentMapper`/`EmpJobMapper`, closing the "composite keys" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: masked/custom-field semantics and entity schema/business-profile review against a live tenant remain standing external blockers. |

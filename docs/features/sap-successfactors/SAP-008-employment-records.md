---
id: "SAP-008"
title: "Employment records"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SAP-008 - Employment records

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-008.
2. **Feature name:** Employment records.
3. **Description:** Preserve employment identity and dates separately from person profiles.
4. **Business value:** Preserve employment identity and dates separately from person profiles.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP EmpEmployment/EnumerateAsync<T>; no dedicated employment method; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add typed employment queries/projection, documented composite keys, multiple employments and extension data; define effective-date and absence rules.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EmpEmployment; proposed EnumerateEmploymentAsync after contract review.
13. **Dependencies:** SAP-002, SAP-004, SAP-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Concurrent employments, absent endDate, SAP date values, composite keys and invisible records. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Concurrent employments, absent endDate, SAP date values, composite keys and invisible records.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures retain employment keys/dates and multiple employments; authorized query scope and field meanings are documented.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** EmpEmployment current metadata/RBP/target population unavailable.

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
| Implementation revision | Uncommitted working tree as of 2026-11-12; see `src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.cs` (`EnumerateEmploymentAsync`) |
| Validation commands / results | `dotnet build Koras.Connect.slnx` - 0 errors; `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj` - 145/145 passed, including `tests/Koras.Connect.Tests/SapEmploymentAndJobTests.cs` |
| Remaining work / blockers | Entity/field RBP and target-population behavior for EmpEmployment remain unverified against a live tenant; composite-key documentation is based on the EmpJob/EmpEmployment shapes already in code, not a reviewed metadata contract |
| Acceptance / completion | Unit-level only: `EnumerateEmploymentPreservesConcurrentRecordsOpenEndedDatesAndUnknownFields` in `SapEmploymentAndJobTests.cs` covers concurrent employments (same userId, different startDate), absent endDate, both `/Date(ms)/` and ISO-8601 date shapes, and `AdditionalData` preservation. Live/sandbox acceptance not recorded. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Added `SuccessFactorsClient.EnumerateEmploymentAsync` convenience wrapper over `EnumerateAsync<EmpEmployment>`, plus dedicated unit tests in `tests/Koras.Connect.Tests/SapEmploymentAndJobTests.cs` covering concurrent employments, open-ended (absent) endDate, both SAP date shapes and unknown-field preservation. `verification` remains `unverified` - no live-tenant evidence was gathered. |

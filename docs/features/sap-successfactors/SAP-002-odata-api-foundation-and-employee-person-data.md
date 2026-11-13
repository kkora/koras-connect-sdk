---
id: "SAP-002"
title: "OData API foundation and employee/person data"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-002 - OData API foundation and employee/person data

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-002.
2. **Feature name:** OData API foundation and employee/person data.
3. **Description:** Read person records independently of other connectors.
4. **Business value:** Read person records independently of other connectors.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP SuccessFactorsClient/PerPerson; E-TEST SapPagesPreserveUnknownFieldsAndEffectiveDatesWithoutChangingNextLink; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Use explicit /odata/v2 root, metadata-named entity sets and d.results/d.__next parsing; preserve typed/unknown fields and masked/absent values.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EnumeratePersonsAsync/EnumerateAsync<T>/PerPerson; proposed Page<T> adapter.
13. **Dependencies:** SAP-001, SDK-007; shared paging contract before SAP-004 adapter completion; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Empty/malformed d.results, null fields, unknown extensions, unsafe entity name and access denial. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Empty/malformed d.results, null fields, unknown extensions, unsafe entity name and access denial.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Person fixtures preserve all mapped/unknown values; denied RBP is not reported as empty data; isolated consumer references no ADP package.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity/field visibility and target population require tenant RBP/current metadata; no universal employee schema.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "unsafe entity name" and "empty/malformed d.results" unit-test-requirement gaps by adding `EnumerateAsyncRejectsUnsafeEntityNameBeforeAnyHttpCall` (a `"Per Person; DROP TABLE"`-style entity string must be rejected by `ValidateEntity` before any request, 0 HTTP calls) and `EnumerateAsyncThrowsRatherThanSilentlyYieldingNothingOnAMalformedBody` (three malformed-shape cases: missing `d`, missing `d.results`, and `d` not an object) to `tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs`, asserting each surfaces a thrown exception rather than a silently-empty enumeration indistinguishable from "no records". Full suite: 508 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Entity/field visibility and target population require tenant RBP/current metadata; no universal employee schema exists or is claimed (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `EnumerateAsyncRejectsUnsafeEntityNameBeforeAnyHttpCall` and `EnumerateAsyncThrowsRatherThanSilentlyYieldingNothingOnAMalformedBody`, closing the "unsafe entity name" and "empty/malformed d.results" unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: entity/field RBP and target-population visibility against a live tenant remain standing external blockers. |

---
id: "ADP-007"
title: "Worker schema mapping"
category: "adp"
priority: "P0"
phase: "P3"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-007 - Worker schema mapping

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-007.
2. **Feature name:** Worker schema mapping.
3. **Description:** Create explicit auditable export/domain projections without losing vendor fields silently.
4. **Business value:** Create explicit auditable export/domain projections without losing vendor fields silently.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-WORKERS typed subset/JSON fragments; E-EXPORT caller projections; no mapping-provider interface; aliases resolve in section 3.
10. **Functional requirements:** Provide opt-in mapper/projection profiles for identity, legal name and assignments; preserve absence/masking; version profiles and distinguish effective assignments; no built-in ADP-to-SAP workflow.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<Worker,T> and MappingResult<T>; current caller Func mappings.
13. **Dependencies:** ADP-019, SDK-002, SDK-013; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing names, multiple assignments, unknown codes, masked identifiers, future properties, effective dates and culture. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing names, multiple assignments, unknown codes, masked identifiers, future properties, effective dates and culture.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Golden synthetic worker-to-neutral projection tests pass; omitted/masked/invalid fields produce explicit documented results; source model remains unchanged.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Field meanings and authorized metadata vary per tenant; obtain exact schema/codes before claiming full worker mapping.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 183, Skipped 1, Failed 0, Total 184 (includes `AdpWorkerMappingTests`) |
| Remaining work / blockers | Mapper profiles are unversioned (a single `WorkerIdentityMapper` shape); no official ADP schema/code-table evidence has been attached, so field meanings are not tenant-confirmed. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Added `Koras.Connect.Adp.Workers.WorkerIdentityMapper` implementing `IRecordMapper<Worker, WorkerIdentity>` and `EffectiveAssignmentSelector.SelectEffective` (`src/Koras.Connect.Adp/Workers/WorkerMapping.cs`). The mapper returns `MappingResult<WorkerIdentity>.Omitted(...)` when legal name is entirely absent, `Failure([...])` with field-level `FieldError`s when given/family name is present-but-incomplete, and `Success(...)` with a `null` `ExternalId` when `workerID` is masked/absent (never fabricated). `EffectiveAssignmentSelector` picks the assignment with the latest `hireDate` on or before the target date whose `terminationDate` (if any) is on or after it. Golden-fixture tests added in `tests/Koras.Connect.Tests/AdpWorkerMappingTests.cs` cover missing legal name, incomplete name, masked/absent identifier, multiple overlapping assignments, no assignment covering the date, and missing/unparseable hire dates. Kept at `implemented` rather than fully accepted because no official ADP schema/code-table evidence or live tenant validation backs the mapped field semantics. |

---
id: "SAP-011"
title: "Manager relationships"
category: "sap-successfactors"
priority: "P1"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-011 - Manager relationships

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-011.
2. **Feature name:** Manager relationships.
3. **Description:** Expose reporting lines with temporal and employment context.
4. **Business value:** Expose reporting lines with temporal and employment context.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP EmpJob.ManagerId only; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Define job/user relationship projections and documented navigation; handle missing/external/inaccessible managers and multiple employments without whole-tenant recursive scans.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EmpJob.ManagerId; proposed typed manager relationship projection.
13. **Dependencies:** SAP-009, SAP-015; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Missing manager, self-reference/cycle, multiple assignments, denied target and date boundary. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Missing manager, self-reference/cycle, multiple assignments, denied target and date boundary.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures preserve temporal/identity context; inaccessible manager is explicit; traversal cannot loop indefinitely.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Navigation names and manager visibility require current metadata/RBP.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "denied target" and "date boundary" unit-test-requirement gaps. Added `ResolveManagerChainAsyncPropagatesAuthorizationExceptionOnADeniedTarget` (confirms a 403 at the underlying `EnumerateJobsAsync` layer propagates as the classified `AuthorizationException`, never swallowed/misreported as "no manager"), `SameStartDateBoundaryBreaksTheTieOnTheHigherSequenceNumber` (two records effective on the exact same date resolve deterministically via the `seqNumber` tie-break), and `AnUnparseableStartDateNeverOutranksARecordWithAValidDate` (a garbage `startDate`, safely treated as `DateTimeOffset.MinValue` by `SafeDate`, never wins over a real date) to `tests/Koras.Connect.Tests/Sap/SapManagerResolutionTests.cs`. Full suite: 527 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Navigation names and manager visibility against live metadata/RBP remain standing external blockers (see above), unchanged by this pass. |
| Acceptance / completion | Unit-level only: `SapManagerResolutionTests.cs` covers missing manager (null `ManagerId`, no error), self-reference, a 3-node cycle (A->B->C->A), multiple concurrent employments resolving to the latest-startDate manager, a maxDepth bound that truncates without throwing on a non-cyclic chain, a denied-target authorization propagation, and two date-boundary tie-break cases. Live/sandbox acceptance not recorded. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `ManagerAssignment { UserId, ManagerId, EffectiveDate }` projection and `SuccessFactorsClient.ResolveManagerChainAsync(userId, maxDepth, ct)` in `SuccessFactorsClient.Managers.cs`, using the same visited-`HashSet<string>` cycle guard already used for OData pagination. Covers missing manager, self-reference, a 3-node cycle and concurrent-employment manager selection in `tests/Koras.Connect.Tests/SapManagerResolutionTests.cs`. Kept `partial`: "denied target" and "date boundary" edge cases from the spec are not separately tested, and navigation is unverified against live metadata. |
| 2026-11-13 | `implemented` | Added `ResolveManagerChainAsyncPropagatesAuthorizationExceptionOnADeniedTarget`, `SameStartDateBoundaryBreaksTheTieOnTheHigherSequenceNumber` and `AnUnparseableStartDateNeverOutranksARecordWithAValidDate`, closing the "denied target" and "date boundary" unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: navigation names/manager visibility against live metadata/RBP remain standing external blockers. |

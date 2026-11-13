---
id: "QLT-009"
title: "Mailing lists and segments"
category: "qualtrics"
priority: "P1"
phase: "P6"
status: "partial"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-009 - Mailing lists and segments

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-009.
2. **Feature name:** Mailing lists and segments.
3. **Description:** Organize contact groups without conflating static lists and dynamic segments.
4. **Business value:** Organize contact groups without conflating static lists and dynamic segments.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No list/segment models or methods in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Verify list membership and segment queries separately; typed IDs/results, authorized reads and explicit writes; preserve consent/opt-out.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed list/segment adapters from current schemas [Q7]; segment retrieval incomplete.
13. **Dependencies:** QLT-006, QLT-004, QLT-012; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Missing list, dynamic changes, duplicate membership, consent changes, partial failure and denied permission. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Missing list, dynamic changes, duplicate membership, consent changes, partial failure and denied permission.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each advertised method has exact current contract/tests; unsupported segment operations remain unknown/absent, never simulated.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** List family documented; segments page fetch failed; actual operation schemas/licenses/permissions unverified.

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
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "missing list"/"denied permission" unit-test-requirement gap by adding `GetMailingListClassifiesForbiddenAndNotFoundWithoutLeakingBody` (403/404) and `CreateMailingListClassifiesForbiddenAndConflictWithoutLeakingBody` (403/409) to `tests/Koras.Connect.Tests/Qualtrics/QualtricsExpansionTests.cs`, matching the classification pattern already used for contacts/surveys. Full suite: 483 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | "Dynamic changes", "duplicate membership", "consent changes" and "partial failure" remain unaddressed - mailing lists in this SDK are static groups only, with no dedicated membership-mutation or segment API yet. Segments (the other half of this feature) remain entirely unaddressed; the [Q7] segment-retrieval schema blocker is still open. Status stays `partial`. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `QualtricsMailingList` DTO and `ListMailingListsAsync`/`GetMailingListAsync` (read, default-enabled) plus `CreateMailingListAsync` (write, explicit `writesEnabled` opt-in) in `src/Koras.Connect.Qualtrics/QualtricsDirectoryClient.cs`, reusing the shared paging engine from QLT-004. **Endpoint shape (`GET/POST /directories/{directoryId}/mailinglists`, `GET /directories/{directoryId}/mailinglists/{mailingListId}`) is based on the publicly documented Qualtrics API v3 contract; it is NOT verified against a live tenant.** Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`ListMailingListsPagesUsingTheSharedPagingHelper`, `CreateMailingListRequiresNameLocallyAndExplicitOptIn`). Segments (the other half of this feature) remain entirely unaddressed — the [Q7] segment-retrieval schema blocker noted below is still open and no segment model/method was added, so this is deliberately `partial`, not `implemented`. `dotnet build`/`dotnet test` passing as of this revision. |
| 2026-11-13 | `partial` | Added `GetMailingListClassifiesForbiddenAndNotFoundWithoutLeakingBody` and `CreateMailingListClassifiesForbiddenAndConflictWithoutLeakingBody`, closing the "missing list"/"denied permission" unit-test-requirement gap. Dynamic-membership mutation, segments, and the [Q7] schema blocker remain open; status stays `partial`. |

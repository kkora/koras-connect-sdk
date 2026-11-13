---
id: "QLT-004"
title: "Pagination and transport resilience"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-004 - Pagination and transport resilience

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-004.
2. **Feature name:** Pagination and transport resilience.
3. **Description:** Traverse results without credential forwarding or infinite loops.
4. **Business value:** Traverse results without credential forwarding or infinite loops.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-QLT nextPage/maxPages; E-SAFETY; E-TEST hostile continuation tests; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Preserve valid nextPage within configured origin/API path; cycle/page guards, neutral adapter and query-bound resume; no provider-wide offset guess.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing survey iterator/TransportOptions; proposed per-family Page<T> adapter.
13. **Dependencies:** SDK-007, SDK-008, SDK-010; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Host/path/protocol changes, repeated URL, empty page with next link, expired cursor and canceled delay. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Host/path/protocol changes, repeated URL, empty page with next link, expired cursor and canceled delay.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Next links are preserved and hostile links rejected before send; future directory/batch families use their own verified paging contracts.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Current per-family cursor/limit/expiration schemas required; no universal pagination assumption.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "expired cursor" unit-test-requirement gap by adding `ResumingAnExpiredCursorSurfacesTheRawProviderStatusWithoutRetryingOrHanging` to `tests/Koras.Connect.Tests/Qualtrics/QualtricsExpansionTests.cs`. Fetches a first page (valid, same-query continuation), then resumes it against a server that now answers the follow-up GET with `400 Bad Request` - simulating a cursor the provider has since expired. Asserts the failure surfaces as the raw `IntegrationRequestException` with `ResponseStatus == BadRequest`, never retried (exactly 2 calls total) and never miscategorized as `QualtricsApiException`, matching the pagination-stays-raw split already established for QLT-005/ADP-010. Full suite: 474 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Current per-family cursor/limit/expiration schemas against a live tenant remain an external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Extracted the previously-inlined cycle/bound-guard pagination loop into a reusable internal engine (`src/Koras.Connect.Qualtrics/QualtricsPaging.cs`: `EnumerateAsync`, `FetchPageAsync`, `ResolveResume`), now shared by `EnumerateSurveysAsync` and the new contacts/mailing-list list methods. Added a `Page<T>`/`ContinuationToken` (`Koras.Connect.Abstractions`) adapter (`QualtricsClient.ListSurveysPageAsync`, `ListContactsPageAsync`) so a caller can pause and resume instead of only re-iterating from scratch; a continuation is rejected if replayed against a different query (`QualtricsPaging.ResolveResume`). Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`EmptyPageWithValidNextLinkContinuesToTheNextPage`, `EnumerateSurveysCycleAndBoundsStillEnforcedAfterPagingExtraction`, `SharedPagingHelperRejectsOffOriginLinkForContactsFamilyToo`, `ListSurveysPageAsyncSupportsPauseAndResumeWithoutRefetchingTheFirstPage`, `ContactsContinuationCannotBeReplayedAgainstTheSurveysEndpoint`). No universal provider-wide offset assumption was introduced; each family still supplies its own start address. `dotnet build`/`dotnet test` passing as of this revision. |
| 2026-11-13 | `implemented` | Added `ResumingAnExpiredCursorSurfacesTheRawProviderStatusWithoutRetryingOrHanging`, closing the "expired cursor" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live per-family cursor/limit/expiration schemas remain a standing external blocker. |

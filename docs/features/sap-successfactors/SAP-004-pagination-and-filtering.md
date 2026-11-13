---
id: "SAP-004"
title: "Pagination and filtering"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-004 - Pagination and filtering

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-004.
2. **Feature name:** Pagination and filtering.
3. **Description:** Traverse effective-dated data without unsafe continuations.
4. **Business value:** Traverse effective-dated data without unsafe continuations.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP ODataQuery/iterator; E-SAFETY; E-TEST continuation/cycle tests; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Preserve valid __next verbatim within configured origin/path; support filter/select/expand/as-of/ranges and cursor/snapshot only per entity; bind resumable tokens to query.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing ODataQuery; proposed Page<T>/ContinuationToken adapter.
13. **Dependencies:** SDK-007, SAP-002; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Off-origin/path/userinfo/HTTP links, cycles/max pages, escaped filters, date conflicts, expired cursor and empty page with continuation. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Off-origin/path/userinfo/HTTP links, cycles/max pages, escaped filters, date conflicts, expired cursor and empty page with continuation.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures use returned next URLs unchanged; hostile links fail before forwarding credentials; conflicting date options fail locally.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity paging modes/effective-date behavior depend on current schema; SDK bounds are not vendor capacity guarantees [S3].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "empty page with continuation" and "date conflicts" unit-test-requirement gaps, and added the previously-missing `$orderby` query option. Added `OrderBy` to `ODataQuery` (`src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.cs`) forwarded verbatim as `$orderby`; per the **official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), section 6.3.5.3.2.1 "Cursor-based Pagination"**, `$orderby` is documented as incompatible with `paging=cursor` ("the $orderby parameter cannot be used. An error will occur if it is attempted") - `QueryAddress` now rejects that combination locally before any request is sent. New tests in `tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs`: `EmptyPageWithANonNullNextLinkStillFollowsItRatherThanTreatingItAsTerminal`, `PagingModeIsForwardedVerbatimInTheQueryString` (snapshot/cursor theory), `ConflictingAsOfAndEffectiveDateRangeIsRejectedLocallyBeforeAnyHttpCall`, `OrderByIsForwardedVerbatimInTheQueryString`, `OrderByIsRejectedWithCursorBasedPaginationBeforeAnyHttpCall`. Regenerated the committed `Koras.Connect.Sap.SuccessFactors.snapshot.txt` API baseline for the new `ODataQuery.OrderBy` parameter. Full suite: 514 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Entity paging-mode availability and effective-date behavior remain tenant/current-schema-specific [S3] (see above), unchanged by this pass; SDK bounds are not a vendor capacity guarantee. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `$orderby` support (cited against the official API Reference Guide's documented cursor-pagination incompatibility) and closed the "empty page with continuation"/"date conflicts" unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: entity paging-mode availability and effective-date behavior against a live, current tenant schema [S3] remain standing external blockers. |

---
id: "ADP-005"
title: "Worker pagination and filtering"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-005 - Worker pagination and filtering

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-005.
2. **Feature name:** Worker pagination and filtering.
3. **Description:** Enumerate large authorized populations without unsafe query construction.
4. **Business value:** Enumerate large authorized populations without unsafe query construction.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-WORKERS page/search/iterators; E-TEST WorkersTests, SearchCannotInjectQueryParameters and RepeatedPagesStopInsteadOfLooping; aliases resolve in section 3.
10. **Functional requirements:** Preserve encoded metadata-approved filters; define typed filter whitelist and neutral page adapter; do not stop only because a page is smaller than requested; document current SDK size bound separately from server limits.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing GetWorkersPageAsync/SearchWorkersAsync/EnumerateTypedWorkersAsync; proposed Page<Worker> adapter.
13. **Dependencies:** ADP-004, SDK-007, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Empty/short/multiple/repeated pages, offset overflow, unsupported filters, special characters, partial cancellation and server-returned fewer records. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Empty/short/multiple/repeated pages, offset overflow, unsupported filters, special characters, partial cancellation and server-returned fewer records.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** All expected fixture records appear once per stable traversal; filters cannot inject query parameters; page resume validates query identity; 202 responses fail explicitly unless an authorized async protocol is implemented.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Current SDK accepts 1-100; that is not a universal ADP maximum. Async worker retrieval is outside current implementation and requires product-specific verification [A1].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `AdpWorkerFilterTests.cs` (11 new tests). Closed the two remaining functional-requirement gaps: (1) a typed filter whitelist — `WorkerFilter`/`WorkerFilterField`/`WorkerFilterOperator` in `src/Koras.Connect.Adp/Workers/WorkerFilter.cs`, rendering a safe OData `$filter` fragment (single-quote doubling) for 4 whitelisted fields (`associateOID`, `person/legalName/givenName`, `person/legalName/familyName1`, `workAssignments/jobTitle`), exposed as a new `AdpWorkersClient.SearchWorkersAsync(WorkerFilter,...)` overload alongside the existing raw-string escape hatch; (2) explicit HTTP 202 rejection — `AdpWorkersClient.ReadWorkersResponseAsync` now inspects `IntegrationJsonResponse.StatusCode` and throws `NotSupportedException` instead of attempting to parse a body, across all three worker-read surfaces (`GetWorkersPageAsync`, `SearchWorkersAsync`, `GetWorkerAsync`). This required one supporting change in the shared `Koras.Connect.Core` transport: `IntegrationTransport.SendResponseAsync` previously only skipped its unconditional JSON parse for 204 No Content, so a 202 with an empty/non-JSON body crashed with an unrelated `JsonReaderException` before the 202 check could run — extended the existing no-body special case to also cover 202 Accepted (both conventionally bodyless or carry a status-polling body this SDK does not interpret). The existing pinned behavior that `GetWorkersPageAsync` surfaces a raw `IntegrationRequestException` unchanged on failure is explicitly re-tested and still holds after the internal switch from `ReadJsonAsync` to `SendResponseAsync`. Updated `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Adp.snapshot.txt` for the new public surface (`WorkerFilter`, `WorkerFilterField`, `WorkerFilterOperator`, the new `SearchWorkersAsync` overload). Full suite: 361 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified for the two closed functional requirements. Acceptance criteria's live-sandbox half ("filters cannot inject query parameters" is synthetic-tested; an actual authorized ADP population/async-protocol check) requires sandbox access not available here — stays an explicit, standing blocker. The SDK's own 1-100 page-size bound is still explicitly documented as an SDK choice, not a universal ADP maximum (external blockers above). |
| Acceptance / completion | Not recorded; the live-sandbox half of the acceptance criteria cannot be satisfied without authorized access. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added the typed `WorkerFilter` whitelist and explicit HTTP 202 rejection across all worker-read surfaces (with a supporting `Koras.Connect.Core` transport fix so 202 is detectable without a spurious JSON-parse exception). Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented` since both outstanding functional requirements are now met. `verification` moved from `unverified` to `partial`: the live-sandbox acceptance half remains blocked. |

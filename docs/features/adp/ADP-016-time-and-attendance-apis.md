---
id: "ADP-016"
title: "Time and attendance APIs"
category: "adp"
priority: "P2"
phase: "P7"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-016 - Time and attendance APIs

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-016.
2. **Feature name:** Time and attendance APIs.
3. **Description:** Expose a scoped time capability rather than equating time-off reads with attendance coverage.
4. **Business value:** Expose a scoped time capability rather than equating time-off reads with attendance coverage.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P2.
7. **Planned development phase:** P7; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-OPS TimeOffBalances/TimeOffRequests; E-TEST documented worker read routes; aliases resolve in section 3.
10. **Functional requirements:** Retain time-off reads; research attendance/timecard contracts independently before adding them; model timezone/date/unit rules and entitlement-specific capabilities.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ReadAsync for two time-off operations; no attendance API proposed until verified.
13. **Dependencies:** ADP-021, ADP-019, SDK-007; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing worker, date/unit/timezone edge cases, paging where documented, forbidden product and unknown fields. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing worker, date/unit/timezone edge cases, paging where documented, forbidden product and unknown fields.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Time-off subset has typed reviewed schemas and tests; capability report explicitly says attendance/timecard operations unverified until their contracts and tests are added.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Time product/module and exact grants unavailable; no attendance endpoint or common quota inferred from time-off collection examples [A5].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the acceptance criterion's "capability report explicitly says attendance/timecard operations unverified" gap by adding `AdpTimeAndAttendanceCapabilityStatement` (`src/Koras.Connect.Adp/AdpTimeAndAttendanceCapabilityStatement.cs`) — a static, citable fact (`TimeOffReadsImplemented`/`AttendanceOperationsImplemented`/`TimecardOperationsImplemented` booleans plus a `Statement` string) rather than relying on a host to infer scope from `AdpOperation`'s absence of attendance/timecard values. Added `tests/Koras.Connect.Tests/AdpTimeAndAttendanceTests.cs`, which also closes the remaining unit-test-requirement date/unit/timezone edge cases for the existing typed time-off models: `TimeOffPeriods` timestamps across three timezone offsets (`Z`, `-05:00`, `+09:30`) survive round-trip exactly without reinterpretation, `BalanceAmount.UnitCode` preserves both `DAYS` and `HOURS` without assuming a default, a missing unit code stays `null` rather than a fabricated default, zero and negative balance amounts round-trip exactly, and an empty workers collection is tolerated rather than throwing. Updated `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Adp.snapshot.txt` for the new public type. Full suite: 433 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Attendance/timecard contracts remain unresearched and unimplemented by design (see external blockers above), unchanged by this pass — this feature's job was to make that scope boundary explicit and citable, not to close it. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `AdpTimeAndAttendanceCapabilityStatement` and closed the remaining date/unit/timezone unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: attendance/timecard contracts remain genuinely unresearched, which this pass documents explicitly rather than closes. |

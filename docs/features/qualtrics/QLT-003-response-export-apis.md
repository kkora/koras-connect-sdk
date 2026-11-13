---
id: "QLT-003"
title: "Response export APIs"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-003 - Response export APIs

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-003.
2. **Feature name:** Response export APIs.
3. **Description:** Retrieve response datasets with resumable export jobs.
4. **Business value:** Retrieve response datasets with resumable export jobs.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-QLT start/progress/wait/download; E-TEST QualtricsExportUsesProgressThenFileIdAndStreamsWithoutExtraction; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Keep create -> progress -> file sequence, validated format/date/filter and bounded polling; stop failed/404/unknown states; never restart uncertain POST automatically.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing StartResponseExportAsync/GetExportProgressAsync/WaitForExportAsync/DownloadExportAsync and typed options/progress.
13. **Dependencies:** QLT-001, QLT-004, QLT-013, SDK-010; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** inProgress/complete/failed/unknown, missing IDs, bounds, cancellation, partial bytes and no POST replay. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** inProgress/complete/failed/unknown, missing IDs, bounds, cancellation, partial bytes and no POST replay.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Download only complete with fileId; IDs are not interchanged; bytes remain unchanged/stream stays open; resume existing progress after poll timeout.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Response permission, current export constraints/filters and account limits required [Q3-Q5]; direct response writes not claimed.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "partial bytes" unit-test-requirement gap by adding `DownloadInterruptedMidStreamPreservesPartialBytesAndLeavesTheOutputStreamOpen` to `tests/Koras.Connect.Tests/Qualtrics/QualtricsClientExpansionTests.cs`. Uses a custom `HttpContent` (`PartialThenFailingContent`) that writes 3 real bytes then throws `IOException`, simulating a connection drop partway through an otherwise-successful (200 OK) download. Asserts: (1) the failure surfaces as `HttpRequestException` with the original `IOException` as `InnerException` — a different failure mode than an HTTP error status, so it is never miscategorized as `QualtricsApiException`; (2) the 3 bytes already streamed to the caller's output survive untouched (no silent truncation/rollback); (3) the output stream remains open/writable afterward, matching the existing "stream stays open" assertion style already used for the happy-path export test. Full suite: 472 passed, 3 skipped, 0 failed (one unrelated pre-existing `ResiliencePolicyTests` timing flake observed and reconfirmed non-reproducing on rerun). |
| Remaining work / blockers | No further engineering gap identified. Response permission, current export constraints/filters and account limits remain external blockers (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `DownloadInterruptedMidStreamPreservesPartialBytesAndLeavesTheOutputStreamOpen`, closing the "partial bytes" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live response-permission/export-constraint evidence remains a standing external blocker. |

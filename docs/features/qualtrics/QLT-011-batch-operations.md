---
id: "QLT-011"
title: "Batch operations"
category: "qualtrics"
priority: "P1"
phase: "P6"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-011 - Batch operations

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-011.
2. **Feature name:** Batch operations.
3. **Description:** Support volume with bounded requests and visible item outcomes.
4. **Business value:** Support volume with bounded requests and visible item outcomes.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SYNC BatchAsync is local chunking; no remote Qualtrics batch; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Choose verified bulk/contact-import contract; model async status/item results/idempotency/limits; local chunks are not a remote batch API.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed family-specific commands/results after schema verification; existing local BatchAsync remains neutral.
13. **Dependencies:** QLT-006, QLT-013, SDK-007; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Batch limits, duplicates, partial failure, expired job, poll interruption, ambiguous POST and cancellation. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Batch limits, duplicates, partial failure, expired job, poll interruption, ambiguous POST and cancellation.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each item has documented outcome; bounded resume does not silently start a new write or introduce scheduler/persistence.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Remote routes/formats/quotas/eligibility unverified; family names do not justify generic batch URLs.

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
| Validation commands / results | Not newly executed during documentation split; baseline evidence is in the specification |
| Remaining work / blockers | See functional requirements, acceptance criteria and external blockers above; replace with precise remaining items as work progresses |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `not-started` | Reviewed during the same work session that advanced QLT-001/004/005/006/009/012/013/014/015/016. Deliberately left `not-started`/blocked: the remote Qualtrics bulk/batch-import routes, formats and quotas are unverified in this codebase, and the task-level guidance explicitly directs against inventing generic batch URLs from family names alone. The new contact/mailing-list writes added under QLT-006/009 are single-record creates/updates with an explicit opt-in gate, not a remote batch/bulk-import API, and do not satisfy this feature. The pre-existing `E-SYNC BatchAsync` remains local chunking only, as already noted in the baseline evidence. Remaining blocker unchanged: remote batch routes/formats/quotas/eligibility must be verified against vendor documentation before any implementation is attempted. |

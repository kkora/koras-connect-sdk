---
id: "QLT-007"
title: "Events and webhook capabilities"
category: "qualtrics"
priority: "P2"
phase: "P7"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-007 - Events and webhook capabilities

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-007.
2. **Feature name:** Events and webhook capabilities.
3. **Description:** React to documented events with host-owned durable delivery.
4. **Business value:** React to documented events with host-owned durable delivery.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P2.
7. **Planned development phase:** P7; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No event/subscription/receiver code; legacy QLT-007; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Choose verified event types, subscription operations, envelope/auth/verification and delivery identity; host owns endpoint/TLS/inbox and deduplication.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed event DTO/client/helpers only after [Q8] operation verification; no embedded server.
13. **Dependencies:** QLT-001, QLT-005, QLT-008; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Unsupported type, forged/stale/duplicate envelope, retries, missing identity, permission denial and explicit opt-in. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Unsupported type, forged/stale/duplicate envelope, retries, missing identity, permission denial and explicit opt-in.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Verified fixtures/auth behavior pass; docs state actual integrity/freshness guarantees and host obligations without borrowing ADP HMAC assumptions.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Current full subscription contract/auth/event schemas and tenant eligibility unverified; family index alone insufficient.

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
| 2026-11-12 | `not-started` | Reviewed during the same work session that advanced QLT-001/004/005/006/009/012/013/014/015/016. Deliberately left `not-started`/blocked: the event-subscription authentication/verification scheme for Qualtrics webhooks is unverified in this codebase (no prior sandbox evidence), and the task-level guidance explicitly directs against inventing a webhook/event contract without vendor-contract verification. No event DTO, subscription client or receiver code was added. Remaining blocker unchanged: [Q8] current full subscription contract/auth/event schemas and tenant eligibility must be verified against vendor documentation (or a live/sandboxed tenant) before any implementation is attempted. |

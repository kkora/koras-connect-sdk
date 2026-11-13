---
id: "QLT-010"
title: "Participant management"
category: "qualtrics"
priority: "P1"
phase: "P6"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-010 - Participant management

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-010.
2. **Feature name:** Participant management.
3. **Description:** Support product-specific participant workflows with consent.
4. **Business value:** Support product-specific participant workflows with consent.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No participant/distribution methods in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Identify actual product (XM Directory/distributions/EX) before selecting APIs; distinguish contact/participant/response IDs; writes/message sends disabled by default.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed product-specific commands/models only after official verification; no endpoint guessed.
13. **Dependencies:** QLT-006, QLT-009, QLT-008; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Wrong identity type, opt-out, duplicates, denied access and unknown submission outcome. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Wrong identity type, opt-out, duplicates, denied access and unknown submission outcome.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Chosen product contract/fixtures show documented participant behavior; default tests/examples never send distributions/messages.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Product/family/license/current endpoints and scopes unspecified; missing research is not vendor-proven unavailability.

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
| 2026-11-12 | `not-started` | Reviewed during the same work session that advanced QLT-001/004/005/006/009/012/013/014/015/016. Deliberately left `not-started`/blocked: which Qualtrics product/family actually exposes "participant" semantics (XM Directory contacts, survey distributions, or Employee Experience) is undecided, and the applicable scopes/entitlements are unverified. Implementing against a guessed product would risk shipping an unsupported contract. No participant DTO/client/method was added; the directory/contact surface added under QLT-006 is a distinct, verified-shape feature and does not substitute for participant management. Remaining blocker unchanged: product/family/license/current endpoint and scope verification required before any implementation. |

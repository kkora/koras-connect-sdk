---
id: "QLT-007"
title: "Events and webhook capabilities"
category: "qualtrics"
priority: "P2"
phase: "P7"
status: "partial"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-14"
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

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [x] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | After an explicit go/no-go call, the user chose to implement subscription *management* now (create/list/delete against the publicly documented Qualtrics API v3 Event Subscriptions contract), explicitly scoped to exclude any receiver/signature-verification server - the same division of responsibility already documented for ADP-018's webhooks (host owns the inbound endpoint, TLS, delivery-authenticity verification and deduplication). Added `QualtricsEventSubscription` DTO and `ListEventSubscriptionsAsync`/`ListEventSubscriptionsPageAsync` (read, default-enabled, reusing the shared paging engine) plus `CreateEventSubscriptionAsync`/`DeleteEventSubscriptionAsync` (write, explicit `writesEnabled` opt-in, classified through `QualtricsApiException` on failure) in `src/Koras.Connect.Qualtrics/QualtricsEventSubscriptionsClient.cs`. `Topics` is modeled as a single comma-joined string per the documented contract, not a JSON array. **Endpoint shape is based on the publicly documented Qualtrics API v3 contract; it is NOT verified against a live tenant** - every new public member's XML doc states this explicitly, matching the established "best-effort, unconfirmed" posture used for QLT-006/009's contact/mailing-list writes. Added `samples/Qualtrics.EventSubscriptions`, an offline console sample (list -> create -> delete, no receiver) verified with `dotnet build`/`dotnet run` on a clean checkout. Covered by `tests/Koras.Connect.Tests/Qualtrics/QualtricsExpansionTests.cs` (`ListEventSubscriptionsPagesUsingTheSharedPagingHelper`, `CreateEventSubscriptionRequiresExplicitOptInAndJoinsTopicsAndRejectsNonHttpsPublicationUrl`, `CreateEventSubscriptionClassifiesForbiddenAndNotFoundWithoutLeakingBody`, `DeleteEventSubscriptionRequiresExplicitOptInAndSendsDelete`, `DeleteEventSubscriptionClassifiesForbiddenAndNotFoundWithoutLeakingBody`). Regenerated the committed `Koras.Connect.Qualtrics.snapshot.txt` API baseline for the new public surface. `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: 499 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No receiver/signature-verification code exists or was attempted by design (host-owned, out of scope for this pass). Status stays `partial`, not `implemented`: [Q8] the full subscription contract/auth/event schemas and tenant eligibility remain unverified against a live tenant or vendor-authoritative schema, and "unsupported type"/"forged/stale/duplicate envelope"/"retries"/"missing identity" unit-test requirements are receiver-side concerns this pass deliberately does not implement. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `not-started` | Reviewed during the same work session that advanced QLT-001/004/005/006/009/012/013/014/015/016. Deliberately left `not-started`/blocked: the event-subscription authentication/verification scheme for Qualtrics webhooks is unverified in this codebase (no prior sandbox evidence), and the task-level guidance explicitly directs against inventing a webhook/event contract without vendor-contract verification. No event DTO, subscription client or receiver code was added. Remaining blocker unchanged: [Q8] current full subscription contract/auth/event schemas and tenant eligibility must be verified against vendor documentation (or a live/sandboxed tenant) before any implementation is attempted. |
| 2026-11-13 | `partial` | After an explicit go/no-go call, implemented subscription management only (create/list/delete) against the publicly documented Event Subscriptions API, explicitly excluding any receiver/signature-verification server. Checked the unit/contract/integration, security/cancellation/resource-ownership, and documentation/consumer-example completion gates; status moves from `not-started` to `partial` (not `implemented`): the [Q8] live-tenant schema/auth verification remains open, and the receiver-side unit-test requirements (forged/stale/duplicate envelope, retries, missing identity) are out of scope by design. |
| 2026-11-14 | `partial` | A Phase 1 review sweep found `CreateEventSubscriptionAsync` joined caller-supplied topics with a raw comma (`string.Join(",", topics)`) without rejecting a topic that itself contains a comma - such a topic would silently be interpreted by Qualtrics as two separate topics instead of being rejected locally. Fixed by validating no topic contains `,` before joining; added a regression test to `QualtricsExpansionTests.cs`. |

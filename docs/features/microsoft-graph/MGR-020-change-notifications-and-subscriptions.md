---
id: "MGR-020"
title: "Change notifications and subscriptions"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-020 - Change notifications and subscriptions

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-020 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-020.
2. **Feature name:** Change notifications and subscriptions.
3. **Description:** Receive webhook push notifications when a resource changes, instead of polling delta queries continuously.
4. **Business value:** Receive webhook push notifications when a resource changes, instead of polling delta queries continuously.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None; this is a webhook-receiver capability with no Phase 1 analog.
10. **Functional requirements:** Create: `POST /subscriptions` [G8]. Webhook validation handshake, exact timing confirmed: Graph POSTs the new subscription endpoint a request containing `?validationToken=`, and the endpoint must echo that token back as **plain text within 10 seconds** [G8]. Normal notification delivery must be acknowledged (2xx) within **3 seconds**; on failure to ack, Graph retries delivery for up to **4 hours**, each attempt with a **10-second** timeout [G8]. Maximum subscription expiration varies sharply by resource type: as short as **60 minutes** for `presence`, up to **43,200 minutes (~30 days)** for several Teams/security resource types [G8]; Outlook messages/events/contacts cap at **10,080 minutes (7 days)**, or just **1,440 minutes (1 day)** if `includeResourceData` is requested [G8]. Three lifecycle notification types exist: `reauthorizationRequired`, `subscriptionRemoved`, `missed` - lifecycle notifications must be acknowledged with **202** [G8].
11. **Nonfunctional requirements:** The webhook receiver endpoint itself is the caller's responsibility to host (this SDK is a client/subscription-manager, not a webhook host); subscription-renewal scheduling is a connector-provided helper given the wide variance in max expiration per resource type.
12. **Public API contracts or proposed contracts:** Official API: `/subscriptions`, the 10-second validation-echo window, the 3-second-ack/4-hour-retry/10-second-per-attempt delivery SLA, the per-resource-type expiration table, and the three lifecycle-notification types - all per [G8]. Proposed C# contracts:

```csharp
public sealed record GraphSubscription(string Id, string Resource, string ChangeType, DateTimeOffset ExpirationDateTime, string NotificationUrl);
public async Task<GraphSubscription> CreateSubscriptionAsync(string resource, string changeType, Uri notificationUrl,
    DateTimeOffset expirationDateTime, bool includeResourceData = false, bool writesEnabled = false, CancellationToken ct = default);
public async Task RenewSubscriptionAsync(string subscriptionId, DateTimeOffset newExpirationDateTime, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** MGR-003.
14. **Security considerations:** Authentication and authorization: Resource-specific read permission (e.g. `Mail.Read` to subscribe to message changes) plus, for lifecycle robustness, the ability to re-authorize. Required permissions/scopes/licenses: Matches whatever the subscribed resource itself requires for a read. Data privacy and security: Notification payloads by default carry only resource identifiers, not resource content, unless `includeResourceData` is explicitly requested - a deliberately higher-sensitivity, shorter-expiration opt-in the SDK should surface as a distinct, clearly-labeled option.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not paginated; subscription renewal must be scheduled proactively given the short expiration windows for some resource types (as short as 60 minutes). Error handling and concurrency: A `reauthorizationRequired` lifecycle event must trigger a token refresh / re-consent flow, not be silently dropped; `subscriptionRemoved` means the subscription must be recreated, not renewed.
16. **Unit-test requirements:** Validation-token echo composed correctly as plain text; renewal scheduling respects the per-resource-type max-expiration table; lifecycle-event handling distinguishes all three types correctly.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for subscription creation, the validation handshake, and each lifecycle notification type. Integration-test requirements: A publicly reachable webhook endpoint and an authorized tenant; not available in a documentation-only pass.
18. **Acceptance criteria:** Subscription creation, the validation handshake, and renewal scheduling all work correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live tenant/public endpoint available this pass; the full per-resource-type max-expiration table was only partially captured in the underlying research and should be re-verified against the live documentation page before being treated as exhaustive.

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
| Implementation revision | Not recorded; no Phase 2 implementation exists yet |
| Validation commands / results | Not recorded; no Phase 2 implementation exists yet |
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live tenant/public endpoint available this pass; the full per-resource-type max-expiration table was only partially captured in the underlying research and should be re-verified against the live documentation page before being treated as exhaustive. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

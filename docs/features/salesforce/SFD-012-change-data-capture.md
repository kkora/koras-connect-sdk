---
id: "SFD-012"
title: "Change Data Capture"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-012 - Change Data Capture

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-012 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-012.
2. **Feature name:** Change Data Capture.
3. **Description:** Receive a durable, ordered stream of create/update/delete/undelete events for subscribed objects, for near-real-time downstream synchronization.
4. **Business value:** Receive a durable, ordered stream of create/update/delete/undelete events for subscribed objects, for near-real-time downstream synchronization.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None; this is an event-streaming capability with no Phase 1 analog.
10. **Functional requirements:** Two transports exist: legacy CometD/Bayeux Streaming API channels (`/data/ChangeEvents`, `/data/{Object}ChangeEvent`, `/data/{Custom}__ChangeEvent`, or a custom channel `/data/{Channel}__chn`), and the Pub/Sub API (SFD-014) - **[snippet]** describes CometD as "not formally deprecated" relative to Pub/Sub, but this framing itself was not independently confirmed and should not be stated as settled vendor guidance. `ChangeEventHeader` fields (snippet, from the Apex reference page): `entityName, changeType (CREATE/UPDATE/DELETE/UNDELETE), changeOrigin, transactionKey, commitTimestamp, recordIds, changedFields, nulledFields`. Supported editions: Enterprise, Unlimited, Performance, Developer [snippet]. CometD concurrent-client caps (snippet): Performance/Unlimited 2,000; Enterprise 1,000; Developer 20.
11. **Nonfunctional requirements:** Long-lived connection management (CometD long-polling or Pub/Sub gRPC stream, per SFD-014's transport decision - Section 6/ADR-P201); reconnect-with-replay-id resilience.
12. **Public API contracts or proposed contracts:** Official API: Channel naming conventions and `ChangeEventHeader` field list are snippet-sourced from `developer.salesforce.com/docs/platform/change-data-capture/guide/{cdc-subscribe-channels,cdc-event-fields-header,cdc-allocations,cdc-intro}.html` and the Apex reference `ChangeEventHeader` class page; not independently re-fetched. Proposed C# contracts:

```csharp
public sealed record SalesforceChangeEvent(string EntityName, string ChangeType, string TransactionKey,
    DateTimeOffset CommitTimestamp, IReadOnlyList<string> RecordIds, IReadOnlyList<string>? ChangedFields);
public IAsyncEnumerable<SalesforceChangeEvent> SubscribeToChangeEventsAsync(string channel, CancellationToken ct = default);
```
13. **Dependencies:** SFD-003, SFD-014 (shares transport-decision context).
14. **Security considerations:** Authentication and authorization: Subscriber needs "API Enabled" permission and CDC enabled for the specific object; CometD uses the same bearer token as REST over a different (Bayeux) protocol. Required permissions/scopes/licenses: CDC enablement is per-object, admin-configured, and edition-gated (SFD-022); free tier limited to 5 objects per the researched snippet. Data privacy and security: Change events carry record IDs and changed-field names, and depending on configuration can carry field values - treated as business data, never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Replay-ID-based resume on reconnect (mechanism details not independently verified this pass - flagged for the same treatment as Dataverse's delta-token-expiry open question). Error handling and concurrency: Connection drop requires reconnect-with-last-known-replay-ID, not a full resync, assuming the replay window has not expired - exact replay-window duration not independently confirmed this pass.
16. **Unit-test requirements:** `ChangeEventHeader` parsing matches the documented field list; reconnect-with-replay-ID resumes without duplicate delivery assumptions beyond what's independently confirmed.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic CDC event fixture matching the documented shape, explicitly labeled snippet-confidence. Integration-test requirements: Authorized sandbox org with CDC enabled on a test object; not available this pass.
18. **Acceptance criteria:** Event parsing and reconnect logic work against synthetic fixtures; the CometD-vs-Pub/Sub transport choice is documented as a connector configuration option, not hardcoded.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** CometD deprecation status, exact replay-window duration, and per-edition event-volume limits are all snippet-only or partially corroborated - re-verify before release (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. CometD deprecation status, exact replay-window duration, and per-edition event-volume limits are all snippet-only or partially corroborated - re-verify before release (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

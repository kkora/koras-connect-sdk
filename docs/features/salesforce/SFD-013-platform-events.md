---
id: "SFD-013"
title: "Platform Events"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-013 - Platform Events

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-013 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-013.
2. **Feature name:** Platform Events.
3. **Description:** Publish and subscribe to custom, schema-defined business events (distinct from CDC's automatic record-change events).
4. **Business value:** Publish and subscribe to custom, schema-defined business events (distinct from CDC's automatic record-change events).
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None; shares SFD-012's event-streaming foundation.
10. **Functional requirements:** Custom Platform Events are defined in Setup/Metadata API and auto-suffixed `__e` [snippet]. Publish: `POST /services/data/vXX.X/sobjects/{EventName}__e/` - i.e., publishing reuses the sObject-create mechanism (SFD-004) rather than a distinct endpoint [snippet, consistent with the sObject model]. Subscribe: CometD channel `/event/{EventName}__e`, or the Pub/Sub API (SFD-014) [snippet]. Two publish modes (snippet): **Publish After Commit** (transactional, counts against the Apex DML governor limit) vs. **Publish Immediately** (non-transactional, counts against a separate 150-call-per-transaction `EventBus.publish()` limit; `allOrNone` is ignored in this mode).
11. **Nonfunctional requirements:** Publish reuses SFD-004's write-opt-in gate; subscribe reuses SFD-012's connection-management approach.
12. **Public API contracts or proposed contracts:** Official API: Publish-via-sObject-create and both channel forms are snippet-sourced from `developer.salesforce.com/docs/platform/platform-events/guide/{platform-events-publish-api,platform-event-extras,platform-events-publish-apex}.html`; not independently re-fetched. Proposed C# contracts:

```csharp
public async Task<string> PublishPlatformEventAsync(string eventApiName, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public IAsyncEnumerable<JsonDocument> SubscribeToPlatformEventAsync(string eventApiName, CancellationToken ct = default);
```
13. **Dependencies:** SFD-004, SFD-012 (shares the subscribe transport).
14. **Security considerations:** Authentication and authorization: Publish requires Create permission on the event's underlying object; subscribe requires the standard CometD/Pub/Sub entitlement. Required permissions/scopes/licenses: Edition-gated daily event-delivery allocation (SFD-022). Data privacy and security: Event payloads are caller-defined business data; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not paginated; publish is a single write per event, never auto-retried; the 150-call `EventBus.publish()` limit is a per-transaction Apex-side governor limit, not directly relevant to this SDK's own REST-based publish path, and is documented here only for completeness. Error handling and concurrency: A publish failure surfaces via the standard sObject-create error shape (SFD-019), since the mechanism is literally an sObject create against the `__e` object.
16. **Unit-test requirements:** Publish composes the correct `{EventName}__e` create request; subscribe parses event payloads against a documented custom-event schema fixture.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic publish/subscribe fixtures, explicitly labeled snippet-confidence. Integration-test requirements: Authorized sandbox org with a custom Platform Event defined; not available this pass.
18. **Acceptance criteria:** Publish and subscribe both work against synthetic fixtures using the sObject-create mechanism correctly.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact allocation/limit numbers are snippet-only (SFD-022 carries the detail); re-verify before release.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Exact allocation/limit numbers are snippet-only (SFD-022 carries the detail); re-verify before release. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

---
id: "SFD-014"
title: "Pub/Sub API"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Confirmed blocked, not attempted: this feature's own specification states implementation must be deferred until the Avro-dependency decision is made and the endpoint/port/header details are independently re-verified, and explicitly instructs 'do not implement against unverified wire-format details.' No code was written against the snippet-only gRPC/Avro wire format; CDC (SFD-012) and Platform Events (SFD-013) were implemented against the alternative CometD transport instead, which carries independently-sourced channel/field documentation."
verification: "unverified"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-014 - Pub/Sub API

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-014 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-014.
2. **Feature name:** Pub/Sub API.
3. **Description:** Provide a single, modern gRPC-based transport for both Platform Events and Change Data Capture, as Salesforce's current recommended event-streaming path.
4. **Business value:** Provide a single, modern gRPC-based transport for both Platform Events and Change Data Capture, as Salesforce's current recommended event-streaming path.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None; this is the one Phase 2 feature across all three connectors that requires a gRPC/Protocol-Buffers dependency rather than plain HTTP+JSON (Section 6, ADR-P201).
10. **Functional requirements:** Transport: gRPC over HTTP/2; RPC envelope uses Protocol Buffers; event payloads are encoded in **Apache Avro**, with a `GetSchema` RPC resolving the Avro schema by schema ID (a schema-registry pattern) [snippet]. Endpoint: `api.pubsub.salesforce.com:7443` - **[snippet, flagged unverified]**: one source suggested port 443 may also work; this must be independently re-checked, not assumed. RPCs (snippet): `Subscribe` (bidirectional stream, pull/credit-based flow control), `ManagedSubscribe`/`ManagedFetchRequest` (beta, server-stored Replay ID), `Publish`/`PublishStream`. Auth metadata on every gRPC call (snippet): `accesstoken`, `instanceurl`, `tenantid` headers - exact header-name case-sensitivity flagged unverified.
11. **Nonfunctional requirements:** Depends on `Grpc.Net.Client` + `Google.Protobuf` (Section 6's explicit, justified exception to the "no non-HTTP-client dependencies" default); Avro deserialization requires either a hand-rolled minimal reader or an additional Avro library dependency - **not yet decided, an open architecture question** (Section 28), since adding a second new binary-protocol dependency for one connector's one P1 feature needs its own justification separate from the already-justified gRPC dependency.
12. **Public API contracts or proposed contracts:** Official API: Every item above is snippet-sourced (the overview page itself returned 403 on direct fetch); sourced from `developer.salesforce.com/docs/platform/pub-sub-api/guide/{supported-auth,rpc-method-headers}.html`, `.../references/methods/{subscribe-rpc,managedsubscribe-rpc}.html`, `.../platform-events/guide/platform-events-event-schema-pubsub-api.html`. This is the lowest-confidence feature in the entire Salesforce catalog. Proposed C# contracts:

```csharp
public interface ISalesforcePubSubClient
{
    IAsyncEnumerable<SalesforcePubSubEvent> SubscribeAsync(string topic, string? replayId = null, CancellationToken ct = default);
    Task PublishAsync(string topic, ReadOnlyMemory<byte> avroPayload, CancellationToken ct = default);
}
public sealed record SalesforcePubSubEvent(string ReplayId, ReadOnlyMemory<byte> AvroPayload, string SchemaId);
```
13. **Dependencies:** SFD-001 (token reuse across transports), SFD-012, SFD-013.
14. **Security considerations:** Authentication and authorization: Same OAuth bearer token as REST, passed via gRPC call metadata rather than an HTTP header, per the documented `accesstoken`/`instanceurl`/`tenantid` triple. Required permissions/scopes/licenses: Same per-object CDC/Platform-Event entitlements as SFD-012/SFD-013, since Pub/Sub is a transport for the same underlying event types. Data privacy and security: Event payloads may carry sensitive business data in Avro-encoded form; never logged in raw or decoded form by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Pull-based credit flow control (client requests N events at a time) rather than push; reconnect uses the Replay ID mechanism, exact resume-window duration not independently confirmed. Error handling and concurrency: gRPC status codes map to the shared `IntegrationExceptionClassifier`'s philosophy but require a new gRPC-specific adapter, since the classifier's current implementation is HTTP-status-code-based only (Core's `IntegrationTransport` is HTTP-only) - documented here as a connector-local extension, not a shared-abstraction change (Section 8.2's "no abstraction creep for one vendor" principle).
16. **Unit-test requirements:** Deferred pending the Avro-library decision above; a gRPC-channel construction unit test (metadata headers attached correctly) is feasible without a live server using a synthetic gRPC test server.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic gRPC service double implementing `Subscribe`/`Publish`, explicitly labeled snippet-confidence for wire-format details. Integration-test requirements: Authorized sandbox org with Pub/Sub API access; not available this pass.
18. **Acceptance criteria:** Deferred beyond basic channel/metadata construction until the Avro-dependency decision is made and the endpoint/port/header details are independently re-verified.
19. **Definition of Done:** Not applicable beyond a design-only state until the open gaps above close; do not implement against unverified wire-format details.
20. **External API entitlement or documentation blockers:** This is the single most speculative feature in the entire 70-feature catalog: endpoint port, RPC method names, and Avro-vs-schema-registry mechanics are all snippet-only with an explicitly flagged internal inconsistency (port 7443 vs. 443). Recommend this be the first Salesforce P1 feature re-verified against a live Salesforce Pub/Sub API session or a freshly-fetched official page before any implementation work begins (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains, and is explicitly deferred by this feature's own Definition of Done ("not applicable beyond a design-only state... do not implement against unverified wire-format details"). This is the single most speculative feature in the entire 70-feature catalog: endpoint port, RPC method names, and Avro-vs-schema-registry mechanics are all snippet-only with an explicitly flagged internal inconsistency (port 7443 vs. 443). Recommend this be the first Salesforce P1 feature re-verified against a live Salesforce Pub/Sub API session or a freshly-fetched official page before any implementation work begins (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `not-started` (confirmed blocked) | Revisited while implementing SFD-012/SFD-013 (which share this feature's event-streaming subject matter but were built against the independently-sourced CometD transport instead). This feature's own spec explicitly instructs deferring implementation until the Avro-dependency decision and endpoint/port/header details are re-verified; no gRPC/Avro code was written, matching the DVT-020 "confirmed blocked, not attempted" precedent rather than guessing an unverified wire format. |

---
id: "MGR-016"
title: "Teams messages and chats"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-016 - Teams messages and chats

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-016 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-016.
2. **Feature name:** Teams messages and chats.
3. **Description:** Read and send messages in Teams channels and chats.
4. **Business value:** Read and send messages in Teams channels and chats.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /teams/{id}/channels/{id}/messages`, `GET /chats/{id}/messages`, `POST` to either to send [confirmed]. **Important, independently confirmed nuance**: application permissions for chat/channel messages **do exist**, but the least-privileged tier is **Resource-Specific Consent (RSC)** - `ChatMessage.Read.Chat`/`ChannelMessage.Read.Group` - which is granted **per-chat/per-channel via a Teams app installation**, not tenant-wide admin consent. Higher, tenant-wide application alternatives (`Chat.Read.All`, etc.) exist but require full tenant-wide admin consent and are a materially different, more privileged configuration path than RSC. **Personal Microsoft accounts are unsupported for every Teams operation**, including this one, consistent with MGR-015.
11. **Nonfunctional requirements:** Explicit write opt-in gate on send; RSC vs. tenant-wide permission choice must be an explicit configuration decision documented to the caller, not silently defaulted.
12. **Public API contracts or proposed contracts:** Official API: Message read/send endpoints and the RSC-vs-tenant-wide application-permission distinction, confirmed. Proposed C# contracts:

```csharp
public sealed class GraphChatMessage { public string? Id { get; set; } public string? Body { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<string> SendChannelMessageAsync(string teamId, string channelId, string body, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** MGR-015.
14. **Security considerations:** Authentication and authorization: RSC (`ChatMessage.Read.Chat`/`ChannelMessage.Read.Group`, per-resource consent via app install) or tenant-wide (`Chat.Read.All`, full admin consent) [G11]. Required permissions/scopes/licenses: One of the two paths above, explicitly chosen; MSA unsupported. Data privacy and security: Chat/channel message content is highly sensitive communication data; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: A send attempted against a chat/channel the RSC-consented Teams app was never installed into fails with a distinct, documented authorization error, not a generic 403.
16. **Unit-test requirements:** RSC-scoped vs. tenant-wide permission configuration both compose correctly; message send/read work under either.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both permission configurations. Integration-test requirements: Authorized tenant with a Teams app installed for RSC; not available this pass.
18. **Acceptance criteria:** Both the RSC and tenant-wide permission paths are documented and work correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** RSC's per-resource consent model is materially more complex to configure than a typical tenant-wide application permission and must be documented clearly to avoid caller confusion (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. RSC's per-resource consent model is materially more complex to configure than a typical tenant-wide application permission and must be documented clearly to avoid caller confusion (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

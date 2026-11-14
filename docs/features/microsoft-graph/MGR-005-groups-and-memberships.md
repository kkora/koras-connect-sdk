---
id: "MGR-005"
title: "Groups and memberships"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-005 - Groups and memberships

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-005 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-005.
2. **Feature name:** Groups and memberships.
3. **Description:** Read and manage directory groups and their membership.
4. **Business value:** Read and manage directory groups and their membership.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Graph-specific.
10. **Functional requirements:** `GET /groups`, `GET /groups/{id}/members`, `POST /groups/{id}/members/$ref`, `DELETE /groups/{id}/members/{id}/$ref` [G1/G2, resource existence confirmed]. Personal Microsoft accounts (MSA) are **not supported** for groups operations - tenant/organizational accounts only [confirmed in Graph research]. **Flagged documentation anomaly, not independently resolved**: one permissions table lists `Group-NestingSupport.ReadWrite.All` as the "least privileged" option for what is described as a read operation - an unusual pairing (a ReadWrite-named scope as the minimum for a read) that was not independently reconciled against a second source and is carried forward as an open item rather than silently corrected.
11. **Nonfunctional requirements:** Explicit write opt-in gate on membership add/remove.
12. **Public API contracts or proposed contracts:** Official API: `/groups`, `/groups/{id}/members`, `$ref` add/remove per [G1]/[G2]. Proposed C# contracts:

```csharp
public sealed class GraphGroup { public string? Id { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphUser> EnumerateGroupMembersAsync(string groupId, GraphQuery? query = null, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003, MGR-004 (members are users).
14. **Security considerations:** Authentication and authorization: `Group.Read.All`/`GroupMember.ReadWrite.All` (application) or delegated equivalents [G11]. Required permissions/scopes/licenses: Admin-consented application permission; MSA accounts unsupported. Data privacy and security: Group membership can reveal organizational structure; treated as business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: A membership operation attempted against an MSA-only context should fail clearly, not silently no-op.
16. **Unit-test requirements:** Membership add/remove `$ref` composition; member enumeration pagination.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for group + membership list. Integration-test requirements: Authorized tenant with real groups; not available this pass.
18. **Acceptance criteria:** Group read and membership mutation both work correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The `Group-NestingSupport.ReadWrite.All` "least privileged for a read" anomaly is an unresolved documentation artifact, not confirmed as the SDK's actual minimum-permission recommendation (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. The `Group-NestingSupport.ReadWrite.All` "least privileged for a read" anomaly is an unresolved documentation artifact, not confirmed as the SDK's actual minimum-permission recommendation (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

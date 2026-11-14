---
id: "MGR-015"
title: "Teams and channels"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-015 - Teams and channels

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-015 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-015.
2. **Feature name:** Teams and channels.
3. **Description:** Enumerate Microsoft Teams and their channels.
4. **Business value:** Enumerate Microsoft Teams and their channels.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /teams`, `GET /teams/{id}/channels` [confirmed]. **Important, independently confirmed caveat**: `GET /teams` (the tenant-wide list) returns **only** `id`, `displayName`, `description`, `visibility` - every other property is returned `null`, not omitted, regardless of permission level; richer per-team detail requires a separate `GET /teams/{id}` call. Application permissions **are supported** for team/channel enumeration (unlike MGR-017's Planner), but every personal Microsoft account (MSA) scenario is unsupported for **any** Teams operation across this entire feature area [confirmed].
11. **Nonfunctional requirements:** Read-only for this feature.
12. **Public API contracts or proposed contracts:** Official API: `/teams`, `/teams/{id}/channels`, the restricted-field-list-on-list-call caveat, and the MSA-unsupported constraint, all confirmed. Proposed C# contracts:

```csharp
public sealed record GraphTeamSummary(string Id, string? DisplayName, string? Description, string? Visibility);
public IAsyncEnumerable<GraphTeamSummary> EnumerateTeamsAsync(CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `Team.ReadBasic.All`/`Channel.ReadBasic.All` (application supported) [G11]. Required permissions/scopes/licenses: Admin-consented application permission; tenant (not MSA) accounts only. Data privacy and security: Team/channel metadata is organizational, low-sensitivity.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: A caller expecting populated fields from the `/teams` list call must be redirected (in documentation, not a runtime exception) to the per-team detail call.
16. **Unit-test requirements:** List-call field restriction correctly reflected in the typed model (nullable fields, not assumed-populated); channel enumeration pagination.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both the restricted list-call shape and a full per-team detail shape. Integration-test requirements: Authorized tenant with Teams provisioned; not available this pass.
18. **Acceptance criteria:** Team/channel enumeration works correctly with the documented field restrictions respected.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-tenant caveat.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. None beyond the standing no-live-tenant caveat. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

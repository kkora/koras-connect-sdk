---
id: "MGR-017"
title: "Planner"
category: "microsoft-graph"
priority: "P2"
phase: "P2-D7"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-017 - Planner

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-017 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-017.
2. **Feature name:** Planner.
3. **Description:** Read and manage Microsoft Planner plans, buckets, and tasks.
4. **Business value:** Read and manage Microsoft Planner plans, buckets, and tasks.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P2.
7. **Planned development phase:** P2-D7; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /me/planner/plans`, `GET /planner/plans/{id}/tasks`, etc. [confirmed resource existence]. **Critical, explicitly and independently confirmed constraint**: the official documentation states, verbatim in substance, that **application permissions are "Not supported"** for Planner - confirmed explicitly in the permissions table for both read and write operation tiers. **Planner is delegated-only; it cannot be driven by a pure unattended daemon app** - this is the single clearest "do not even attempt app-only" case in the entire 70-feature catalog and must be documented prominently, not discovered by a caller via a confusing runtime failure.
11. **Nonfunctional requirements:** Delegated-token-source required; the connector should fail configuration validation fast if only an app-only token source is wired in for this feature.
12. **Public API contracts or proposed contracts:** Official API: Plan/task resource shapes and the explicit app-only-unsupported constraint, confirmed. Proposed C# contracts:

```csharp
public sealed class GraphPlannerTask { public string? Id { get; set; } public string? Title { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphPlannerTask> EnumeratePlanTasksAsync(string planId, CancellationToken ct = default); // delegated-only
```
13. **Dependencies:** MGR-001 (delegated path specifically).
14. **Security considerations:** Authentication and authorization: Delegated only - `Tasks.Read`/`Tasks.ReadWrite` (delegated). Required permissions/scopes/licenses: A signed-in user context; no application-permission path exists at all. Data privacy and security: Task content is caller-owned business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: Configuration validation rejects an app-only-only setup for this feature before any call is attempted.
16. **Unit-test requirements:** App-only-only configuration is rejected locally with a clear, specific message naming Planner's delegated-only constraint - this is the primary test for this feature, more important than ordinary CRUD coverage.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a plan/task list under a delegated token. Integration-test requirements: Authorized tenant with a signed-in test user and Planner provisioned; not available this pass.
18. **Acceptance criteria:** The delegated-only constraint is enforced locally and documented prominently.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No application-permission path exists at all for Planner - this is a vendor constraint, not an SDK limitation to be worked around.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No application-permission path exists at all for Planner - this is a vendor constraint, not an SDK limitation to be worked around. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

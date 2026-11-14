---
id: "MGR-011"
title: "SharePoint sites and document libraries"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-011 - SharePoint sites and document libraries

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-011 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-011.
2. **Feature name:** SharePoint sites and document libraries.
3. **Description:** Resolve and navigate SharePoint sites and their document libraries (which are themselves Drive resources, MGR-009).
4. **Business value:** Resolve and navigate SharePoint sites and their document libraries (which are themselves Drive resources, MGR-009).
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** Site ID is a 3-part composite string: `{hostname},{site-collection-guid},{site-guid}` [confirmed]. Resolution paths: `GET /sites/root` (tenant root site), `GET /sites/{hostname}` (e.g. `contoso.sharepoint.com`), `GET /sites/{hostname}:/{server-relative-path}` (a specific site by path) [confirmed]. A site's document libraries are exposed as Drive resources via `GET /sites/{id}/drives`, reusing MGR-009's Drive model.
11. **Nonfunctional requirements:** Read-only for this feature.
12. **Public API contracts or proposed contracts:** Official API: Composite site-ID shape and all three resolution paths, confirmed. Proposed C# contracts:

```csharp
public sealed record GraphSite(string Id, string? DisplayName, string? WebUrl);
public async Task<GraphSite> GetSiteByPathAsync(string hostname, string? serverRelativePath = null, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003, MGR-009.
14. **Security considerations:** Authentication and authorization: `Sites.Read.All` (application or delegated) [G11]. Required permissions/scopes/licenses: As above. Data privacy and security: Site metadata is organizational, not row-level business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single reads; drive-list reuses MGR-019. Error handling and concurrency: Standard classification; a malformed composite site ID is rejected locally before send.
16. **Unit-test requirements:** All three resolution-path URL compositions match the documented syntax; composite site-ID parsing round-trips.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a site resolved by path. Integration-test requirements: Authorized tenant with SharePoint provisioned; not available this pass.
18. **Acceptance criteria:** Site resolution works correctly for all three documented path forms.
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

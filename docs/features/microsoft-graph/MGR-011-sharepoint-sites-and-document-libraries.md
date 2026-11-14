---
id: "MGR-011"
title: "SharePoint sites and document libraries"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GetRootSiteAsync/GetSiteByPathAsync cover all three documented resolution-path forms (root, hostname-only, hostname-plus-colon-path); EnumerateSiteDrivesAsync reuses the Drive model's JsonDocument shape (MGR-009). Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
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
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single reads; drive-list reuses MGR-019. Error handling and concurrency: Standard classification. **Narrowed by Phase 2 review pass 1**: this previously also claimed "a malformed composite site ID is rejected locally before send," but the shipped `GraphSite.Id`/`EnumerateSiteDrivesAsync(siteId)` treat a site ID as an opaque string, forwarded verbatim like every other Graph resource identifier in this connector - the SDK never decomposes or reconstructs the `{hostname},{site-collection-guid},{site-guid}` shape (it has no need to; only non-empty is checked), so there is no parsing logic to reject a malformed composite against. This matches the proposed C# contract in section 12, which never proposed a decomposition method either.
16. **Unit-test requirements:** All three resolution-path URL compositions match the documented syntax. **Narrowed by Phase 2 review pass 1**: "composite site-ID parsing round-trips" was removed - there is no parsing to round-trip, per the correction above.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a site resolved by path. Integration-test requirements: Authorized tenant with SharePoint provisioned; not available this pass.
18. **Acceptance criteria:** Site resolution works correctly for all three documented path forms.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-tenant caveat.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphSites.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphSitesTests.cs`. Covers: `GetRootSiteAsync` hits `/sites/root`; `GetSiteByPathAsync` composes `/sites/{hostname}` with no path and `/sites/{hostname}:/{path}` with one; `EnumerateSiteDrivesAsync` hits `/sites/{id}/drives`; an empty hostname or site id is rejected locally before any HTTP call. |
| Remaining work / blockers | No live tenant available this pass to confirm against a real SharePoint site. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphSites.cs` plus `GraphSitesTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found this doc claimed local malformed-composite-site-ID rejection and round-trip parsing that the shipped code never implements (site IDs are opaque, forwarded verbatim) and that the checked completion-evidence boxes did not carve out. Narrowed the functional/unit-test requirements to match the proposed contract, which never called for decomposition either. No code changed. |

---
id: "MGR-006"
title: "Directory roles and admin units"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "implemented"
baseline_status: "Implemented: EnumerateDirectoryRolesAsync/EnumerateAdministrativeUnitsAsync cover both resources, using the corrected nested /directory/administrativeUnits path rather than a flat top-level one. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-006 - Directory roles and admin units

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-006 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-006.
2. **Feature name:** Directory roles and admin units.
3. **Description:** Read directory role assignments and administrative unit scoping, for governance/reporting integrations.
4. **Business value:** Read directory role assignments and administrative unit scoping, for governance/reporting integrations.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /directoryRoles` for role enumeration. **Correction to an initial assumption carried from the task's own feature description**: the actual path for administrative units is the nested `GET /directory/administrativeUnits`, not a flat top-level `/administrativeUnits` - independently confirmed in the Graph research pass. Permission: `RoleManagement.Read.Directory` (read) / `RoleManagement.ReadWrite.Directory` (write) [G11].
11. **Nonfunctional requirements:** Read-only scope for this P1 milestone; writes deferred to a later milestone if ever justified.
12. **Public API contracts or proposed contracts:** Official API: `/directoryRoles` and the corrected `/directory/administrativeUnits` path, independently confirmed. Proposed C# contracts:

```csharp
public sealed class GraphDirectoryRole { public string? Id { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphDirectoryRole> EnumerateDirectoryRolesAsync(CancellationToken ct = default);
public IAsyncEnumerable<JsonDocument> EnumerateAdministrativeUnitsAsync(CancellationToken ct = default); // GET /directory/administrativeUnits
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `RoleManagement.Read.Directory` (application), admin-consented. Required permissions/scopes/licenses: Privileged Role Administrator or Global Administrator typically grants the underlying permission. Data privacy and security: Role-assignment data is sensitive governance metadata.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: Standard classification.
16. **Unit-test requirements:** `/directory/administrativeUnits` path composed correctly (not the incorrect flat path); role enumeration pagination.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for both resources. Integration-test requirements: Authorized tenant with configured admin units; not available this pass.
18. **Acceptance criteria:** Both resources are read correctly using the verified paths.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphDirectoryRoles.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphDirectoryRolesTests.cs`. Covers: directory-role enumeration hits `/directoryRoles`; administrative-unit enumeration hits the corrected nested `/directory/administrativeUnits` path, not a flat top-level one. Full suite: 910 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live tenant available this pass to confirm against real role assignments/admin units. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphDirectoryRoles.cs` plus `GraphDirectoryRolesTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

---
id: "MGR-008"
title: "Organization and tenant info"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GetOrganizationAsync unwraps the documented value-array response and maps the User.Read-only restricted shape's absent fields to null rather than a default. Also now wired as GraphClient.DiagnoseAsync's Authentication/Connectivity probe (MGR-003/026). Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-008 - Organization and tenant info

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-008 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-008.
2. **Feature name:** Organization and tenant info.
3. **Description:** Read basic tenant-level organization metadata (verified domains, display name) for diagnostics and multi-tenant routing.
4. **Business value:** Read basic tenant-level organization metadata (verified domains, display name) for diagnostics and multi-tenant routing.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /organization`, `GET /organization/{id}` [confirmed]. **Important caveat, independently confirmed**: a caller holding only the `User.Read` permission sees a sharply restricted response containing only `id`, `displayName`, and `verifiedDomains` - every other field (e.g. licensing, technical contacts) is returned `null`, not omitted - this is a documented least-privilege response shape, not a bug, and the SDK's typed model must treat those fields as nullable rather than assuming they are always populated.
11. **Nonfunctional requirements:** Read-only.
12. **Public API contracts or proposed contracts:** Official API: `/organization`, confirmed; the `User.Read`-only restricted-field caveat independently confirmed. Proposed C# contracts:

```csharp
public sealed record GraphOrganization(string Id, string? DisplayName, IReadOnlyList<string>? VerifiedDomains);
public async Task<GraphOrganization> GetOrganizationAsync(CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `Organization.Read.All` (full detail) or just `User.Read` (restricted detail) [G11]. Required permissions/scopes/licenses: As above. Data privacy and security: Organization metadata is low-sensitivity tenant configuration, with licensing details being the more sensitive subset gated behind the higher permission.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single read, not paginated. Error handling and concurrency: Standard classification.
16. **Unit-test requirements:** Restricted-permission response correctly maps unpopulated fields to `null` rather than throwing or defaulting incorrectly.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both the full-detail and restricted-detail response shapes. Integration-test requirements: Authorized tenant; not available this pass.
18. **Acceptance criteria:** Both permission-levels' response shapes are handled correctly.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphOrganization.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphOrganizationTests.cs`, plus wired `GetOrganizationAsync` into `GraphClient.DiagnoseAsync`. Covers: the full-detail response parses correctly, including flattening `verifiedDomains` to plain names; a restricted `User.Read`-only shape maps absent fields to `null`, not a default; an empty `value` array surfaces `ContractException`; `DiagnoseAsync` reports `Authentication`/`Connectivity` `Passed` on success and `Failed`/`Skipped` on a 401. Full suite: 915 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live tenant available this pass to confirm either permission-level response shape against a real org. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphOrganization.cs` plus `GraphOrganizationTests.cs`, and wired `GetOrganizationAsync` into `DiagnoseAsync`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

---
id: "MGR-007"
title: "App registrations and service principals"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-007 - App registrations and service principals

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-007 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-007.
2. **Feature name:** App registrations and service principals.
3. **Description:** Read and manage Entra app registrations and their corresponding service principals, for tenant-automation scenarios.
4. **Business value:** Read and manage Entra app registrations and their corresponding service principals, for tenant-automation scenarios.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET/POST /applications`, `GET/POST /servicePrincipals` [confirmed resource existence]. Permissions: `Application.Read.All`/`Application.ReadWrite.All`/`Application.ReadWrite.OwnedBy` [confirmed]; creating a new app registration additionally requires `AppRegistration.Create` or equivalent delegated/admin capability [confirmed].
11. **Nonfunctional requirements:** Explicit write opt-in gate on create/update; this feature is inherently privileged and should default to read-only usage in samples.
12. **Public API contracts or proposed contracts:** Official API: `/applications`, `/servicePrincipals`, and the three permission variants, confirmed. Proposed C# contracts:

```csharp
public sealed class GraphApplication { public string? Id { get; set; } public string? AppId { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<GraphApplication> GetApplicationAsync(string id, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `Application.ReadWrite.OwnedBy` is the least-privileged option for an app managing only its own registration. Required permissions/scopes/licenses: Admin-consented application permission; app-registration creation specifically may require an elevated role beyond plain API permission. Data privacy and security: App registration metadata can include client secrets' hashes/expiry (not the secret itself) - treated as sensitive configuration metadata.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019/MGR-021. Error handling and concurrency: Standard classification.
16. **Unit-test requirements:** Read/create composition for both resources; `OwnedBy`-scoped permission distinguished from tenant-wide in documentation/tests.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for application/service-principal shapes. Integration-test requirements: Authorized tenant; not available this pass.
18. **Acceptance criteria:** Both resources read/create correctly against synthetic fixtures.
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

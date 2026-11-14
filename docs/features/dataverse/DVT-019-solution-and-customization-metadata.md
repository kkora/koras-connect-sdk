---
id: "DVT-019"
title: "Solution and customization metadata"
category: "dataverse"
priority: "P2"
phase: "P2-D7"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-019 - Solution and customization metadata

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-019 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-019.
2. **Feature name:** Solution and customization metadata.
3. **Description:** Discover which customization solution/publisher a component belongs to, and attach newly created components to a specific unmanaged solution during development/ALM workflows.
4. **Business value:** Discover which customization solution/publisher a component belongs to, and attach newly created components to a specific unmanaged solution during development/ALM workflows.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P2.
7. **Planned development phase:** P2-D7; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** Every solution has a **Publisher** with a customization prefix applied to every component's schema name to avoid cross-solution naming collisions (e.g. `new_`, `contoso_`) - corroborated by search of the official solution-concepts-ALM documentation, not independently fetched in full this session [vendor research gap]. The Web API exposes a `solutions` entity set (directly confirmed: the fetched actions-and-functions page uses `POST .../solutions/Microsoft.Dynamics.CRM.ExportTranslation` as a worked example) [D14]. The header `MSCRM.SolutionUniqueName` attaches a newly created component to a specific unmanaged solution rather than the tenant's default solution - directly confirmed on the headers table of the compose-HTTP-requests page [D3]. Solution import/export action signatures (`ImportSolution`/`ExportSolution`) were **not independently fetched this session** [vendor research gap].
11. **Nonfunctional requirements:** None beyond standard metadata-read behavior.
12. **Public API contracts or proposed contracts:** Official API: `MSCRM.SolutionUniqueName` header [D3]; `solutions` entity set existence [D14]; publisher/prefix concept and import/export actions not independently re-verified this session. Proposed C# contracts:

```csharp
public async Task<Guid> CreateRowInSolutionAsync(string entitySet, JsonElement fields, string solutionUniqueName, bool writesEnabled = false, CancellationToken ct = default);
// Sends the MSCRM.SolutionUniqueName header alongside DVT-004's CreateRowAsync.
```
13. **Dependencies:** DVT-004, DVT-009.
14. **Security considerations:** Authentication and authorization: Solution-component operations typically require System Customizer/System Administrator-level security roles, not ordinary table-level privilege. Required permissions/scopes/licenses: Elevated customization-level security role; this is a genuinely higher entitlement bar than any other Dataverse feature in this catalog and should be documented as such to callers. Data privacy and security: Solution/publisher metadata is not row-level business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses DVT-004/006 unchanged for the `solutions` entity set itself. Error handling and concurrency: Unchanged generic classification.
16. **Unit-test requirements:** `MSCRM.SolutionUniqueName` header sent correctly on a create request; `solutions` entity set CRUD via the generic DVT-004 primitives.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a header-qualified create request. Integration-test requirements: Authorized sandbox with System Customizer privilege and a real unmanaged solution - not available this pass.
18. **Acceptance criteria:** The `MSCRM.SolutionUniqueName` header mechanism works against a synthetic fixture; import/export actions are explicitly deferred, not guessed.
19. **Definition of Done:** Standard template; `ImportSolution`/`ExportSolution` action signatures remain a documented open gap (Section 28), not implemented until independently verified.
20. **External API entitlement or documentation blockers:** Publisher/prefix mechanics and solution import/export action signatures were not independently re-fetched this session.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Publisher/prefix mechanics and solution import/export action signatures were not independently re-fetched this session. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

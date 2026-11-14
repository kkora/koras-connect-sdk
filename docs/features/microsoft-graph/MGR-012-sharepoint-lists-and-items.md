---
id: "MGR-012"
title: "SharePoint lists and items"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: EnumerateListsAsync/EnumerateListItemsAsync/CreateListItemAsync/UpdateListItemFieldsAsync all require a siteId, satisfying the no-top-level-/lists/{id}-resource constraint by construction (no method accepts a bare listId). GraphListItem.Fields is nullable (fixed in review pass 1 - a response omitting fields, e.g. via a narrowing $select, previously risked an NRE on first indexer access rather than a documented null). Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-012 - SharePoint lists and items

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-012 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-012.
2. **Feature name:** SharePoint lists and items.
3. **Description:** Read and write SharePoint list items (distinct from document-library files).
4. **Business value:** Read and write SharePoint list items (distinct from document-library files).
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /sites/{id}/lists`, `GET /sites/{id}/lists/{id}/items`, `POST /sites/{id}/lists/{id}/items`, `PATCH /sites/{id}/lists/{id}/items/{id}/fields` [confirmed]. **Important, independently confirmed constraint**: lists are reached only through a site-scoped path - **there is no top-level `/lists/{id}` resource**; every list operation must be addressed via its owning site ID.
11. **Nonfunctional requirements:** Explicit write opt-in gate on item create/update.
12. **Public API contracts or proposed contracts:** Official API: Site-scoped list/item paths and the no-top-level-resource constraint, confirmed. Proposed C# contracts:

```csharp
public sealed record GraphListItem(string Id, IReadOnlyDictionary<string, JsonElement> Fields);
public async Task<string> CreateListItemAsync(string siteId, string listId, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** MGR-011.
14. **Security considerations:** Authentication and authorization: `Sites.ReadWrite.All` (application or delegated) for writes; `Sites.Read.All` for reads [G11]. Required permissions/scopes/licenses: As above. Data privacy and security: List item field values are caller-owned business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: A caller attempting a bare `/lists/{id}` path (without a site prefix) is rejected locally, since this is a connector-level API-shape constraint the SDK should enforce rather than let fail confusingly server-side.
16. **Unit-test requirements:** Site-scoped path composed correctly; item create/update respects the write-opt-in gate; field update targets `/fields` sub-resource correctly.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a list + items page. Integration-test requirements: Authorized tenant with a SharePoint list; not available this pass.
18. **Acceptance criteria:** List/item CRUD works correctly via the site-scoped path only.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphListItems.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphListItemsTests.cs`. Covers: list/item enumeration hit the site-scoped resources; `GraphListItem` parses the nested `fields` sub-resource correctly; create/update both require the shared `writesEnabled` opt-in; create posts the documented `{"fields":...}` envelope and parses the returned `id`; update targets the `.../items/{itemId}/fields` sub-resource specifically; a response omitting `fields` entirely now deserializes to a documented `null` rather than risking an NRE (review pass 1 fix - `EnumerateListItemsLeavesFieldsNullRatherThanThrowingWhenTheResponseOmitsIt`). |
| Remaining work / blockers | No live tenant available this pass to confirm against a real SharePoint list. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphListItems.cs` plus `GraphListItemsTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found `GraphListItem.Fields` was non-nullable despite being a reference-typed property that can legitimately deserialize to `null` (response omitting `fields`) - fixed the type and added a regression test. |

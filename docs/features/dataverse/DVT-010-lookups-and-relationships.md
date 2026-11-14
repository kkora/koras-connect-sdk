---
id: "DVT-010"
title: "Lookups and relationships"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-010 - Lookups and relationships

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-010 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-010.
2. **Feature name:** Lookups and relationships.
3. **Description:** Set, read, and remove relationships between rows (single-valued lookups and collection-valued 1:N/N:N associations).
4. **Business value:** Set, read, and remove relationships between rows (single-valued lookups and collection-valued 1:N/N:N associations).
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific; the `@odata.bind`/`$ref` mechanics are unique to Dataverse's OData dialect among this SDK's connectors.
10. **Functional requirements:** On create/update, set a single-valued navigation property via `"{navprop}@odata.bind": "/contacts(guid)"`; clear it by setting the bind property (or the plain nav-property name) to `null` [D12, D9]. Generic alternative for an existing row: `PUT {entityset}(guid)/{navprop}/$ref` with body `{"@odata.id": "<absolute URL>"}` to set, `DELETE` the same path to clear [D12]. Collection-valued (1:N and N:N) relationships: `POST {entityset}(guid)/{collectionNavProp}/$ref` with `{"@odata.id": "..."}` to add a member, `DELETE {entityset}(guid)/{collectionNavProp}(otherGuid)/$ref` to remove one [D12]. Retrieved single-valued lookup values surface as `_{navprop}_value`, with `Microsoft.Dynamics.CRM.lookuplogicalname`/`associatednavigationproperty` annotations available only when `Prefer: odata.include-annotations` is sent [D12] - the connector must document that a plain GET does not reveal which table a polymorphic lookup actually points to without that header.
11. **Nonfunctional requirements:** Explicit write opt-in on bind/associate/disassociate operations.
12. **Public API contracts or proposed contracts:** Official API: `@odata.bind`, `$ref` PUT/POST/DELETE forms - all per [D12]/[D9]. Proposed C# contracts:

```csharp
public async Task BindLookupAsync(string entitySet, Guid id, string navigationProperty, string targetEntitySet, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
public async Task ClearLookupAsync(string entitySet, Guid id, string navigationProperty, bool writesEnabled = false, CancellationToken ct = default);
public async Task AssociateAsync(string entitySet, Guid id, string collectionNavigationProperty, string targetEntitySet, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
public async Task DisassociateAsync(string entitySet, Guid id, string collectionNavigationProperty, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** DVT-004, DVT-009 (to know which properties are navigation properties).
14. **Security considerations:** Authentication and authorization: Associating/disassociating requires the Append/AppendTo privilege on the respective tables, in addition to Write on the primary row. Required permissions/scopes/licenses: Append (on the row gaining the association) and AppendTo (on the row being associated) privileges. Data privacy and security: Relationship data can itself reveal sensitive business structure (e.g., which account a confidential opportunity belongs to); not specially redacted beyond the SDK's general row-data policy.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single operations; never auto-retried. Error handling and concurrency: A bind to a nonexistent target row surfaces as a documented 4xx the connector classifies, not a silent no-op.
16. **Unit-test requirements:** `@odata.bind` payload shape on create; `$ref` PUT/POST/DELETE for single- and collection-valued cases; clearing a lookup via `null`; operations without `writesEnabled` throw before any HTTP call.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for each of the four documented relationship-mutation shapes. Integration-test requirements: Authorized sandbox with two related, disposable rows.
18. **Acceptance criteria:** All four relationship-mutation shapes work against synthetic fixtures without the caller hand-building `@odata.bind`/`$ref` payloads.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Polymorphic lookup target-table disclosure requires `Prefer: odata.include-annotations`, which the connector must always send on lookup-bearing reads or document as an opt-in - a design choice, not a vendor limitation, deferred to implementation (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Polymorphic lookup target-table disclosure requires `Prefer: odata.include-annotations`, which the connector must always send on lookup-bearing reads or document as an opt-in - a design choice, not a vendor limitation, deferred to implementation (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

---
id: "DVT-020"
title: "Virtual table capability discovery"
category: "dataverse"
priority: "P2"
phase: "P2-D7"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-020 - Virtual table capability discovery

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-020 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-020.
2. **Feature name:** Virtual table capability discovery.
3. **Description:** Detect when a table is a virtual table (backed by an external data source rather than native Dataverse storage) so a caller does not assume full OData feature parity (writes, certain query options) that virtual tables may not support.
4. **Business value:** Detect when a table is a virtual table (backed by an external data source rather than native Dataverse storage) so a caller does not assume full OData feature parity (writes, certain query options) that virtual tables may not support.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P2.
7. **Planned development phase:** P2-D7; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** A virtual table is composed of a data provider, a data-source row, and the virtual-table definition itself; built-in providers include SQL Server, SharePoint, Fabric, and a generic OData v4 provider - from the Web API's perspective a virtual table still appears as an ordinary `EntitySet` [vendor research, architecture confirmed via search, not independently fetched in full this session]. **Exact CRUD/query-option constraints specific to virtual tables (e.g., which OData features are unsupported) were not independently fetched this session** - this is a genuine, explicitly flagged gap, not an assumed "works exactly like a native table" claim.
11. **Nonfunctional requirements:** None beyond standard metadata-read behavior.
12. **Public API contracts or proposed contracts:** Official API: Builds on DVT-009's `EntityDefinitions` metadata surface; virtual-table-specific capability constraints are an open vendor-research gap. Proposed C# contracts:

```csharp
public sealed record DataverseVirtualTableInfo(string EntityLogicalName, bool IsVirtual, string? DataProviderName);
public Task<DataverseVirtualTableInfo> GetVirtualTableInfoAsync(string entitySet, CancellationToken ct = default); // reads from EntityDefinitions (DVT-009)
```
13. **Dependencies:** DVT-009.
14. **Security considerations:** Authentication and authorization: Unchanged from DVT-009. Required permissions/scopes/licenses: None beyond standard metadata-read. Data privacy and security: Not applicable; metadata only.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Unchanged from DVT-006/009. Error handling and concurrency: A write attempted against a read-only virtual table should surface the server's own documented rejection rather than the SDK silently assuming the write will succeed - but the exact rejection shape is part of the open gap above.
16. **Unit-test requirements:** `EntityDefinitions` parsing correctly identifies a virtual-table flag/provider name from a synthetic fixture matching the documented metadata shape, once that exact metadata field name is confirmed (currently an open gap - tests cannot be written against an unverified field name without risking asserting a guessed shape).
17. **Integration-test requirements:** Contract-test scenarios: Deferred until the metadata field name is independently confirmed. Integration-test requirements: Authorized sandbox with a real virtual table configured - not available this pass.
18. **Acceptance criteria:** Deferred - this feature cannot be marked beyond "Not started"/"Planned" until the specific metadata field distinguishing a virtual table is independently verified.
19. **Definition of Done:** Not applicable until the vendor-research gap closes; do not implement a guessed field name.
20. **External API entitlement or documentation blockers:** Exact virtual-table capability constraints and the specific metadata field distinguishing a virtual table were not independently fetched this session - this is the clearest Dataverse instance of "deliberately deferred over risk of inventing an unverified contract," the same category QLT-007/SAP-007 occupied before their own resolutions elsewhere in this SDK's history (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Exact virtual-table capability constraints and the specific metadata field distinguishing a virtual table were not independently fetched this session - this is the clearest Dataverse instance of "deliberately deferred over risk of inventing an unverified contract," the same category QLT-007/SAP-007 occupied before their own resolutions elsewhere in this SDK's history (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

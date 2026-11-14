---
id: "DVT-005"
title: "OData filtering, selection and ordering"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-005 - OData filtering, selection and ordering

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-005 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-005.
2. **Feature name:** OData filtering, selection and ordering.
3. **Description:** Let callers retrieve exactly the columns and rows they need rather than every column of every row.
4. **Business value:** Let callers retrieve exactly the columns and rows they need rather than every column of every row.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Follows the exact query-builder pattern `SuccessFactorsClient.QueryAddress`/ADP's `SearchWorkersAsync` filter whitelist already establish.
10. **Functional requirements:** Support `$select`, `$filter`, `$orderby`, `$expand`, `$apply` (aggregation), `$top`, `$count` [D6]. Explicitly reject (fail locally, not forward to the server) the three documented unsupported standard OData options: `$skip`, `$search`, `$format` [D6] - Dataverse paginates via `$skiptoken`/`odata.maxpagesize` (DVT-006), not `$skip`. Document the specific Dataverse-vs-FetchXml query limitations verified: no joins without a declared relationship, no nested `$expand` on an N:N relationship, no cross-table column comparison inside `$filter` [D6]. Parameter aliases (`@p1=...`) are supported in `$filter`/`$orderby` but **not** in `$expand` [D6] - the query builder must not silently alias an `$expand` clause.
11. **Nonfunctional requirements:** Query strings built via the same `Uri.EscapeDataString`-based safe-composition pattern every existing connector uses; no raw string interpolation of caller-supplied filter text without escaping.
12. **Public API contracts or proposed contracts:** Official API: Query-option support/exclusions per [D6]. Proposed C# contracts:

```csharp
public sealed record DataverseQuery(string? Select = null, string? Filter = null, string? OrderBy = null,
    string? Expand = null, string? Apply = null, int? Top = null, bool Count = false);
```
13. **Dependencies:** DVT-003, DVT-004.
14. **Security considerations:** Authentication and authorization: Unchanged from DVT-004. Required permissions/scopes/licenses: Column-level security (if configured on the tenant) can hide a selected column's value even when the row itself is readable - this is a genuine, tenant-specific entitlement outcome, not an SDK bug, and must be documented as such rather than treated as a parse failure. Data privacy and security: `$filter`/`$select` values may themselves be sensitive (they can reference field names implying business meaning); redact query strings from default logging exactly as ADP's `ODataQuery`/SAP's `ODataQuery` already redact `ToString()`.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Without `$top`, Dataverse returns up to 5,000 standard-table rows or 500 elastic-table rows per request [D6] - this interacts directly with DVT-006's pagination and must be documented together, not assumed to be "all rows." Error handling and concurrency: A `$skip`/`$search`/`$format` request throws `ArgumentException` locally before any HTTP call, mirroring the ADP whitelist-filter precedent.
16. **Unit-test requirements:** `$skip`/`$search`/`$format` rejected locally; query-string composition for each supported option matches the documented syntax; a filter string containing special characters round-trips through `Uri.EscapeDataString` correctly.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for a `$select`+`$filter`+`$orderby` combination against a documented example query shape. Integration-test requirements: Authorized sandbox table with column-level security configured, to confirm the "readable row, hidden column" behavior empirically.
18. **Acceptance criteria:** Every supported option composes correctly; every unsupported option fails locally.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The exact set of entities Dataverse disallows cross-table `$filter` comparisons on is tenant/table-schema-specific and not enumerable from generic documentation.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. The exact set of entities Dataverse disallows cross-table `$filter` comparisons on is tenant/table-schema-specific and not enumerable from generic documentation. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

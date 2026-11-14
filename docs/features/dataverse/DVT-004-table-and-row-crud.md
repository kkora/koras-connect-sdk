---
id: "DVT-004"
title: "Table and row CRUD"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: CreateRowAsync/GetRowAsync/UpdateRowAsync/DeleteRowAsync on DataverseClient, covering the documented OData-EntityId-header and @odata.id-annotation create-response shapes. Unit/contract-tested; no live environment available this pass."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# DVT-004 - Table and row CRUD

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-004 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-004.
2. **Feature name:** Table and row CRUD.
3. **Description:** Create, read, update and delete individual Dataverse table rows.
4. **Business value:** Create, read, update and delete individual Dataverse table rows.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific; follows the exact CRUD-wrapping pattern `SuccessFactorsClient.UpsertAsync`/`EnumerateAsync<T>` already establish for a different OData dialect.
10. **Functional requirements:** `POST {entityset}` creates (`204 No Content` + `OData-EntityId` response header identifying the new row, or `201 Created` with a body when `Prefer: return=representation` is sent); `GET {entityset}(guid)` reads one row; `PATCH {entityset}(guid)` updates (`204`, or `200` with `return=representation`); `DELETE {entityset}(guid)` deletes (`204`); `PUT {entityset}(guid)/{property}` with `{"value": ...}` updates a single property only [D9, D10]. The connector must read `OData-EntityId` on create to recover the new row's GUID when the caller did not request `return=representation`.
11. **Nonfunctional requirements:** Cancellation-aware; explicit opt-in gate (`writesEnabled: bool = false`) on create/update/delete, matching every existing Phase 1 write operation's convention.
12. **Public API contracts or proposed contracts:** Official API: POST/GET/PATCH/DELETE/PUT as above, confirmed directly on [D9]/[D10]. Proposed C# contracts:

```csharp
public async Task<Guid> CreateRowAsync(string entitySet, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> GetRowAsync(string entitySet, Guid id, ODataQuery? query = null, CancellationToken ct = default);
public async Task UpdateRowAsync(string entitySet, Guid id, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task DeleteRowAsync(string entitySet, Guid id, bool writesEnabled = false, CancellationToken ct = default);
public async Task UpdatePropertyAsync(string entitySet, Guid id, string property, JsonElement value, bool writesEnabled = false, CancellationToken ct = default);
// UpdatePropertyAsync (the fifth documented shape, PUT {entityset}(guid)/{property} with a {"value": ...} body)
// was found missing by Phase 2 review pass 1 despite this doc's acceptance criteria claiming all five shapes
// round-trip - added to close that gap rather than walking back the claim.
```
13. **Dependencies:** DVT-001, DVT-002, DVT-003.
14. **Security considerations:** Authentication and authorization: Bearer token from DVT-001; the specific table + operation must additionally be covered by the Application User's/delegated user's Dataverse security role - a 403 here is a genuine entitlement denial, not an SDK bug. Required permissions/scopes/licenses: A Dataverse security role granting Create/Read/Write/Delete privilege on the specific table, at a scope (User/Business Unit/Organization) covering the target row. Data privacy and security: Row field values are caller-owned business data; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-row operations are not paginated; GET reuses the shared GET-retry policy, writes are never auto-retried (matching every existing connector's "never replay an uncertain write" policy). Error handling and concurrency: 404 on GET/PATCH/DELETE for a nonexistent or security-filtered-out row (Dataverse does not distinguish "doesn't exist" from "exists but you can't see it" at this layer); `DataverseApiException` wraps single-row reads; concurrency handling is DVT-013's **requirement**, but its `If-Match`/`If-None-Match`/`DataverseConcurrencyException` implementation lives in this same file (`DataverseRows.cs`), alongside the row CRUD it extends - flagged by Phase 2 review pass 1 as previously undocumented here (DVT-013's own tracker already attributes the code correctly).
16. **Unit-test requirements:** Create without `writesEnabled` throws before any HTTP call; create recovers the GUID from `OData-EntityId`; create with `return=representation` recovers the GUID from the body instead; update/delete require the opt-in gate; 404 on a missing row surfaces correctly; malformed response body surfaces a contract error, not a silent empty result.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for each of the five documented response shapes (204+header, 201+body, 200+body, 204 bare, 206-style single-property PUT). Integration-test requirements: Authorized sandbox table + disposable test rows; explicit cleanup/reconciliation approach required before any write test runs.
18. **Acceptance criteria:** All five documented request/response shapes round-trip correctly against synthetic fixtures; no write is sent without explicit opt-in.
19. **Definition of Done:** Standard template; live sandbox CRUD evidence deferred.
20. **External API entitlement or documentation blockers:** No live Dataverse tenant available this pass; exact security-role-to-privilege mapping for a given tenant is inherently tenant-specific and cannot be verified from documentation alone.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `CreateRowAsync`/`GetRowAsync`/`UpdateRowAsync`/`DeleteRowAsync`/`UpdatePropertyAsync` to `src/Koras.Connect.Dataverse/DataverseRows.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseRowCrudTests.cs`. Covers: create without `writesEnabled` throws before any HTTP call; create recovers the GUID from the `OData-EntityId` header (204 path) and from an `@odata.id` annotation (representation path); create with neither recoverable shape surfaces `ContractException`; update/delete require the opt-in gate and are never retried; a missing row on GET surfaces `DataverseApiException` with `StatusCode == NotFound`; unsafe/malformed entity-set names rejected locally before any HTTP call; `UpdatePropertyAsync` (added in review pass 1 to close a real gap - the doc claimed all five shapes round-trip, but the fifth, single-property PUT, had no implementation at all) requires the opt-in gate, rejects an unsafe property name locally, and sends the documented `PUT {entityset}(guid)/{property}` address with a `{"value": ...}` body. Also fixed a disposal leak: `UpdateRowAsync`/`DeleteRowAsync` discarded the `IntegrationJsonResponse` returned by `SendResponseAsync` without disposing it. Required adding `odata-entityid` to `IntegrationTransport`'s safe-response-header allowlist in `Koras.Connect.Core` (a standard, non-sensitive OData header; no other connector is affected). |
| Remaining work / blockers | No live Dataverse tenant available this pass; exact security-role-to-privilege mapping for a given tenant is inherently tenant-specific and cannot be verified from documentation alone. Consumer documentation/sample deferred to DVT-022. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added row CRUD methods on `DataverseClient` plus `DataverseRowCrudTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live Dataverse tenant available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found the fifth documented shape (single-property `PUT`) was entirely unimplemented and untested despite the doc's own acceptance criteria claiming otherwise - added `UpdatePropertyAsync` and its tests to close the gap. Also fixed a disposal leak in `UpdateRowAsync`/`DeleteRowAsync` (discarded `IntegrationJsonResponse` never disposed), and clarified that DVT-013's concurrency implementation, not just its requirement, lives in this file. |

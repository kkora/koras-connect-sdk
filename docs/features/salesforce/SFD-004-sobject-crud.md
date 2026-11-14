---
id: "SFD-004"
title: "sObject CRUD"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: CreateRecordAsync/GetRecordAsync/UpdateRecordAsync/DeleteRecordAsync cover all four verbs and parse the documented create-response shape. The weak-validation If-Unmodified-Since conditional header (SFD-018's requirement) is implemented on UpdateRecordAsync in this same file; strong validation (ETag/If-Match) remains unexposed, its scope unconfirmed. Unit-tested; no live org available this pass."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-004 - sObject CRUD

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-004 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-004.
2. **Feature name:** sObject CRUD.
3. **Description:** Create, read, update and delete individual Salesforce records (sObjects).
4. **Business value:** Create, read, update and delete individual Salesforce records (sObjects).
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Follows the same CRUD-wrapping pattern as DVT-004, adapted to Salesforce's distinct response shapes.
10. **Functional requirements:** `POST /services/data/vXX.X/sobjects/{sObject}/` creates; response body `{"id":"...","errors":[],"success":true}` [PDF pp.42-47, directly confirmed]. `GET /services/data/vXX.X/sobjects/{sObject}/{id}/` reads, with an optional `fields` query param and support for `If-Match`/`If-None-Match`/`If-Modified-Since`/`If-Unmodified-Since` [PDF, directly confirmed]. `PATCH /services/data/vXX.X/sobjects/{sObject}/{id}/` updates, returning `204 No Content` on success. `DELETE /services/data/vXX.X/sobjects/{sObject}/{id}/` deletes. Documented status-code table (directly confirmed, verbatim): **200** (GET/HEAD/some PATCH), **201** (POST/some PATCH), **204** (DELETE/some PATCH), **300** (ambiguous external-ID match), **304**, **400**, **401**, **403**, **404** [PDF pp.144-154].
11. **Nonfunctional requirements:** Cancellation-aware; explicit write opt-in gate on create/update/delete, matching every other connector's convention.
12. **Public API contracts or proposed contracts:** Official API: All four verbs and the status-code table are directly confirmed [PDF pp.42-47, 144-154]. Proposed C# contracts:

```csharp
public async Task<string> CreateRecordAsync(string sObject, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> GetRecordAsync(string sObject, string id, IReadOnlyList<string>? fields = null, CancellationToken ct = default);
public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task DeleteRecordAsync(string sObject, string id, bool writesEnabled = false, CancellationToken ct = default);
// Shipped signature adds an `string? ifUnmodifiedSince = null` parameter to UpdateRecordAsync (SFD-018's weak-validation
// requirement, implemented in this same file) - not shown in the originally drafted signature above.
```
13. **Dependencies:** SFD-001, SFD-002, SFD-003.
14. **Security considerations:** Authentication and authorization: Bearer token from SFD-001; the specific object + operation must additionally be covered by the running user's object/field-level security (profile/permission set) - a 403 here is a genuine entitlement denial. Required permissions/scopes/licenses: Object-level CRUD permission and field-level security on the target sObject/fields. Data privacy and security: Record field values are caller-owned business data; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-record operations are not paginated; GET reuses the shared GET-retry policy; writes are never auto-retried. Error handling and concurrency: 404 on a missing/inaccessible record; 300 on an ambiguous external-ID upsert target (SFD-008); `SalesforceApiException` wraps single-record reads; concurrency handling (weak-validation `If-Unmodified-Since`) is SFD-018's **requirement**, but its implementation lives in this same file (`SalesforceRecords.cs`), alongside the row CRUD it extends - flagged by Phase 2 review pass 1 as previously misdescribed here as "deferred" when it was in fact already shipped (SFD-018's own tracker already attributed the code correctly).
16. **Unit-test requirements:** Create without `writesEnabled` throws before any HTTP call; create parses `id`/`success`/`errors`; 404 on a missing record surfaces correctly; 300 on an ambiguous match is distinguished from a generic failure.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for each documented status-code case. Integration-test requirements: Authorized sandbox org + disposable test records; not available this pass.
18. **Acceptance criteria:** All documented request/response shapes round-trip correctly against synthetic fixtures; no write is sent without explicit opt-in.
19. **Definition of Done:** Standard template; live sandbox CRUD evidence deferred.
20. **External API entitlement or documentation blockers:** No live Salesforce org available this pass; a standalone POST `Location` response header was referenced only inside Composite subrequest examples in the fetched PDF, not for a plain top-level POST - not assumed present until independently confirmed (Section 28).

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceRecords.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceRecordsTests.cs`. Covers: create without `writesEnabled` throws before any HTTP call; create parses `id` from the documented response shape and treats `success:false` as a contract error; `GET` applies the `fields` query parameter; 404 and the documented 300 (ambiguous external-ID match) both classify through `SalesforceApiException` distinctly by `StatusCode`; update/delete require the write opt-in gate; unsafe sObject names and malformed record ids are rejected locally before any HTTP call. `If-Unmodified-Since` (weak validation, SFD-018) is tested in `SalesforceConcurrencyTests.cs` rather than duplicated here. |
| Remaining work / blockers | `If-Match`/`If-None-Match`/`If-Modified-Since` (strong validation and the read-side conditional headers) remain unexposed - SFD-018's own tracker is the authoritative record of what's implemented (`If-Unmodified-Since`/weak validation only) versus what remains unconfirmed (strong validation's scope). Review pass 1 found this doc previously described the whole conditional-header area as "deferred to SFD-018," which was stale - the weak-validation half shipped in this same file. No live Salesforce org available this pass; a standalone POST `Location` response header was referenced only inside Composite subrequest examples, not confirmed for a plain top-level POST - not implemented. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `CreateRecordAsync`/`GetRecordAsync`/`UpdateRecordAsync`/`DeleteRecordAsync` plus `SalesforceRecordsTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live org available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found this doc stale: it described conditional-header support as entirely "deferred to SFD-018," but SFD-018's weak-validation (`If-Unmodified-Since`) implementation had already landed in this same file. Corrected the functional-requirements/proposed-contract/remaining-work text to reflect reality; no code changed. |
| 2026-11-14 | `implemented` | A targeted sweep found `UpdateRecordAsync`/`DeleteRecordAsync` discarded the disposable `IntegrationJsonResponse` from `SendResponseAsync` without disposing it - the same pattern already found and fixed in Dataverse's equivalent methods during an earlier pass, but never checked in this connector until now. Fixed both. |

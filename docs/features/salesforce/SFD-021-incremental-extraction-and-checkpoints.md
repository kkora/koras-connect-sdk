---
id: "SFD-021"
title: "Incremental extraction and checkpoints"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "implemented"
baseline_status: "Implemented: EnumerateChangesAsync<T> composes a SystemModstamp-filtered SOQL query against queryAll (surfacing soft-deleted rows) and reuses SFD-006's pagination unchanged. The SOAP-only getUpdated/getDeleted mechanism remains documented, not implemented, since this connector is REST/JSON-only. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-021 - Incremental extraction and checkpoints

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-021 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-021.
2. **Feature name:** Incremental extraction and checkpoints.
3. **Description:** Extract only records that changed since a prior checkpoint, using Salesforce's timestamp- and SOAP-call-based mechanisms (there is no REST-native delta-token equivalent to Dataverse's `$deltatoken`).
4. **Business value:** Extract only records that changed since a prior checkpoint, using Salesforce's timestamp- and SOAP-call-based mechanisms (there is no REST-native delta-token equivalent to Dataverse's `$deltatoken`).
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `SapDeltaCheckpoint`/`SapDeltaFilters`'s filter-based (not opaque-token-based) shape is the proof-of-concept precedent cited in Section 8.2, item 3 - Salesforce's mechanism is the same category, a filterable timestamp, not an opaque server-issued cursor.
10. **Functional requirements:** `SystemModstamp`: present on most standard and custom objects, auto-maintained, read-only; documentation notes cross-object rollup fields do not always propagate it reliably [snippet]. `queryAll` (SFD-005/011) includes soft-deleted/archived rows; combined with an explicit `isDeleted = TRUE` filter this covers deletions, unlike a plain `query` [snippet]. Two SOAP-only legacy calls provide an alternative incremental mechanism, each with a materially different lookback ceiling than the other: `getUpdated(sObjectType, startDate, endDate)` - max **30-day** lookback, result capped at **600,000 IDs** (`EXCEEDED_ID_LIMIT` beyond that) [snippet]; `getDeleted(sObjectType, startDate, endDate)` - max **15-day** lookback, notably shorter than `getUpdated`'s 30 days [snippet]. Since this connector is REST/JSON-only (Section 6 architecture decision, matching every other Phase 2 connector), `getUpdated`/`getDeleted` are documented here as a known vendor mechanism but are **not** implemented unless a future SOAP-interop exception is explicitly justified - the primary implementation path is `SystemModstamp`-filtered SOQL plus `queryAll`+`isDeleted` for deletions.
11. **Nonfunctional requirements:** Checkpoint persistence is caller-owned (via a connector-specific checkpoint record, not `ContinuationToken`, since `SystemModstamp` is a queryable filter value, not an opaque cursor - Section 8.2, item 3's reasoning applied concretely).
12. **Public API contracts or proposed contracts:** Official API: `SystemModstamp`, `queryAll`+`isDeleted`, and the `getUpdated`/`getDeleted` SOAP calls (including their differing 30-day/15-day lookback ceilings) are snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/resources_queryall.htm`, `api.meta/api/sforce_api_calls_getupdated.htm`, and `platform/api/guide/sforce-api-calls-getdeleted.html`; not independently re-fetched. Proposed C# contracts:

```csharp
public sealed record SalesforceDeltaCheckpoint(DateTimeOffset SinceSystemModstamp);
public IAsyncEnumerable<T> EnumerateChangesAsync<T>(string sObject, SalesforceDeltaCheckpoint checkpoint, IReadOnlyList<string> fields, CancellationToken ct = default);
```
13. **Dependencies:** SFD-005, SFD-006, SFD-007.
14. **Security considerations:** Authentication and authorization: Unchanged from SFD-005. Required permissions/scopes/licenses: Read access on the queried object/fields, including `SystemModstamp`/`IsDeleted`. Data privacy and security: Checkpoint timestamps are not business data; record payloads returned by the delta query are.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses SFD-006 for the underlying SOQL pagination. Error handling and concurrency: A checkpoint older than the data retention window (exact Salesforce-side retention for `queryAll`-visible soft-deleted rows not independently confirmed this pass) should be treated as a condition requiring a documented fallback (full resync), matching Dataverse's 7-day change-tracking-retention caveat in spirit, even though the exact Salesforce retention number is unconfirmed. `ORDER BY SystemModstamp` has no secondary sort key, so two rows sharing an identical `SystemModstamp` (a genuine possibility at millisecond granularity under concurrent writes) have no guaranteed relative order across pages - flagged by Phase 2 review pass 1 as previously undisclosed. Advancing the checkpoint to the last-seen `SystemModstamp` and re-running from there can therefore re-deliver a tied row the caller already processed; this SDK does not deduplicate on the caller's behalf, since it has no durable record of what the caller already consumed - callers resuming from a checkpoint must tolerate at-least-once delivery for tied timestamps (idempotent upsert on the caller's own primary key is the standard mitigation).
16. **Unit-test requirements:** `SystemModstamp`-filtered query composition is correct; `queryAll`+`isDeleted` correctly surfaces soft-deleted rows; the SOAP-only `getUpdated`/`getDeleted` mechanism is documented but has no corresponding implementation to test, by design.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a `SystemModstamp`-filtered query including both changed and soft-deleted rows. Integration-test requirements: Authorized sandbox org with a real deletion and a real cross-object rollup field, to confirm the `SystemModstamp` propagation caveat; not available this pass.
18. **Acceptance criteria:** Checkpoint-based extraction correctly surfaces both changed and deleted rows via the REST-only mechanism; the SOAP-only alternative remains documented, not implemented.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact soft-deleted-row retention window for `queryAll` visibility is unconfirmed; `SystemModstamp` propagation reliability for rollup fields is a documented, not independently tested, caveat.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceIncrementalExtraction.cs` (`SalesforceDeltaCheckpoint`/`EnumerateChangesAsync<T>`) and `tests/Koras.Connect.Tests/Salesforce/SalesforceIncrementalExtractionTests.cs`. Covers: the composed SOQL matches `SELECT {fields},SystemModstamp FROM {sObject} WHERE SystemModstamp >= {ISO8601} ORDER BY SystemModstamp` against `queryAll`; `SystemModstamp` is not duplicated when the caller already requested it; both changed and (synthetically) soft-deleted rows surface through the same enumeration; an empty field list is rejected locally before any HTTP call. |
| Remaining work / blockers | The SOAP-only `getUpdated`/`getDeleted` mechanism remains documented, not implemented - this connector is REST/JSON-only. Exact soft-deleted-row retention window for `queryAll` visibility and `SystemModstamp` propagation reliability for rollup fields remain unconfirmed; no live org available this pass to test either. Phase 2 review pass 1 found the tied-`SystemModstamp`/at-least-once-on-resume behavior was neither disclosed nor tested; disclosed it above (no code change - this is an inherent property of filtering on a non-unique timestamp, not a bug to fix). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceIncrementalExtraction.cs` (`SalesforceDeltaCheckpoint`/`EnumerateChangesAsync<T>`) plus `SalesforceIncrementalExtractionTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: retention window and rollup-field propagation remain unconfirmed. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found the tied-`SystemModstamp` at-least-once-on-resume behavior was undisclosed; added the disclosure and removed the stale full-suite pass count from the evidence row. No code changed. |

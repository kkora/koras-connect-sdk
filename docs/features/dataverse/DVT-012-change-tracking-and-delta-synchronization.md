---
id: "DVT-012"
title: "Change tracking and delta synchronization"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: GetChangesAsync sends Prefer: odata.track-changes, separates changed rows from documented deleted-row entries, and carries both @odata.nextLink and @odata.deltaLink as the same opaque ContinuationToken. Unit-tested; no live change-tracked table available this pass."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# DVT-012 - Change tracking and delta synchronization

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-012 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-012.
2. **Feature name:** Change tracking and delta synchronization.
3. **Description:** Extract only rows that changed since a prior checkpoint, instead of re-reading an entire table on every sync.
4. **Business value:** Extract only rows that changed since a prior checkpoint, instead of re-reading an entire table on every sync.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `ContinuationToken` reused for the opaque delta-link token (Section 8.2, item 3); the mechanism itself (header-triggered, response-embedded delta link) has no Phase 1 precedent - SAP's `SapDeltaCheckpoint` is filter-based, not token-based.
10. **Functional requirements:** Change tracking must be explicitly enabled per table (`EntityMetadata.ChangeTrackingEnabled`) and is **irreversible once turned on** [D13] - the connector must treat "change tracking not enabled for this table" as a documented, non-retryable configuration state, not a transient error. Request: `GET {entityset}?$select=...` plus `Prefer: odata.track-changes` [D13]. The response carries `@odata.deltaLink` (containing an opaque `$deltatoken=...`); following it returns only changed/created rows, plus deleted rows represented as `{"@odata.context": ".../$deltaEntity#{entityset}/$deletedEntity", "id": "...", "reason": "deleted"}` [D13] - **this is the one Phase 2 mechanism, across all three connectors, that natively reports deletions**, unlike the Phase 1 `Reconciliation.DetectChangesAsync` fallback (Section 8.1), which deliberately never does. `$filter`, `$orderby`, `$expand`, and `$top` are **not supported together with `odata.track-changes`** and the server errors if combined [D13] - reject this combination locally. Default unprocessed-change retention is **7 days**, configurable via `Organization.ExpireChangeTrackingInDays` [D13] - a caller resuming from an older delta token than the retention window must be told explicitly to fall back to a full resync, not receive a silently-wrong partial result.
11. **Nonfunctional requirements:** Cancellation-aware; the delta token is treated as a sensitive opaque value, never logged.
12. **Public API contracts or proposed contracts:** Official API: `Prefer: odata.track-changes`, `@odata.deltaLink`, deleted-row shape, incompatible query options, 7-day default retention - all per [D13]. Proposed C# contracts:

```csharp
public sealed record DataverseDeletedRow(Guid Id);
public sealed record DataverseDeltaPage<T>(IReadOnlyList<T> Changed, IReadOnlyList<DataverseDeletedRow> Deleted, ContinuationToken? DeltaLink);
public Task<DataverseDeltaPage<T>> GetChangesAsync<T>(string entitySet, string? select = null, ContinuationToken? resumeFrom = null, CancellationToken ct = default);
```
13. **Dependencies:** DVT-003, DVT-005, DVT-006, DVT-009 (to confirm `ChangeTrackingEnabled` before attempting).
14. **Security considerations:** Authentication and authorization: Unchanged; requires Read privilege on the tracked table. Required permissions/scopes/licenses: `ChangeTrackingEnabled` must already be turned on for the table by a tenant administrator - this is an irreversible, tenant-level configuration prerequisite the SDK cannot enable itself via the Web API alone (it is a metadata/customization operation, DVT-019's territory, and even there is documented as one-way). Data privacy and security: Delta tokens are opaque and sensitive; deleted-row entries carry only the row `id`, no business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: A delta response can itself be paginated via the normal `@odata.nextLink` mechanism (DVT-006) before eventually yielding `@odata.deltaLink`; reuses the shared continuation-safety plumbing. Error handling and concurrency: Combining `odata.track-changes` with `$filter`/`$orderby`/`$expand`/`$top` throws `ArgumentException` locally; an expired delta token should classify distinctly (a documented, recoverable-via-full-resync condition) rather than as a generic contract error.
16. **Unit-test requirements:** Changed-row page parses correctly; deleted-row shape parses correctly and is surfaced distinctly from changed rows; incompatible query-option combination rejected locally; resuming from a `@odata.deltaLink` round-trips without re-enumerating already-seen rows; an expired-token server response classifies as a distinct, documented condition.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture reproducing the documented changed+deleted mixed response shape. Integration-test requirements: Authorized sandbox table with change tracking enabled and a real deletion performed, to observe the deleted-row shape live - not available this pass.
18. **Acceptance criteria:** Changed and deleted rows are both surfaced correctly from a synthetic fixture; the incompatible-query-option guard is enforced locally.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live tenant with change tracking enabled available this pass; exact server error shape/code for an expired delta token was not independently fetched this session (Section 28 open question).

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseDelta.cs` (`DataverseDeletedRow`, `DataverseDeltaPage<T>`, `GetChangesAsync`) and `tests/Koras.Connect.Tests/Dataverse/DataverseDeltaTests.cs`. Returns `DataverseDeltaPage<JsonElement>` rather than the proposed generic `GetChangesAsync<T>` - matching `EnumerateAsync`'s existing `JsonElement`-based convention in this connector rather than introducing a second, inconsistent generic-typed read path. The incompatible-query-option guard is enforced by omission: no `$filter`/`$orderby`/`$expand`/`$top` parameter exists on this method at all (same "unsupported options have no property to request them through" precedent as DVT-005), so there is nothing to validate against. Covers: the `Prefer: odata.track-changes` header is sent; changed and deleted rows are correctly separated from the documented mixed response shape; a malformed deleted-row entry (missing/unparsable `id`) surfaces `ContractException`; both `@odata.nextLink` (mid-response pagination) and `@odata.deltaLink` (next-sync resume point) are carried identically as `Next`; resuming from a prior continuation forwards it unmodified via `EndpointSafety.Continuation`; `$select` composes correctly. Full suite: 686 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live tenant with change tracking enabled available this pass - "not enabled for this table" and "expired delta token" responses are not distinguished from any other server error, since their exact documented error shapes were not independently confirmed this session (standing Section 28 gap); callers should treat any unexpected `GetChangesAsync` failure as a signal to consider a full resync. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `GetChangesAsync`/`DataverseDeltaPage<T>`/`DataverseDeletedRow` plus `DataverseDeltaTests.cs` (adapted to a `JsonElement`-based, non-generic signature for consistency with `EnumerateAsync`). Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live change-tracked tenant available this pass, and the expired-token/not-enabled error shapes remain unconfirmed. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 correctly flagged that the "Functional requirements and public API contract satisfied" checkbox was checked despite functional requirement #15 (classify an expired delta token distinctly) being unmet - unchecked that gate rather than fabricating a classification for a server error shape this session never independently confirmed (standing Section 28/external-blocker gap, same category as SFD-014's "confirmed blocked, not attempted"). No code changed: guessing at an unverified vendor error shape would risk silently misclassifying a real failure as something else. |

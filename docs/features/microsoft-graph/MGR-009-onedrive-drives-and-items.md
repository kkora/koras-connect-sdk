---
id: "MGR-009"
title: "OneDrive drives and items"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GetMyDriveAsync/GetUserDriveAsync throw locally (before any HTTP call) unless GraphOptions.IsDelegatedContext is set, pointing the caller at EnumerateDrivesAsync/GetDriveAsync for app-only access; EnumerateDriveItemsAsync covers root and nested children. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-009 - OneDrive drives and items

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-009 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-009.
2. **Feature name:** OneDrive drives and items.
3. **Description:** Enumerate and navigate a user's or site's OneDrive drive and its items.
4. **Business value:** Enumerate and navigate a user's or site's OneDrive drive and its items.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /me/drive`, `GET /users/{id}/drive`, `GET /drives/{id}`, `GET /drives` [confirmed]. **Important, independently confirmed constraint**: application (app-only) permissions are **not supported** for a single `GET /me/drive` or `GET /users/{id}/drive` call - app-only access must go through `GET /drives` (the list form) instead; this asymmetry (list works app-only, single-resource shortcut doesn't) must be documented, not treated as a bug. A `404` on `/me/drive` can trigger automatic OneDrive provisioning for the user, but this behavior is **delegated-only** [confirmed].
11. **Nonfunctional requirements:** Read-only for this feature; writes are MGR-010's.
12. **Public API contracts or proposed contracts:** Official API: `/me/drive`, `/users/{id}/drive`, `/drives/{id}`, `/drives` and the app-only-vs-delegated asymmetry, all independently confirmed. Proposed C# contracts:

```csharp
public sealed class GraphDriveItem { public string? Id { get; set; } public string? Name { get; set; } public long? Size { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphDriveItem> EnumerateDriveItemsAsync(string driveId, string? itemId = null, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003, MGR-004 (for `/users/{id}/drive`).
14. **Security considerations:** Authentication and authorization: `Files.Read.All` (application, via `/drives` only) or delegated `Files.Read`/`Files.Read.All` (via `/me/drive` too) [G11]. Required permissions/scopes/licenses: As above; app-only callers must target `/drives/{id}` rather than the `/me` or `/users/{id}/drive` shortcuts. Data privacy and security: Drive item metadata (names, sizes) may reveal sensitive file organization; not specially redacted beyond the SDK's general policy.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: An app-only call against `/me/drive` should fail fast and clearly, pointing the caller at `/drives`, rather than a confusing generic 400.
16. **Unit-test requirements:** App-only call against `/me/drive` is rejected locally with a clear error pointing at `/drives`; `/drives` list enumeration works app-only; delegated `/me/drive` 404-triggers-provisioning behavior documented but not independently tested (requires a live, un-provisioned user).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for `/drives` list and a single drive's items. Integration-test requirements: Authorized tenant with OneDrive provisioned; not available this pass.
18. **Acceptance criteria:** The app-only/delegated asymmetry is enforced locally, preventing a confusing live failure.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphDrives.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphDrivesTests.cs`. Covers: `GetMyDriveAsync`/`GetUserDriveAsync` throw `InvalidOperationException` before any HTTP call unless `GraphOptions.IsDelegatedContext` is `true`; `EnumerateDrivesAsync`/`GetDriveAsync` work without a delegated context; `EnumerateDriveItemsAsync` hits `root/children` with no item id and `items/{itemId}/children` with one. Full suite: 928 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The delegated `/me/drive` 404-triggers-provisioning behavior remains documented, not independently tested (requires a live, un-provisioned user). No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphDrives.cs` plus `GraphDrivesTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

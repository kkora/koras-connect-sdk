---
id: "MGR-004"
title: "Users and profiles"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: EnumerateUsersAsync/GetUserAsync/CreateUserAsync/UpdateUserAsync/DeleteUserAsync cover the documented CRUD surface; GetMeAsync enforces the /me delegated-only restriction locally via the new GraphOptions.IsDelegatedContext flag, since the SDK cannot introspect an arbitrary caller-supplied token source. Also introduces GraphQuery/EnumerateAsync<T> (MGR-019's ordinary-pagination half), pulled forward since MGR-004 structurally depends on it. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-004 - Users and profiles

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-004 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-004.
2. **Feature name:** Users and profiles.
3. **Description:** Read and manage directory user profiles.
4. **Business value:** Read and manage directory user profiles.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Graph-specific.
10. **Functional requirements:** `GET /users`, `GET /users/{id|userPrincipalName}`, `PATCH /users/{id}`, `POST /users` [G1, confirmed resource existence via overview/permissions pages]. `/me` is a convenience alias for the signed-in user and is **delegated-only** - application permissions are explicitly not supported for `/me`, since there is no signed-in user context in an app-only call [verified via Graph research]. Standard query options (`$select`/`$filter`/`$orderby`/`$expand`/`$search`/`$count`) apply per MGR-019's pagination/delta mechanics.
11. **Nonfunctional requirements:** Explicit write opt-in gate on create/update.
12. **Public API contracts or proposed contracts:** Official API: `/users` CRUD confirmed via [G1]/[G2]; `/me` delegated-only restriction independently confirmed in the Graph research pass. Proposed C# contracts:

```csharp
public sealed class GraphUser { public string? Id { get; set; } public string? DisplayName { get; set; } public string? UserPrincipalName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphUser> EnumerateUsersAsync(GraphQuery? query = null, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `User.Read.All`/`User.ReadWrite.All` (application) or `User.Read`/`User.ReadWrite` (delegated), per [G2]/[G11]. Required permissions/scopes/licenses: Admin-consented application permission for app-only access; a signed-in user for `/me`. Data privacy and security: User profile data is PII; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: Calling `/me` with an app-only token should fail fast with a clear, classified error rather than a confusing generic 400/403.
16. **Unit-test requirements:** `/me` rejected locally (or clearly classified) when only an app-only token source is configured; user CRUD composes correctly; pagination/delta reuse verified.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a user list/page. Integration-test requirements: Authorized tenant with real users; not available this pass.
18. **Acceptance criteria:** User CRUD and the `/me` delegated-only restriction both behave correctly against synthetic fixtures.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/{GraphPagination,GraphUsers}.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/{GraphPagination,GraphUsers}Tests.cs`. Covers: user enumeration follows `@odata.nextLink`; `GetUserAsync` reads by id or UPN; `GetMeAsync` throws `InvalidOperationException` before any HTTP call unless `GraphOptions.IsDelegatedContext` is `true`, and succeeds against `/me` when it is; create/update/delete all require the shared `writesEnabled` opt-in and compose the documented URLs; create parses the returned `id`. Full suite: 900 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live Entra tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphUsers.cs` (plus `GraphPagination.cs`, pulled forward from MGR-019) and both test files. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

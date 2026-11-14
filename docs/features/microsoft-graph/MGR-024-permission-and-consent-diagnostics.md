---
id: "MGR-024"
title: "Permission and consent diagnostics"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: ListGrantsForCurrentAppAsync/ListMyGrantsAsync cover both documented list endpoints; BuildAdminConsentUrl is a pure local URL composer rejecting tenant==\"common\" before building anything. Also wires a new Permissions diagnostic stage into GraphClient.DiagnoseAsync (MGR-003/026), which required adding Permissions to the shared Koras.Connect.Abstractions.DiagnosticStage enum as an optional, Graph-specific stage. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-024 - Permission and consent diagnostics

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-024 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-024.
2. **Feature name:** Permission and consent diagnostics.
3. **Description:** Let a caller inspect which Graph permissions are actually granted/consented for their app registration, to diagnose "why did this call fail with 403" before it happens in production.
4. **Business value:** Let a caller inspect which Graph permissions are actually granted/consented for their app registration, to diagnose "why did this call fail with 403" before it happens in production.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Graph-specific; conceptually parallel to every connector's `DiagnoseAsync`, extended here with a Graph-specific permission inspection step beyond the generic connectivity probe.
10. **Functional requirements:** `oAuth2PermissionGrant` resource shape (confirmed): `clientId`, `consentType`, `principalId`, `resourceId`, `scope` (a space-delimited string, **max 3,850 characters** - confirmed exact figure). Tenant-wide list: `GET /oauth2PermissionGrants` (requires `Directory.Read.All`); per-user: `GET /me/oauth2PermissionGrants` [confirmed]. Admin-consent redirect endpoint, confirmed exact shape: `https://login.microsoftonline.com/{tenant}/v2.0/adminconsent?client_id=...&scope=...&redirect_uri=...&state=...` - `{tenant}` here must be a GUID, a verified domain, or the literal `organizations`, and is **explicitly documented as never valid as `common`** for this specific endpoint [confirmed].
11. **Nonfunctional requirements:** Read-only diagnostic surface; never issues a mutating request.
12. **Public API contracts or proposed contracts:** Official API: `oAuth2PermissionGrant` shape (including the exact 3,850-character scope-string limit), both list endpoints, and the admin-consent URL shape (including the `common`-never-valid constraint) - all confirmed. Proposed C# contracts:

```csharp
public sealed record GraphPermissionGrant(string ClientId, string ConsentType, string? PrincipalId, string ResourceId, string Scope);
public Task<IReadOnlyList<GraphPermissionGrant>> ListGrantsForCurrentAppAsync(CancellationToken ct = default);
public Uri BuildAdminConsentUrl(string tenant, string clientId, IReadOnlyList<string> scopes, Uri redirectUri, string? state = null);
```
13. **Dependencies:** MGR-001, MGR-002, MGR-003.
14. **Security considerations:** Authentication and authorization: `Directory.Read.All` (application) for the tenant-wide list; a signed-in user for the per-user list. Required permissions/scopes/licenses: As above. Data privacy and security: Permission-grant data reveals which scopes an app can exercise - treated as sensitive configuration/security metadata, not logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019 for the list endpoints. Error handling and concurrency: `BuildAdminConsentUrl` rejects `tenant == "common"` locally before constructing a URL, since the vendor documentation explicitly states this is never valid for this specific endpoint.
16. **Unit-test requirements:** `tenant == "common"` rejected locally in `BuildAdminConsentUrl`; grant-list parsing handles the 3,850-character scope-string boundary; admin-consent URL composition matches the documented query-parameter shape exactly.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a permission-grant list. Integration-test requirements: Authorized tenant to confirm the admin-consent redirect flow end-to-end; not available this pass.
18. **Acceptance criteria:** Permission-grant inspection and admin-consent URL construction both work correctly against synthetic fixtures, with the `common`-rejection rule enforced.
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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphPermissionDiagnostics.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphPermissionDiagnosticsTests.cs`; added `Permissions` to the shared `Koras.Connect.Abstractions.DiagnosticStage` enum (documented as an optional, connector-specific stage) and wired it into `GraphClient.DiagnoseAsync` with matching tests in `GraphClientTests.cs`. Covers: both list endpoints hit their documented resources; `BuildAdminConsentUrl` rejects `tenant == \"common\"` locally and composes the exact documented query shape, correctly omitting `state` when not supplied; `DiagnoseAsync` reports `Permissions` `Passed`/`Skipped`/`Failed` correctly depending on prior-stage outcome and the grant-list call's own result. Full suite: 1008 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The 3,850-character scope-string boundary is not locally validated, since this is a read path parsing whatever Graph returns, not a value this SDK composes. No live tenant available this pass to confirm the admin-consent redirect flow end-to-end. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphPermissionDiagnostics.cs` plus tests, and the new shared `DiagnosticStage.Permissions` value wired into `DiagnoseAsync`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

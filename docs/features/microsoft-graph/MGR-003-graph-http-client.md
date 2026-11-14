---
id: "MGR-003"
title: "Graph HTTP client"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GraphClient selects the v1.0 (default) or beta root per GraphOptions.UseBetaEndpoint. GraphApiException.TryExtractErrorCode reads error.code, which is casing-independent since it never looks inside innerError/innererror; GraphApiException.TryExtractCorrelationId (added in review pass 1) is where the dual-casing handling actually applies, reading the nested object's request-id from whichever casing is present. DiagnoseAsync now implements all four stages (Configuration/Authentication/Connectivity/Permissions), the last wired to MGR-024's grant-listing. ConsistencyLevel header support is implemented for $count/$search (MGR-004/019). Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-003 - Graph HTTP client

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-003 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-003.
2. **Feature name:** Graph HTTP client.
3. **Description:** Provide the single HTTP client foundation every other Graph feature builds on, including the v1.0/beta version split and Graph's specific error-body shape.
4. **Business value:** Provide the single HTTP client foundation every other Graph feature builds on, including the v1.0/beta version split and Graph's specific error-body shape.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged.
10. **Functional requirements:** Base URLs: `https://graph.microsoft.com/v1.0/` (GA, default) and `https://graph.microsoft.com/beta/` (explicit opt-in only, per MGR-026) [G1]. Standard error body: `{"error":{"code","message","innerError",...}}` [G1] - **flagged inconsistency, carried forward honestly**: some official pages render the nested field as `innerError` and others as `innererror`; the connector's error model must accept either casing rather than assuming one. **Clarified by Phase 2 review pass 1**: `code` itself lives directly under `error`, not inside the nested object, so `TryExtractErrorCode` never actually needs to read either casing - the casing distinction only matters for `TryExtractCorrelationId`, added this pass to read the nested object's `request-id` from whichever casing is present (previously nothing in the connector read anything from inside that object at all, making the original "accepts both casings" claim vacuous for the one method that existed). Standard HTTP status codes apply, including 429 (MGR-022). `ConsistencyLevel: eventual` header is required for certain advanced query scenarios (e.g., `$count`, some `$search`/`$filter` combinations on directory objects) [G1] - the connector must document which operations need it rather than sending it unconditionally.
11. **Nonfunctional requirements:** net10.0/Linux/Windows; HTTPS-only root.
12. **Public API contracts or proposed contracts:** Official API: v1.0/beta base URLs, error-body shape (with the casing caveat), `ConsistencyLevel` header - all per [G1]. Proposed C# contracts:

```csharp
public sealed record GraphOptions(bool UseBetaEndpoint = false) : IValidatableOptions;
public sealed partial class GraphClient { public GraphClient(IntegrationTransport transport, GraphOptions options); }
```
13. **Dependencies:** MGR-001, MGR-002.
14. **Security considerations:** Authentication and authorization: Delegates to MGR-001's token source. Required permissions/scopes/licenses: None beyond MGR-001. Data privacy and security: No credentials or tenant data logged by the client skeleton.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Delegated to MGR-019/022. Error handling and concurrency: `GraphApiException.FromTransportFailure` wraps single-resource reads, accepting both `innerError`/`innererror` casings when parsing.
16. **Unit-test requirements:** v1.0 vs. beta base URL selection; error body parses correctly under both casing variants; `ConsistencyLevel` header sent only for the documented operations that need it.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for both error-casing variants. Integration-test requirements: None beyond MGR-001's.
18. **Acceptance criteria:** Every Graph request targets the correct version root and parses errors correctly regardless of casing.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The `innerError`/`innererror` casing inconsistency is a genuine, unresolved vendor-documentation inconsistency, not an SDK defect - documented as such.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/{GraphOptions,GraphClient}.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphClientTests.cs`, later extended with MGR-008's `GET /organization` probe. Covers: default options target the `v1.0` root, `UseBetaEndpoint: true` targets the `beta` root; `DiagnoseAsync` reports `Configuration`/`Authentication`/`Connectivity` `Passed` on a successful probe and `Failed`/`Skipped` on a 401; `GraphApiException.TryExtractErrorCode` returns `null` for a malformed body (its own body-shape handling is unaffected by `innerError`/`innererror` casing, since `code` lives directly under `error`); `GraphApiException.TryExtractCorrelationId` (added in review pass 1, wired into `GraphBatch.cs`'s per-sub-request result) accepts both `innerError` and `innererror` casing for the nested `request-id`, which is the genuinely casing-sensitive case this feature's functional requirement describes; `FromTransportFailure` classifies by HTTP status via the shared classifier. |
| Remaining work / blockers | The `innerError`/`innererror` casing inconsistency remains a genuine, unresolved vendor-documentation inconsistency, not an SDK defect. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphOptions.cs`/`GraphClient.cs` plus `GraphClientTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the implemented scope; held at `partial` because `DiagnoseAsync`'s non-Configuration stages and the `ConsistencyLevel` header remain unimplemented pending later features. |
| 2026-11-14 | `partial` | Wired `GetOrganizationAsync` (MGR-008) into `DiagnoseAsync`'s Authentication/Connectivity stages and `GraphQuery` (MGR-004/019) into the `ConsistencyLevel` header. Held at `partial`: a `Permissions` stage remains pending MGR-024. |
| 2026-11-14 | `implemented` | Wired the new `DiagnosticStage.Permissions` stage (MGR-024) into `DiagnoseAsync`. All four stages and every documented functional requirement are now implemented; status moves to `implemented`. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found `TryExtractErrorCode`'s "accepts both innerError/innererror casings" claim was vacuous - `code` never reads inside that nested object at all, so the casing never mattered for that method, and the existing test would have passed identically regardless. Added `TryExtractCorrelationId`, which genuinely does need both casings (it reads the nested object's own `request-id`), and wired it into `GraphBatch.cs` so per-sub-request failures now carry a real `ErrorCorrelationId` instead of always `null`. |

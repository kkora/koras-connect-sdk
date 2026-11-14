---
id: "MGR-003"
title: "Graph HTTP client"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "partial"
baseline_status: "Implemented: GraphClient selects the v1.0 (default) or beta root per GraphOptions.UseBetaEndpoint; GraphApiException.TryExtractErrorCode accepts both innerError/innererror casings. DiagnoseAsync so far implements only the Configuration stage - the Authentication/Connectivity probe (GET /organization) is deferred to MGR-008, and a Permissions stage to MGR-024, per this feature's own cross-reference to those features. ConsistencyLevel header support is deferred to the specific operations that need it (MGR-005/006). Unit-tested."
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
10. **Functional requirements:** Base URLs: `https://graph.microsoft.com/v1.0/` (GA, default) and `https://graph.microsoft.com/beta/` (explicit opt-in only, per MGR-026) [G1]. Standard error body: `{"error":{"code","message","innerError",...}}` [G1] - **flagged inconsistency, carried forward honestly**: some official pages render the nested field as `innerError` and others as `innererror`; the connector's error model must accept either casing rather than assuming one. Standard HTTP status codes apply, including 429 (MGR-022). `ConsistencyLevel: eventual` header is required for certain advanced query scenarios (e.g., `$count`, some `$search`/`$filter` combinations on directory objects) [G1] - the connector must document which operations need it rather than sending it unconditionally.
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

- [x] Functional requirements and public API contract satisfied (Configuration-only diagnostic stage so far - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (`DiagnoseAsync`'s Authentication/Connectivity/Permissions stages remain pending MGR-008/MGR-024).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/{GraphOptions,GraphClient}.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphClientTests.cs`. Covers: default options target the `v1.0` root, `UseBetaEndpoint: true` targets the `beta` root; `DiagnoseAsync` reports `Configuration` `Passed` without any HTTP call; `GraphApiException.TryExtractErrorCode` accepts both `innerError` and `innererror` casing and returns `null` for a malformed body; `FromTransportFailure` classifies by HTTP status via the shared classifier. Full suite: 877 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | `DiagnoseAsync` implements only the `Configuration` stage - `Authentication`/`Connectivity` will wire to MGR-008's `GET /organization` once that feature exists, and a `Permissions` stage to MGR-024's grant-listing once that feature exists, per this feature's own cross-reference. The `ConsistencyLevel` header is now sent by `GraphQuery`/`EnumerateAsync<T>` (MGR-004/019) whenever `$count` or `$search` is used - the only two operations this connector currently knows need it. The `innerError`/`innererror` casing inconsistency remains a genuine, unresolved vendor-documentation inconsistency, not an SDK defect. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphOptions.cs`/`GraphClient.cs` plus `GraphClientTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the implemented scope; held at `partial` because `DiagnoseAsync`'s non-Configuration stages and the `ConsistencyLevel` header remain unimplemented pending later features. |

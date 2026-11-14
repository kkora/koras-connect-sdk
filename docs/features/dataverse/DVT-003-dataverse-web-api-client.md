---
id: "DVT-003"
title: "Dataverse Web API client"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: DataverseClient composes {environment-url}/api/data/v9.2/, requires OData-MaxVersion/OData-Version headers, and enforces the documented 32 KB (64 KB in $batch) URL-length and 260-character segment-length bounds locally before send. Unit-tested; no live environment available this pass."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-003 - Dataverse Web API client

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-003 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-003.
2. **Feature name:** Dataverse Web API client.
3. **Description:** Provide the single HTTP client foundation (base URL composition, required headers, version policy) every other Dataverse feature builds on.
4. **Business value:** Provide the single HTTP client foundation (base URL composition, required headers, version policy) every other Dataverse feature builds on.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged (Section 8.1), matching exactly how `SuccessFactorsClient` wraps `IntegrationTransport` over SAP's own OData V2 root today.
10. **Functional requirements:** Compose the base URL as `{environment-url}/api/data/v9.2/` [D3]; require `OData-MaxVersion: 4.0`, `OData-Version: 4.0`, `Accept: application/json` on every request [D1, D3]; enforce the documented 32 KB max URL length (64 KB inside `$batch`) and 260-character max individual OData path-segment length [D3] as local pre-send validation, not merely a server-side surprise. Current version is `v9.2`; code written against `v9.0`/`v9.1` keeps working on later minor versions, but new capabilities are not backported to earlier minor versions [D3] - the connector targets `v9.2` fixed, with the version string isolated to one constant for a future bump.
11. **Nonfunctional requirements:** net10.0/Linux/Windows; HTTPS-only root (`EndpointSafety.ValidateRoot` already enforces this); bounded request/response size.
12. **Public API contracts or proposed contracts:** Official API: Base URL pattern and header requirements per [D1]/[D3]; `v9.2` current version [D3]. Proposed C# contracts:

```csharp
public sealed record DataverseOptions(Uri EnvironmentRoot, int MaxPageSize = 5000) : IValidatableOptions;
public sealed partial class DataverseClient
{
    public DataverseClient(IntegrationTransport transport, DataverseOptions options);
}
```
13. **Dependencies:** DVT-001, DVT-002.
14. **Security considerations:** Authentication and authorization: Delegates to DVT-001's token source via `IRequestAuthenticator`/`BearerTokenAuthenticator`, unchanged. Required permissions/scopes/licenses: None beyond DVT-001. Data privacy and security: No credentials or row data logged by the client skeleton itself.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Delegated to DVT-006/DVT-014 respectively; this feature only establishes the client skeleton. Error handling and concurrency: Delegates to the shared `IntegrationExceptionClassifier`; a `DataverseApiException.FromTransportFailure` wrapper is added here for single-resource reads, matching the ADP/SAP/Qualtrics precedent (Section 6.1).
16. **Unit-test requirements:** Required headers present on every request; oversized URL/segment rejected locally before send; non-HTTPS/credential-bearing root rejected at construction (`EndpointSafety.ValidateRoot`, already proven behavior).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture asserting header shape against a captured-from-documentation example request. Integration-test requirements: None beyond DVT-002's.
18. **Acceptance criteria:** Every Dataverse request carries the three required headers and a validated, bounded URL.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond DVT-001/002's.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseClient.cs`/`DataverseOptions.cs` and `DataverseClientTests.cs`. Covers: non-HTTPS/credential-bearing root rejected at construction (reusing `EndpointSafety.ValidateRoot`), page-size bounds, base URL composed as `{environment-url}/api/data/v9.2/`, every request carrying `OData-MaxVersion: 4.0`/`OData-Version: 4.0`/`Accept: application/json`. The 32 KB/64 KB URL-length and 260-character segment-length local pre-send validation (`ValidateRequestUri`) is implemented but not yet exercised by a dedicated test in this commit - no request built by DVT-001/002/003 alone produces a URL long enough to trigger it; coverage is deferred to DVT-004's CRUD paths, which build URLs from caller-supplied table/row identifiers. Full suite: 594 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | `ValidateRequestUri`'s oversized-URL/segment rejection needs a dedicated test once a caller-controlled long identifier exists (DVT-004). No live Dataverse environment available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `DataverseClient`/`DataverseOptions`/`DataverseApiException` plus `DataverseClientTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: the URL/segment-length bound is implemented but not yet directly tested (tracked, see Remaining work), and no live environment is available this pass. |

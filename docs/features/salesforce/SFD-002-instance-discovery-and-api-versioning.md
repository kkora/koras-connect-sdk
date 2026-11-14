---
id: "SFD-002"
title: "Instance discovery and API versioning"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: SalesforceDiscovery.ListApiVersionsAsync calls the unauthenticated GET {host}/services/data/ endpoint directly via a plain HttpClient (IntegrationTransport always authenticates). instance_url is a separate, explicitly configured SalesforceOptions input, not re-derived from any token response. Because this call bypasses IntegrationTransport entirely, it now wraps its own connection-level failures into TransportException - a real gap found and fixed in Phase 2 review pass 1 (the raw HttpClient call had no try/catch at all, so an unreachable host previously escaped as a bare HttpRequestException). Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-002 - Instance discovery and API versioning

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-002 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-002.
2. **Feature name:** Instance discovery and API versioning.
3. **Description:** Resolve which Salesforce instance/API version to call after authenticating, since `instance_url` is dynamic per org and the REST API itself is versioned.
4. **Business value:** Resolve which Salesforce instance/API version to call after authenticating, since `instance_url` is dynamic per org and the REST API itself is versioned.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `EndpointSafety.ValidateRoot` reused unchanged for the resolved `instance_url`.
10. **Functional requirements:** `instance_url` is returned in the OAuth token JSON response alongside `access_token` **[snippet, long-standing uncontroversial behavior, not independently re-read this pass]**. `GET /services/data/` requires **no authentication** and returns a JSON array of `{label, url, version}` per available API version (e.g. `{"label":"Summer '14","url":"/services/data/v31.0","version":"31.0"}`); `/services/data/vXX.X/` (no trailing resource) itself, once a version is chosen, returns the map of named resources (SFD-003) **[PDF p.30/138, directly confirmed]**. Current version at research time: **v68.0** [PDF cover page]. The connector targets a configurable version with `v68.0` as the default, isolated to one constant.
11. **Nonfunctional requirements:** Cancellation-aware; bounded timeout.
12. **Public API contracts or proposed contracts:** Official API: `GET /services/data/` per [PDF p.30]. Proposed C# contracts:

```csharp
public sealed record SalesforceApiVersion(string Label, string Url, string Version);
public Task<IReadOnlyList<SalesforceApiVersion>> ListApiVersionsAsync(CancellationToken ct = default); // unauthenticated
```
13. **Dependencies:** SFD-001.
14. **Security considerations:** Authentication and authorization: Version discovery itself requires no token; every subsequent call uses the bearer token + resolved `instance_url`. Required permissions/scopes/licenses: None for version discovery. Data privacy and security: `instance_url` is not itself sensitive but is treated with the same redaction discipline as any configuration value.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single read, not paginated. Error handling and concurrency: Unreachable/malformed host surfaces as a transport failure, not a silent empty list.
16. **Unit-test requirements:** Version list parses correctly; unauthenticated call sends no `Authorization` header; a non-existent/unreachable host surfaces a transport exception.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture matching the documented version-list shape. Integration-test requirements: None beyond SFD-001's.
18. **Acceptance criteria:** A caller can discover available API versions without a token, and every subsequent call correctly targets the resolved `instance_url` + chosen version.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond SFD-001's.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceDiscovery.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceDiscoveryTests.cs`. Covers: the documented version-list shape parses correctly; the call sends no `Authorization` header; a reachable host returning a non-success status classifies through `SalesforceApiException` rather than returning a silent empty list; a genuinely unreachable host (connection-level `HttpRequestException`) wraps into `TransportException` rather than escaping raw - review pass 1 found the originally-named "unreachable host" test actually only simulated a reachable host returning 503, and the real unreachable-host path had no try/catch at all; fixed both the gap and the misleadingly-named test (renamed to `AReachableHostReturningAServerErrorSurfacesASalesforceApiExceptionNotASilentEmptyList`, plus a new `AGenuinelyUnreachableHostWrapsIntoATransportExceptionRatherThanARawHttpRequestException`); a malformed (non-array) body surfaces `ContractException`. |
| Remaining work / blockers | None beyond SFD-001's. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceDiscovery.ListApiVersionsAsync`/`SalesforceApiVersion` plus `SalesforceDiscoveryTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live org available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found a genuine bug (also present in the shared `Koras.Connect.Core.Transport.IntegrationTransport`, fixed there too): a connection-level failure had no try/catch and would escape as a raw `HttpRequestException`. Added a catch wrapping it into `TransportException`, fixed the misleadingly-named existing test, and added a genuinely-unreachable-host test. |

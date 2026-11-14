---
id: "DVT-002"
title: "Environment discovery and connection"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-002 - Environment discovery and connection

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-002 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-002.
2. **Feature name:** Environment discovery and connection.
3. **Description:** Resolve which Dataverse environment (organization) a caller's credentials should target, and validate connectivity to it, before any table operation is attempted.
4. **Business value:** Resolve which Dataverse environment (organization) a caller's credentials should target, and validate connectivity to it, before any table operation is attempted.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `EndpointSafety.ValidateRoot` reused unchanged for the resolved environment API root; `IConnector.DiagnoseAsync` reused for the connectivity probe (Section 8.1).
10. **Functional requirements:** Support two discovery modes: (a) caller supplies the environment URL directly (`https://{org}.api.crm{region}.dynamics.com/`) - the common case once an environment is chosen; (b) optional Global Discovery Service (GDS) lookup, `GET https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances`, returning `Instance` records (`ApiUrl`, `Id`, `EnvironmentId`, `UniqueName`, `FriendlyName`, `Region`, `Version`, `TenantId`) for environments the authenticated identity can access [D5]. Confirm connectivity via the `WhoAmI` unbound function, `GET {environment}/api/data/v9.2/WhoAmI`, which returns the caller's `UserId`/`BusinessUnitId`/`OrganizationId` - documented as a connectivity check, not a discovery mechanism [D5]. GDS endpoint varies by sovereign cloud: Commercial `globaldisco.crm.dynamics.com`, GCC `...crm9.dynamics.com`, GCC-High `...crm.microsoftdynamics.us`, DoD `...crm.appsplatform.us`, China `...crm.dynamics.cn` [D5] - the connector must accept an explicit GDS root rather than hardcoding the commercial-cloud default silently.
11. **Nonfunctional requirements:** Bounded timeout; cancellation-aware; no environment auto-selection when GDS returns more than one instance (caller must disambiguate explicitly).
12. **Public API contracts or proposed contracts:** Official API: `GET /api/discovery/v2.0/Instances` on the GDS host [D5]; `GET {environment}/api/data/v9.2/WhoAmI` [D5, D1]. Proposed C# contracts:

```csharp
public sealed record DataverseInstance(string ApiUrl, string Id, string EnvironmentId, string UniqueName,
    string FriendlyName, string Region, string Version, string TenantId);
public sealed record WhoAmIResult(Guid UserId, Guid BusinessUnitId, Guid OrganizationId);
public interface IDataverseDiscoveryClient
{
    IAsyncEnumerable<DataverseInstance> ListInstancesAsync(CancellationToken cancellationToken = default);
}
```
13. **Dependencies:** DVT-001.
14. **Security considerations:** Authentication and authorization: GDS and `WhoAmI` both require the same bearer token as any other Dataverse call; GDS specifically needs a token scoped to the GDS host itself, not the target environment. Required permissions/scopes/licenses: None beyond a valid Entra identity; GDS only lists environments the identity already has some access to. Data privacy and security: `DataverseInstance`/`WhoAmIResult` contain tenant/org/environment identifiers, not employee or business data; still excluded from default telemetry per the shared safe-logging policy.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: `WhoAmI` is a single read; reuses `IntegrationTransport.ReadJsonAsync`'s existing GET-retry behavior unchanged. GDS instance listing is not documented as paginated in the fetched research. Error handling and concurrency: 401/403 classify via the existing shared classifier; an empty GDS instance list is a valid, non-error outcome (no accessible environments) and must not be conflated with an authentication failure.
16. **Unit-test requirements:** `WhoAmI` 200 response parses `UserId`/`BusinessUnitId`/`OrganizationId`; GDS response with zero/one/multiple instances; malformed GDS response surfaces a contract error, not a crash.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic GDS/`WhoAmI` fixtures matching the documented shapes. Integration-test requirements: Requires a real Entra identity with at least one accessible Dataverse environment.
18. **Acceptance criteria:** A caller supplying a direct environment URL never needs GDS; a caller using GDS receives every accessible instance without the SDK silently picking one.
19. **Definition of Done:** Standard template; live GDS/WhoAmI sandbox evidence deferred.
20. **External API entitlement or documentation blockers:** No live Dataverse tenant available this pass.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [ ] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; no Phase 2 implementation exists yet |
| Validation commands / results | Not recorded; no Phase 2 implementation exists yet |
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live Dataverse tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

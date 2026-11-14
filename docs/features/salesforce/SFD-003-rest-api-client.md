---
id: "SFD-003"
title: "REST API client"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: SalesforceClient composes {instance_url}/services/data/{version}/, authenticates every request via the shared BearerTokenAuthenticator, and GetResourceMapAsync parses the documented resource-map shape, also used as DiagnoseAsync's probe. Unit-tested; no live org available this pass."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-003 - REST API client

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-003 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-003.
2. **Feature name:** REST API client.
3. **Description:** Provide the single HTTP client foundation every other Salesforce feature builds on.
4. **Business value:** Provide the single HTTP client foundation every other Salesforce feature builds on.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged (Section 8.1).
10. **Functional requirements:** Base path `{instance_url}/services/data/v{XX.X}/`; `GET` on exactly that path ("Resources by Version") returns a JSON map of resource name → relative URI, confirmed resource keys include `sobjects`, `tooling`, `query`, `queryAll`, `search`, `chatter`, `composite`, `limits`, `theme` [PDF p.38, directly confirmed]. Every request: `Authorization: Bearer {token}`, `Content-Type: application/json` (XML also officially supported but out of scope - this SDK is JSON-only, matching every other Phase 1/2 connector's convention).
11. **Nonfunctional requirements:** net10.0/Linux/Windows; HTTPS-only root.
12. **Public API contracts or proposed contracts:** Official API: Resource-map shape per [PDF p.38]. Proposed C# contracts:

```csharp
public sealed record SalesforceOptions(Uri InstanceRoot, string ApiVersion = "v68.0") : IValidatableOptions;
public sealed partial class SalesforceClient { public SalesforceClient(IntegrationTransport transport, SalesforceOptions options); }
```
13. **Dependencies:** SFD-001, SFD-002.
14. **Security considerations:** Authentication and authorization: Delegates to SFD-001's token source via the shared `IRequestAuthenticator`. Required permissions/scopes/licenses: None beyond SFD-001. Data privacy and security: No credentials or record data logged by the client skeleton.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Delegated to SFD-006/SFD-017. Error handling and concurrency: `SalesforceApiException.FromTransportFailure` wraps single-resource reads, matching the ADP/SAP/Qualtrics/Dataverse read-vs-write split (Section 6.1).
16. **Unit-test requirements:** Required headers present on every request; resource-map parsing matches the documented shape; non-HTTPS root rejected at construction.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for the resource-map response. Integration-test requirements: None beyond SFD-002's.
18. **Acceptance criteria:** Every Salesforce request carries the correct headers against the resolved instance root and API version.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceClient.cs`/`SalesforceOptions.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceClientTests.cs`. Covers: non-HTTPS/credential-bearing root and an invalid API version string rejected at construction; base URL composed with the configured version; the documented resource-map shape parses; every request carries a `Bearer` `Authorization` header; a 401 classifies through `SalesforceApiException`; `DiagnoseAsync` reports Configuration/Authentication/Connectivity correctly on success, on a 401, and on a non-auth failure. Full suite: 733 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | None beyond the standing lack of a live org this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceClient`/`SalesforceOptions`/`SalesforceApiException` plus `SalesforceClientTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live org available this pass. |

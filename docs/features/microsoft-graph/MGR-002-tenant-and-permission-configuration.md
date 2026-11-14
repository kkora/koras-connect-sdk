---
id: "MGR-002"
title: "Tenant and permission configuration"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# MGR-002 - Tenant and permission configuration

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-002 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-002.
2. **Feature name:** Tenant and permission configuration.
3. **Description:** Document and validate which Graph permissions (delegated vs. application) a caller's app registration actually holds, before any downstream feature assumes access it doesn't have.
4. **Business value:** Document and validate which Graph permissions (delegated vs. application) a caller's app registration actually holds, before any downstream feature assumes access it doesn't have.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Graph-specific; this feature is primarily configuration/documentation over MGR-001/MGR-024.
10. **Functional requirements:** Delegated permissions require a signed-in user and are represented as `oauth2PermissionGrant` records; application permissions require tenant-admin consent and are represented as `appRoleAssignment` records [G2]. Granting an application permission for Graph specifically requires the **Privileged Role Administrator** role (or Global Administrator); granting most other delegated/admin-consent scenarios needs Cloud Application Administrator or Application Administrator [G2]. Per-tenant permission-request limits vary by audience: observed figures of roughly 400/155/300/30 requested permissions depending on app registration type [G2] - the SDK must document this as a configuration constraint a caller may hit when requesting many scopes, not something the SDK can work around.
11. **Nonfunctional requirements:** None beyond documentation/validation.
12. **Public API contracts or proposed contracts:** Official API: Permission-grant representations and admin-role requirements per [G2]. Proposed C# contracts:

```csharp
public sealed record GraphPermissionRequirement(string Permission, bool IsApplicationPermission, string Reason);
public sealed record GraphConnectionOptions(string TenantId, string ClientId, IReadOnlyList<GraphPermissionRequirement> RequiredPermissions) : IValidatableOptions;
```
13. **Dependencies:** MGR-001.
14. **Security considerations:** Authentication and authorization: Builds on MGR-001. Required permissions/scopes/licenses: Declared per downstream feature; this feature's job is to make that declaration explicit and checkable (MGR-024). Data privacy and security: Permission scope strings are configuration metadata, not sensitive business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable. Error handling and concurrency: A caller declaring a required permission the app registration does not hold should fail fast and clearly at configuration-validation time, not as a confusing 403 deep inside a downstream call.
16. **Unit-test requirements:** `GraphConnectionOptions` validation rejects a configuration declaring zero required permissions when a feature mandates at least one; delegated-vs-application permission distinction is preserved through the options model.
17. **Integration-test requirements:** Contract-test scenarios: None beyond MGR-024's diagnostic probe. Integration-test requirements: Authorized Entra tenant to confirm the per-audience permission-request-count limits empirically; not available this pass.
18. **Acceptance criteria:** Every downstream MGR feature's required permissions are declared through this one typed mechanism, not scattered ad hoc strings.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact per-audience permission-request-count limits were documented with some uncertainty in the source research; treat as approximate until independently reconfirmed.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Exact per-audience permission-request-count limits were documented with some uncertainty in the source research; treat as approximate until independently reconfirmed. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

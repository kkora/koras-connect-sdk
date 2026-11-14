---
id: "SFD-001"
title: "OAuth authentication"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-001 - OAuth authentication

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-001 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-001.
2. **Feature name:** OAuth authentication.
3. **Description:** Authenticate against a Salesforce org as either an unattended service (Client Credentials) or on behalf of a signed-in user, without the SDK generating or persisting long-lived secrets.
4. **Business value:** Authenticate against a Salesforce org as either an unattended service (Client Credentials) or on behalf of a signed-in user, without the SDK generating or persisting long-lived secrets.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`RedactedCredential<T>` unchanged where the grant type matches; the JWT Bearer flow (certificate-based, no interactive step, closest to Salesforce's recommended unattended pattern) is new connector-owned code, mirroring DVT-001's certificate-assertion decision.
10. **Functional requirements:** Token endpoint: `POST https://{host}/services/oauth2/token`, where `{host}` is `login.salesforce.com`, `test.salesforce.com` (sandbox), or the org's My Domain host `https://{MyDomainName}.my.salesforce.com` [PDF p.6, snippet for host variants]. Support, in priority order: (1) Client Credentials flow for server-to-server; (2) JWT Bearer flow (certificate-signed assertion) for unattended access without a stored secret; (3) a caller-supplied delegated token source (authorization-code flow is explicitly out of scope for the SDK to implement itself - it requires an interactive browser redirect). **[PDF, directly confirmed, p.6]**: "Creating connected apps is restricted as of Spring '26... we recommend using **External Client Apps (ECA)** instead" - the connector's documentation must default callers toward ECA, with legacy Connected App support retained since many existing orgs still use it. The token response includes `access_token` and `instance_url` together (SFD-002) - **[snippet]**, consistent with long-standing, uncontroversial Salesforce behavior but not independently re-read this pass.
11. **Nonfunctional requirements:** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware; secret/certificate never persisted by the SDK.
12. **Public API contracts or proposed contracts:** Official API: `/services/oauth2/token` [PDF p.6]; ECA guidance directly confirmed [PDF p.6]; specific flow parameter names (`grant_type=client_credentials` etc.) are standard OAuth2 and not independently re-verified against the Salesforce-specific page this pass. Proposed C# contracts:

```csharp
public sealed record SalesforceClientCredentialsOptions(Uri TokenEndpoint, string ClientId, string ClientSecret) : IValidatableOptions;
public sealed record SalesforceJwtBearerOptions(Uri TokenEndpoint, string ClientId, string Subject,
    Func<CancellationToken, ValueTask<X509Certificate2>> CertificateSource) : IValidatableOptions;
```
13. **Dependencies:** None (first Salesforce feature).
14. **Security considerations:** Authentication and authorization: Client Credentials / JWT Bearer are both app-only; no interactive browser step. Required permissions/scopes/licenses: An External Client App (or legacy Connected App) with OAuth scopes including `api` and, for JWT Bearer, a pre-registered certificate and a pre-authorized running-user; "Username-Password" flow is explicitly being retired per vendor guidance and is not implemented. Data privacy and security: Client secret/certificate never logged; `RedactedCredential<T>` reused.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to token acquisition; reuses `ClientCredentialsTokenSource`'s cached-with-expiry-margin behavior for the Client Credentials case. Error handling and concurrency: Non-2xx token response classifies as `AuthenticationException`; concurrent callers share one in-flight acquisition.
16. **Unit-test requirements:** Expired cache triggers reacquisition; JWT assertion correctly signed and time-bounded; invalid certificate rejected before any network call; non-2xx response classifies as `AuthenticationException`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic token-endpoint fixture for both Client Credentials and JWT Bearer grant shapes. Integration-test requirements: Requires a real Salesforce org with an authorized External Client App/Connected App; not available this pass.
18. **Acceptance criteria:** A caller obtains a bearer token via Client Credentials or JWT Bearer with no interactive prompt.
19. **Definition of Done:** Standard template; live sandbox gate explicitly deferred (Section 28).
20. **External API entitlement or documentation blockers:** Authorization-code/device flows are deliberately out of scope for SDK-managed acquisition (require a browser); exact JWT claim set was snippet-only, not independently re-verified - a release blocker for calling this `implemented`/`verified` rather than `implemented`/`partial`.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Authorization-code/device flows are deliberately out of scope for SDK-managed acquisition (require a browser); exact JWT claim set was snippet-only, not independently re-verified - a release blocker for calling this `implemented`/`verified` rather than `implemented`/`partial`. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

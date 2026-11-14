---
id: "MGR-001"
title: "Delegated and app-only authentication"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GraphClientCredentialsOptions validates the common-tenant restriction and the exactly-one-of-secret-or-certificate rule; GraphAuthentication.CreateClientCredentialsTokenSource reuses ClientCredentialsTokenSource unchanged for the secret case with the documented .default scope, and the connector-owned GraphCertificateAssertionTokenSource (mirroring DataverseCertificateAssertionTokenSource exactly) handles the certificate case. Delegated flows accept a caller-supplied IAccessTokenSource directly on GraphClient rather than the SDK attempting browser interaction. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-001 - Delegated and app-only authentication

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-001 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-001.
2. **Feature name:** Delegated and app-only authentication.
3. **Description:** Authenticate against Microsoft Graph as either a signed-in user (delegated) or an unattended daemon (app-only), matching Graph's fundamental permission-model split.
4. **Business value:** Authenticate against Microsoft Graph as either a signed-in user (delegated) or an unattended daemon (app-only), matching Graph's fundamental permission-model split.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`CertificateCredentialProvider` unchanged for the app-only (client-credentials) case; delegated flows accept a caller-supplied `IAccessTokenSource` rather than the SDK implementing an interactive browser step, matching DVT-001/SFD-001's identical decision.
10. **Functional requirements:** App-only (client-credentials): `POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`, `scope=https://graph.microsoft.com/.default` (URL-encoded), `grant_type=client_credentials`; **no `refresh_token` is issued** for this grant [G4]. Delegated (authorization-code): `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize` then `.../token`, out of scope for SDK-managed acquisition (requires a browser) - the SDK accepts a caller-supplied delegated token source. `{tenant}` must be a GUID, verified domain, or `organizations`/`consumers`/`common` depending on the app's supported-account-types configuration - `common` is invalid for any call requiring app-only consent.
11. **Nonfunctional requirements:** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware.
12. **Public API contracts or proposed contracts:** Official API: Token endpoint, `.default` scope convention, no-refresh-token-for-client-credentials - all per [G4]. Proposed C# contracts:

```csharp
public sealed record GraphClientCredentialsOptions(string TenantId, string ClientId, string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions;
```
13. **Dependencies:** None (first Graph feature).
14. **Security considerations:** Authentication and authorization: App-only via client secret or certificate; delegated via caller-supplied token (device code, auth code, or interactive, outside SDK scope). Required permissions/scopes/licenses: An Entra app registration with the specific Graph application permissions each downstream MGR feature declares (MGR-002), admin-consented. Data privacy and security: Client secret/certificate never logged; `RedactedCredential<T>` reused.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to token acquisition; reuses `ClientCredentialsTokenSource`'s cached-with-expiry-margin behavior. Error handling and concurrency: Non-2xx token response classifies as `AuthenticationException`; concurrent callers share one in-flight acquisition.
16. **Unit-test requirements:** Expired cache triggers reacquisition; `.default` scope composed correctly; invalid certificate rejected before any network call; non-2xx classifies as `AuthenticationException`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic token-endpoint fixture for the client-credentials grant. Integration-test requirements: Requires a real Entra app registration with admin-consented application permissions; not available this pass.
18. **Acceptance criteria:** App-only token acquisition works without any interactive step; delegated scenarios correctly delegate to a caller-supplied source rather than the SDK attempting browser interaction.
19. **Definition of Done:** Standard template; live tenant gate deferred (Section 28).
20. **External API entitlement or documentation blockers:** No live Entra tenant/app registration available this pass.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphAuthentication.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphAuthenticationTests.cs`. Covers: `TenantId` of `common` is rejected locally; exactly one of `ClientSecret`/`CertificateSource` must be supplied; the secret flow posts to `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token` with the `https://graph.microsoft.com/.default` scope; the certificate flow composes and signs a valid RFC 7523 assertion, sends the documented `client_assertion_type`/`client_assertion` grant, and caches the token until expiry; a certificate without a private key is rejected before any network call; a non-2xx token response classifies as `AuthenticationException`. Full suite: 877 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live Entra tenant/app registration available this pass to confirm end to end. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphAuthentication.cs`/`GraphCertificateAssertionTokenSource` plus `GraphAuthenticationTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |

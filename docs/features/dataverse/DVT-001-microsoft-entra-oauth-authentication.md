---
id: "DVT-001"
title: "Microsoft Entra OAuth authentication"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: client-secret path delegates to Koras.Connect.Authentication.ClientCredentialsTokenSource; certificate path adds DataverseCertificateAssertionTokenSource (RFC 7523 JWT bearer). Unit/contract-tested; no live Entra/Dataverse tenant available this pass."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-001 - Microsoft Entra OAuth authentication

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-001 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-001.
2. **Feature name:** Microsoft Entra OAuth authentication.
3. **Description:** Authenticate the SDK against a Dataverse environment as either an unattended application ("connect as an app") or a signed-in user, without the SDK generating, storing, or caching long-lived secrets itself.
4. **Business value:** Authenticate the SDK against a Dataverse environment as either an unattended application ("connect as an app") or a signed-in user, without the SDK generating, storing, or caching long-lived secrets itself.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`CertificateCredentialProvider`/`RedactedCredential<T>` unchanged (Section 8.1); no new Core/Abstractions type required.
10. **Functional requirements:** Support the application (client-credentials) flow against Microsoft Entra ID with either a client secret or an X.509 certificate; require the caller to supply a pre-provisioned Entra app registration plus a Dataverse **Application User** bound to that registration with an assigned security role (app-only access consumes no license seat - confirmed [D4]); support an optional delegated flow for interactive/testing scenarios via a caller-supplied token source, matching the existing `IAccessTokenSource` seam rather than embedding MSAL's interactive UI. Token scope for client-credentials: `{environment-url}/.default`; for delegated/public-client: `{environment-url}/user_impersonation` [D4]. Never assume `common`/`organizations`/`consumers` Entra tenant aliases are valid for a Dataverse app-only scenario - require an explicit tenant GUID or verified domain.
11. **Nonfunctional requirements:** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware; no secret persisted to disk by the SDK itself.
12. **Public API contracts or proposed contracts:** Official API: Token endpoint is the standard Microsoft Entra v2.0 endpoint `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token` (generic Entra behavior, confirmed via the Entra identity-platform docs the Graph research also cites; **not restated verbatim on the Dataverse-specific `authenticate-oauth` page itself** - flagged as a documentation gap in the vendor research, not fabricated here). Scope formats `{environment-url}/.default` and `{environment-url}/user_impersonation` are confirmed directly on [D4]. Proposed C# contracts:

```csharp
public sealed record DataverseClientCredentialsOptions(Uri TenantTokenEndpoint, string ClientId, string EnvironmentUrl,
    string? ClientSecret = null, Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions;
// Reuses Koras.Connect.Authentication.ClientCredentialsTokenSource for the secret case;
// a certificate-based client-assertion token source is new connector-owned code (JWT client assertion is an
// Entra-wide mechanism, not Dataverse-specific, so it is implemented once in Koras.Connect.Dataverse and not
// proposed as a shared Core type until a second Phase 2 connector needs the identical client-assertion shape).
```
13. **Dependencies:** None (first Dataverse feature).
14. **Security considerations:** Authentication and authorization: Client-credentials (app-only) or authorization-code/device-code (delegated, via caller-supplied token). No Basic/API-key mode exists for Dataverse. Required permissions/scopes/licenses: An Entra app registration; a Dataverse **Application User** record with an assigned Dataverse security role covering every table/operation the connector will call (DVT-001 itself needs none beyond "can obtain a token" - operation-level security roles are DVT-004+'s concern). No Power Apps/Dynamics 365 user license is consumed by app-only access [D4]; delegated access requires a licensed interactive user. Data privacy and security: Client secret/certificate never logged; `RedactedCredential<T>.ToString()` override reused.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable to token acquisition itself; reuses `ClientCredentialsTokenSource`'s existing cached-with-expiry-margin behavior. Error handling and concurrency: 401 from the token endpoint → `AuthenticationException` (existing classifier, unchanged). Concurrent `GetTokenAsync` callers share one in-flight acquisition (existing `ClientCredentialsTokenSource` semaphore behavior).
16. **Unit-test requirements:** Expired/skewed cache triggers reacquisition; concurrent callers share one token request; invalid certificate (no private key) rejected before any network call; non-2xx token response classifies as `AuthenticationException`; canceled acquisition propagates `OperationCanceledException`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic token-endpoint fixture matching the documented `client_credentials` grant request/response shape. Integration-test requirements: Requires a real Entra app registration + Dataverse Application User in an authorized sandbox environment; not available in this session.
18. **Acceptance criteria:** A caller can obtain a bearer token for a configured environment using only a client ID + secret/certificate, with no interactive prompt, and the token is reused until near expiry.
19. **Definition of Done:** Matches Phase 1's Section 26 Definition of Done template; this feature's live sandbox gate is explicitly deferred (Section 28 open question).
20. **External API entitlement or documentation blockers:** The exact Dataverse-specific token-endpoint restatement (vs. generic Entra docs) is a documented gap in the official docs, not an SDK limitation (Section 3 vendor research). No live Dataverse tenant/Entra app registration is available in this documentation pass.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseAuthentication.cs` (`DataverseClientCredentialsOptions`, `DataverseAuthentication.CreateTokenSource`, `DataverseCertificateAssertionTokenSource`) and `tests/Koras.Connect.Tests/Dataverse/DataverseAuthenticationTests.cs`. Covers: exactly-one-of-secret-or-certificate validation, non-HTTPS token endpoint rejection, secret path returns the reused `ClientCredentialsTokenSource`, certificate path builds and RS256-signs a 3-part RFC 7523 JWT (`x5t` thumbprint, `aud`/`iss`/`sub`/`nbf` verified, signature verified against the certificate's public key), cache/expiry-margin renewal, `Invalidate()` forcing reacquisition, certificate without a private key rejected before any network call, non-success token response surfaces `AuthenticationException`, malformed token response shapes never poison the cache, canceled acquisition never reaches the handler. Full suite: 594 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live Entra tenant/Dataverse Application User available this pass to exercise the real `/oauth2/v2.0/token` grant end-to-end; the certificate-assertion flow is tested against the documented RFC 7523 shape, not a live Entra response. Consumer documentation/sample deferred to DVT-022. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `DataverseClientCredentialsOptions`/`DataverseAuthentication`/`DataverseCertificateAssertionTokenSource` plus `DataverseAuthenticationTests.cs` (18 tests). Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live Entra/Dataverse tenant available this pass. |

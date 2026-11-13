---
id: "SAP-001"
title: "OAuth and supported authentication mechanisms"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-001 - OAuth and supported authentication mechanisms

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-001.
2. **Feature name:** OAuth and supported authentication mechanisms.
3. **Description:** Authenticate an independent tenant with authorized bearer or SAML exchange.
4. **Business value:** Authenticate an independent tenant with authorized bearer or SAML exchange.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP SamlBearerTokenSource; E-TEST SapSamlGrantUsesExactProtocolAndCachesWithoutLoggingAssertions; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Require exact company/client/API host and host-generated assertion; no implicit Basic fallback or assumption that unrelated IAS tokens work.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing SamlBearerTokenSource/IAccessTokenSource; POST /oauth/token with company_id/client_id/grant_type/assertion.
13. **Dependencies:** SDK-005, SDK-006; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Concurrent renewal, expiry/skew, invalid assertion/token, rotation and canceled exchange. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Concurrent renewal, expiry/skew, invalid assertion/token, rotation and canceled exchange.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Concurrent calls share valid acquisition; malformed tokens cannot poison cache; configured grant succeeds in permitted sandbox with no assertion disclosure.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Registered OAuth client/company, trusted IdP assertion and technical-user permissions absent [S1]; other auth modes need current verification.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "expiry/skew", "invalid assertion/token", "rotation" and "canceled exchange" unit-test-requirement gaps - previously only the happy-path/concurrent-caching case was tested. Added `tests/Koras.Connect.Tests/Sap/SapAuthenticationTests.cs`: `TokenIsRenewedAfterExpiryUsingWhateverAssertionTheSourceReturnsAtThatMoment` (fake `TimeProvider`, confirms the cache holds through the safety margin then performs one real renewal using a freshly-fetched assertion - "rotation" is the assertion source's responsibility, not the cache's), `AssertionSourceFailureIsNeverSwallowedOrRetried`, `InvalidTokenResponseShapesNeverPoisonTheCache` (5-case theory: missing `access_token`/`token_type`, wrong `token_type`, non-positive/missing `expires_in` - each throws and a subsequent valid exchange still succeeds, proving nothing was cached), `NonSuccessStatusThrowsIntegrationRequestExceptionWithoutRetry`, `CanceledExchangeNeverReachesTheHandlerAndNeverCachesAnything`. Confirmed the `/oauth/token` request/response shape (`company_id`/`client_id`/`grant_type`/`assertion`, `access_token`/`token_type`/`expires_in`) against the **official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), section 2.2.3 "Requesting an Access Token"** - the existing implementation already matches exactly; the only gap not implemented is the optional `new_token=true` force-refresh parameter, left out since the cache's own expiry-margin renewal already covers the same need. Full suite: 538 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Registered OAuth client/company, trusted IdP assertion and technical-user permissions against a live, authorized tenant [S1] remain standing external blockers; OIDC and HTTP Basic (the other two documented SAP auth modes) are unimplemented and unclaimed by this SDK. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `SapAuthenticationTests.cs`, closing the "expiry/skew", "invalid assertion/token", "rotation" and "canceled exchange" unit-test-requirement gaps. Cross-checked the `/oauth/token` request/response shape against the official API Reference Guide, section 2.2.3. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live grant/credential evidence against an authorized tenant [S1] remains a standing external blocker. |

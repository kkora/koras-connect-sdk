---
id: "QLT-001"
title: "Authentication and API token configuration"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-001 - Authentication and API token configuration

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-001.
2. **Feature name:** Authentication and API token configuration.
3. **Description:** Authenticate against the correct data center without leaking tokens.
4. **Business value:** Authenticate against the correct data center without leaking tokens.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-QLT Create; E-TRANSPORT HeaderTokenAuthenticator; E-TEST QualtricsUsesOnlyApiTokenAndPagesSurveys; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Use rotating host X-API-TOKEN source, reject blank/newline tokens and disable redirects; OAuth remains a separately verified extension, not interchangeable silently.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing QualtricsClient.Create/IAccessTokenSource; proposed DI token registration.
13. **Dependencies:** SDK-005, SDK-006, SDK-004; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Header injection, empty source, rotation, off-origin links, redirects, cross-account isolation and redaction. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Header injection, empty source, rotation, off-origin links, redirects, cross-account isolation and redaction.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Create adds X-API-TOKEN without bearer; separate accounts cannot exchange credentials; Access API requirements are documented.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** API feature/Access API permission and authorized token/data center absent [Q1].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the two remaining unit-test-requirement gaps. (1) "redirects": confirmed already covered by `HttpClientHardeningTests.QualtricsNamedClientDisablesRedirectsAndCookies` (asserts the DI-registered `Koras.Qualtrics.Api` named client's primary handler has `AllowAutoRedirect = false` and `UseCookies = false`) - no new code needed. (2) "cross-account isolation": added `TwoClientInstancesWithDifferentTokenSourcesNeverLeakEachOthersToken` to `tests/Koras.Connect.Tests/Qualtrics/QualtricsClientExpansionTests.cs`, constructing two independent `QualtricsClient` instances over two independent `HttpClient`/token-source pairs and asserting every request each client sends carries only its own account's `X-API-TOKEN` value, confirming `HeaderTokenAuthenticator` holds no static/shared state that could leak a token across accounts in the same process. Full suite: 475 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. The API feature/Access API permission and an authorized token/data center for live verification remain an external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `AddKorasQualtrics` DI registration (`src/Koras.Connect.Qualtrics/QualtricsServiceCollectionExtensions.cs`) mirroring `AdpServiceCollectionExtensions.AddKorasAdp`: named `HttpClient` via `IHttpClientFactory` with `RemoveAllLoggers()`, duplicate-registration rejection, and `QualtricsOptions.Validate()` (new `IValidatableOptions` implementation in `QualtricsClient.cs`) run before any HTTP client is built. Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`AddKorasQualtricsRejectsDuplicateRegistration`, `AddKorasQualtricsRejectsUnsafeOrWrongRootBeforeAnyHttpAttempt`, `AddKorasQualtricsRejectsBadMaxPages`, `AddKorasQualtricsRegistersAResolvableClientWithoutContactingNetworkDuringRegistration`). Authentication itself is unchanged (still `HeaderTokenAuthenticator`/X-API-TOKEN); OAuth and live data-center/token verification remain unaddressed, so status stays `partial`. `dotnet build Koras.Connect.slnx` and `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj` passing as of this revision. |
| 2026-11-13 | `implemented` | Closed the "redirects" (already covered by an existing test, newly cited as evidence) and "cross-account isolation" (new `TwoClientInstancesWithDifferentTokenSourcesNeverLeakEachOthersToken` test) unit-test-requirement gaps. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: the Access API permission and an authorized token/data center for live verification remain a standing external blocker. |

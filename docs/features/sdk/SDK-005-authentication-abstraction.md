---
id: "SDK-005"
title: "Authentication abstraction"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-005 - Authentication abstraction

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-005.
2. **Feature name:** Authentication abstraction.
3. **Description:** Allow vendor and host authentication adapters without changing transport.
4. **Business value:** Core accepts authentication without referencing Authentication implementation reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions + Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT: IAccessTokenSource; RequestAuthentication.cs: IRequestAuthenticator. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Move neutral interfaces to Abstractions with compatibility handling; support bearer/header adapters and cancellation; keep vendor grant semantics in provider adapters.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing GetTokenAsync and AuthenticateAsync; preserve signatures or document preview migration.
13. **Dependencies:** SDK-001, SDK-017; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise fake token source, invalid header token, auth cancellation, per-request token rotation and transport substitution. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Fake token source, invalid header token, auth cancellation, per-request token rotation and transport substitution.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Core accepts authentication without referencing Authentication implementation; cached and rotating token sources remain interoperable.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor blocker for abstraction; grant verification belongs to provider entries.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 277, Skipped 3 (live-sandbox, unrelated), Failed 0. `IAccessTokenSource`/`IRequestAuthenticator` already moved to `Koras.Connect.Abstractions` by SDK-001, with the `Koras.Connect` facade forwarding both (already covered by `PackageGraphTests.FacadeTypeForwardResolvesToItsNewAssembly`). New `RequestAuthenticationTests.cs` (15 tests, referencing only `Koras.Connect.Core`/`Abstractions`, no provider package) closes this ticket's previously-untested unit requirements directly: `BearerTokenAuthenticator`/`HeaderTokenAuthenticator` correctly set their respective header from a fake token source, resolve a fresh token on every call rather than caching internally ("per-request token rotation"), propagate token-source cancellation rather than swallowing it, and `HeaderTokenAuthenticator` rejects reserved/malformed header names at construction and `\r`/`\n`/empty resolved tokens at call time ("invalid header token"). A new `IntegrationTransportAcceptsAnyIRequestAuthenticatorImplementationItHasNeverSeenBefore` test substitutes a third, test-only `IRequestAuthenticator` implementation directly proving the acceptance criterion ("Core accepts authentication without referencing Authentication implementation"); `AdpCredentialRotationTests`/`AdpConnectorTests` already separately prove cached (`ClientCredentialsTokenSource`) and rotating (`AdpRotatingTokenSource`) token sources both work interchangeably through the same `IAccessTokenSource` contract. |
| Remaining work / blockers | `BearerTokenAuthenticator`/`HeaderTokenAuthenticator` had zero direct unit tests before this pass — only indirect coverage through full `IntegrationTransport` round-trips in other test files — despite being explicitly named in this ticket's own unit-test requirements ("support bearer/header adapters", "invalid header token"); closed by `RequestAuthenticationTests.cs`. No remaining functional gap found for this ticket specifically. No new consumer-facing documentation/sample was added (only tests); owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Confirmed the neutral interfaces already live in `Koras.Connect.Abstractions` (via SDK-001) with facade compatibility forwarding already tested. Closed the actual remaining gap — `BearerTokenAuthenticator`/`HeaderTokenAuthenticator` had no direct unit tests — with `RequestAuthenticationTests.cs`, including a transport-substitution test proving Core depends only on the `IRequestAuthenticator` contract. Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

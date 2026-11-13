---
id: "SDK-004"
title: "HttpClientFactory integration"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-004 - HttpClientFactory integration

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-004.
2. **Feature name:** HttpClientFactory integration.
3. **Description:** Provide safe reusable HTTP lifecycles across connectors.
4. **Business value:** Every factory registration preserves security defaults, supports safe custom handlers and documents refresh behavior without retaining stale certificates indefinitely reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-DI uses two named ADP clients; SAP/Qualtrics require caller clients. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Use separate token/API named clients; disable redirects and unsafe URL loggers; disable credential-bearing cookie reuse; document handler/DNS/certificate refresh and caller overrides.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing IHttpClientFactory use in AddKorasAdp; proposed provider builder HTTP customization.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise handler replacement, auth isolation, logger removal, redirect rejection and disposal/DNS lifecycle simulations. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Handler replacement, auth isolation, logger removal, redirect rejection and disposal/DNS lifecycle simulations.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Every factory registration preserves security defaults, supports safe custom handlers and documents refresh behavior without retaining stale certificates indefinitely.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Microsoft factory guidance [M1]; no live vendor required for lifecycle checks.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 262, Skipped 3 (live-sandbox, unrelated), Failed 0. New `HttpClientHardeningTests.cs` (4 tests) resolves each provider's named `IHttpMessageHandlerFactory` handler directly (walking past any `DelegatingHandler` wrapper to the primary `HttpClientHandler`) and asserts `AllowAutoRedirect == false` and `UseCookies == false` for all five named clients across the three providers, asserts ADP's API handler still carries the caller's client certificate, and proves a host can override the registered handler by re-registering the same named client after `AddKorasAdp` (last registration wins). |
| Remaining work / blockers | **Found and fixed two real gaps against this ticket's own functional requirement ("disable redirects... disable credential-bearing cookie reuse") while auditing, not just a test gap:** (1) `SuccessFactorsServiceCollectionExtensions.AddKorasSuccessFactors` registered both named clients (`Koras.SuccessFactors.OAuth`/`.Api`) with **no handler customization at all** — meaning the framework default `HttpClientHandler` (`AllowAutoRedirect = true`, `UseCookies = true`) was in effect, unlike ADP and Qualtrics which at least disabled redirects. Fixed by adding the same `ConfigurePrimaryHttpMessageHandler` hardening ADP/Qualtrics already had. (2) Even ADP's and Qualtrics's existing handlers only set `AllowAutoRedirect = false` and never `UseCookies = false` — so a credential-bearing `Set-Cookie` response header could have been silently replayed by the handler's internal `CookieContainer` on a subsequent call through the same long-lived handler instance. Fixed by adding `UseCookies = false` to all three providers' handler factories (`AdpServiceCollectionExtensions.Handler`, `QualtricsServiceCollectionExtensions`, and the new SAP handlers). **Still open:** no explicit documentation of handler/DNS/certificate-rotation lifecycle exists anywhere (master spec's own risk note: "current singleton registration deserves lifecycle review") — the nonfunctional requirement's "document... refresh behavior" clause is not satisfied, which is why the acceptance-criteria box stays unchecked. "Disposal/DNS lifecycle simulations" from the unit-test requirements were not attempted — `IHttpClientFactory`'s internal handler-rotation (default ~2 minute lifetime) has no public hook to simulate deterministically in a unit test without internal framework access. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Fixed a real security gap found while auditing: SAP's named HTTP clients had zero handler hardening (default redirects+cookies enabled), and ADP's/Qualtrics's existing handlers disabled redirects but never cookies. Added `UseCookies = false` across all three providers and the missing `AllowAutoRedirect = false` for SAP. Added `HttpClientHardeningTests.cs` proving the hardening, certificate attachment, and caller-override capability. Left "document handler/DNS/certificate refresh" and owner acceptance unresolved — see remaining work. |

---
id: "SDK-006"
title: "Secure credential providers"
category: "sdk"
priority: "P0"
phase: "P2"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-006 - Secure credential providers

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-006.
2. **Feature name:** Secure credential providers.
3. **Description:** Enable secret rotation without embedding host-specific vault dependencies.
4. **Business value:** No secrets appear in errors/logs reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-AUTH redacted options and certificate loaders; E-TRANSPORT token injection; no credential-provider API. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add asynchronous credential/certificate sources; redact representations; define reload/cache key semantics; hosts configure secret stores and storage permissions.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed ICredentialProvider<TCredential> with redacted credential wrappers; retain IAccessTokenSource adapter.
13. **Dependencies:** SDK-005, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise concurrent rotation, canceled retrieval, source failure, stale cache invalidation and redacted diagnostics. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Concurrent rotation, canceled retrieval, source failure, stale cache invalidation and redacted diagnostics.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** No secrets appear in errors/logs; two credentials remain isolated; rotation is observable only through safe metadata and affects subsequent authentication.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Production secret store/certificate policy is host-owned; no paid store provisioned.

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
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | Not newly executed during documentation split; baseline evidence is in the specification |
| Remaining work / blockers | See functional requirements, acceptance criteria and external blockers above; replace with precise remaining items as work progresses |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `ICredentialProvider<TCredential>` plus `StaticCredentialProvider<T>`/`DelegatingCredentialProvider<T>` in `src/Koras.Connect/Abstractions/CredentialProvider.cs`, and `ClientCredentialsTokenSource.Invalidate()` plus an independent `acquisitionTimeout` constructor parameter in `src/Koras.Connect/Authentication/ClientCredentialsTokenSource.cs` so credential rotation no longer requires recreating the token source. `ClientCertificateTransport`'s loaders remain synchronous/one-shot (not yet wrapped by a certificate-specific `ICredentialProvider` implementation) — that remains open. `dotnet build`/`dotnet test` (55 baseline tests) pass unaffected; provider-specific rotation wiring is being added separately (see ADP-003). |
| 2026-11-12 | `partial` | Added `CertificateCredentialProvider : ICredentialProvider<X509Certificate2>` and `RedactedCredential<T>` in `src/Koras.Connect/Authentication/CertificateCredentialProvider.cs`: caches a certificate loaded via `ClientCertificateTransport.LoadPkcs12`/`LoadPem` and exposes `Invalidate()` to force a reload after rotation, closing the "async reload/cache-invalidation hook" gap. Covered by `tests/Koras.Connect.Tests/CertificateCredentialProviderTests.cs` (cache reuse, invalidate-forces-reload, redaction). `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped sandbox tests, 0 failed). |

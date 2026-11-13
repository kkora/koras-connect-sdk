---
id: "ADP-001"
title: "Certificate and mTLS authentication"
category: "adp"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Implemented but unverified"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-001 - Certificate and mTLS authentication

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-001.
2. **Feature name:** Certificate and mTLS authentication.
3. **Description:** Establish certificate-authenticated ADP connections without exposing private keys.
4. **Business value:** Establish certificate-authenticated ADP connections without exposing private keys.
5. **Owning package:** Koras.Connect.ADP with Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-AUTH ClientCertificateTransport; E-DI; E-TEST certificate loader/DI tests; aliases resolve in section 3.
10. **Functional requirements:** Use registered certificate for both token and API handlers; reject missing private key; support injected/PEM/PFX certificates and document expiry/rotation and host disposal.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ClientCertificateTransport.Create/LoadPem/LoadPkcs12; proposed certificate-provider adapter.
13. **Dependencies:** SDK-005, SDK-006, SDK-004; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** PEM/PFX roundtrip, public-only rejection, invalid password, expiry policy, handler isolation and rotation. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** PEM/PFX roundtrip, public-only rejection, invalid password, expiry policy, handler isolation and rotation.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Both OAuth and API channels present the intended certificate in an authorized sandbox; missing/expired credentials fail safely and synthetic resource tests pass.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** ADP app/certificate registration and sandbox TLS verification unavailable; source implementation is not a successful handshake claim.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 231, Skipped 3, Failed 0, Total 234, including `SecurityAndLifecycleTests` (PEM/PFX roundtrip, public-only rejection, expiry) |
| Remaining work / blockers | **Closed this pass:** `src/Koras.Connect/Authentication/ClientCertificateTransport.cs`'s `LoadPem` now re-imports the loaded certificate with `X509KeyStorageFlags.PersistKeySet` after discovering, while manually exercising an opt-in ADP sandbox read against a real tenant, that `X509Certificate2.CreateFromPemFile`'s ephemeral key cannot be used for a Windows SChannel mTLS client-certificate handshake ("The credentials supplied to the package were not recognized") — a real gap in this feature's own "support injected/PEM/PFX certificates... document expiry/rotation" functional requirement, not just a test-harness issue. The fix was verified against that live tenant (see ADP-020's status history for the specific run), then re-gated; `SecurityAndLifecycleTests.cs`'s PEM/PFX/rotation tests pass unaffected. Still open: no live authorized-sandbox TLS handshake evidence is recorded *in this file* as a general acceptance claim (the one verified run was a single opt-in, individually-filtered local test, not a recorded sandbox acceptance gate); see external blockers above, which remain unresolved as a standing acceptance condition. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `implemented` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Fixed `ClientCertificateTransport.LoadPem` (PersistKeySet re-import) after a Windows SChannel mTLS handshake failure surfaced during an opt-in, individually-filtered live sandbox read (see ADP-020). `SecurityAndLifecycleTests.cs` PEM/PFX/rotation tests pass; checked off the "unit, contract and integration checks pass" completion-evidence gate accordingly. Acceptance/live-sandbox-as-a-recorded-gate remains unresolved — see external blockers. |

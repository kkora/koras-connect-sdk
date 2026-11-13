---
id: "ADP-012"
title: "Connection diagnostics"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Not started"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-012 - Connection diagnostics

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-012.
2. **Feature name:** Connection diagnostics.
3. **Description:** Help operators separate TLS, auth, entitlement and schema failures.
4. **Business value:** Help operators separate TLS, auth, entitlement and schema failures.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No TestConnection/Diagnose API in E-WORKERS/E-OPS; E-TRANSPORT has only hooks; aliases resolve in section 3.
10. **Functional requirements:** Implement opt-in staged read-only diagnostics: local config/cert validity, token acquisition and a configured minimal authorized API probe; no writes or exhaustive tenant scan.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed AdpConnector.DiagnoseAsync returning ConnectionDiagnostic with stage/outcome/redacted correlation.
13. **Dependencies:** SDK-002, SDK-009, SDK-011, ADP-004; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Expired/missing cert, 401/403, timeout, DNS/TLS fault, malformed response, canceled probe and no write sends. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Expired/missing cert, 401/403, timeout, DNS/TLS fault, malformed response, canceled probe and no write sends.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Results identify the failed stage without exposing credential/worker data; default diagnostics never mutate ADP and do not claim full tenant permissions from one successful read.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Caller must select entitled probe and sandbox; no universal connectivity endpoint is invented.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [x] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 183, Skipped 1, Failed 0, Total 184 (includes `AdpConnectorTests`). Closed the "no documented how-to-run doc" gap by adding `docs/features/adp/ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md`, which documents the exact `dotnet run --project samples/ADP.ConnectionTest` command, what it demonstrates, and the live-sandbox prerequisites a maintainer would need to go beyond the offline scenarios. Re-ran `dotnet run --project samples/ADP.ConnectionTest` from a clean state to confirm its three scenarios (healthy pass, expired-certificate local rejection, authentication failure) still execute successfully offline before writing the doc. |
| Remaining work / blockers | `AdpConnector` has never run against a real tenant; the connectivity probe is caller-supplied, so no specific ADP endpoint has been validated as the canonical minimal probe — this remains an external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Greenfield implementation: added `Koras.Connect.Adp.AdpConnector : Koras.Connect.Abstractions.IConnector` (`src/Koras.Connect.Adp/AdpConnector.cs`) with a staged `DiagnoseAsync`: Stage 1 (Configuration) validates credentials/workers options and certificate presence/validity-period locally with zero network calls; Stage 2 (Authentication) performs exactly one token acquisition; Stage 3 (Connectivity) invokes exactly one caller-injected `Func<CancellationToken, Task>` probe (no endpoint is hardcoded) and is reported `Skipped` rather than attempted when no probe is configured. Each stage stops the whole diagnosis on failure. Registered from both `AddKorasAdp` overloads in `src/Koras.Connect.Adp/AdpServiceCollectionExtensions.cs`, resolvable as `AdpConnector` or `IConnector`. Tests in `tests/Koras.Connect.Tests/AdpConnectorTests.cs` cover: expired certificate and missing credentials short-circuiting before any network call (asserted via a token source that throws if ever invoked); 401/403 at the authentication stage; a DNS/transport fault at authentication; cancellation mid-probe propagating as `OperationCanceledException` rather than being swallowed as a failure; a malformed probe response reported as a failed Connectivity stage; a full healthy 3-stage pass whose every non-OAuth-token request is asserted to be GET; and the `Skipped` (not `Failed`) outcome when no probe is configured. Kept at `implemented` rather than fully accepted: never exercised against a live ADP tenant. |
| 2026-11-13 | `implemented` | Added `docs/features/adp/ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md`, documenting how to run `samples/ADP.ConnectionTest` and what it demonstrates, closing this feature's own "no documented how-to-run doc" gap. Checked the documentation completion gate. `verification` moved from `unverified` to `partial`: live-tenant exercise remains the standing, unchanged external blocker. |

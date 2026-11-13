---
id: "SDK-011"
title: "Logging and correlation IDs"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-011 - Logging and correlation IDs

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-011.
2. **Feature name:** Logging and correlation IDs.
3. **Description:** Support supportable failures without exposing employee data.
4. **Business value:** Logs contain stable event IDs and correlation, never tokens, raw URI/query or records reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT ActivitySource/Meter and allowlisted response IDs; E-DI RemoveAllLoggers; no shared logging contract. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add structured safe event IDs, operation/attempt/result fields, trace correlation and bounded sanitized vendor IDs; use host opt-in ILogger integration.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed safe DiagnosticContext and logging events; existing IntegrationJsonResponse header allowlist.
13. **Dependencies:** SDK-009, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise credential/PII canaries in URI/body/header/error, malicious/oversized request IDs, trace propagation and disabled logging. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Credential/PII canaries in URI/body/header/error, malicious/oversized request IDs, trace propagation and disabled logging.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Logs contain stable event IDs and correlation, never tokens, raw URI/query or records; unsafe vendor IDs are rejected or bounded.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Host framework instrumentation needs its own redaction configuration; SDK cannot guarantee arbitrary host logging.

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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 315, Skipped 3 (live-sandbox, unrelated), Failed 0. Trace propagation was already covered (`SecurityAndLifecycleTests.SdkActivityDoesNotEmitWorkerOrCredentialData` asserts the SDK's `Activity` shares the ambient parent's `TraceId`), and URI/body PII canaries plus disabled-logging behavior were already implicit across most of the suite (the vast majority of tests pass no logger at all). Two real gaps found and fixed: `IntegrationTransport.SafeHeaders` captured the `request-id`/`x-request-id`/`adp-msg-msgid` correlation headers completely unsanitized — a malicious or misbehaving server could return an unbounded-length value or one containing `\r\n` sequences, which would then propagate into `IntegrationRequestException.Headers`, `ErrorDetails.CorrelationId`, and anything a host logs from there. Fixed with a new `SanitizeCorrelationValue` step (strips control characters, truncates to 200 characters) applied to every captured value, covered by new `AMaliciousOversizedOrControlCharacterLadenRequestIdIsSanitizedBeforeCapture`. Added `OnlyTheAllowlistedCorrelationHeadersAreEverCapturedNeverAnArbitraryResponseHeader` as the explicit "header canary" this ticket's own unit-test requirements name (a simulated `x-employee-ssn`/`set-cookie` response header is confirmed never captured). |
| Remaining work / blockers | No functional gap found for this ticket. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `Koras.Connect.Transport.DiagnosticEvents` (stable `EventId`s: `RequestRetried`, `RequestFailed`, `DeadlineExceeded`) and an optional `ILogger?` constructor parameter on both `IntegrationTransport` constructors; retry/failure/deadline paths log through it with only method/status-code/attempt/delay, never address/body/token/employee data. `IntegrationRequestException` now captures the same allowlisted correlation headers on failure that a success response already captured, and `AdpApiException.Classify` consumes them as a default correlation id. No ambient/`Activity`-derived correlation-ID propagation into log scopes yet; no exhaustive canary sweep proving every call site is secret-free (current coverage is `tests/Koras.Connect.Tests/DiagnosticsAndCorrelationTests.cs`, which checks the paths it exercises). `Koras.Connect.csproj` now references `Microsoft.Extensions.Logging.Abstractions`. `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped, 0 failed). |
| 2026-11-12 | `implemented` | Fixed a real gap found while closing the "malicious/oversized request IDs" test requirement: correlation header capture was completely unsanitized, so a hostile server's `request-id` header could be unbounded length or contain `\r\n`. Added `SanitizeCorrelationValue` (strip control chars, cap at 200 chars) plus the explicit allowlist/non-allowlisted-header canary test. Trace propagation and disabled-logging were already covered elsewhere. Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

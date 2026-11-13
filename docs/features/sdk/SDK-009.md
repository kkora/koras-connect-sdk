---
id: "SDK-009"
title: "Structured exception hierarchy"
category: "sdk"
priority: "P0"
phase: "P2"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-009 - Structured exception hierarchy

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-009.
2. **Feature name:** Structured exception hierarchy.
3. **Description:** Let hosts classify failures safely and consistently.
4. **Business value:** Callers can distinguish auth, throttle and schema failures without parsing messages reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT IntegrationRequestException with status/attempts; auth also throws HttpRequestException. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Define configuration/authentication/authorization/throttle/transport/contract exceptions, safe provider codes and correlation; preserve inner exceptions only when sanitized.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed IntegrationException family and safe ErrorDetails; retain HTTP status/attempts compatibility.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise 401/403/404/429/5xx, malformed successful payload, unknown provider error, secret-bearing body and cancellation passthrough. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** 401/403/404/429/5xx, malformed successful payload, unknown provider error, secret-bearing body and cancellation passthrough.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Callers can distinguish auth, throttle and schema failures without parsing messages; error text and serialized fields omit payloads/secrets.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Vendor-specific error schemas require authoritative evidence; unknown responses use neutral classification.

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
| 2026-11-12 | `partial` | Added the proposed `IntegrationException` family in `src/Koras.Connect/Exceptions/IntegrationExceptions.cs`: `ConfigurationException`, `AuthenticationException`, `AuthorizationException`, `ThrottleException` (carries `RetryAfter`), `TransportException`, `ContractException`, plus `ErrorDetails(Code, CorrelationId)` and a static `IntegrationExceptionClassifier.Classify(HttpStatusCode, attempts, correlationId, retryAfter)` mapping 401/403/429/5xx/other into the right subclass. The pre-existing `IntegrationRequestException` is retained unchanged for compatibility (not replaced). Provider connectors are being wired to classify into this hierarchy at their boundary; see ADP-010/SAP-005/QLT-005 trackers. `dotnet build`/`dotnet test` (55 baseline tests) pass with the new types added; no test yet exercises the classifier itself end-to-end from this file's own scope. Still partial: `ClientCredentialsTokenSource` auth failures still throw raw `HttpRequestException`/`JsonException`, not yet retrofitted to the new hierarchy. |
| 2026-11-12 | `partial` | Retrofit: `ClientCredentialsTokenSource.GetTokenAsync` now throws `AuthenticationException` (with a sanitized `oauth_http_{status}` code) for a non-success OAuth response, instead of a bare `HttpRequestException` — the malformed-but-200-response cases deliberately keep throwing `JsonException` (a contract failure, not an auth failure; also pinned by the pre-existing `CoreTests.InvalidTokensAreRejected`, left unchanged). `IntegrationRequestException` now carries the same allowlisted correlation headers on failure that a success response already carried, and `AdpApiException.Classify` defaults its `correlationId` from those headers automatically. Covered by new `tests/Koras.Connect.Tests/DiagnosticsAndCorrelationTests.cs`. `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped, 0 failed). |

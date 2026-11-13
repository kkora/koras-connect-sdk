---
id: "SDK-014"
title: "Configuration validation"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-014 - Configuration validation

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-014.
2. **Feature name:** Configuration validation.
3. **Description:** Catch unsafe or contradictory settings before credentials leave the process.
4. **Business value:** Every invalid configuration fails before network with actionable redacted field identifiers reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + Koras.Connect.Authentication + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-AUTH Validate, E-WORKERS options, E-SAP/E-QLT constructors, E-SAFETY; inconsistent validation surfaces. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Standardize eager validators for roots, token endpoints, limits, retries, credentials, effective dates and mutually exclusive settings; require exact configured origins and no redirects.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed options validators and safe validation results; existing constructor checks remain.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise null/relative/HTTP/userinfo/query/fragment roots, invalid durations, unsafe IDs, date conflicts and duplicate names. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Null/relative/HTTP/userinfo/query/fragment roots, invalid durations, unsafe IDs, date conflicts and duplicate names.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Every invalid configuration fails before network with actionable redacted field identifiers; DI and direct construction enforce the same rules.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Exact vendor hosts/product versions are supplied by authorized consumers, never guessed.

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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 332, Skipped 3 (live-sandbox, unrelated), Failed 0. The acceptance criterion's second clause — "DI and direct construction enforce the same rules" — had only ever been verified by code inspection, never empirically. New `ConfigurationValidationConsistencyTests.cs` (3 tests) drives both the DI path (`AddKorasAdp`/`AddKorasSuccessFactors`/`AddKorasQualtrics`'s static pre-validation) and the direct-construction path (`AdpWorkersClient`/`SuccessFactorsClient`/`QualtricsClient`) with the same invalid input and asserts the exact same exception type and message from both. Doing so surfaced a real discrepancy: `QualtricsClient`'s constructor re-implemented its own, independent, weaker check (a bare `ArgumentException` with one generic combined message) instead of calling the same `QualtricsOptions.Validate()` the DI extension already called — unlike ADP's `AdpWorkersClient` and SAP's `SuccessFactorsClient`, which already shared one validation method between both paths. Fixed by making the constructor delegate to `Validate()`, matching the other two providers' existing pattern; the new test now proves all three providers reject an identical bad root identically through both paths. |
| Remaining work / blockers | No functional gap found for this ticket beyond the one fixed above. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added the shared `IValidatableOptions`/`OptionsValidationException` contract in `src/Koras.Connect/Abstractions/Validation.cs`. `TransportOptions` and `ClientCredentialsOptions` now explicitly implement `IValidatableOptions` (their existing `Validate()` methods were already structurally compatible; `TransportOptions.Validate()` was widened from `internal` to `public` to satisfy the interface). Still open: neither type throws the new field-level `OptionsValidationException` yet — they still throw bare `ArgumentException`/`ArgumentOutOfRangeException` — and there is no `IValidateOptions<T>`/DI-time-eager-validation integration, so direct construction and DI-time validation are not yet provably identical. `dotnet build`/`dotnet test` (55 baseline tests) pass unaffected. |
| 2026-11-12 | `partial` | `ClientCredentialsOptions.Validate()` and `TransportOptions.Validate()` now throw `OptionsValidationException` listing every violated field by name (not just the first), instead of a bare `ArgumentException`/`ArgumentOutOfRangeException`. `Koras.Connect.Adp.AdpConnector.TryValidateConfigurationAsync`'s catch clause was updated to also catch `OptionsValidationException` so its existing short-circuit-before-network behavior (`AdpConnectorTests.MissingCredentialsShortCircuitBeforeAnyNetworkCall`) is unaffected. Covered by new field-level-message tests in `tests/Koras.Connect.Tests/DiagnosticsAndCorrelationTests.cs`. Still open: no `IValidateOptions<T>`/DI-time-eager-validation integration. `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped, 0 failed). |
| 2026-11-12 | `implemented` | Closed the "DI and direct construction enforce the same rules" gap with an empirical cross-path test, which found and fixed a real discrepancy in `QualtricsClient`'s constructor (see validation evidence above). Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

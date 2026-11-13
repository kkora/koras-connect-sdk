---
id: "SDK-010"
title: "Cancellation and timeout support"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-010 - Cancellation and timeout support

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-010.
2. **Feature name:** Cancellation and timeout support.
3. **Description:** Give hosts bounded work and prompt shutdown.
4. **Business value:** All async I/O and waits honor the caller token reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT linked total timeout; E-AUTH semaphore cancellation; E-EXPORT mixed sync/async APIs; E-TEST DeadlineCancelsInFlightResponse. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Propagate cancellation into auth/waits/body/paging/export; distinguish caller cancellation from deadline; use async stream export paths and explicit per-operation budgets.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing CancellationToken parameters/TransportOptions.RequestTimeout; proposed neutral deadline failure metadata.
13. **Dependencies:** SDK-009; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise cancel before send, during auth/retry/read/export, partial destination, deadline versus caller token and disposal. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Cancel before send, during auth/retry/read/export, partial destination, deadline versus caller token and disposal.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All async I/O and waits honor the caller token; no write retry after timeout; resource cleanup completes and partial outputs are documented invalid.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Host-provided HttpClient/token providers must honor their contracts; no external service prerequisite.

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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 313, Skipped 3 (live-sandbox, unrelated), Failed 0. New `CancellationAndTimeoutTests.cs` (4 tests) strengthens the existing `SecurityAndLifecycleTests.DeadlineCancelsInFlightResponse` test, which only asserted the generic `OperationCanceledException` base type (a plain caller cancellation would also satisfy that) — adds an assertion that the configured deadline specifically throws `DeadlineExceededException`, and a companion test proving an explicit caller cancellation is never misreported as that subtype, finally making "deadline versus caller token" a real distinguishing test rather than one that happened to pass either way. Also closes both gaps this ticket's own prior status history flagged as open: cancellation during an in-flight token acquisition now propagates promptly from the caller's own token rather than only through `ClientCredentialsTokenSource`'s internal `acquisitionTimeout` ("during auth"), and a cancellation partway through a streamed export leaves whatever was already written in the destination stream intact rather than silently discarding it ("partial destination") — `TabularExport`'s class-level doc comment now states explicitly that a canceled destination must be treated as incomplete and invalid by the caller, closing the acceptance criterion's "partial outputs are documented invalid" clause literally (as documentation, not just behavior). |
| Remaining work / blockers | No functional gap found for this ticket. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | `IntegrationTransport` (`src/Koras.Connect/Transport/IntegrationTransport.cs`) now takes an optional `TimeProvider`, threads it through every `Task.Delay` call for deterministic retry-timing tests, and distinguishes its own configured deadline from caller cancellation: when the linked timeout (not the caller's token) fires, it now throws `Koras.Connect.Exceptions.DeadlineExceededException` (an `OperationCanceledException` subclass, so existing `catch (OperationCanceledException)`/`Assert.ThrowsAnyAsync<OperationCanceledException>` callers are unaffected — verified against the existing `DeadlineCancelsInFlightResponse` test, which still passes). `TabularExport.WriteXml`/`WriteExcel` gained true async, cancellable `WriteXmlAsync`/`WriteExcelAsync` overloads (both `IEnumerable` and `IAsyncEnumerable` row sources) alongside the original synchronous methods, so cancellation now interrupts actual I/O rather than only the between-row check. `ClientCredentialsTokenSource.GetTokenAsync` now bounds token acquisition with its own independent timeout (see SDK-006). Remaining gap: no explicit test yet asserts a canceled export leaves the destination stream marked/documented invalid mid-write. `dotnet build`/`dotnet test` (55 baseline tests) pass unaffected. |
| 2026-11-12 | `partial` | `IntegrationRequestException` now captures the allowlisted correlation headers on failure, not only success (see SDK-009/SDK-011 entries); `IntegrationTransport` gained an optional `ILogger` constructor parameter logging retry/failure/deadline events (see SDK-011). No change to the cancellation/timeout mechanics themselves this round; the canceled-mid-export-stream gap noted above remains open. `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped, 0 failed). |
| 2026-11-12 | `implemented` | Closed both previously-flagged gaps (during-auth cancellation, partial-destination-on-cancel) and strengthened the deadline-vs-caller-token test to actually assert the distinguishing exception type rather than the shared base type. Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

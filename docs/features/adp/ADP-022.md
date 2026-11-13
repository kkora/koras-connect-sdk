---
id: "ADP-022"
title: "Documentation and runnable examples"
category: "adp"
priority: "P0"
phase: "P3-P4"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-022 - Documentation and runnable examples

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-022.
2. **Feature name:** Documentation and runnable examples.
3. **Description:** Let engineers adopt supported operations reproducibly and safely.
4. **Business value:** Let engineers adopt supported operations reproducibly and safely.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3-P4; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAMPLE packaging smoke and README snippets; no dedicated ADP auth/export/connection examples; aliases resolve in section 3.
10. **Functional requirements:** Provide minimal Authentication, WorkerExport and ConnectionTest examples with synthetic/offline modes; document mTLS, credentials, cancellation, operation gates, output sensitivity and migration.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed samples/ADP.Authentication, ADP.WorkerExport, ADP.ConnectionTest; keep PackageConsumer.
13. **Dependencies:** ADP-012, ADP-008, ADP-001, ADP-020; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Compile examples, offline execution, missing settings diagnostics, cancellation and secret/PII canaries. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Compile examples, offline execution, missing settings diagnostics, cancellation and secret/PII canaries.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** All three examples compile/run offline from clean checkout; optional sandbox mode clearly states prerequisites and cannot mutate payroll/lifecycle records by default.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Live sample mode blocked by missing authorized sandbox; offline examples are not blocked.

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
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 231, Skipped 3, Failed 0, Total 234 (includes `AdpSampleWalkthroughTests`); `dotnet run --project samples/ADP.Authentication`, `dotnet run --project samples/ADP.WorkerExport` and `dotnet run --project samples/ADP.ConnectionTest` each executed successfully end to end, fully offline, from this checkout |
| Remaining work / blockers | **Closed this pass:** the earlier scope restriction that blocked `samples/`/`Koras.Connect.slnx` no longer applies. Added three real standalone console projects: `samples/ADP.Authentication`, `samples/ADP.WorkerExport`, `samples/ADP.ConnectionTest`, each following the exact `samples/Qualtrics.SurveyExport` pattern (project-reference-only `.csproj`, top-level-statement `Program.cs`, no network access). Checked first whether `samples/Qualtrics.SurveyExport`/`samples/PackageConsumer` are referenced from `Koras.Connect.slnx` — neither is (only `src/*` and `tests/Koras.Connect.Tests` are) — so, per the task's own instruction, the three new projects were **not** added to `Koras.Connect.slnx` either; they build and run standalone via `dotnet run --project samples/<name>`. `tests/Koras.Connect.Tests/AdpSampleWalkthroughTests.cs` is left in place unmodified (out of this session's "new files only" test scope) as a continuously-run regression complement, not a substitute, now that the real sample projects exist. Remaining gap: the live-sandbox-mode prerequisites documentation mentioned in the functional requirements is still not written up separately from this tracker. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `tests/Koras.Connect.Tests/AdpSampleWalkthroughTests.cs` as a scope-compliant substitute for the requested `samples/` console projects (see the remaining-work note above for the exact restriction that prevented creating real sample projects in this pass). Covers: (a) certificate-bound mTLS transport plus OAuth client-credentials auth against a mocked token endpoint and API; (b) streaming CSV export of workers via `WorkerExportProjection` + `CsvExportProvider` directly from a mocked `EnumerateTypedWorkersAsync`; (c) a full healthy `AdpConnector.DiagnoseAsync` three-stage pass. All three build and run fully offline via `dotnet test`. Status intentionally left `partial`, not `implemented`, because the literal deliverable (runnable `samples/` projects) was not produced. |
| 2026-11-12 | `partial` | Added the literal deliverable: `samples/ADP.Authentication` (loads a certificate via `ClientCertificateTransport.LoadPkcs12`, builds `ClientCredentialsTokenSource`, acquires a token and makes one authenticated worker read, all against an in-process mocked `HttpMessageHandler`), `samples/ADP.WorkerExport` (streams a mocked `AdpWorkersClient.EnumerateTypedWorkersAsync` through `WorkerIdentityMapper` and `WorkerExportProjection`/`CsvExportProvider`, demonstrating the fixed safe-column-only export and an already-cancelled-token path honoring cancellation before any row is written), and `samples/ADP.ConnectionTest` (three `AdpConnector.DiagnoseAsync` scenarios against mocked handlers: a healthy pass, a locally-rejected expired certificate that never reaches the network, and an Authentication-stage failure). Each `dotnet run --project samples/<name>` executed successfully offline from this checkout (console output recorded in this session's report). None are referenced from `Koras.Connect.slnx`, matching the existing `samples/Qualtrics.SurveyExport`/`samples/PackageConsumer` precedent of standing alone. Still `partial`: no live-sandbox-mode documentation was added, and `AdpSampleWalkthroughTests.cs` was left in place as a regression complement rather than removed. |

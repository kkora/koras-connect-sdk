# ADP manual test guide

[Feature tracker](../README.md) | [Category](README.md) | [Sample walkthrough and live-sandbox prerequisites](ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md)

A human-executable test script for the ADP connector: a sample walkthrough a tester can run today with zero
credentials, plus a numbered QA test-case list a tester records pass/fail against. This complements, not
replaces, the automated suite in `tests/Koras.Connect.Tests/Adp/` and `AdpSandboxTests.cs` — those run in CI (or
opt-in, for the sandbox cases); this document is for a person manually confirming the same behavior by eye before
a release.

## Part 1 — Offline sample walkthrough

Every command below runs fully offline against a synthetic `HttpMessageHandler`; none contact a live ADP tenant,
and none require credentials. See
[ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md](ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md) for the full
command list and the "Demonstrates" table per sample. Run every sample once before starting Part 2; a failure
here means do not proceed to sandbox testing.

```
dotnet build Koras.Connect.slnx -c Release
dotnet run --project samples/ADP.Authentication
dotnet run --project samples/ADP.WorkerExport
dotnet run --project samples/ADP.ConnectionTest
dotnet run --project samples/ADP.WorkerWrites
dotnet run --project samples/ADP.WorkerRetrieval
dotnet run --project samples/ADP.IncrementalSync
dotnet run --project samples/ADP.EventNotifications
dotnet run --project samples/ADP.Resilience
```

## Part 2 — Manual QA test-case script

Record a result (Pass/Fail/Blocked) and the date for each row. "Preconditions" beyond a clean checkout and
`dotnet build` are called out per case. Cases marked **Sandbox** require the credentials in
[ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md](ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md#live-sandbox-mode-prerequisites)
and a maintainer temporarily removing that test's `[Fact(Skip = "...")]` attribute; do not attempt them without an
authorized ADP sandbox tenant.

| Case | Area | Preconditions | Steps | Expected result |
|---|---|---|---|---|
| TC-ADP-01 | Certificate authentication (offline) | none | Run `dotnet run --project samples/ADP.Authentication`. | Console shows a token acquired via `ClientCredentialsTokenSource` and one authenticated worker read against the mocked handler; no plaintext secret or certificate key is printed. |
| TC-ADP-02 | Worker export (offline) | none | Run `dotnet run --project samples/ADP.WorkerExport`. | CSV and XLSX exports complete using only `WorkerExportProjection`'s safe column set (no SSN/compensation); the already-canceled-token scenario writes zero rows. |
| TC-ADP-03 | Connection diagnostics (offline) | none | Run `dotnet run --project samples/ADP.ConnectionTest`. | Three `AdpConnector.DiagnoseAsync` outcomes print: a healthy 3-stage pass, a locally-rejected expired certificate (no network call made), and an Authentication-stage failure. |
| TC-ADP-04 | Write commands and safety gates (offline) | none | Run `dotnet run --project samples/ADP.WorkerWrites`. | `ChangeManagerCommand`/`ModifyDepartmentAllocationsCommand`/`TerminateAssignmentCommand`/`ChangePayDataInputCommand` build correctly; a payroll submission without the explicit opt-in is rejected locally with zero network calls; an `AdpEntitlementTracker`-denied operation is rejected before any request is sent. |
| TC-ADP-05 | Worker retrieval surface (offline) | none | Run `dotnet run --project samples/ADP.WorkerRetrieval`. | Single-worker found/not-found, automatic paging, typed `WorkerFilter`/`WorkerSort`, resumable `ContinuationToken` paging to its terminal empty page, typed catalog reads, and an explicit HTTP 202 rejection all print correctly. |
| TC-ADP-06 | Incremental sync (offline) | none | Run `dotnet run --project samples/ADP.IncrementalSync`. | First scan reports every worker new; second scan reports only the one worker that actually changed; an interrupted third scan leaves the on-disk `FileWorkerFingerprintStore` snapshot from the second scan intact (verify by re-running and confirming the interrupted scan's partial data did not persist). |
| TC-ADP-07 | Event notifications (offline) | none | Run `dotnet run --project samples/ADP.EventNotifications`. | Valid publisher-header signature is accepted, a tampered one is rejected; polling path re-reads the named worker from source before acknowledging; duplicate message ids are suppressed; acknowledgement only happens after the re-read succeeds. |
| TC-ADP-08 | Resilience (offline) | none | Run `dotnet run --project samples/ADP.Resilience`. | Credential rotation reacquires a token automatically without recreating dependents; `AdpApiException` carries correlation ids and correct classification across 401/403/404/429/500; GET retries, POST never retries; a bounded token-acquisition timeout fires independently of a non-firing 10-minute outer cancellation. |
| TC-ADP-09 | Automated regression complement | none | Run `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --filter FullyQualifiedName~Adp`. | All ADP-scoped tests pass; the two `AdpSandboxTests` sandbox cases report `Skipped` (not Passed, not Failed, not absent) when sandbox settings are unset. |
| TC-ADP-10 **(Sandbox)** | Live connection diagnostics | Authorized sandbox tenant; `KORAS_ADP_SANDBOX_*` settings configured; maintainer removes the `Skip` attribute on `LiveTenantConnectionDiagnosticsRequireExplicitOptIn`. | Run that one test individually: `dotnet test --filter FullyQualifiedName~LiveTenantConnectionDiagnosticsRequireExplicitOptIn`. | Currently blocked: this test is scaffolding only and throws `InvalidOperationException` by design until an authorized `AdpConnector.DiagnoseAsync` sandbox read is implemented and recorded in ADP-020. Expected result today is a deliberate failure, not a pass — do not reinterpret a pass as evidence until the implementation gap is closed. |
| TC-ADP-11 **(Sandbox)** | Live single-worker read | Same as TC-ADP-10, plus `KORAS_ADP_SANDBOX_ASSOCIATE_OID` referencing a worker the configured OAuth client/population is actually authorized to read; remove `Skip` on `SingleWorkerReadIsAuthorizedAndReturnsExpectedOid`. | Run `dotnet test --filter FullyQualifiedName~SingleWorkerReadIsAuthorizedAndReturnsExpectedOid`. | Exactly one authorized GET is issued; the returned worker's `AssociateOid` matches the configured OID (case-insensitive). Re-apply the `Skip` attribute afterward so CI and credential-less machines are unaffected. |
| TC-ADP-12 **(Sandbox)** | Live export of one worker | Same as TC-ADP-11; remove `Skip` on `SingleWorkerReadExportsDocumentedColumnsToCsv`. | Run `dotnet test --filter FullyQualifiedName~SingleWorkerReadExportsDocumentedColumnsToCsv`. | A CSV is written to the OS temp directory (never the repository) containing only the documented safe columns (associate OID, given/family name, job title, hire/termination date) for the one configured worker — no SSN, other government identifiers, compensation or raw vendor extension data. Delete the temp file after inspecting it. |

Re-apply every `[Fact(Skip = "...")]` attribute touched for sandbox cases before committing; these tests must
never run unattended in CI without real credentials.

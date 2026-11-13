# ADP sample projects and live-sandbox prerequisites

[Feature tracker](../README.md) | [Category](README.md)

Closes the "how to run this sample" gap recorded in [ADP-012](ADP-012-connection-diagnostics.md) and the
"live-sandbox-mode prerequisites documentation" gap recorded in
[ADP-022](ADP-022-documentation-and-runnable-examples.md). Every command below is copy-pasteable from a clean
checkout at the repository root.

## Running the offline samples

All four sample projects are fully offline by default: every HTTP call is answered by an in-process mocked
`HttpMessageHandler`, never a live ADP tenant. None are referenced from `Koras.Connect.slnx` (matching the existing
`samples/Qualtrics.SurveyExport`/`samples/PackageConsumer` precedent), so run each directly:

```
dotnet run --project samples/ADP.Authentication
dotnet run --project samples/ADP.WorkerExport
dotnet run --project samples/ADP.ConnectionTest
dotnet run --project samples/ADP.WorkerWrites
```

| Sample | Demonstrates |
|---|---|
| `ADP.Authentication` | Loading a certificate via `ClientCertificateTransport.LoadPkcs12`, building a `ClientCredentialsTokenSource`, acquiring a token and making one authenticated worker read (ADP-001/ADP-002). |
| `ADP.WorkerExport` | Streaming `AdpWorkersClient.EnumerateTypedWorkersAsync` through `WorkerIdentityMapper` and `WorkerExportProjection`/`CsvExportProvider` — the fixed, safe-column-only export — including an already-canceled-token path that honors cancellation before any row is written (ADP-007/ADP-008). |
| `ADP.ConnectionTest` | Three `AdpConnector.DiagnoseAsync` scenarios: a healthy 3-stage pass, a locally-rejected expired certificate that never reaches the network, and an Authentication-stage failure (ADP-012). |
| `ADP.WorkerWrites` | The typed write-command builders — `ChangeManagerCommand`, `ModifyDepartmentAllocationsCommand`, `TerminateAssignmentCommand`, `ChangePayDataInputCommand` — and their two safety gates: an explicit payroll opt-in rejected locally with zero network calls when omitted, and an `AdpEntitlementTracker`-recorded denial rejected before any request is sent (ADP-013/ADP-014/ADP-015/ADP-021). Every submission below still only reaches a mocked endpoint, never a live tenant. |

`tests/Koras.Connect.Tests/AdpSampleWalkthroughTests.cs` exercises the same scenarios continuously in CI as a
regression complement; the `samples/` projects are the literal runnable deliverable, not a replacement for it.

## Live-sandbox-mode prerequisites

**No sample or test in this repository defaults to contacting a live ADP tenant.** Everything above runs fully
offline. A small set of opt-in, individually-filtered checks exist in
`tests/Koras.Connect.Tests/AdpSandboxTests.cs` for a maintainer who has real sandbox credentials; running them
requires all of the following:

1. **An authorized ADP sandbox tenant, OAuth client and registered client certificate.** This SDK does not provision
   or request one — see `docs/features/ADP-FEATURE-MATRIX.md`'s external-blocker notes for every operation.
2. **The following settings**, set once via `dotnet user-secrets set <key> <value>` from `tests/Koras.Connect.Tests`
   (preferred — avoids a secret sitting in shell history or a `.env` file) or as environment variables of the same
   name:

   | Setting | Purpose |
   |---|---|
   | `KORAS_ADP_SANDBOX_TENANT` | Tenant identifier, for operator reference only. |
   | `KORAS_ADP_SANDBOX_CLIENT_ID` | OAuth client id. |
   | `KORAS_ADP_SANDBOX_CLIENT_SECRET` | OAuth client secret. |
   | `KORAS_ADP_SANDBOX_CERT_PATH` | Public certificate: a PEM file (paired with `KORAS_ADP_SANDBOX_KEY_PATH`) or a standalone PKCS#12 (`.pfx`/`.p12`) bundle. |
   | `KORAS_ADP_SANDBOX_KEY_PATH` | Private key path for the PEM form; omit entirely for a PKCS#12 bundle. |
   | `KORAS_ADP_SANDBOX_CERT_PASSWORD` | PKCS#12 password, if any; unused for the PEM form. |
   | `KORAS_ADP_SANDBOX_API_ROOT` | The tenant's ADP API root. |
   | `KORAS_ADP_SANDBOX_AUTH_ENDPOINT` | The tenant's OAuth token endpoint. |
   | `KORAS_ADP_SANDBOX_ASSOCIATE_OID` | One associate OID the configured OAuth client/population is actually authorized to read. |

   (`AdpSandboxTests.SandboxConfigured` checks all of these except the certificate-form-dependent pair.)
3. **Removing the individual test's `[Fact(Skip = "...")]` attribute** (or otherwise running that one test with
   skip-attributes disabled) for a deliberate, individually-filtered local run. These tests report `Skipped` by
   default rather than silently passing or being absent, so a maintainer always sees that a live check did not run
   — see the class-level remarks on `AdpSandboxTests` for why a fully dynamic runtime skip is not used instead
   (it reports as a hard failure under this repository's exact xunit/xunit.runner.visualstudio combination).

Only **read-only** sandbox checks exist today. No test, sample or this SDK's own code performs a live write
(hire/rehire/termination/payroll/etc.) against any tenant automatically, and the same explicit, individually-filtered
opt-in step above is a hard precondition even for a read. A live **write** case would additionally require: a
separately authorized, disposable sandbox population (never a production tenant), an explicit test-only enablement
flag distinct from read access, and a recorded cleanup plan executed before the run is considered complete — none of
which exists in this repository today (see ADP-020's status for why).

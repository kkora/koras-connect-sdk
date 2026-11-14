# Salesforce manual test guide

[Feature tracker](../README.md) | [Category](README.md)

A human-executable test script for the Salesforce connector: a sample walkthrough a tester can run today with
zero credentials, plus a numbered QA test-case list a tester records pass/fail against. This complements, not
replaces, the automated suite in `tests/Koras.Connect.Tests/Salesforce/` (19 synthetic-`HttpMessageHandler` test
files covering SFD-001 through SFD-021); this document is for a person manually confirming the same behavior by
eye before a release, and is not itself an automated test.

## Part 1 — Offline sample walkthrough

Every command below runs fully offline against a synthetic `HttpMessageHandler`; none contact a live Salesforce
org, and none require credentials. Run every sample once before starting Part 2; a failure here means do not
proceed to sandbox testing.

```
dotnet build Koras.Connect.slnx -c Release
dotnet run --project samples/Salesforce.OfflineWalkthrough
dotnet run --project samples/Salesforce.AdvancedWrites
dotnet run --project samples/Salesforce.BulkDataJobs
dotnet run --project samples/Salesforce.EventStreaming
```

| Sample | Demonstrates |
|---|---|
| `Salesforce.OfflineWalkthrough` | Connected App client-credentials token exchange, `DiagnoseAsync`'s three stages (SFD-001/SFD-003), create/read/update/delete of an Account via `CreateRecordAsync`/`GetRecordAsync`/`UpdateRecordAsync`/`DeleteRecordAsync` gated behind the explicit `writesEnabled: true` opt-in (SFD-004), a two-page SOQL enumeration via `EnumerateQueryAsync` following `nextRecordsUrl` (SFD-005/SFD-006), and a `DescribeObjectAsync` call returning field metadata and `keyPrefix` (SFD-007). |
| `Salesforce.AdvancedWrites` | `UpsertByExternalIdAsync`'s three documented outcomes — insert (201/Created), update (204/NoContent), and an ambiguous multi-match (300/Multiple Choices) returning candidate ids (SFD-008); a `/composite` call via `ExecuteCompositeAsync` where a second subrequest references an earlier, not-yet-committed subrequest's id (SFD-009); and conditional updates using `ifUnmodifiedSince`, including a clean update and a stale copy that raises `SalesforceConcurrencyException` (SFD-018). |
| `Salesforce.BulkDataJobs` | A Bulk API 2.0 ingest job's full lifecycle — `CreateIngestJobAsync`, `UploadIngestBatchAsync`, `CloseIngestJobAsync`, `GetIngestJobStateAsync`, `DownloadSuccessfulResultsAsync` (SFD-010) — and a Bulk API 2.0 query job's lifecycle — `CreateQueryJobAsync`, `GetQueryJobStateAsync`, and `EnumerateQueryResultsAsync` paginating CSV result pages via the `Sforce-Locator` response header (SFD-011). |
| `Salesforce.EventStreaming` | Change Data Capture delivered over the CometD/Bayeux long-polling protocol (handshake → subscribe → connect) via `SubscribeToChangeEventsAsync`, surfacing a typed change event with entity name, change type, record ids and changed fields (SFD-012); publishing a Platform Event over plain REST via `PublishPlatformEventAsync` gated behind `writesEnabled: true` (SFD-013); and subscribing to that same Platform Event channel via `SubscribeToPlatformEventAsync`, also over CometD/Bayeux (SFD-013). |

## Part 2 — Manual QA test-case script

Record a result (Pass/Fail/Blocked) and the date for each row. "Preconditions" beyond a clean checkout and
`dotnet build` are called out per case. Cases marked **(Sandbox)** require a real, authorized Salesforce org and
are genuinely manual procedures today — there is no opt-in sandbox test harness for Salesforce (no
`*Sandbox*` file exists under `tests/Koras.Connect.Tests/Salesforce/`, unlike ADP's
`tests/Koras.Connect.Tests/Adp/AdpSandboxTests.cs`). Do not attempt a Sandbox case without an org and credentials
you are explicitly authorized to use.

| Case | Area | Preconditions | Steps | Expected result |
|---|---|---|---|---|
| TC-SFD-01 | Auth, diagnostics, CRUD, SOQL, describe (offline) | none | Run `dotnet run --project samples/Salesforce.OfflineWalkthrough`. | Console shows a token acquired via `SalesforceAuthentication.CreateClientCredentialsTokenSource`, all three `DiagnoseAsync` stages (Configuration/Authentication/Connectivity) print, an Account is created/read/updated/deleted (each write only after the explicit `writesEnabled: true` argument), a two-page SOQL enumeration prints both pages' account names, and a describe call reports the field count and `keyPrefix` for Account; no network access occurs. |
| TC-SFD-02 | Upsert, composite, conditional updates (offline) | none | Run `dotnet run --project samples/Salesforce.AdvancedWrites`. | Upsert-by-External-ID prints all three outcomes (insert/update/ambiguous with candidate ids); the `/composite` call prints both subrequests succeeding, with the Contact subrequest's `AccountId` resolved against the Account subrequest's not-yet-committed id; the clean conditional update succeeds and the stale one is rejected with a caught `SalesforceConcurrencyException`. |
| TC-SFD-03 | Bulk API 2.0 ingest and query jobs (offline) | none | Run `dotnet run --project samples/Salesforce.BulkDataJobs`. | The ingest job is created, a CSV batch is uploaded, the job is closed, state polling reports `JobComplete`, and the downloaded successful-results CSV contains the inserted contact's `sf__Id`/`sf__Created`/name columns; the query job is created, state polling reports `JobComplete`, and `EnumerateQueryResultsAsync` yields two result pages driven by the `Sforce-Locator` header (non-null locator then `null`). |
| TC-SFD-04 | Change Data Capture and Platform Events (offline) | none | Run `dotnet run --project samples/Salesforce.EventStreaming`. | The CDC subscription completes a handshake/subscribe/connect sequence over `/cometd/68.0/` and prints one `UPDATE` change event for `Account` with its record id and changed field (`Name`); a Platform Event is published via REST (id printed) only after `writesEnabled: true`; the subsequent subscription to the same event channel over CometD/Bayeux prints the received payload's `Amount__c` value. |
| TC-SFD-05 | Automated regression complement | none | Run `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --filter FullyQualifiedName~Salesforce`. | All 19 Salesforce-scoped test files pass; no test is skipped (there is no sandbox-gated test in this connector's suite today). |
| TC-SFD-06 **(Sandbox)** | Live OAuth client-credentials token exchange and connection diagnostics | Authorized Salesforce sandbox org with a Connected App configured for the OAuth 2.0 client-credentials flow. Proposed (not implemented) `KORAS_SALESFORCE_SANDBOX_*` convention: `KORAS_SALESFORCE_SANDBOX_MY_DOMAIN_URL` (e.g. `https://your-domain.my.salesforce.com`), `KORAS_SALESFORCE_SANDBOX_CLIENT_ID`, `KORAS_SALESFORCE_SANDBOX_CLIENT_SECRET`. No existing test or code reads these names today; a tester must wire them in by hand. | Adapt `samples/Salesforce.OfflineWalkthrough/Program.cs` by hand: replace the `SyntheticHandler`-backed `oauthHttp`/`apiHttp` `HttpClient` instances with plain `new HttpClient()` instances, point `SalesforceClientCredentialsOptions` and `SalesforceOptions` at the real my-domain URL and the real client id/secret (read from the environment variables above, never hard-coded), and run `client.DiagnoseAsync()`. | A real access token is obtained from the org's `/services/oauth2/token` endpoint; all three `DiagnoseAsync` stages report `Passed` against the real org; no secret or token value is printed to the console. |
| TC-SFD-07 **(Sandbox)** | Live SOQL query against a real object | Same as TC-SFD-06, plus `KORAS_SALESFORCE_SANDBOX_QUERY_SOQL` — a proposed variable holding a read-only SOQL statement (e.g. `SELECT Id, Name FROM Account LIMIT 5`) against an object the configured Connected App is authorized to read. Proposed convention only; not wired into any existing code. | Using the hand-adapted client from TC-SFD-06, call `client.EnumerateQueryAsync<JsonElement>(soql)` with the configured query and enumerate all pages to completion by hand. | Every record in the real org matching the query is returned across however many pages the org's `nextRecordsUrl` chaining produces, with no unhandled exception and no write of any kind occurring. |
| TC-SFD-08 **(Sandbox)** | Live Bulk API 2.0 ingest and query job end to end | Same as TC-SFD-06, plus `KORAS_SALESFORCE_SANDBOX_BULK_SOBJECT` (a disposable, non-production sObject the tester is authorized to insert into) and `KORAS_SALESFORCE_SANDBOX_TEST_RECORD_ID` — a proposed variable holding one existing record id the tester is authorized to read back via the query job. Proposed convention only; not wired into any existing code. Requires an explicit cleanup plan before the run is considered complete. | Using the hand-adapted client, run the same sequence as `samples/Salesforce.BulkDataJobs/Program.cs` against the real org: `CreateIngestJobAsync` with `writesEnabled: true`, upload a small real CSV batch for the configured sObject, close the job, poll state to `JobComplete`, download successful results; separately, `CreateQueryJobAsync` for a SOQL statement selecting the configured test record id, poll to `JobComplete`, and enumerate result pages. **After the run, delete every record this ingest job created in the org** before considering the case complete. | The ingest job reaches `JobComplete` and the downloaded successful-results CSV lists the real inserted record's server-assigned id; the query job reaches `JobComplete` and returns the configured test record. Cleanup (deleting the inserted records) is confirmed before closing out the case. |

These Sandbox cases describe a genuinely manual, ad hoc procedure performed by hand-editing a copy of a sample's
`Program.cs` against a real org today — not an automated, opt-in test suite with a `Skip`-gated `[Fact]` like
ADP's `AdpSandboxTests`. No such Salesforce sandbox harness exists in this repository; the
`KORAS_SALESFORCE_SANDBOX_*` names above are a proposed convention only, modeled on
`tests/Koras.Connect.Tests/Sdk/SandboxConfiguration.cs`'s env-var/user-secrets/local-json layering, and are not
read by any code in this repository today.

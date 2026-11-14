# Dataverse manual test guide

[Feature tracker](../README.md) | [Category](README.md)

A human-executable test script for the Dataverse connector: a sample walkthrough a tester can run today with
zero credentials, plus a numbered QA test-case list a tester records pass/fail against. This complements, not
replaces, the automated suite in `tests/Koras.Connect.Tests/Dataverse/` — those tests run in CI; this document is
for a person manually confirming the same behavior by eye before a release. Unlike the ADP connector, Dataverse
has no opt-in live-sandbox test harness in this repository today (no `tests/Koras.Connect.Tests/Dataverse/*Sandbox*`
file exists) — the Sandbox cases in Part 2 below describe a genuinely manual procedure, not an automated,
individually-filtered test suite a maintainer un-skips.

## Part 1 — Offline sample walkthrough

Every command below runs fully offline against a synthetic `HttpMessageHandler`; none contact a live Dataverse
environment or Entra tenant, and none require credentials. Run every sample once before starting Part 2; a
failure here means do not proceed to sandbox testing.

```
dotnet build Koras.Connect.slnx -c Release
dotnet run --project samples/Dataverse.OfflineWalkthrough
dotnet run --project samples/Dataverse.ActionsFunctionsAndAttachments
dotnet run --project samples/Dataverse.BatchAndRelationships
dotnet run --project samples/Dataverse.MetadataChoicesAndDelta
```

| Sample | Demonstrates |
|---|---|
| `Dataverse.OfflineWalkthrough` | Entra client-credentials token exchange via `DataverseAuthentication.CreateTokenSource` (DVT-001), a `WhoAmI` connectivity check (DVT-002), generic row create/read/update/delete via `CreateRowAsync`/`GetRowAsync`/`UpdateRowAsync`/`DeleteRowAsync` against the `accounts` entity set gated behind the explicit `writesEnabled: true` opt-in (DVT-004), paginated enumeration via `EnumerateAsync` following an `@odata.nextLink` across two pages (DVT-006), and `$metadata` retrieval parsed into entity descriptors via `GetMetadataAsync`/`ParseEntityDescriptors` (DVT-009). |
| `Dataverse.ActionsFunctionsAndAttachments` | Unbound and bound action invocation (`InvokeUnboundActionAsync` for `Merge`, `InvokeBoundActionAsync` for `EscalateAccount` on a specific account) and parameterized/parameterless function invocation (`InvokeFunctionAsync` for `RetrieveTotalRecordCount` and `WhoAmI`) (DVT-015); uploading, downloading and deleting a file/image column (`entityimage`) via `UploadFileColumnAsync`/`DownloadFileColumnAsync`/`DeleteFileColumnAsync` (DVT-016); and creating a legacy annotation (Note) attachment with a base64 document body by reusing generic row CRUD against the `annotations` entity set (DVT-017). All mutating calls pass `writesEnabled: true` explicitly. |
| `Dataverse.BatchAndRelationships` | A single `$batch` call containing one standalone GET plus a changeset with two writes (a contact create and an account update), parsed from the real multipart/mixed response format (DVT-007); upsert by alternate key (`UpsertByAlternateKeyAsync` keyed on `emailaddress1`, reporting whether the row was created) (DVT-008); and relationship management — binding/clearing a single-valued lookup (`BindLookupAsync`/`ClearLookupAsync` on `primarycontactid`) and associating/disassociating a collection-valued relationship (`AssociateAsync`/`DisassociateAsync` on `contact_customer_accounts`) (DVT-010). All mutating calls pass `writesEnabled: true` explicitly. |
| `Dataverse.MetadataChoicesAndDelta` | `$metadata` parsing via `RefreshMetadataAsync`/`ParseEntityDescriptors`, enumerating entity definitions (`EnumerateEntityDefinitionsAsync`) and global option sets (`EnumerateGlobalOptionSetsAsync`) against the Dataverse metadata endpoints (DVT-009); reading a row with `includeFormattedValues: true` and resolving a choice column's human-readable label via `TryGetFormattedValue` (DVT-011); and walking change-tracking (delta) pages via `GetChangesAsync` with a `deltatoken`-based resume, including a deleted-record entry and reaching the terminal `@odata.deltaLink` (DVT-012). |

## Part 2 — Manual QA test-case script

Record a result (Pass/Fail/Blocked) and the date for each row. "Preconditions" beyond a clean checkout and
`dotnet build` are called out per case. Cases marked **(Sandbox)** require an authorized live Dataverse
environment and are genuinely manual procedures today — see the note at the end of this section.

| Case | Area | Preconditions | Steps | Expected result |
|---|---|---|---|---|
| TC-DVT-01 | Auth, WhoAmI, CRUD, pagination, metadata (offline) | none | Run `dotnet run --project samples/Dataverse.OfflineWalkthrough`. | A synthetic token is acquired and one `WhoAmI` call prints a `UserId`; an account row is created, read back (`name=Contoso Ltd`), updated, enumerated across two pages (`Fabrikam Inc` then `Adventure Works` via `@odata.nextLink`/`skiptoken`), then deleted; `$metadata` is parsed to report one entity type (`account`). No network access occurs and no plaintext secret is printed. |
| TC-DVT-02 | Actions, functions, file columns, attachments (offline) | none | Run `dotnet run --project samples/Dataverse.ActionsFunctionsAndAttachments`. | The unbound `Merge` action and bound `EscalateAccount` action both invoke successfully (the latter printing an `EscalatedOn` timestamp); `RetrieveTotalRecordCount` and parameterless `WhoAmI` functions both return expected values; the `entityimage` file column is uploaded, downloaded back byte-for-byte, then deleted; a PDF annotation (Note) is created against the `annotations` entity set. All writes pass through the `writesEnabled: true` opt-in. |
| TC-DVT-03 | Batch/changesets, upsert, relationships (offline) | none | Run `dotnet run --project samples/Dataverse.BatchAndRelationships`. | The `$batch` call returns one successful standalone read plus a changeset with a contact creation and an account update, each reported `succeeded=true`; upserting a contact by `emailaddress1` reports `created=true`; the primary-contact lookup is bound then cleared, and the contact is associated with then disassociated from the account's `contact_customer_accounts` collection — all without error. |
| TC-DVT-04 | Metadata, choices, formatted values, delta (offline) | none | Run `dotnet run --project samples/Dataverse.MetadataChoicesAndDelta`. | `$metadata` reports one entity type (`account`); entity definitions enumerate `account` and `contact`; the global option set `account_statuscode` enumerates `Active`/`Inactive`; the account's `statuscode` reads back as raw `1` with formatted value `Active`; the first delta page reports one changed account and one deleted record id, and resuming from its `deltatoken` reaches the terminal `@odata.deltaLink` with zero changed/deleted entries. |
| TC-DVT-05 | Automated regression complement | none | Run `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --filter FullyQualifiedName~Dataverse`. | All Dataverse-scoped tests pass (19 test files covering DVT-001 through DVT-021 as of this writing); no test is skipped, since no Dataverse sandbox test exists to skip. |
| TC-DVT-06 **(Sandbox)** | Live Entra client-credentials token exchange | Authorized Entra app registration with a Dataverse client-credentials grant; proposed `KORAS_DATAVERSE_SANDBOX_TENANT_ID`, `KORAS_DATAVERSE_SANDBOX_CLIENT_ID`, `KORAS_DATAVERSE_SANDBOX_CLIENT_SECRET`, `KORAS_DATAVERSE_SANDBOX_ENVIRONMENT_URL` set as a proposed convention — these are not read by any code or test in this repository today. | Adapt `samples/Dataverse.OfflineWalkthrough/Program.cs` by hand: replace the synthetic `oauthHttp`/`apiHttp` `HttpClient`s with plain `new HttpClient()` instances, and build `DataverseClientCredentialsOptions` from the four settings above instead of the sample's literal placeholders, then call `DataverseAuthentication.CreateTokenSource` and request a token. | A real bearer token is returned by Entra ID (inspect only that acquisition succeeds and a non-empty `access_token` is present; never log the token itself). |
| TC-DVT-07 **(Sandbox)** | Live WhoAmI / connection check | Same as TC-DVT-06, plus a real `DataverseClient` built with `DataverseOptions(new Uri(<KORAS_DATAVERSE_SANDBOX_ENVIRONMENT_URL>))`. | Using the adapted client from TC-DVT-06, call `client.WhoAmIAsync()` (or `DataverseClient.DiagnoseAsync`'s Connectivity stage, if adapted the same way). | `WhoAmIAsync` returns a real `UserId`/`BusinessUnitId`/`OrganizationId` for the authenticated service principal; no mutating request is issued. |
| TC-DVT-08 **(Sandbox)** | Live paginated query | Same as TC-DVT-07, plus proposed `KORAS_DATAVERSE_SANDBOX_TABLE` naming a table the configured client is authorized to read, with enough rows in the live environment to span at least two pages. | Call `client.EnumerateAsync(<KORAS_DATAVERSE_SANDBOX_TABLE>)` and iterate through at least two pages. | Rows are returned across more than one HTTP page via a real `@odata.nextLink`/`skiptoken`, matching the row count visible in the Dataverse environment for that table (spot-check a few rows against the Power Apps UI or Advanced Find). |
| TC-DVT-09 **(Sandbox)** | Live create/update/delete with explicit write opt-in | Same as TC-DVT-08, plus proposed `KORAS_DATAVERSE_SANDBOX_ROW_ID` is **not** required for this case since it creates its own disposable row; a table the client is authorized to create/update/delete rows in (ideally a dedicated test/sandbox table, never a production table). | Call `client.CreateRowAsync(table, fields, writesEnabled: true)` to create one disposable row; capture its `ETag` (via a follow-up `GetRowAsync`) and call `UpdateRowAsync(table, id, fields, ifMatchETag: <etag>, writesEnabled: true)` to update it under optimistic concurrency (DVT-013); immediately call `DeleteRowAsync(table, id, writesEnabled: true)` to clean up. | The row is created, the `If-Match` header carrying the captured ETag causes the update to succeed with no stale-write conflict, and the row no longer exists in the environment after the delete — confirm this by attempting a `GetRowAsync` on the same id and observing a not-found result. If any step fails partway, manually delete the orphaned row from the live environment before ending the session; never leave a sandbox test row behind. |

The `KORAS_DATAVERSE_SANDBOX_*` names above are a proposed convention only, modeled on the layered
(`appsettings.local.json` → .NET User Secrets → environment variables) precedent in
`tests/Koras.Connect.Tests/Sdk/SandboxConfiguration.cs`; no Dataverse-specific sandbox test file exists in this
repository to read them. Every Sandbox case in this section is a genuinely manual, ad hoc procedure performed by
hand against an authorized sandbox Dataverse environment today — not an automated opt-in test suite with
`[Fact(Skip = "...")]`-gated tests like ADP's `AdpSandboxTests.cs`.

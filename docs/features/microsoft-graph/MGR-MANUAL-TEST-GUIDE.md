# Microsoft Graph manual test guide

[Feature tracker](../README.md) | [Category](README.md)

A human-executable test script for the Microsoft Graph connector: a sample walkthrough a tester can run today
with zero credentials, plus a numbered QA test-case list a tester records pass/fail against. This complements,
not replaces, the automated suite in `tests/Koras.Connect.Tests/MicrosoftGraph/` — those tests run in CI; this
document is for a person manually confirming the same behavior by eye before a release. It is not itself an
automated test.

## Part 1 — Offline sample walkthrough

Every command below runs fully offline against a synthetic `HttpMessageHandler`; none contact a live Entra
tenant, and none require a tenant id, client secret, certificate, or any other credential. Run every sample
once before starting Part 2; a failure here means do not proceed to sandbox testing.

```
dotnet build Koras.Connect.slnx -c Release
dotnet run --project samples/MicrosoftGraph.OfflineWalkthrough
dotnet run --project samples/MicrosoftGraph.BatchUploadAndDiagnostics
dotnet run --project samples/MicrosoftGraph.SharePointAndOutlook
dotnet run --project samples/MicrosoftGraph.TeamsAndPlannerCollaboration
```

| Sample | Demonstrates |
|---|---|
| `MicrosoftGraph.OfflineWalkthrough` | App-only client-credentials token exchange via `GraphAuthentication.CreateClientCredentialsTokenSource`, `GraphClient.DiagnoseAsync`'s four stages (Configuration/Authentication/Connectivity/Permissions), paginated user and group enumeration via `@odata.nextLink`, OneDrive drive and root-folder-item enumeration, one full delta-sync pass over `/users/delta` (a `nextLink` page followed by a terminal `deltaLink`), and v1.0-vs-beta endpoint selection via `GraphOptions(UseBetaEndpoint: true)` — explicitly labeled a non-production demonstration of the selection mechanism only (MGR-001/MGR-003/MGR-004/MGR-005/MGR-009/MGR-019/MGR-024/MGR-026). |
| `MicrosoftGraph.BatchUploadAndDiagnostics` | `ExecuteBatchAsync`'s id-based (not array-position-based) response mapping across three sub-requests returned out of order, the resumable large-file upload-session lifecycle (`CreateUploadSessionAsync` → two sequential 320 KiB-aligned fragments via `UploadSessionFragmentAsync` → `GetUploadSessionStatusAsync` resuming a second session from its reported `nextExpectedRanges` → `CancelUploadSessionAsync` on an abandoned session), and permission/consent diagnostics via `ListGrantsForCurrentAppAsync`, a delegated-context `ListMyGrantsAsync`, and `BuildAdminConsentUrl` (non-production demo only, never POSTed anywhere) (MGR-010/MGR-021/MGR-023/MGR-024). |
| `MicrosoftGraph.SharePointAndOutlook` | SharePoint root-site and path-based site resolution, document-library (drive) and list enumeration, list-item read/create/update, and the Outlook surface — mail enumeration plus calendar-event enumeration/create/update/delete — all exercised under app-only `GraphOptions` (no delegated-only `/me` restriction applies to any call here) (MGR-009/MGR-011/MGR-012/MGR-013/MGR-014). |
| `MicrosoftGraph.TeamsAndPlannerCollaboration` | Teams enumeration, channel enumeration/message read/send, 1:1 chat message read/send, Planner plan and task enumeration (constructed with `GraphOptions(IsDelegatedContext: true)` throughout because Planner has no application-permission path at all — a confirmed vendor constraint, not an SDK limitation), and Microsoft To Do list/task enumeration and task creation (MGR-015/MGR-016/MGR-017/MGR-018). |

## Part 2 — Manual QA test-case script

Record a result (Pass/Fail/Blocked) and the date for each row. "Preconditions" beyond a clean checkout and
`dotnet build` are called out per case. Cases marked **(Sandbox)** describe a genuinely manual procedure against
a real, authorized Microsoft Entra test tenant — see the note at the end of this section for why these are not
an automated opt-in suite today.

| Case | Area | Preconditions | Steps | Expected result |
|---|---|---|---|---|
| TC-MGR-01 | Auth, diagnostics, users/groups, OneDrive, delta, v1.0-vs-beta (offline) | none | Run `dotnet run --project samples/MicrosoftGraph.OfflineWalkthrough`. | Console shows a synthetic app-only token acquired; all four `DiagnoseAsync` stages report an outcome; two pages of users and one page of groups enumerate; one drive and its root-folder item enumerate; a delta sync prints one changed user from the `nextLink` page and one from the `deltaLink` page with a non-null captured delta link; a beta-endpoint organization read succeeds and is labeled a non-production demo. No network access occurs. |
| TC-MGR-02 | Batch requests, resumable uploads, permission diagnostics (offline) | none | Run `dotnet run --project samples/MicrosoftGraph.BatchUploadAndDiagnostics`. | All 3 batch sub-requests report `succeeded=true` and print in request order (`0`, `1`, `2`) despite the synthetic server returning them out of order (`2`, `0`, `1`), proving id-based mapping; the first upload session completes in two fragments (a 320 KiB-aligned fragment then a smaller final fragment); the second session's status call reports `nextExpectedRanges: 0-` and the resumed fragment completes it; the third session is created then cancelled with no error; tenant-wide and delegated-user permission grants both enumerate; the admin-consent URL prints but is never sent anywhere. |
| TC-MGR-03 | SharePoint sites/lists, Outlook mail/calendar (offline) | none | Run `dotnet run --project samples/MicrosoftGraph.SharePointAndOutlook`. | Root site and a path-resolved "Marketing" site both read correctly; one document library and one list enumerate; one list item reads, one is created and its fields updated; one user's mail enumerates; calendar events enumerate, then one is created, updated (subject change), and deleted — all under app-only `GraphOptions` with no delegated-only restriction encountered. |
| TC-MGR-04 | Teams/chats, Planner, Microsoft To Do (offline) | none | Run `dotnet run --project samples/MicrosoftGraph.TeamsAndPlannerCollaboration`. | One team, its channels, and channel messages enumerate, then a channel message sends; 1:1 chat messages enumerate and a chat message sends; Planner plans and plan tasks enumerate under the delegated-context client (no app-only Planner call is attempted anywhere in the sample); To Do lists and tasks enumerate, then a new task is created. |
| TC-MGR-05 | Automated regression complement | none | Run `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --filter "FullyQualifiedName~.Graph"`. | All Microsoft Graph-scoped tests (the `Graph*Tests` classes under `tests/Koras.Connect.Tests/MicrosoftGraph/`) pass; the unrelated `PackageGraphTests` class is excluded by this filter (its fully qualified name has no `.Graph`-prefixed type). No test is skipped, since no Graph sandbox harness exists today. |
| TC-MGR-06 **(Sandbox)** | Live app-only token acquisition | Authorized Entra test tenant and app registration with client-credentials (secret or certificate) configured for it. Proposed (not implemented) settings: `KORAS_GRAPH_SANDBOX_TENANT_ID`, `KORAS_GRAPH_SANDBOX_CLIENT_ID`, `KORAS_GRAPH_SANDBOX_CLIENT_SECRET` — following the layered env-var/user-secrets/local-json convention in `tests/Koras.Connect.Tests/Sdk/SandboxConfiguration.cs`, but **no code in this repository reads these names today**; a tester must wire them up by hand. | Adapt `samples/MicrosoftGraph.OfflineWalkthrough`: replace its `SyntheticHandler`-backed `HttpClient` with a plain `new HttpClient()`, construct `GraphClientCredentialsOptions` from the real tenant/client id and secret, call `GraphAuthentication.CreateClientCredentialsTokenSource` against the real `HttpClient`, and run `await tokens.GetTokenAsync()` by hand (e.g. in a scratch console app or script) pointed at the real Entra token endpoint. | A real bearer access token is returned (non-empty JWT-shaped string) with no exception; print only its length/expiry, never the token value itself, to any shared log. |
| TC-MGR-07 **(Sandbox)** | Live `DiagnoseAsync` and a real `/organization` read | Same as TC-MGR-06, plus a `GraphClient` built against `new Uri("https://graph.microsoft.com/")` with a real `IntegrationTransport` wrapping the real token source. | Using the real `GraphClient` from TC-MGR-06, call `await client.DiagnoseAsync()` and separately `await client.GetOrganizationAsync()`. | All four `DiagnoseAsync` stages report `Passed` against the real tenant; `GetOrganizationAsync` returns the real tenant's `displayName` and at least one verified domain, matching MGR-008's documented restricted-detail response shape (no full organization profile fields beyond what `User.Read`-level access returns). |
| TC-MGR-08 **(Sandbox)** | Live OneDrive or SharePoint read | Same as TC-MGR-06, plus a proposed `KORAS_GRAPH_SANDBOX_DRIVE_ID` (or `KORAS_GRAPH_SANDBOX_SITE_ID`) naming a drive or site the configured app registration is actually authorized to read. Proposed convention only; not wired into any existing test or sample. | Using the real `GraphClient`, call `await client.EnumerateDriveItemsAsync(driveId)` (or the SharePoint equivalents from `MicrosoftGraph.SharePointAndOutlook`, pointed at the real root) with the configured id. | The real drive's or site's items enumerate without error; no write call is made in this case. |
| TC-MGR-09 **(Sandbox)** | Live `$batch` request | Same as TC-MGR-06, plus authorization for at least one read-only resource addressable from `$batch` (for example `/organization`). | Using the real `GraphClient`, call `await client.ExecuteBatchAsync(...)` with 2-3 read-only (`HttpMethod.Get`) sub-requests against resources the app registration can read; `writesEnabled` stays `false` since every sub-request is a GET. | Each `BatchItemResult` reports `Succeeded = true` with its real response body, mapped back to the correct sub-request `Id` regardless of response order — confirming the id-based mapping proven offline in TC-MGR-02 also holds against the real Graph service, which does not guarantee response order. |

These four **(Sandbox)** cases are genuinely manual, ad hoc procedures today: unlike ADP's opt-in
`AdpSandboxTests.cs` harness (gated by `[Fact(Skip = "...")]` a maintainer temporarily removes), **no
`*Sandbox*` test file exists under `tests/Koras.Connect.Tests/MicrosoftGraph/` for Microsoft Graph**, no
`KORAS_GRAPH_SANDBOX_*` environment variable is read by any code in this repository, and no automated opt-in
suite exists to run these checks — a tester must adapt the offline samples' approach by hand, point them at a
real `HttpClient` and a real Entra app registration, and record the result here rather than via `dotnet test`.

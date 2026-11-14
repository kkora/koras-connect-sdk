# Qualtrics manual test guide

[Feature tracker](../README.md) | [Category](README.md)

A human-executable test script for the Qualtrics connector: a sample walkthrough a tester can run today with zero
credentials, plus a numbered QA test-case list a tester records pass/fail against. This complements, not replaces,
the automated suite in `tests/Koras.Connect.Tests/Qualtrics/` — those run in CI; this document is for a person
manually confirming the same behavior by eye before a release. Unlike the ADP connector, Qualtrics has no opt-in
live-sandbox test harness in this repository today; Part 2's Sandbox cases below describe a genuinely manual,
ad hoc procedure rather than a test a maintainer flips a `Skip` attribute on.

## Part 1 — Offline sample walkthrough

Every command below runs fully offline against a synthetic `HttpMessageHandler`; none contact a live Qualtrics
tenant, none require an API token or data center, and none are referenced from `Koras.Connect.slnx` (same
`samples/Qualtrics.SurveyExport` precedent noted in the ADP sample-prerequisites doc). Run every sample once before
starting Part 2; a failure here means do not proceed to sandbox testing.

```
dotnet build Koras.Connect.slnx -c Release
dotnet run --project samples/Qualtrics.SurveyExport
dotnet run --project samples/Qualtrics.DirectoryContacts
dotnet run --project samples/Qualtrics.EventSubscriptions
```

| Sample | Demonstrates |
|---|---|
| `Qualtrics.SurveyExport` | `EnumerateSurveysAsync` discovering one survey, `StartResponseExportAsync` starting a response export job, then a simulated interruption-and-resume: one direct `GetExportProgressAsync` poll observing `inProgress`, a "process restart" comment, and `WaitForExportAsync` resuming with the *same* progress ID (never starting a new export) until `complete`, followed by `DownloadExportAsync` streaming the file (QLT-002/QLT-003). |
| `Qualtrics.DirectoryContacts` | The XM Directory contact/mailing-list surface: `ListContactsAsync`/`GetContactAsync` reads, an opt-in (`writesEnabled: true`) `CreateContactAsync`, a `ContactChange`-based `UpdateContactAsync` that serializes only the one field explicitly changed (last name) so a full-object PUT can never accidentally null out email/first name, and `ListMailingListsAsync`/`CreateMailingListAsync` (QLT-006/QLT-009/QLT-015). |
| `Qualtrics.EventSubscriptions` | The event-subscription *registration* management surface only — `ListEventSubscriptionsAsync`, an opt-in `CreateEventSubscriptionAsync` joining multiple topics into one comma-separated value, and an opt-in `DeleteEventSubscriptionAsync`. The sample explicitly notes this client is not a webhook receiver: standing up the inbound endpoint, verifying delivery authenticity/freshness, and deduplicating retries remain host-owned (QLT-007). |

## Part 2 — Manual QA test-case script

Record a result (Pass/Fail/Blocked) and the date for each row. "Preconditions" beyond a clean checkout and
`dotnet build` are called out per case. Cases marked **(Sandbox)** require manually adapting a sample's code to use
a real `HttpClient` and a real `X-API-TOKEN` against an authorized Qualtrics account — there is no opt-in automated
sandbox harness for Qualtrics in this repository today (unlike ADP's `AdpSandboxTests.cs`); do not attempt them
without an authorized Qualtrics sandbox/test account and without being prepared to edit a copy of the sample by
hand.

| Case | Area | Preconditions | Steps | Expected result |
|---|---|---|---|---|
| TC-QLT-01 | Survey export, poll-resume, download (offline) | none | Run `dotnet run --project samples/Qualtrics.SurveyExport`. | One survey is discovered; an export job starts and returns a progress ID; a direct poll reports `inProgress` at 40%; after the simulated "process restart," `WaitForExportAsync` resumes polling with the *same* progress ID and reaches `complete` with a file ID; the file downloads and its first line is printed. No live tenant is contacted. |
| TC-QLT-02 | Directory and contact operations (offline) | none | Run `dotnet run --project samples/Qualtrics.DirectoryContacts`. | One contact lists and one fetches by ID; a contact is created only after `writesEnabled: true`; updating the created contact via `ContactChange.SetLastName` sends only the last-name field (console confirms "only that field was sent"); one mailing list lists and one creates. No live tenant is contacted. |
| TC-QLT-03 | Event subscription registration (offline) | none | Run `dotnet run --project samples/Qualtrics.EventSubscriptions`. | One subscription lists; a new subscription is created only after `writesEnabled: true`, with its two topics joined into one comma-separated value; the created subscription is deleted only after `writesEnabled: true`. Console explicitly states this is registration management only, not a webhook receiver. No live tenant is contacted. |
| TC-QLT-04 | Automated regression complement | none | Run `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj --filter FullyQualifiedName~Qualtrics`. | All Qualtrics-scoped tests pass (per QLT-008's recorded evidence: offline/missing-token rejection, schema-drift/unknown-field preservation, continuation-cycle bounds, no leaked errors, interrupted-download partial-byte preservation). No test in this filter contacts a live tenant. |
| TC-QLT-05 **(Sandbox)** | Live token authentication | Authorized Qualtrics account with an active API token; proposed `KORAS_QUALTRICS_SANDBOX_DATACENTER` and `KORAS_QUALTRICS_SANDBOX_API_TOKEN` values available (a proposed naming convention only, not read by any code today). | Copy `samples/Qualtrics.SurveyExport`'s structure, but replace the `SyntheticHandler`-backed `HttpClient` with a plain `new HttpClient()`, replace `StaticToken` with the real token string, and build `QualtricsOptions` with `new Uri($"https://{datacenter}.qualtrics.com/API/v3/")`. Call `EnumerateSurveysAsync()` once. | The real survey list for the authorized account enumerates without a 401/403; an invalid or revoked token instead surfaces `QualtricsApiException` classified as `AuthenticationException` (401) or `AuthorizationException` (403), per `QualtricsApiException.Classify`. Never print the token value. |
| TC-QLT-06 **(Sandbox)** | Live response export, poll, download | Same as TC-QLT-05, plus a proposed `KORAS_QUALTRICS_SANDBOX_SURVEY_ID` naming a real survey the authorized account/token may export responses from (proposed convention only, not wired into any existing test). | Using the same hand-adapted client, call `StartResponseExportAsync(surveyId, new ResponseExportOptions("csv"))`, then `WaitForExportAsync` (default 2-second polling interval, up to 120 polls per `QualtricsClient.WaitForExportAsync`'s bounds) until `complete`, then `DownloadExportAsync` to a local `MemoryStream` or temp file. | The export reaches `complete` with a real `fileId`; the downloaded bytes are a valid CSV with a header row; if the export instead reports `failed`, the client throws `InvalidOperationException` rather than returning a corrupt result. Delete any downloaded file after inspecting it; this is a read-only operation against existing survey data. |
| TC-QLT-07 **(Sandbox)** | Live directory/contact read | Same as TC-QLT-05, plus a proposed `KORAS_QUALTRICS_SANDBOX_DIRECTORY_ID` naming a real XM Directory the authorized token may read (proposed convention only). | Using the hand-adapted client from TC-QLT-05, call `ListContactsAsync(directoryId)` and `GetContactAsync(directoryId, contactId)` for one real contact ID returned by the list. | Real contacts enumerate without error; the single-contact fetch returns the same contact's fields; no write call (`CreateContactAsync`/`UpdateContactAsync`) is made in this case — it is read-only. |
| TC-QLT-08 **(Sandbox)** | Live rate-limit (429) classification | Same as TC-QLT-05. This case only has a reasonable chance of triggering naturally; do not attempt to deliberately hammer a live tenant to force a 429. | While running TC-QLT-06 or TC-QLT-07, if the account happens to be rate-limited, observe the resulting exception. | A 429 response surfaces as `QualtricsApiException` with `StatusCode == HttpStatusCode.TooManyRequests` and a populated `RetryAfter` when the server supplied one (`QualtricsApiException.Classify`/`ThrottleException`); retry/backoff itself is the shared `IntegrationTransport`'s responsibility, not something this client re-implements. If no 429 occurs during normal use, mark this case Blocked rather than fabricating one. |

These Sandbox cases are genuinely manual, ad hoc procedures today — editing a copy of a sample by hand and running
it once against a live account — not an automated opt-in test suite with a `[Fact(Skip = "...")]` a maintainer
un-skips, the way ADP's `AdpSandboxTests.cs` works. The `KORAS_QUALTRICS_SANDBOX_*` variable names above are a
proposed convention only, modeled on `tests/Koras.Connect.Tests/Sdk/SandboxConfiguration.cs`'s precedent
(environment variable, with user-secrets/`appsettings.local.json` layering as a future option); no code in this
repository reads them today.

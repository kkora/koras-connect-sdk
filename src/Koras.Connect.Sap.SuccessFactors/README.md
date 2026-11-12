# Koras.Connect.Sap.SuccessFactors

An explicit, host-configured SAP SuccessFactors OData V2 client. No UI-host or API-host inference, no
auto-discovered entities beyond what is documented below, and no implicit writes.

## Configuration (`SuccessFactorsOptions`)

```csharp
public sealed record SuccessFactorsOptions(Uri ODataRoot, int PageSize = 100, int MaxPages = 10000);
```

Validated by the `SuccessFactorsClient` constructor (and by `SuccessFactorsClient.ValidateOptions(...)` for
fail-fast DI registration):

- `ODataRoot` must be an absolute `https://` URI with no embedded credentials, query string or fragment, and its
  path must end with `/odata/v2/` (trailing slash normalized automatically).
- `PageSize` must be in `[1, 1000]`.
- `MaxPages` must be in `[1, 100000]` - this is an absolute pagination bound, not a tuning knob; it exists to turn
  a runaway/cyclic `__next` link into a thrown `InvalidDataException` instead of an unbounded loop.

`ODataQuery` additionally rejects combining `AsOfDate` with `FromDate`/`ToDate`, an inverted `FromDate`/`ToDate`
range, and any `Paging` value other than `"snapshot"`/`"cursor"`.

## `SamlBearerTokenSource` setup requirements

The SDK never generates, signs or caches a SAML assertion. You supply:

- `endpoint` - the exact tenant OAuth token endpoint; its path must end with `/oauth/token`.
- `companyId`, `clientId` - the registered OAuth client identity for the tenant.
- `assertions` - a delegate returning a **fresh**, Base64-encoded SAML2 bearer assertion on every call (typically
  obtained from your IdP). A null delegate throws `ArgumentNullException` at construction time.

Tokens are cached in-memory and refreshed with a safety margin before expiry; concurrent callers single-flight the
refresh.

## Supported entities

| Entity set | Type | Convenience method |
|---|---|---|
| `PerPerson` | `PerPerson` | `EnumeratePersonsAsync` |
| `EmpEmployment` | `EmpEmployment` | `EnumerateEmploymentAsync` |
| `EmpJob` | `EmpJob` | `EnumerateJobsAsync` |
| any other entity set your tenant's `$metadata` confirms | caller-supplied `T` | `EnumerateAsync<T>(entity, query, ct)` |

All three typed entities preserve unknown/unmodeled server fields via a `[JsonExtensionData] AdditionalData`
dictionary, and represent SAP date fields as a raw `JsonElement?` rather than guessing a shape - use
`SapDates.ParseUtc`/`ParseUtcOrNull` to convert either the legacy `/Date(ms)/` epoch form or an ISO-8601 string
into a UTC `DateTimeOffset`. An unrecognized-but-present date value throws `FormatException` rather than silently
becoming `null`.

`EmpJob.SequenceNumber` accepts both a JSON number and a JSON string (`[JsonNumberHandling(AllowReadingFromString)]`)
since SAP has been observed to return either shape.

### Manager relationships (`SAP-011`)

`ResolveManagerChainAsync(userId, maxDepth, ct)` walks `EmpJob.ManagerId` one hop at a time (one filtered
`EnumerateJobsAsync` call per hop - never a whole-tenant scan). A user with multiple concurrent `EmpJob` records is
resolved to the record with the latest parseable `startDate` (ties broken by the higher `SequenceNumber`). A
revisited user id - including an immediate self-reference - throws `InvalidDataException`; a missing manager ends
the chain normally with a null `ManagerId` on the last entry.

### Delta/checkpoint reads (`SAP-013`)

`SapDeltaCheckpoint { EntityName, ModificationField, LastValue }` plus `SapDeltaFilters.BuildFilter(checkpoint,
overlap)` build an OData `$filter` lower-bounding a documented modification-time field, widened by an overlap
window to tolerate clock skew/same-timestamp writes. `SapDeltaSync.RunAsync(...)` composes
`SuccessFactorsClient.EnumerateAsync` with `Koras.Connect.Synchronization.Reconciliation.DetectChangesAsync` and
only returns an advanced checkpoint after the **entire** scan completes without throwing - a failed/canceled scan
never advances the checkpoint.

### Metadata (`SAP-014`)

`GetMetadataAsync`/`RefreshMetadataAsync` fetch and parse `$metadata` with DTD/external-entity resolution disabled
and an 8,000,000-character document cap (`SuccessFactorsClient.MaxMetadataCharacters`), applied both before and
during XML parsing. `GetMetadataAsync` reparses only when the fetched text's SHA-256 hash differs from the
currently cached document (it still always makes the network call); `RefreshMetadataAsync` always reparses.
`SuccessFactorsClient.ParseEntityDescriptors(document)` projects `EntityType`/`Property`/`Key` elements into
`EntityDescriptor { Name, Keys, Properties }` independent of the exact EDM namespace version returned.

### Data mapping (`SAP-015`)

`PerPersonMapper : IRecordMapper<PerPerson, PersonProfile>` demonstrates the `Koras.Connect.Abstractions` mapping
contract: a record missing both identifiers is `Omitted`, an unparseable date is a field-level `Failure`, and a
successful map always carries a UTC `DateTimeOffset?` date.

### Error classification (`SAP-005`)

Read paths (`EnumerateAsync<T>`/`GetMetadataAsync`/`RefreshMetadataAsync`) classify a failed HTTP response via
`Koras.Connect.Exceptions.IntegrationExceptionClassifier`, so callers can catch `AuthenticationException` (401),
`AuthorizationException` (403, RBP/target-population denial), `ThrottleException` (429) and `TransportException`
(5xx) distinctly instead of one generic exception type. **`UpsertAsync` intentionally keeps raising the raw
`IntegrationRequestException`** so existing row-level upsert diagnostics (`UpsertResult`) are unaffected.
`SapODataError.TryParseSanitizedCode(rawJson)` extracts a sanitized `error.code` from SAP's documented OData JSON
error envelope without ever reading or logging `error.message.value`; it is a standalone utility since the
transport does not retain response bodies for failed requests today.

## Writes (`UpsertAsync`)

`UpsertAsync` is **disabled by default**. Pass `writesEnabled: true` explicitly after you have verified the target
entity's upsert support and the technical user's RBP/target-population grants - the SDK does not perform or infer
that verification for you. A 200-level HTTP response can still carry row-level failures; always inspect every
`UpsertResult.Succeeded`, never assume success from the outer HTTP status alone. Failed upsert calls raise the raw
`IntegrationRequestException` and are never automatically retried.

## Dependency injection

```csharp
services.AddKorasSuccessFactors(new SuccessFactorsConnectionOptions(
    ODataRoot: new Uri("https://api.example.successfactors.com/odata/v2/"),
    TokenEndpoint: new Uri("https://api.example.successfactors.com/oauth/token"),
    CompanyId: "your-company-id",
    ClientId: "your-registered-client-id",
    AssertionSource: ct => yourIdp.GetFreshSamlAssertionAsync(ct)));
```

All configuration is validated synchronously inside `AddKorasSuccessFactors` - before any `HttpClient` or handler
is created - so an unsafe root or a missing assertion source is rejected before any HTTP attempt. Registering a
second `SuccessFactorsClient` into the same `IServiceCollection` throws `InvalidOperationException`; use a separate
service provider per tenant.

## Not yet implemented

- Organizational foundation objects (`FODepartment`/`FODivision`/`FOLocation`, `EnumerateFoundationObjectAsync<T>`)
  are explicitly **best-effort scaffolding** - the entity-set names are not verified against a live tenant's
  `$metadata`. Confirm the real entity-set name/shape via `GetMetadataAsync()`/`ParseEntityDescriptors` first.
- No `IConnector`/`DiagnoseAsync` implementation yet - there is no live, read-only, opt-in connectivity probe.
- No automatic wiring of `SapODataError` into the live error path (see "Error classification" above) - the
  transport does not retain failed-response bodies today.
- No business-rule validation of SAP target population / RBP grants beyond what SAP itself enforces at request
  time; the SDK surfaces the resulting 401/403 but does not pre-check entitlements.
- No live-tenant integration tests - all tests in this repository use a mocked `HttpMessageHandler`.

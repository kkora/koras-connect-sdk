# Koras Connect SDK - Phase 2 Master Feature Specification

## 1. Document Control

| Field | Value |
|---|---|
| Document | Phase 2 master architecture and feature index, version 1.0 |
| Audit date | November 13, 2026, America/New_York |
| Assignment | Phase 2 documentation only; stop before implementation |
| Repository | `C:\repos\koras-connect-sdk` (local working copy); Unix-style path `/Users/kan/Projects/koras-connect-sdk` and `C:\repos\Projects\koras-connect-sdk` referenced in the assignment were not found on this machine - the actual working tree audited is `C:\repos\koras-connect-sdk` |
| Origin | `https://github.com/kkora/koras-connect-sdk` (confirmed via `git remote -v`) |
| Branch at audit | `develop`, clean working tree, 31 local commits ahead of `origin/develop` at audit time |
| Default branch | `develop` (confirmed via `origin/HEAD -> origin/develop`); `origin/main` also exists remotely |
| Documentation branch | `docs/phase-2-master-features`, branched from `develop` at the audit commit; this document is committed there only |
| Product owner | Repository owner; acceptance is not inferred from authorship or merging |
| Specification authority | This document for Phase 2 shared/connector architecture; `docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md` remains authoritative for Phase 1 (SDK/ADP/SAP SuccessFactors/Qualtrics) |
| Approval | Phase 2 documentation creation/validation/local commit authorized by this assignment; Phase 2 implementation, package publication, infrastructure deployment and `main` promotion are **not** authorized by this document |
| Prohibited source | `kkora/AdpIntegration` was not read, fetched or referenced in producing this document |
| Post-audit actions | At explicit user direction *after* this document's initial commit (`eb063ba` on `docs/phase-2-master-features`), the branch was fast-forward merged into `develop` (still `eb063ba`), and the 70 features were then split into one tracker file per feature under `docs/features/{dataverse,salesforce,microsoft-graph}/` (commits `e67871a`, `b0a20ee`), matching the Phase 1 SDK/ADP/SAP SuccessFactors/Qualtrics convention. The rows above describe the state at the original audit and are preserved as history, not updated in place. This document remains the single authoritative source for Phase 2 shared architecture and cross-connector Sections 15-30; per-feature status/evidence now lives in each split file, not here. |

Status vocabulary matches the Phase 1 master precisely, for consistency across both documents: **Not started** means no executable feature implementation found; **Planned** means a designed future scope with no implementation; **Partially implemented** means useful code exists but stated requirements remain; **Implemented but unverified** means source covers the bounded scope but execution evidence has not been independently established; **Tested** requires recorded execution tied to a revision and test scope; **Accepted** requires explicit acceptance against all criteria; **Blocked** means a specific prerequisite prevents the next required action. Every Phase 2 feature below starts from **Not started** unless repository evidence states otherwise - no Phase 2 connector code exists at the time of this audit. Priority (P0/P1/P2) is urgency; milestone (P2-D0 through P2-D7) is a development grouping; they are separate axes, mirroring the Phase 1 priority/phase separation.

## 2. Executive Summary

Koras Connect SDK Phase 1 (Foundation and HR Connectors) is substantially implemented: a five-package shared foundation (`Koras.Connect.Abstractions`, `Koras.Connect.Core`, `Koras.Connect.Authentication`, `Koras.Connect.Export`, plus the transitional `Koras.Connect` type-forwarding facade) and three independent HR connectors (`Koras.Connect.Adp`, `Koras.Connect.Sap.SuccessFactors`, `Koras.Connect.Qualtrics`) are present, building, and covered by 557 passing synthetic tests (3 skipped pending live credentials) as of this audit. All twenty SDK foundation features and all sixty HR-connector features (22 ADP, 16 SAP SuccessFactors, 16 Qualtrics plus 6 additional entries now present under `qualtrics/`) carry per-feature tracking files with evidence-based `status`/`verification` metadata; eighteen of the twenty SDK features are `implemented` or `partial`, leaving only package signing (SDK-015) and a documentation website (SDK-020) genuinely not started. This is a materially more complete foundation than Phase 1's own master document describes in its still-aspirational Section 6 - the eight-package split it proposed is, in fact, already five packages deep and working.

This document defines Phase 2: three new, mutually independent connector packages - `Koras.Connect.Dataverse` (Microsoft Dataverse / Power Platform), `Koras.Connect.Salesforce` (Salesforce REST/Bulk/Pub-Sub), and `Koras.Connect.Microsoft.Graph` (Microsoft Graph v1.0) - each consuming the existing `Koras.Connect.Abstractions`/`Koras.Connect.Core`/`Koras.Connect.Authentication` foundation the same way the three Phase 1 connectors do, and each independently installable without the other two or without any Phase 1 connector. It catalogs 70 features (22 Dataverse, 22 Salesforce, 26 Microsoft Graph) against officially researched vendor API contracts, assesses exactly which shared SDK contracts are reusable as-is versus which need a narrow, justified extension, and defines a seven-milestone implementation roadmap (P2-D0 through P2-D7). No Phase 2 source code, project file, dependency, workflow or other repository artifact is modified by this document; the only change in this commit is the single Markdown file at `docs/features/KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md`.

## 3. Repository Audit

### 3.1 Audit method

Performed directly against the live working tree (no assumptions carried over from the Phase 1 master's prose, which pre-dates several since-completed migrations): `git remote -v`, `git branch -a`, `git status`, `git log` confirmed identity/branch/cleanliness; every `.cs` file under `src/Koras.Connect.Abstractions/`, `src/Koras.Connect.Core/`, `src/Koras.Connect.Authentication/`, `src/Koras.Connect.Export/`, and the `Koras.Connect` facade's `TypeForwards.cs` was read in full; `Koras.Connect.slnx`, `Directory.Build.props`, `global.json`, every `src/*/*.csproj`, and `.github/workflows/ci.yml` were read; `docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md` (977 lines) was read in full as the Phase 1 baseline; every `docs/features/sdk/SDK-0NN-*.md` frontmatter `status`/`baseline_status` field was checked directly rather than trusting the Phase 1 master's frozen snapshot; the full local test suite was executed (`dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`). `kkora/AdpIntegration` was never read or fetched, per instruction.

### 3.2 Current package graph (evidence, not proposal)

Unlike the Phase 1 master document (which as of its own November 12, 2026 audit date still described an eight-package split as a *proposal*, Section 6: "Current runtime graph: `Koras.Connect.Adp`, `Koras.Connect.Sap.SuccessFactors` and `Koras.Connect.Qualtrics` reference `Koras.Connect`... Migration: snapshot existing public API/package metadata first; split neutral contracts, then move implementations"), the migration described there has since been completed. `Koras.Connect.slnx` lists eight projects today:

| Package | `PackageId` | Internal `ProjectReference`s | External `PackageReference`s |
|---|---|---|---|
| `Koras.Connect.Abstractions` | `Koras.Connect.Abstractions` | None | None |
| `Koras.Connect.Core` | `Koras.Connect.Core` | Abstractions | `Microsoft.Extensions.Logging.Abstractions` 10.0.0 |
| `Koras.Connect.Authentication` | `Koras.Connect.Authentication` | Abstractions, Core | None |
| `Koras.Connect.Export` | `Koras.Connect.Export` | Abstractions | None |
| `Koras.Connect` (facade) | `Koras.Connect` | Abstractions, Core, Authentication, Export | None (type-forwarding only, `Facade/TypeForwards.cs`) |
| `Koras.Connect.Adp` | `Koras.Connect.Adp` | Abstractions, Core, Authentication | `Microsoft.Extensions.Http` 10.0.0, `Microsoft.Extensions.Options` 10.0.0 |
| `Koras.Connect.Sap.SuccessFactors` | `Koras.Connect.Sap.SuccessFactors` | Abstractions, Core | `Microsoft.Extensions.Http` 10.0.0 |
| `Koras.Connect.Qualtrics` | `Koras.Connect.Qualtrics` | Abstractions, Core | `Microsoft.Extensions.Http` 10.0.0 |

A `Koras.Connect.Observability` package does **not** exist yet (confirmed: `src/` contains exactly the eight directories above, no ninth). All eight packages target `net10.0` only (`Directory.Build.props`), with `TreatWarningsAsErrors=true`, `Nullable=enable`, `GenerateDocumentationFile=true` (CS1591 suppressed), deterministic builds, and a shared `Version=0.1.0-preview.2`. `global.json` pins SDK `10.0.100` with `rollForward: latestFeature`.

### 3.3 Current shared public contracts (exact, read from source)

These are the contracts every Phase 2 connector is expected to consume. Full signatures are reproduced because the assignment requires this document to "drive future autonomous Codex implementation" - paraphrasing invites drift.

**`Koras.Connect.Abstractions`** (no HTTP implementation; pure contracts):
- `Connector.cs`: `EntitlementState` enum (`Unknown`/`Configured`/`Verified`/`Denied`), `EntitlementEvidence(EntitlementState State, DateTimeOffset? RecordedAt, string? Reference)` record, `ConnectorDescriptor(string Provider, string Product, string ApiVersion, IReadOnlyList<string> SupportedOperations, IReadOnlyDictionary<string, EntitlementEvidence> Entitlements)` record, `DiagnosticStage` enum (`Configuration`/`Authentication`/`Connectivity`), `DiagnosticOutcome` enum (`Passed`/`Failed`/`Skipped`), `ConnectionDiagnostic(DiagnosticStage Stage, DiagnosticOutcome Outcome, string SanitizedCode, string? CorrelationId, DateTimeOffset Timestamp, string? RecommendedNextStep)` record, `IConnector { ConnectorDescriptor Descriptor; ValueTask<IReadOnlyList<ConnectionDiagnostic>> DiagnoseAsync(CancellationToken); }`.
- `CredentialProvider.cs`: `ICredentialProvider<TCredential> { ValueTask<TCredential> GetAsync(CancellationToken); }`, `StaticCredentialProvider<TCredential>`, `DelegatingCredentialProvider<TCredential>`.
- `Mapping.cs`: `FieldError(string Field, string Reason)` record, `MappingResult<TTarget>` (`Value`/`OmittedReason`/`Errors`/`IsSuccess`/`IsOmitted`, factories `Success`/`Omitted`/`Failure`), `IRecordMapper<TSource, TTarget> { MappingResult<TTarget> Map(TSource source); }`.
- `Paging.cs`: `ContinuationToken(string Provider, string QueryFingerprint, string OpaqueValue, DateTimeOffset? ExpiresAt)` record, `Page<T>(IReadOnlyList<T> Items, ContinuationToken? Next)` record.
- `Export/ExportContracts.cs`: `ExportColumn<T>(string Name, Func<T, object?> Select, string NullText, string? Format)`, `ExportSchema<T>` (ordered columns + `CultureInfo`, `Project(T)`), `IExportProvider { string Format; Task WriteAsync<T>(Stream, IAsyncEnumerable<T>, ExportSchema<T>, CancellationToken); }`.
- `Transport/AccessToken.cs`: `IAccessTokenSource { ValueTask<string> GetTokenAsync(CancellationToken); }` (namespace `Koras.Connect.Transport`, despite living in the Abstractions assembly).
- `Transport/RequestAuthenticator.cs`: `IRequestAuthenticator { ValueTask AuthenticateAsync(HttpRequestMessage, CancellationToken); }`.
- `Validation.cs`: `IValidatableOptions { void Validate(); }`, `OptionsValidationException(string optionsTypeName, IReadOnlyList<string> failures)`.

**`Koras.Connect.Core`**:
- `Exceptions/IntegrationExceptions.cs` (namespace `Koras.Connect.Exceptions`): `ErrorDetails(string Code, string? CorrelationId)`, abstract `IntegrationException(string message, ErrorDetails details, Exception? inner)`, sealed subclasses `ConfigurationException`, `AuthenticationException`, `AuthorizationException`, `ThrottleException(... TimeSpan? RetryAfter)`, `TransportException`, `ContractException`, `DeadlineExceededException : OperationCanceledException`; `IntegrationExceptionClassifier.Classify(HttpStatusCode, int attempts, string? correlationId, TimeSpan? retryAfter)` maps 401→Authentication, 403→Authorization, 429→Throttle, 408/500/502/503/504→Transport, everything else→Contract.
- `Transport/EndpointSafety.cs` (namespace `Koras.Connect.Transport`): `ValidateRoot(Uri)` (absolute HTTPS, no userinfo/query/fragment, trailing-slash normalized), `Segment(string)` (single unencoded path segment, rejects `.`/`..`/`/`/`\`/`%`), `Continuation(Uri root, string link)` (confines a server-supplied next-link to the configured origin+path prefix).
- `Transport/IntegrationTransport.cs`: `IntegrationRequestException(HttpStatusCode, int Attempts, IReadOnlyDictionary<string,string[]>? Headers)`, `IntegrationJsonResponse(JsonDocument Body, IReadOnlyDictionary<string,string[]> Headers, HttpStatusCode StatusCode)`, `TransportOptions : IValidatableOptions` (`MaxRetries` 0-10 default 3, `BaseDelay` default 250ms, `MaxDelay` default 30s max 5min, `RequestTimeout` default 100s max 30min), `IntegrationTransport` class with `ReadJsonAsync`/`SendJsonAsync`/`SendResponseAsync`/`CopyToAsync`/`ReadTextAsync` - **GET-only automatic retry** on transient `HttpRequestException` and the configured transient status codes, bounded exponential backoff, honors `Retry-After` up to `MaxDelay`.
- `Transport/RequestAuthentication.cs`: `BearerTokenAuthenticator(IAccessTokenSource)`, `HeaderTokenAuthenticator(string header, IAccessTokenSource)` (rejects `Authorization`/`Host` as the header name, rejects CR/LF in the resolved token).
- `Synchronization/Reconciliation.cs` (namespace `Koras.Connect.Synchronization`): `RecordChange<T>(string Key, string Fingerprint, T Value)`, `Reconciliation.DetectChangesAsync<T>(IAsyncEnumerable<T>, Func<T,string> keySelector, IReadOnlyDictionary<string,string> previous, CancellationToken)` (SHA-256-fingerprint full-snapshot diff; throws on duplicate/empty key; **never reports deletions** - a key present in `previous` but absent from the new scan is silently not yielded, by design) and `Reconciliation.BatchAsync<T>(IAsyncEnumerable<T>, int size, CancellationToken)`.

**`Koras.Connect.Authentication`**:
- `CertificateCredentialProvider` (load-once-cache X.509 cert from PKCS12/PEM, `Invalidate()` for rotation), `ClientCertificateTransport` (`Create`, `LoadPkcs12`, `LoadPem` - PEM path re-imports with `PersistKeySet` for Windows SChannel compatibility), `ClientCredentialsTokenSource : IAccessTokenSource` (OAuth 2.0 client-credentials grant, Basic auth header, cached with an expiry safety margin `min(60s, 10%)`), `RedactedCredential<T>` (`ToString()` override only - does not scrub arbitrary members).

**`Koras.Connect.Export`**: `CsvExportProvider`, `XmlExportProvider`, `XlsxExportProvider`, `JsonExportProvider`, all `IExportProvider`, backed by `TabularExport`'s static stream writers.

### 3.4 Shared SDK feature status (read directly from `docs/features/sdk/SDK-0NN-*.md` frontmatter, not the frozen Phase 1 master snapshot)

| Feature | Current `status` | Current `verification` |
|---|---|---|
| SDK-001 .NET 10 solution and package structure | implemented | - |
| SDK-002 Connector interface contracts | implemented | - |
| SDK-003 Dependency injection extensions | implemented | - |
| SDK-004 HttpClientFactory integration | implemented | - |
| SDK-005 Authentication abstraction | implemented | - |
| SDK-006 Secure credential providers | implemented | - |
| SDK-007 Pagination abstractions | implemented | - |
| SDK-008 Retry and resilience policies | implemented | - |
| SDK-009 Structured exception hierarchy | implemented | - |
| SDK-010 Cancellation and timeout support | implemented | - |
| SDK-011 Logging and correlation IDs | implemented | - |
| SDK-012 OpenTelemetry instrumentation | partial | - |
| SDK-013 Export provider interfaces | implemented | - |
| SDK-014 Configuration validation | implemented | - |
| SDK-015 Package signing and provenance | **not-started** | - |
| SDK-016 NuGet publishing workflow | implemented | - |
| SDK-017 API compatibility checks | implemented | - |
| SDK-018 Security scanning and SBOM | partial | - |
| SDK-019 Versioning and release notes | implemented | - |
| SDK-020 SDK documentation website | **not-started** | - |

18 of 20 shared SDK features are `implemented` or `partial`; only package signing/provenance and a documentation website remain genuinely not started, and neither blocks Phase 2 connector implementation. This is the accurate current foundation-readiness picture Section 8 relies on.

### 3.5 CI, test, and sample evidence

`.github/workflows/ci.yml` runs on an `[ubuntu-latest, windows-latest]` matrix for every push to `main`/`develop`/`feature/**`/`fix/**`/`chore/**` and every pull request: `dotnet restore/build/test/pack` against `Koras.Connect.slnx`, `python eng/check_vulnerabilities.py`, `eng/validate_packages.py`, `eng/check_version_consistency.py`, `eng/check_sources.py`, `eng/generate_sbom.py`, `eng/generate_tooling_inventory.py`, `eng/release_checks.py`, the `tests/release` Python unittest suite, then builds and runs both `samples/PackageConsumer` and `samples/PackageConsumer.NextGen` against packed NuGet artifacts. At audit time, `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj` reports **557 passed, 3 skipped (live-tenant-gated ADP sandbox tests), 0 failed**, organized under `tests/Koras.Connect.Tests/{Sdk,Contracts,Adp,Sap,Qualtrics,ApiSnapshots,Certificates}/`. Eighteen standalone sample console projects exist under `samples/` (8 ADP, 3 Qualtrics, 5 SAP SuccessFactors, plus the two package-consumer compatibility samples), each a `ProjectReference`-only, offline-synthetic-handler demonstration with no live credentials. `tests/Koras.Connect.Tests/ApiSnapshots/*.snapshot.txt` hold a committed reflection-based public-API baseline per shipping assembly (SDK-002/SDK-017's enforcement mechanism) that every Phase 2 package will need an equivalent entry for.

### 3.6 Existing connector DI-extension naming convention (precedent for Phase 2)

| Package | Extension method | Signature (abbreviated) |
|---|---|---|
| `Koras.Connect.Adp` | `AddKorasAdp` | `(IServiceCollection, ClientCredentialsOptions, AdpWorkersOptions, X509Certificate2) -> IServiceCollection` |
| `Koras.Connect.Adp` | `AddKorasAdpWithRotatingCredentials` | `(IServiceCollection, ICredentialProvider<ClientCredentialsOptions>, ...) -> IServiceCollection` |
| `Koras.Connect.Sap.SuccessFactors` | `AddKorasSuccessFactors` | `(IServiceCollection, SuccessFactorsConnectionOptions, TransportOptions?) -> IServiceCollection` |
| `Koras.Connect.Qualtrics` | `AddKorasQualtrics` | `(IServiceCollection, Action<QualtricsOptionsBuilder>, Func<IServiceProvider,IAccessTokenSource>, TransportOptions?) -> IServiceCollection` |

Every existing extension: validates options synchronously before any `HttpClient`/handler is created (fail fast), registers one or more named `IHttpClientFactory` clients via `AddHttpClient("Koras.{Product}.{Role}")` with `RemoveAllLoggers()` and `ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })`, and rejects a duplicate registration in the same `IServiceCollection` with `InvalidOperationException` ("use separate service providers for separate tenants"). Section 6 proposes `AddKorasDataverse`/`AddKorasSalesforce`/`AddKorasMicrosoftGraph` following this exact pattern.

## 4. Product Phase 2 Scope

Phase 2 (CRM and Microsoft Enterprise Connectors) adds three independently installable, host-neutral .NET integration libraries - Dataverse, Salesforce, Microsoft Graph - to the Phase 1 foundation (Abstractions/Core/Authentication/Export plus ADP/SAP SuccessFactors/Qualtrics). Phase 2 responsibilities mirror Phase 1's: API clients, authentication token acquisition, typed/extension-preserving models, pagination/delta adapters, structured errors, and reusable mapping - all consuming the shared foundation, none depending on each other or on any Phase 1 connector. Phase 2 explicitly excludes: persistent scheduling, cross-system workflow orchestration (no Dataverse-to-Salesforce sync job, no Graph-triggered Power Automate flow authoring), tenant/environment administration, subscription billing, dashboards, webhook receiver hosting, and production job hosting - these remain CLI/Cloud responsibilities per the Phase 1 master's Section 22 boundary table, unchanged here. A host can adopt exactly one Phase 2 package (for example, only `Koras.Connect.Microsoft.Graph`) without pulling in Dataverse, Salesforce, or any Phase 1 connector.

Phase 3 (future connector expansion) is explicitly out of scope and not sized here.

## 5. Architectural Principles

These restate and extend the Phase 1 master's Section 5 principles for the three new connectors; nothing here supersedes Phase 1:

1. **Independent engineering.** Specifications are built from this repository's existing contracts plus official Microsoft Learn (Dataverse, Graph) and Salesforce Developer (developer.salesforce.com) documentation only. `kkora/AdpIntegration` is never consulted.
2. **Acyclic, provider-neutral layering.** Dataverse, Salesforce, and Microsoft Graph each depend only on `Koras.Connect.Abstractions`/`Core`/`Authentication` (and, where a connector chooses to expose export convenience helpers, `Koras.Connect.Export`). None of the three may depend on one another, on any Phase 1 connector, or on `Koras.Connect.Observability` (not yet built).
3. **No abstraction creep for one vendor.** Dataverse's OData v4 change-tracking delta tokens, Salesforce's SOQL `nextRecordsUrl`, and Microsoft Graph's `@odata.deltaLink`/`@odata.nextLink` are all genuinely different protocols wearing a superficially similar OData-flavored vocabulary. `Koras.Connect.Abstractions.Page<T>`/`ContinuationToken` already model "a page plus an opaque, provider-bound continuation" generically enough to wrap all three without widening the shared type; no new shared interface is proposed solely to unify them further (see Section 8).
4. **Explicit composition over service location.** DI extensions (`AddKorasDataverse`, `AddKorasSalesforce`, `AddKorasMicrosoftGraph`) are additive sugar; every public client type remains directly constructible with explicit dependencies, exactly like `SuccessFactorsClient`/`QualtricsClient`/`AdpWorkersClient` today.
5. **Cancellation propagates everywhere** - authentication, transport, enumeration, batch/bulk polling, delta sync, file upload/download sessions.
6. **Never infer a remote grant from local configuration.** A configured Dataverse security role, a configured Salesforce connected-app scope, or a configured Graph API permission is a *local opt-in to attempt*, never evidence the tenant actually granted it. `ConnectorDescriptor.Entitlements`/`EntitlementState` (already shared) models this distinction for all three new connectors identically to how `AdpEntitlementTracker` uses it today.
7. **Preserve unknown fields; make lossy mapping intentional.** Every typed Dataverse/Salesforce/Graph model carries a `[JsonExtensionData]` (or equivalent) bag, matching every existing Phase 1 DTO (`Worker`, `PerPerson`, `QualtricsContact`, etc.).
8. **Fail safely on invalid configuration before sending credentials**, matching `IValidatableOptions`'s existing contract.
9. **Separate four evidence tiers**: source implementation, synthetic offline test execution, authorized live validation, and product-owner acceptance. No Phase 2 feature below is marked beyond "Not started" because none has any implementation yet; this document is itself the first evidence artifact for all seventy.
## 6. Target Package Architecture

All three target packages initially support `net10.0` only, matching every existing package (`Directory.Build.props` is shared and is not proposed to change). Each is released independently; none requires the other two Phase 2 packages or any Phase 1 connector to install or function.

| Exact target package ID | Namespace root | Responsibilities | Proposed external `PackageReference`s | DI extension |
|---|---|---|---|---|
| `Koras.Connect.Dataverse` | `Koras.Connect.Dataverse` | Microsoft Entra OAuth token acquisition for Dataverse; environment/instance resolution; OData v4 Web API client (CRUD, query options, batch/changeset, upsert/alternate keys, metadata, change tracking, actions/functions, file/image columns); typed models for common tables with extension-data passthrough | `Microsoft.Extensions.Http` 10.0.0, `Microsoft.Extensions.Options` 10.0.0 (matching ADP's existing pattern; no Dataverse-specific SDK dependency - see Section 9.21) | `AddKorasDataverse` |
| `Koras.Connect.Salesforce` | `Koras.Connect.Salesforce` | OAuth 2.0 (client-credentials and JWT-bearer flows); instance/API-version discovery; REST sObject CRUD; SOQL query + pagination; Describe/metadata; external-ID upsert; Composite API; Bulk API 2.0 ingest/query; typed sObject models with extension-data passthrough | `Microsoft.Extensions.Http` 10.0.0 (matching SAP SuccessFactors'/Qualtrics' existing pattern); Pub/Sub API (SFD-014) requires `Grpc.Net.Client` and `Google.Protobuf` - flagged as a distinct, optional dependency decision, not a default package reference (see Section 9.14 and Section 25 ADR-P201) | `AddKorasSalesforce` |
| `Koras.Connect.Microsoft.Graph` | `Koras.Connect.Microsoft.Graph` | Microsoft Entra delegated and app-only OAuth; Graph v1.0 HTTP client; users/groups/directory roles/applications/organization; OneDrive/SharePoint drives/items/sites/lists; Outlook mail/calendar; Teams/channels/chats; Planner/To Do; delta query and change-notification subscription *management* (not a webhook receiver); batch requests; typed models with extension-data passthrough | `Microsoft.Extensions.Http` 10.0.0 only - the **official `Microsoft.Graph`/`Microsoft.Graph.Core` NuGet SDK is deliberately not a dependency**; this package is a from-scratch, lightweight typed client over the same `IntegrationTransport`/`IRequestAuthenticator` contracts every other Koras connector uses, consistent with every existing connector's "no vendor SDK dependency" precedent (see Section 25 ADR-P202) | `AddKorasMicrosoftGraph` |

Each package follows the exact existing connector skeleton precedent (`ProjectReference` to Abstractions+Core(+Authentication where the connector owns its own token acquisition, which all three do), named `IHttpClientFactory` clients with `RemoveAllLoggers()`/redirects-disabled/cookies-disabled handlers, synchronous `IValidatableOptions.Validate()` before any `HttpClient` is constructed, duplicate-registration rejection per `IServiceCollection`). `Koras.Connect.Export` is referenced only by host application code composing a connector's typed enumeration with an `IExportProvider`, exactly as no existing connector references Export transitively - none of the three new packages adds a hard dependency on Export.

### 6.1 Package naming, namespace, and public surface conventions

- **PackageId = root namespace**, matching the `Koras.Connect.Sap.SuccessFactors` precedent exactly (not `Koras.Connect.Microsoft.Graph` → namespace `Koras.Connect.MicrosoftGraph`; the dotted form is kept in both PackageId and namespace, consistent with how SAP SuccessFactors's own dotted PackageId/namespace already exists in this repository).
- **Client type naming**: `DataverseClient`, `SalesforceClient`, `GraphClient` - matching `SuccessFactorsClient`/`QualtricsClient`'s precedent of dropping the vendor-API-boilerplate suffix.
- **Options record naming**: `DataverseConnectionOptions`, `SalesforceConnectionOptions`, `GraphConnectionOptions` - matching `SuccessFactorsConnectionOptions`'s precedent (not `AdpWorkersOptions`'s per-feature-area naming, since each Phase 2 connector's root client, like SAP SuccessFactors's, covers one coherent API surface rather than ADP's several independently-optioned sub-clients).
- **Exception wrapping**: each connector gets its own `DataverseApiException`/`SalesforceApiException`/`GraphApiException : Koras.Connect.Exceptions.IntegrationException`, following the `AdpApiException`/`QualtricsApiException` factory-pair pattern (`Classify(HttpStatusCode, ...)`/`FromTransportFailure(IntegrationRequestException)`), wired into single-resource reads; pagination/bulk-job-polling/batch operations stay pinned to the raw `IntegrationRequestException` contract so callers can inspect `ResponseStatus` directly - the exact read-vs-pagination/write split already established for ADP-010/SAP-005/QLT-005.
- **Configuration validation**: every options record implements `Koras.Connect.Abstractions.IValidatableOptions`.
- **API compatibility baseline**: each package gets a `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.{Package}.snapshot.txt` entry wired into the existing `ApiSnapshotTests.cs` reflection-based baseline comparison (SDK-017), from its first commit.

## 7. Package Dependency Matrix

| Package | Direct internal dependencies | Must not depend on |
|---|---|---|
| `Koras.Connect.Dataverse` | Abstractions, Core, Authentication | Salesforce, Microsoft.Graph, Adp, Sap.SuccessFactors, Qualtrics, Export, Observability |
| `Koras.Connect.Salesforce` | Abstractions, Core, Authentication | Dataverse, Microsoft.Graph, Adp, Sap.SuccessFactors, Qualtrics, Export, Observability |
| `Koras.Connect.Microsoft.Graph` | Abstractions, Core, Authentication | Dataverse, Salesforce, Adp, Sap.SuccessFactors, Qualtrics, Export, Observability |

An arrow means a direct permitted dependency; absence means prohibited unless a future decision updates this specification, identical in spirit to the Phase 1 master's Section 7. The three Phase 2 packages form three independent leaves off the same shared foundation - no edge exists between any pair of them, and no edge exists from any Phase 2 package back to any Phase 1 connector. This graph is acyclic by construction (three leaves off a four-node DAG: Abstractions ← Core ← Authentication ← {Dataverse, Salesforce, Microsoft.Graph}); P2-D1 must add a `PackageGraphTests.cs`-style assertion (the existing Phase 1 test already enumerates known assemblies and their allowed references - see `tests/Koras.Connect.Tests/Contracts/PackageGraphTests.cs`) covering the three new packages before any connector code lands, so the acyclic property is enforced by CI from the first Phase 2 commit, not merely documented here.

Host applications may reference any subset of {Dataverse, Salesforce, Microsoft.Graph, Adp, Sap.SuccessFactors, Qualtrics} simultaneously - for example, a host syncing Dataverse contacts from a Qualtrics survey roster references both `Koras.Connect.Dataverse` and `Koras.Connect.Qualtrics` directly; the SDK itself defines no cross-connector orchestration type for that scenario (Section 4, Section 22 boundary).

## 8. Shared SDK Compatibility Assessment

This section replaces guesswork with the Section 3.3/3.4 audit findings: almost every shared contract Phase 2 needs already exists and needs no change. Three narrow, justified extensions are proposed; everything else is "reuse as-is."

### 8.1 Reusable as-is (no change required)

| Shared contract | Why it already fits all three Phase 2 connectors |
|---|---|
| `IConnector`/`ConnectorDescriptor`/`EntitlementState`/`ConnectionDiagnostic` | Dataverse's security-role grants, Salesforce's connected-app/profile permission sets, and Graph's delegated-vs-application permission grants are all "local configuration enables an attempt; remote grant confirmed only after an authorized call" - exactly `EntitlementState`'s existing four states. No new enum value is needed. |
| `ICredentialProvider<TCredential>`/`StaticCredentialProvider`/`DelegatingCredentialProvider` | Dataverse app-only client-secret/certificate credentials, Salesforce connected-app client-secret/JWT-signing-certificate credentials, and Graph app-registration client-secret/certificate credentials are all `TCredential` instances behind the same provider shape already used by `CertificateCredentialProvider`/ADP's rotating-credential extension. |
| `IAccessTokenSource`/`IRequestAuthenticator`/`BearerTokenAuthenticator`/`HeaderTokenAuthenticator` | All three Phase 2 APIs use `Authorization: Bearer <token>` (Dataverse and Graph via Microsoft Entra; Salesforce via its own OAuth token endpoint) - no new authenticator type is needed; `HeaderTokenAuthenticator` remains unused by Phase 2 (none of the three uses a non-Authorization token header, unlike Qualtrics' `X-API-TOKEN`). |
| `IntegrationTransport`/`TransportOptions`/`IntegrationRequestException`/`EndpointSafety` | Every Phase 2 connector is HTTPS JSON-over-REST (Graph and Salesforce natively; Dataverse's OData v4 JSON responses too) with the exact same GET-retry/backoff/Retry-After/continuation-boundary needs already implemented. `EndpointSafety.Continuation` directly protects Dataverse's `@odata.nextLink`/`@odata.deltaLink`, Salesforce's `nextRecordsUrl`, and Graph's `@odata.nextLink`/`@odata.deltaLink` identically to how it already protects SAP SuccessFactors's `__next` and Qualtrics' `nextPage`. |
| `IntegrationExceptionClassifier`/`IntegrationException` hierarchy | 401/403/429/5xx classify identically for all three vendors; Dataverse additionally documents a specific 412 (`ConcurrencyVersionMismatch`) for ETag mismatches, which is a `ContractException`-shaped case the classifier's existing default arm already covers without modification (see Section 19 for the connector-specific wrapping each client adds on top). |
| `Page<T>`/`ContinuationToken` | Generic enough to wrap Dataverse's `$skiptoken`-bearing `@odata.nextLink`, Salesforce's `nextRecordsUrl`, and Graph's `$skiptoken`/`$deltatoken`-bearing `@odata.nextLink`/`@odata.deltaLink` without widening the record. `ContinuationToken.QueryFingerprint` already prevents a token issued for one query/entity from being replayed against another, which is exactly the hygiene all three vendors' opaque tokens need. |
| `IRecordMapper<TSource,TTarget>`/`MappingResult<TTarget>`/`FieldError` | Directly reusable for Dataverse-row→neutral, sObject→neutral, and Graph-resource→neutral projections, matching every existing Phase 1 mapper (`PerPersonMapper`, `EmpEmploymentMapper`, `QualtricsContactMapper`). |
| `IValidatableOptions`/`OptionsValidationException` | Directly reusable for `DataverseConnectionOptions`/`SalesforceConnectionOptions`/`GraphConnectionOptions`. |
| `IExportProvider`/`ExportSchema<T>` (host-composed, not a hard dependency) | Unchanged; a host exports Dataverse rows, sObjects, or Graph resources the same way it exports ADP workers today - no connector-specific export logic. |
| `Reconciliation.DetectChangesAsync`/`RecordChange<T>` | Directly reusable as the fallback full-snapshot reconciliation path for any Phase 2 entity that lacks (or whose host chooses not to use) a native delta/change-tracking mechanism - most usefully for Salesforce objects with Change Data Capture disabled, or Dataverse tables with change tracking disabled. |

### 8.2 Narrow, justified extensions (new shared contracts proposed)

Per the assignment's explicit instruction to "avoid expanding shared abstractions merely to accommodate one vendor-specific operation," each proposed addition below is needed by **at least two of the three** Phase 2 connectors (and, in two cases, already has a latent need in Phase 1 too) - none is a single-vendor accommodation.

1. **`IBatchRequestExecutor` is *not* proposed as a shared abstraction.** Dataverse's `$batch` multipart/mixed changeset semantics, Salesforce's `/composite`/`/composite/batch` JSON-array semantics, and Graph's `/$batch` JSON-array-with-`dependsOn`-ordering semantics are different enough (different wire formats, different atomicity units - Dataverse's changeset is all-or-nothing, Salesforce's composite batch is independently-evaluated-per-subrequest by default, Graph's batch is independently-evaluated-per-subrequest with optional explicit `dependsOn` sequencing) that forcing one interface would either leak a lowest-common-denominator shape or hide vendor-specific atomicity guarantees a caller needs to reason about correctly. **Decision: each connector defines its own `{Connector}BatchRequest`/`{Connector}BatchResponse` types; Section 18 documents the three shapes side-by-side instead of unifying them.** This is the clearest instance of "explain which capabilities remain vendor-specific" the assignment calls for.
2. **A shared `PartialBatchFailure`/`BatchItemResult` *shape convention* (not a new interface) is proposed for `Koras.Connect.Abstractions`**: `BatchItemResult<T>(int Index, bool Succeeded, T? Value, ErrorDetails? Error)`. All three vendors return a mixed success/failure array for a multi-item write (Dataverse's `odata.continue-on-error` batch response, Salesforce's Composite/Bulk per-record result array, Graph's per-sub-request batch response array) - this is the exact same "distinguish success from partial failure without silently dropping information" need `MappingResult<T>` already fills for mapping, applied to batch writes. **This is a genuinely cross-connector, already-proven shape** (ADP's `UpsertResult`/SAP's `UpsertResult` already hand-roll an equivalent per-connector type for their own single-entity upserts); formalizing it in Abstractions lets Dataverse/Salesforce/Graph (and, optionally, a future Phase-1 refactor) share one result-row shape instead of three independent reinventions. Added in P2-D1 before any connector batch feature is implemented.
3. **A shared `DeltaCheckpoint`/delta-filter *pattern* is documented, not newly coded**: SAP SuccessFactors's existing `SapDeltaCheckpoint`/`SapDeltaFilters` (Core-adjacent but SAP-specific today) already prove the right shape - `(string EntityName, string ModificationOrDeltaField, string LastValue)` plus a connector-specific filter/token builder. Dataverse's change-tracking delta link, Salesforce's `SystemModstamp`-filtered SOQL (or CDC/Pub-Sub event stream), and Graph's native `@odata.deltaLink` token are three different enough *mechanisms* (query-filter-based vs. event-stream-based vs. opaque-link-based) that a single shared interface would either force Graph's token-based delta into a filter-string shape it doesn't have, or force Salesforce's modstamp filter into an opaque-token shape that discards its queryability. **Decision: no new shared interface; each connector's delta/incremental-sync feature (DVT-012, SFD-021, MGR-019) documents its own mechanism, with `ContinuationToken` reused wherever the vendor's own mechanism is genuinely an opaque token (Dataverse's `$deltatoken`, Graph's `$deltatoken`) and a connector-specific checkpoint record where it is a queryable filter value (Salesforce's `SystemModstamp`).**

No other shared abstraction change is proposed. Everything in 8.1 is sufficient for 67 of the 70 features below; the three extensions above are scoped narrowly and justified by genuine cross-connector reuse, not single-vendor convenience.
## 9. Dataverse Feature Matrix

Official sources checked November 13, 2026: [D1: Web API overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview), [D2: API limits](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/api-limits), [D3: Compose HTTP requests](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/compose-http-requests-handle-errors), [D4: Authenticate](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/authenticate-oauth), [D5: Discovery service](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/discovery-service), [D6: Query data overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query/overview), [D7: Page results](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query/page-results), [D8: Batch operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/execute-batch-operations-using-web-api), [D9: Create row](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/create-table-row), [D10: Update/delete row](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/update-delete-table-rows), [D11: Service documents/metadata query](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api), [D12: Associate/disassociate](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/associate-disassociate-table-rows), [D13: Change tracking](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-change-tracking-synchronize-data-external-systems), [D14: Actions/functions](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/use-web-api-actions), [D15: File columns](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-column-data). Current API version `v9.2` (base `https://{org}.api.crm{region}.dynamics.com/api/data/v9.2/`); OData v4.0 conformance (not 4.01); `$skip`/`$search`/`$format` unsupported.

| ID | Feature | Priority | Milestone | Status |
|---|---|---|---|---|
| [DVT-001](#dvt-001) | Microsoft Entra OAuth authentication | P0 | P2-D2 | Not started |
| [DVT-002](#dvt-002) | Environment discovery and connection | P0 | P2-D2 | Not started |
| [DVT-003](#dvt-003) | Dataverse Web API client | P0 | P2-D2 | Not started |
| [DVT-004](#dvt-004) | Table and row CRUD | P0 | P2-D2 | Not started |
| [DVT-005](#dvt-005) | OData filtering, selection and ordering | P0 | P2-D2 | Not started |
| [DVT-006](#dvt-006) | Pagination and continuation links | P0 | P2-D2 | Not started |
| [DVT-007](#dvt-007) | Batch and changeset operations | P0 | P2-D2 | Not started |
| [DVT-008](#dvt-008) | Upsert and alternate keys | P0 | P2-D2 | Not started |
| [DVT-009](#dvt-009) | Table and column metadata | P0 | P2-D2 | Not started |
| [DVT-010](#dvt-010) | Lookups and relationships | P0 | P2-D2 | Not started |
| [DVT-011](#dvt-011) | Choice, status and state metadata | P0 | P2-D2 | Not started |
| [DVT-012](#dvt-012) | Change tracking and delta synchronization | P0 | P2-D2 | Not started |
| [DVT-013](#dvt-013) | ETags and optimistic concurrency | P0 | P2-D2 | Not started |
| [DVT-014](#dvt-014) | Service protection limits and throttling | P0 | P2-D2 | Not started |
| [DVT-015](#dvt-015) | Actions and functions | P1 | P2-D5 | Not started |
| [DVT-016](#dvt-016) | File and image columns | P1 | P2-D5 | Not started |
| [DVT-017](#dvt-017) | Attachments and notes | P1 | P2-D5 | Not started |
| [DVT-018](#dvt-018) | Dynamics 365 specialization | P1 | P2-D5 | Not started |
| [DVT-019](#dvt-019) | Solution and customization metadata | P2 | P2-D7 | Not started |
| [DVT-020](#dvt-020) | Virtual table capability discovery | P2 | P2-D7 | Not started |
| [DVT-021](#dvt-021) | Typed models and mappings | P0 | P2-D2 | Not started |
| [DVT-022](#dvt-022) | Contract tests, diagnostics and examples | P0 | P2-D2/P2-D6 | Not started |

## 10. Dataverse Detailed Feature Specifications

<a id="dvt-001"></a>
### DVT-001 - Microsoft Entra OAuth authentication

**Business purpose.** Authenticate the SDK against a Dataverse environment as either an unattended application ("connect as an app") or a signed-in user, without the SDK generating, storing, or caching long-lived secrets itself.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started - no Dataverse code exists in this repository.
**Repository evidence.** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`CertificateCredentialProvider`/`RedactedCredential<T>` unchanged (Section 8.1); no new Core/Abstractions type required.
**Functional requirements.** Support the application (client-credentials) flow against Microsoft Entra ID with either a client secret or an X.509 certificate; require the caller to supply a pre-provisioned Entra app registration plus a Dataverse **Application User** bound to that registration with an assigned security role (app-only access consumes no license seat - confirmed [D4]); support an optional delegated flow for interactive/testing scenarios via a caller-supplied token source, matching the existing `IAccessTokenSource` seam rather than embedding MSAL's interactive UI. Token scope for client-credentials: `{environment-url}/.default`; for delegated/public-client: `{environment-url}/user_impersonation` [D4]. Never assume `common`/`organizations`/`consumers` Entra tenant aliases are valid for a Dataverse app-only scenario - require an explicit tenant GUID or verified domain.
**Nonfunctional requirements.** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware; no secret persisted to disk by the SDK itself.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseClientCredentialsOptions(Uri TenantTokenEndpoint, string ClientId, string EnvironmentUrl,
    string? ClientSecret = null, Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions;
// Reuses Koras.Connect.Authentication.ClientCredentialsTokenSource for the secret case;
// a certificate-based client-assertion token source is new connector-owned code (JWT client assertion is an
// Entra-wide mechanism, not Dataverse-specific, so it is implemented once in Koras.Connect.Dataverse and not
// proposed as a shared Core type until a second Phase 2 connector needs the identical client-assertion shape).
```
**Official API endpoints, methods and versions (verified).** Token endpoint is the standard Microsoft Entra v2.0 endpoint `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token` (generic Entra behavior, confirmed via the Entra identity-platform docs the Graph research also cites; **not restated verbatim on the Dataverse-specific `authenticate-oauth` page itself** - flagged as a documentation gap in the vendor research, not fabricated here). Scope formats `{environment-url}/.default` and `{environment-url}/user_impersonation` are confirmed directly on [D4].
**Authentication and authorization.** Client-credentials (app-only) or authorization-code/device-code (delegated, via caller-supplied token). No Basic/API-key mode exists for Dataverse.
**Required API permissions, scopes or licenses.** An Entra app registration; a Dataverse **Application User** record with an assigned Dataverse security role covering every table/operation the connector will call (DVT-001 itself needs none beyond "can obtain a token" - operation-level security roles are DVT-004+'s concern). No Power Apps/Dynamics 365 user license is consumed by app-only access [D4]; delegated access requires a licensed interactive user.
**Pagination, throttling and retry behavior.** Not applicable to token acquisition itself; reuses `ClientCredentialsTokenSource`'s existing cached-with-expiry-margin behavior.
**Error handling and concurrency.** 401 from the token endpoint → `AuthenticationException` (existing classifier, unchanged). Concurrent `GetTokenAsync` callers share one in-flight acquisition (existing `ClientCredentialsTokenSource` semaphore behavior).
**Data privacy and security.** Client secret/certificate never logged; `RedactedCredential<T>.ToString()` override reused.
**Dependencies.** None (first Dataverse feature).
**Unit-test scenarios.** Expired/skewed cache triggers reacquisition; concurrent callers share one token request; invalid certificate (no private key) rejected before any network call; non-2xx token response classifies as `AuthenticationException`; canceled acquisition propagates `OperationCanceledException`.
**Contract-test scenarios.** Synthetic token-endpoint fixture matching the documented `client_credentials` grant request/response shape.
**Integration-test requirements.** Requires a real Entra app registration + Dataverse Application User in an authorized sandbox environment; not available in this session.
**Acceptance criteria.** A caller can obtain a bearer token for a configured environment using only a client ID + secret/certificate, with no interactive prompt, and the token is reused until near expiry.
**Definition of Done.** Matches Phase 1's Section 26 Definition of Done template; this feature's live sandbox gate is explicitly deferred (Section 28 open question).
**Known limitations and blockers.** The exact Dataverse-specific token-endpoint restatement (vs. generic Entra docs) is a documented gap in the official docs, not an SDK limitation (Section 3 vendor research). No live Dataverse tenant/Entra app registration is available in this documentation pass.

<a id="dvt-002"></a>
### DVT-002 - Environment discovery and connection

**Business purpose.** Resolve which Dataverse environment (organization) a caller's credentials should target, and validate connectivity to it, before any table operation is attempted.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `EndpointSafety.ValidateRoot` reused unchanged for the resolved environment API root; `IConnector.DiagnoseAsync` reused for the connectivity probe (Section 8.1).
**Functional requirements.** Support two discovery modes: (a) caller supplies the environment URL directly (`https://{org}.api.crm{region}.dynamics.com/`) - the common case once an environment is chosen; (b) optional Global Discovery Service (GDS) lookup, `GET https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances`, returning `Instance` records (`ApiUrl`, `Id`, `EnvironmentId`, `UniqueName`, `FriendlyName`, `Region`, `Version`, `TenantId`) for environments the authenticated identity can access [D5]. Confirm connectivity via the `WhoAmI` unbound function, `GET {environment}/api/data/v9.2/WhoAmI`, which returns the caller's `UserId`/`BusinessUnitId`/`OrganizationId` - documented as a connectivity check, not a discovery mechanism [D5]. GDS endpoint varies by sovereign cloud: Commercial `globaldisco.crm.dynamics.com`, GCC `...crm9.dynamics.com`, GCC-High `...crm.microsoftdynamics.us`, DoD `...crm.appsplatform.us`, China `...crm.dynamics.cn` [D5] - the connector must accept an explicit GDS root rather than hardcoding the commercial-cloud default silently.
**Nonfunctional requirements.** Bounded timeout; cancellation-aware; no environment auto-selection when GDS returns more than one instance (caller must disambiguate explicitly).
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseInstance(string ApiUrl, string Id, string EnvironmentId, string UniqueName,
    string FriendlyName, string Region, string Version, string TenantId);
public sealed record WhoAmIResult(Guid UserId, Guid BusinessUnitId, Guid OrganizationId);
public interface IDataverseDiscoveryClient
{
    IAsyncEnumerable<DataverseInstance> ListInstancesAsync(CancellationToken cancellationToken = default);
}
```
**Official API endpoints, methods and versions (verified).** `GET /api/discovery/v2.0/Instances` on the GDS host [D5]; `GET {environment}/api/data/v9.2/WhoAmI` [D5, D1].
**Authentication and authorization.** GDS and `WhoAmI` both require the same bearer token as any other Dataverse call; GDS specifically needs a token scoped to the GDS host itself, not the target environment.
**Required API permissions, scopes or licenses.** None beyond a valid Entra identity; GDS only lists environments the identity already has some access to.
**Pagination, throttling and retry behavior.** `WhoAmI` is a single read; reuses `IntegrationTransport.ReadJsonAsync`'s existing GET-retry behavior unchanged. GDS instance listing is not documented as paginated in the fetched research.
**Error handling and concurrency.** 401/403 classify via the existing shared classifier; an empty GDS instance list is a valid, non-error outcome (no accessible environments) and must not be conflated with an authentication failure.
**Data privacy and security.** `DataverseInstance`/`WhoAmIResult` contain tenant/org/environment identifiers, not employee or business data; still excluded from default telemetry per the shared safe-logging policy.
**Dependencies.** DVT-001.
**Unit-test scenarios.** `WhoAmI` 200 response parses `UserId`/`BusinessUnitId`/`OrganizationId`; GDS response with zero/one/multiple instances; malformed GDS response surfaces a contract error, not a crash.
**Contract-test scenarios.** Synthetic GDS/`WhoAmI` fixtures matching the documented shapes.
**Integration-test requirements.** Requires a real Entra identity with at least one accessible Dataverse environment.
**Acceptance criteria.** A caller supplying a direct environment URL never needs GDS; a caller using GDS receives every accessible instance without the SDK silently picking one.
**Definition of Done.** Standard template; live GDS/WhoAmI sandbox evidence deferred.
**Known limitations and blockers.** No live Dataverse tenant available this pass.

<a id="dvt-003"></a>
### DVT-003 - Dataverse Web API client

**Business purpose.** Provide the single HTTP client foundation (base URL composition, required headers, version policy) every other Dataverse feature builds on.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged (Section 8.1), matching exactly how `SuccessFactorsClient` wraps `IntegrationTransport` over SAP's own OData V2 root today.
**Functional requirements.** Compose the base URL as `{environment-url}/api/data/v9.2/` [D3]; require `OData-MaxVersion: 4.0`, `OData-Version: 4.0`, `Accept: application/json` on every request [D1, D3]; enforce the documented 32 KB max URL length (64 KB inside `$batch`) and 260-character max individual OData path-segment length [D3] as local pre-send validation, not merely a server-side surprise. Current version is `v9.2`; code written against `v9.0`/`v9.1` keeps working on later minor versions, but new capabilities are not backported to earlier minor versions [D3] - the connector targets `v9.2` fixed, with the version string isolated to one constant for a future bump.
**Nonfunctional requirements.** net10.0/Linux/Windows; HTTPS-only root (`EndpointSafety.ValidateRoot` already enforces this); bounded request/response size.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseOptions(Uri EnvironmentRoot, int MaxPageSize = 5000) : IValidatableOptions;
public sealed partial class DataverseClient
{
    public DataverseClient(IntegrationTransport transport, DataverseOptions options);
}
```
**Official API endpoints, methods and versions (verified).** Base URL pattern and header requirements per [D1]/[D3]; `v9.2` current version [D3].
**Authentication and authorization.** Delegates to DVT-001's token source via `IRequestAuthenticator`/`BearerTokenAuthenticator`, unchanged.
**Required API permissions, scopes or licenses.** None beyond DVT-001.
**Pagination, throttling and retry behavior.** Delegated to DVT-006/DVT-014 respectively; this feature only establishes the client skeleton.
**Error handling and concurrency.** Delegates to the shared `IntegrationExceptionClassifier`; a `DataverseApiException.FromTransportFailure` wrapper is added here for single-resource reads, matching the ADP/SAP/Qualtrics precedent (Section 6.1).
**Data privacy and security.** No credentials or row data logged by the client skeleton itself.
**Dependencies.** DVT-001, DVT-002.
**Unit-test scenarios.** Required headers present on every request; oversized URL/segment rejected locally before send; non-HTTPS/credential-bearing root rejected at construction (`EndpointSafety.ValidateRoot`, already proven behavior).
**Contract-test scenarios.** Synthetic fixture asserting header shape against a captured-from-documentation example request.
**Integration-test requirements.** None beyond DVT-002's.
**Acceptance criteria.** Every Dataverse request carries the three required headers and a validated, bounded URL.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond DVT-001/002's.

<a id="dvt-004"></a>
### DVT-004 - Table and row CRUD

**Business purpose.** Create, read, update and delete individual Dataverse table rows.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific; follows the exact CRUD-wrapping pattern `SuccessFactorsClient.UpsertAsync`/`EnumerateAsync<T>` already establish for a different OData dialect.
**Functional requirements.** `POST {entityset}` creates (`204 No Content` + `OData-EntityId` response header identifying the new row, or `201 Created` with a body when `Prefer: return=representation` is sent); `GET {entityset}(guid)` reads one row; `PATCH {entityset}(guid)` updates (`204`, or `200` with `return=representation`); `DELETE {entityset}(guid)` deletes (`204`); `PUT {entityset}(guid)/{property}` with `{"value": ...}` updates a single property only [D9, D10]. The connector must read `OData-EntityId` on create to recover the new row's GUID when the caller did not request `return=representation`.
**Nonfunctional requirements.** Cancellation-aware; explicit opt-in gate (`writesEnabled: bool = false`) on create/update/delete, matching every existing Phase 1 write operation's convention.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<Guid> CreateRowAsync(string entitySet, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> GetRowAsync(string entitySet, Guid id, ODataQuery? query = null, CancellationToken ct = default);
public async Task UpdateRowAsync(string entitySet, Guid id, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task DeleteRowAsync(string entitySet, Guid id, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** POST/GET/PATCH/DELETE/PUT as above, confirmed directly on [D9]/[D10].
**Authentication and authorization.** Bearer token from DVT-001; the specific table + operation must additionally be covered by the Application User's/delegated user's Dataverse security role - a 403 here is a genuine entitlement denial, not an SDK bug.
**Required API permissions, scopes or licenses.** A Dataverse security role granting Create/Read/Write/Delete privilege on the specific table, at a scope (User/Business Unit/Organization) covering the target row.
**Pagination, throttling and retry behavior.** Single-row operations are not paginated; GET reuses the shared GET-retry policy, writes are never auto-retried (matching every existing connector's "never replay an uncertain write" policy).
**Error handling and concurrency.** 404 on GET/PATCH/DELETE for a nonexistent or security-filtered-out row (Dataverse does not distinguish "doesn't exist" from "exists but you can't see it" at this layer); `DataverseApiException` wraps single-row reads; concurrency handling is DVT-013's.
**Data privacy and security.** Row field values are caller-owned business data; never logged by default.
**Dependencies.** DVT-001, DVT-002, DVT-003.
**Unit-test scenarios.** Create without `writesEnabled` throws before any HTTP call; create recovers the GUID from `OData-EntityId`; create with `return=representation` recovers the GUID from the body instead; update/delete require the opt-in gate; 404 on a missing row surfaces correctly; malformed response body surfaces a contract error, not a silent empty result.
**Contract-test scenarios.** Synthetic fixtures for each of the five documented response shapes (204+header, 201+body, 200+body, 204 bare, 206-style single-property PUT).
**Integration-test requirements.** Authorized sandbox table + disposable test rows; explicit cleanup/reconciliation approach required before any write test runs.
**Acceptance criteria.** All five documented request/response shapes round-trip correctly against synthetic fixtures; no write is sent without explicit opt-in.
**Definition of Done.** Standard template; live sandbox CRUD evidence deferred.
**Known limitations and blockers.** No live Dataverse tenant available this pass; exact security-role-to-privilege mapping for a given tenant is inherently tenant-specific and cannot be verified from documentation alone.

<a id="dvt-005"></a>
### DVT-005 - OData filtering, selection and ordering

**Business purpose.** Let callers retrieve exactly the columns and rows they need rather than every column of every row.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** Follows the exact query-builder pattern `SuccessFactorsClient.QueryAddress`/ADP's `SearchWorkersAsync` filter whitelist already establish.
**Functional requirements.** Support `$select`, `$filter`, `$orderby`, `$expand`, `$apply` (aggregation), `$top`, `$count` [D6]. Explicitly reject (fail locally, not forward to the server) the three documented unsupported standard OData options: `$skip`, `$search`, `$format` [D6] - Dataverse paginates via `$skiptoken`/`odata.maxpagesize` (DVT-006), not `$skip`. Document the specific Dataverse-vs-FetchXml query limitations verified: no joins without a declared relationship, no nested `$expand` on an N:N relationship, no cross-table column comparison inside `$filter` [D6]. Parameter aliases (`@p1=...`) are supported in `$filter`/`$orderby` but **not** in `$expand` [D6] - the query builder must not silently alias an `$expand` clause.
**Nonfunctional requirements.** Query strings built via the same `Uri.EscapeDataString`-based safe-composition pattern every existing connector uses; no raw string interpolation of caller-supplied filter text without escaping.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseQuery(string? Select = null, string? Filter = null, string? OrderBy = null,
    string? Expand = null, string? Apply = null, int? Top = null, bool Count = false);
```
**Official API endpoints, methods and versions (verified).** Query-option support/exclusions per [D6].
**Authentication and authorization.** Unchanged from DVT-004.
**Required API permissions, scopes or licenses.** Column-level security (if configured on the tenant) can hide a selected column's value even when the row itself is readable - this is a genuine, tenant-specific entitlement outcome, not an SDK bug, and must be documented as such rather than treated as a parse failure.
**Pagination, throttling and retry behavior.** Without `$top`, Dataverse returns up to 5,000 standard-table rows or 500 elastic-table rows per request [D6] - this interacts directly with DVT-006's pagination and must be documented together, not assumed to be "all rows."
**Error handling and concurrency.** A `$skip`/`$search`/`$format` request throws `ArgumentException` locally before any HTTP call, mirroring the ADP whitelist-filter precedent.
**Data privacy and security.** `$filter`/`$select` values may themselves be sensitive (they can reference field names implying business meaning); redact query strings from default logging exactly as ADP's `ODataQuery`/SAP's `ODataQuery` already redact `ToString()`.
**Dependencies.** DVT-003, DVT-004.
**Unit-test scenarios.** `$skip`/`$search`/`$format` rejected locally; query-string composition for each supported option matches the documented syntax; a filter string containing special characters round-trips through `Uri.EscapeDataString` correctly.
**Contract-test scenarios.** Synthetic fixtures for a `$select`+`$filter`+`$orderby` combination against a documented example query shape.
**Integration-test requirements.** Authorized sandbox table with column-level security configured, to confirm the "readable row, hidden column" behavior empirically.
**Acceptance criteria.** Every supported option composes correctly; every unsupported option fails locally.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The exact set of entities Dataverse disallows cross-table `$filter` comparisons on is tenant/table-schema-specific and not enumerable from generic documentation.

<a id="dvt-006"></a>
### DVT-006 - Pagination and continuation links

**Business purpose.** Traverse a result set larger than one page without the caller re-implementing Dataverse's specific paging mechanics.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `Page<T>`/`ContinuationToken`/`EndpointSafety.Continuation` reused unchanged (Section 8.1), matching the exact pattern `QualtricsPaging`/SAP SuccessFactors's `EnumerateAsync<T>` already implement for their own `nextPage`/`__next` links.
**Functional requirements.** Honor `Prefer: odata.maxpagesize=N` to request a specific page size, maximum **5,000** for both standard and elastic tables (larger values are documented as ignored by the server, not an error) [D7]. Parse `@odata.nextLink`, which embeds a Dataverse-proprietary, opaque, URL-encoded XML `$skiptoken=<cookie>` - treat it as fully opaque and forward it unmodified via `EndpointSafety.Continuation`, never attempt to decode or reconstruct it [D7]. `$top` and `Prefer: odata.maxpagesize` are mutually exclusive - if both are present, `$top` is documented as ignored by the server [D7]; the connector should reject supplying both locally rather than relying on server-side silent precedence. No `$skip`-based paging exists at all [D6, D7].
**Nonfunctional requirements.** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
**Public C# interfaces/model contracts (proposed).**
```csharp
public IAsyncEnumerable<T> EnumerateAsync<T>(string entitySet, DataverseQuery? query = null, int? maxPageSize = null, CancellationToken ct = default);
public Task<Page<T>> EnumeratePageAsync<T>(string entitySet, DataverseQuery? query = null, ContinuationToken? resumeFrom = null, int? maxPageSize = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `Prefer: odata.maxpagesize`, `@odata.nextLink`/`$skiptoken` per [D7].
**Authentication and authorization.** Unchanged.
**Required API permissions, scopes or licenses.** None beyond the underlying table read privilege.
**Pagination, throttling and retry behavior.** GET pages reuse the shared GET-retry policy; a `$skiptoken` is never retried across a different page boundary than the one it was issued for.
**Error handling and concurrency.** Cycle/max-page-count guard throws `InvalidDataException`, matching every existing connector; an off-origin or malformed `@odata.nextLink` is rejected by `EndpointSafety.Continuation` unchanged.
**Data privacy and security.** The `$skiptoken` is treated as a sensitive opaque value, matching `ContinuationToken`'s existing design intent - never logged.
**Dependencies.** DVT-003, DVT-005.
**Unit-test scenarios.** `Prefer: odata.maxpagesize` honored up to 5,000, values above 5,000 still function (server-ignored, not client-rejected, since the documented behavior is server-side silent capping not an error); simultaneous `$top`+`odata.maxpagesize` rejected locally; off-origin/malformed `@odata.nextLink` rejected; empty page with a non-null `@odata.nextLink` still followed (matching the "empty page with continuation" pattern already tested for SAP/Qualtrics).
**Contract-test scenarios.** Synthetic multi-page fixture with a realistic opaque `$skiptoken` value.
**Integration-test requirements.** Authorized sandbox table with more than 5,000 rows, to observe real pagination - not available this pass.
**Acceptance criteria.** A caller can enumerate an arbitrarily large table via `IAsyncEnumerable<T>` without manually handling `@odata.nextLink`.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact per-tenant/per-table row-count thresholds that trigger multi-page results are not independently verifiable without a live large table.

<a id="dvt-007"></a>
### DVT-007 - Batch and changeset operations

**Business purpose.** Submit multiple create/update/delete operations in one HTTP round trip, with Dataverse's specific atomicity semantics.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `BatchItemResult<T>` (Section 8.2, item 2) reused for the mixed-outcome response shape; no other reuse - the multipart/mixed wire format itself is new, connector-owned code.
**Functional requirements.** `POST {environment}/api/data/v9.2/$batch` with `Content-Type: multipart/mixed; boundary="batch_<id>"`; each top-level part is `Content-Type: application/http` + `Content-Transfer-Encoding: binary` wrapping one HTTP request [D8]. Up to **1,000** individual requests per batch; batches cannot nest [D8]. A **changeset** (`Content-Type: multipart/mixed; boundary="changeset_<id>"`, nested inside the batch body) groups write operations with **all-or-nothing atomicity** - any single failure rolls back the entire changeset; `GET` is not permitted inside a changeset [D8]. Cross-request references within a changeset use `Content-ID` + `$<id>` substitution to let request N reference the not-yet-committed result of request M [D8]. `Prefer: odata.continue-on-error` changes a plain (non-changeset) batch's behavior from "stop at first failure" to "return 200 with per-item success/failure in the response body" [D8]. All line endings inside the multipart body must be CRLF [D8] - a documented, easy-to-get-wrong wire-format detail the SDK must handle internally so callers never construct the multipart body by hand.
**Nonfunctional requirements.** Bounded batch size (reject >1,000 locally before send); cancellation-aware; explicit write opt-in gate on any batch containing a write.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseBatchRequest(HttpMethod Method, string RelativeUrl, JsonElement? Body = null, string? ContentId = null);
public sealed record DataverseChangeset(IReadOnlyList<DataverseBatchRequest> Requests); // all-or-nothing
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
    IReadOnlyList<DataverseBatchRequest> requests, IReadOnlyList<DataverseChangeset>? changesets = null,
    bool continueOnError = false, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `$batch` multipart/mixed shape, 1,000-request limit, changeset atomicity, CRLF requirement - all per [D8].
**Authentication and authorization.** One bearer token covers the whole batch; individual sub-requests are still evaluated against the same security-role entitlements as if sent standalone.
**Required API permissions, scopes or licenses.** Union of whatever privileges the contained sub-requests individually require.
**Pagination, throttling and retry behavior.** A `GET` sub-request inside a non-changeset batch can itself return a paginated result; the batch as a whole is never auto-retried (it may contain writes).
**Error handling and concurrency.** A changeset failure rolls back atomically per the documented semantics - the SDK must surface which sub-request failed and that the whole changeset was rolled back, not report partial success for a changeset.
**Data privacy and security.** Batch bodies may contain business data across many rows at once; never logged by default.
**Dependencies.** DVT-003, DVT-004, DVT-008 (upsert can appear inside a batch).
**Unit-test scenarios.** >1,000 requests rejected locally; a changeset failure reports rollback for every member, not just the failing one; `Content-ID` cross-reference substitution round-trips; CRLF line endings are produced regardless of host OS line-ending defaults; `odata.continue-on-error` surfaces per-item results instead of throwing at first failure.
**Contract-test scenarios.** Synthetic multipart/mixed fixture matching the documented request/response wire format exactly, including a nested changeset.
**Integration-test requirements.** Authorized sandbox with disposable rows for a real changeset-rollback observation.
**Acceptance criteria.** A caller never hand-constructs multipart/mixed text; changeset atomicity and non-changeset partial-failure behavior are both observably correct against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live tenant to observe real rollback timing/behavior this pass.

<a id="dvt-008"></a>
### DVT-008 - Upsert and alternate keys

**Business purpose.** Create-or-update a row keyed by a business-meaningful alternate key instead of Dataverse's internal GUID, avoiding a separate existence-check round trip.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** Follows the exact upsert-gating pattern `SuccessFactorsClient.UpsertAsync`/Qualtrics' `CreateContactAsync` opt-in-write convention already establish.
**Functional requirements.** `PATCH {entityset}(key1=val1,key2=val2)` - creates the row if the alternate-key combination does not exist, updates it if it does [D10]. `If-Match: *` forces update-only (errors if absent, rather than creating); `If-None-Match: *` forces create-only (errors if the key already exists) [D10]. Because the plain success response (204 + `OData-EntityId`) is identical for both outcomes, `Prefer: return=representation` is required to distinguish "a new row was created" (201) from "an existing row was updated" (200) [D10]. Alternate keys are discoverable via the CSDL `$metadata` document's annotations (DVT-009) - the connector must not assume every table has an alternate key, and must surface a clear local error if the caller requests upsert-by-key on a table/key combination it has not verified exists.
**Nonfunctional requirements.** Explicit write opt-in gate, matching every other write operation in this specification.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseUpsertResult(Guid RowId, bool Created);
public async Task<DataverseUpsertResult> UpsertByAlternateKeyAsync(string entitySet,
    IReadOnlyDictionary<string, string> alternateKey, JsonElement fields,
    DataverseUpsertMode mode = DataverseUpsertMode.CreateOrUpdate, bool writesEnabled = false, CancellationToken ct = default);
public enum DataverseUpsertMode { CreateOrUpdate, UpdateOnly, CreateOnly }
```
**Official API endpoints, methods and versions (verified).** Alternate-key PATCH URL syntax, `If-Match`/`If-None-Match` semantics, `return=representation` disambiguation - all per [D10].
**Authentication and authorization.** Unchanged; the caller's security role must cover both Create and Write privilege on the table, since either outcome is possible.
**Required API permissions, scopes or licenses.** Create + Write privilege on the target table.
**Pagination, throttling and retry behavior.** Single-row operation; never auto-retried (it is a write).
**Error handling and concurrency.** `If-Match: *` failing when the key does not exist, or `If-None-Match: *` failing when it does, both surface as documented 4xx failures the connector classifies, not generic contract errors.
**Data privacy and security.** Alternate-key values and row fields are caller-owned business data.
**Dependencies.** DVT-004, DVT-009.
**Unit-test scenarios.** Create-vs-update outcome correctly distinguished via `return=representation` status code; `UpdateOnly`/`CreateOnly` modes send the correct `If-Match`/`If-None-Match` header and surface the correct error when violated; upsert without `writesEnabled` throws before any HTTP call.
**Contract-test scenarios.** Synthetic fixtures for both the 201 (created) and 200 (updated) response shapes.
**Integration-test requirements.** Authorized sandbox table with a real alternate key configured.
**Acceptance criteria.** A caller can distinguish create-from-update outcomes reliably and cannot accidentally upsert against an unverified alternate key.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Which tables have alternate keys configured is entirely tenant-specific.

<a id="dvt-009"></a>
### DVT-009 - Table and column metadata

**Business purpose.** Discover a table's actual current schema (columns, types, keys) rather than assuming a hardcoded shape, mirroring the metadata-driven discipline SAP SuccessFactors's `GetMetadataAsync`/`ParseEntityDescriptors` already establish for a different OData dialect.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** Directly parallels `SuccessFactorsClient.Metadata.cs`'s existing CSDL-parsing pattern (`EntityDescriptor`, size cap, content-hash parse cache, XXE-safe `XmlReaderSettings`) - the same approach is proposed here against Dataverse's own `$metadata`, not a shared implementation (the two CSDL dialects differ enough - EntityDefinitions/Attributes metadata-query entities vs. SAP's plain CSDL - that copying the SAP class verbatim would be wrong, but the *pattern* - size cap, hash-cached reparse, XXE defenses - transfers directly).
**Functional requirements.** Fetch the CSDL metadata document via `GET {environment}/api/data/v9.2/$metadata` (optionally `?annotations=true`, or `Prefer: odata.include-annotations="*"`) [D11]. Separately, query the richer metadata-entity surface: `EntityDefinitions` (e.g. filter by `ChangeTrackingEnabled eq true`) and nested `Attributes` (e.g. `EntityDefinitions(LogicalName='account')/Attributes(LogicalName='...')/Microsoft.Dynamics.CRM.FileAttributeMetadata`) [D11]. Namespace is `Microsoft.Dynamics.CRM` (alias `mscrm`) [D11]. The service document, `GET {environment}/api/data/v9.2/` (no further path), lists every `EntitySet` [D11].
**Nonfunctional requirements.** Same defense-in-depth as SAP SuccessFactors's existing metadata handling: explicit document-size cap checked before parsing, XXE/DTD-disabled `XmlReaderSettings`, content-hash-keyed reparse cache so an unchanged schema is not reparsed on every call.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseEntityDescriptor(string LogicalName, IReadOnlyList<string> Keys, IReadOnlyList<string> Properties);
public Task<XDocument> GetMetadataAsync(CancellationToken ct = default);
public Task<XDocument> RefreshMetadataAsync(CancellationToken ct = default);
public static IReadOnlyList<DataverseEntityDescriptor> ParseEntityDescriptors(XDocument metadata);
public IAsyncEnumerable<JsonDocument> EnumerateEntityDefinitionsAsync(DataverseQuery? query = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `$metadata`, service document, `EntityDefinitions`/`Attributes` - all per [D11].
**Authentication and authorization.** Metadata read generally requires no table-specific privilege beyond a valid session, but the richer `EntityDefinitions` surface can still be restricted on some tenants.
**Required API permissions, scopes or licenses.** None specific beyond a valid token, in the general case.
**Pagination, throttling and retry behavior.** `EntityDefinitions`/`Attributes` enumeration reuses DVT-006's pagination; `$metadata` itself is a single document fetch.
**Error handling and concurrency.** Oversized metadata document rejected before parsing (`InvalidDataException`), matching SAP's existing behavior exactly.
**Data privacy and security.** Metadata/schema is not row-level business data but can reveal customization details some tenants consider sensitive; still excluded from aggressive default logging as a precaution.
**Dependencies.** DVT-003.
**Unit-test scenarios.** CSDL parses correctly independent of exact EDM namespace version; oversized document rejected; XXE/DTD payload rejected; changed-content triggers reparse on `GetMetadataAsync`, `RefreshMetadataAsync` always reparses; `EntityDefinitions` filter-by-`ChangeTrackingEnabled` round-trips.
**Contract-test scenarios.** Synthetic CSDL fixture plus a synthetic `EntityDefinitions` JSON fixture.
**Integration-test requirements.** Authorized sandbox environment's real `$metadata` document, to confirm the parser handles actual current schema size/shape - not available this pass.
**Acceptance criteria.** Entity/key/property discovery works against both a minimal synthetic fixture and (once available) a real tenant's metadata document.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live tenant metadata document available this pass; real-world metadata document size (potentially large for a heavily customized tenant) is unverified against the proposed size cap.

<a id="dvt-010"></a>
### DVT-010 - Lookups and relationships

**Business purpose.** Set, read, and remove relationships between rows (single-valued lookups and collection-valued 1:N/N:N associations).
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific; the `@odata.bind`/`$ref` mechanics are unique to Dataverse's OData dialect among this SDK's connectors.
**Functional requirements.** On create/update, set a single-valued navigation property via `"{navprop}@odata.bind": "/contacts(guid)"`; clear it by setting the bind property (or the plain nav-property name) to `null` [D12, D9]. Generic alternative for an existing row: `PUT {entityset}(guid)/{navprop}/$ref` with body `{"@odata.id": "<absolute URL>"}` to set, `DELETE` the same path to clear [D12]. Collection-valued (1:N and N:N) relationships: `POST {entityset}(guid)/{collectionNavProp}/$ref` with `{"@odata.id": "..."}` to add a member, `DELETE {entityset}(guid)/{collectionNavProp}(otherGuid)/$ref` to remove one [D12]. Retrieved single-valued lookup values surface as `_{navprop}_value`, with `Microsoft.Dynamics.CRM.lookuplogicalname`/`associatednavigationproperty` annotations available only when `Prefer: odata.include-annotations` is sent [D12] - the connector must document that a plain GET does not reveal which table a polymorphic lookup actually points to without that header.
**Nonfunctional requirements.** Explicit write opt-in on bind/associate/disassociate operations.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task BindLookupAsync(string entitySet, Guid id, string navigationProperty, string targetEntitySet, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
public async Task ClearLookupAsync(string entitySet, Guid id, string navigationProperty, bool writesEnabled = false, CancellationToken ct = default);
public async Task AssociateAsync(string entitySet, Guid id, string collectionNavigationProperty, string targetEntitySet, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
public async Task DisassociateAsync(string entitySet, Guid id, string collectionNavigationProperty, Guid targetId, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `@odata.bind`, `$ref` PUT/POST/DELETE forms - all per [D12]/[D9].
**Authentication and authorization.** Associating/disassociating requires the Append/AppendTo privilege on the respective tables, in addition to Write on the primary row.
**Required API permissions, scopes or licenses.** Append (on the row gaining the association) and AppendTo (on the row being associated) privileges.
**Pagination, throttling and retry behavior.** Single operations; never auto-retried.
**Error handling and concurrency.** A bind to a nonexistent target row surfaces as a documented 4xx the connector classifies, not a silent no-op.
**Data privacy and security.** Relationship data can itself reveal sensitive business structure (e.g., which account a confidential opportunity belongs to); not specially redacted beyond the SDK's general row-data policy.
**Dependencies.** DVT-004, DVT-009 (to know which properties are navigation properties).
**Unit-test scenarios.** `@odata.bind` payload shape on create; `$ref` PUT/POST/DELETE for single- and collection-valued cases; clearing a lookup via `null`; operations without `writesEnabled` throw before any HTTP call.
**Contract-test scenarios.** Synthetic fixtures for each of the four documented relationship-mutation shapes.
**Integration-test requirements.** Authorized sandbox with two related, disposable rows.
**Acceptance criteria.** All four relationship-mutation shapes work against synthetic fixtures without the caller hand-building `@odata.bind`/`$ref` payloads.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Polymorphic lookup target-table disclosure requires `Prefer: odata.include-annotations`, which the connector must always send on lookup-bearing reads or document as an opt-in - a design choice, not a vendor limitation, deferred to implementation (Section 28).

<a id="dvt-011"></a>
### DVT-011 - Choice, status and state metadata

**Business purpose.** Resolve the human-readable label behind a Dataverse choice/status/state numeric value without the caller hardcoding a label-to-value mapping that can change per environment/language.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific.
**Functional requirements.** Request `Prefer: odata.include-annotations="OData.Community.Display.V1.FormattedValue"` to receive a `{property}@OData.Community.Display.V1.FormattedValue` sibling property carrying the current label for any choice/status/state column, including `statecode`/`statuscode` [D6, confirmed via the query-overview annotation mechanism]. Enumerate global option sets via `GET {environment}/api/data/v9.2/GlobalOptionSetDefinitions`, filterable by name, e.g. `GlobalOptionSetDefinitions(Name='incident_caseorigincode')` - **this entity-set name is corroborated by search of the official create-update-optionsets documentation page, not independently re-verified by a full-page fetch this session; treat the entity-set name as high-confidence but flag it for re-verification before release** (explicit vendor-research gap, Section 3 carried forward honestly rather than silently upgraded to "verified").
**Nonfunctional requirements.** None beyond standard read behavior.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseOptionSetValue(int Value, string? FormattedLabel);
public IAsyncEnumerable<JsonDocument> EnumerateGlobalOptionSetsAsync(string? filterByName = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `Prefer: odata.include-annotations` formatted-value mechanism confirmed [D6]; `GlobalOptionSetDefinitions` entity-set name is a documented-but-not-independently-refetched fact (see above).
**Authentication and authorization.** Unchanged.
**Required API permissions, scopes or licenses.** None specific.
**Pagination, throttling and retry behavior.** Reuses DVT-006 for `GlobalOptionSetDefinitions` enumeration.
**Error handling and concurrency.** A row read without the annotation `Prefer` header simply omits the formatted-value sibling property - not an error, just absent data; the connector must not treat its absence as a parse failure.
**Data privacy and security.** Choice labels are schema metadata, not row data.
**Dependencies.** DVT-004, DVT-009.
**Unit-test scenarios.** Formatted-value sibling property parsed correctly when present; absent gracefully when the `Prefer` header was not sent; `statecode`/`statuscode` specifically round-trip through the same mechanism as any other choice column.
**Contract-test scenarios.** Synthetic fixture with and without the annotation header.
**Integration-test requirements.** Authorized sandbox table with a real global option set, to confirm the `GlobalOptionSetDefinitions` entity-set name before release.
**Acceptance criteria.** Formatted labels resolve correctly when requested; the global-option-set entity-set name is re-verified against a live tenant or a freshly re-fetched official page before this feature ships as `implemented`.
**Definition of Done.** Standard template; the open vendor-research gap above is a release blocker for the `GlobalOptionSetDefinitions` path specifically, not for the formatted-value mechanism.
**Known limitations and blockers.** `GlobalOptionSetDefinitions` entity-set name needs one more independent confirmation before release (Section 28).

<a id="dvt-012"></a>
### DVT-012 - Change tracking and delta synchronization

**Business purpose.** Extract only rows that changed since a prior checkpoint, instead of re-reading an entire table on every sync.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `ContinuationToken` reused for the opaque delta-link token (Section 8.2, item 3); the mechanism itself (header-triggered, response-embedded delta link) has no Phase 1 precedent - SAP's `SapDeltaCheckpoint` is filter-based, not token-based.
**Functional requirements.** Change tracking must be explicitly enabled per table (`EntityMetadata.ChangeTrackingEnabled`) and is **irreversible once turned on** [D13] - the connector must treat "change tracking not enabled for this table" as a documented, non-retryable configuration state, not a transient error. Request: `GET {entityset}?$select=...` plus `Prefer: odata.track-changes` [D13]. The response carries `@odata.deltaLink` (containing an opaque `$deltatoken=...`); following it returns only changed/created rows, plus deleted rows represented as `{"@odata.context": ".../$deltaEntity#{entityset}/$deletedEntity", "id": "...", "reason": "deleted"}` [D13] - **this is the one Phase 2 mechanism, across all three connectors, that natively reports deletions**, unlike the Phase 1 `Reconciliation.DetectChangesAsync` fallback (Section 8.1), which deliberately never does. `$filter`, `$orderby`, `$expand`, and `$top` are **not supported together with `odata.track-changes`** and the server errors if combined [D13] - reject this combination locally. Default unprocessed-change retention is **7 days**, configurable via `Organization.ExpireChangeTrackingInDays` [D13] - a caller resuming from an older delta token than the retention window must be told explicitly to fall back to a full resync, not receive a silently-wrong partial result.
**Nonfunctional requirements.** Cancellation-aware; the delta token is treated as a sensitive opaque value, never logged.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseDeletedRow(Guid Id);
public sealed record DataverseDeltaPage<T>(IReadOnlyList<T> Changed, IReadOnlyList<DataverseDeletedRow> Deleted, ContinuationToken? DeltaLink);
public Task<DataverseDeltaPage<T>> GetChangesAsync<T>(string entitySet, string? select = null, ContinuationToken? resumeFrom = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `Prefer: odata.track-changes`, `@odata.deltaLink`, deleted-row shape, incompatible query options, 7-day default retention - all per [D13].
**Authentication and authorization.** Unchanged; requires Read privilege on the tracked table.
**Required API permissions, scopes or licenses.** `ChangeTrackingEnabled` must already be turned on for the table by a tenant administrator - this is an irreversible, tenant-level configuration prerequisite the SDK cannot enable itself via the Web API alone (it is a metadata/customization operation, DVT-019's territory, and even there is documented as one-way).
**Pagination, throttling and retry behavior.** A delta response can itself be paginated via the normal `@odata.nextLink` mechanism (DVT-006) before eventually yielding `@odata.deltaLink`; reuses the shared continuation-safety plumbing.
**Error handling and concurrency.** Combining `odata.track-changes` with `$filter`/`$orderby`/`$expand`/`$top` throws `ArgumentException` locally; an expired delta token should classify distinctly (a documented, recoverable-via-full-resync condition) rather than as a generic contract error.
**Data privacy and security.** Delta tokens are opaque and sensitive; deleted-row entries carry only the row `id`, no business data.
**Dependencies.** DVT-003, DVT-005, DVT-006, DVT-009 (to confirm `ChangeTrackingEnabled` before attempting).
**Unit-test scenarios.** Changed-row page parses correctly; deleted-row shape parses correctly and is surfaced distinctly from changed rows; incompatible query-option combination rejected locally; resuming from a `@odata.deltaLink` round-trips without re-enumerating already-seen rows; an expired-token server response classifies as a distinct, documented condition.
**Contract-test scenarios.** Synthetic fixture reproducing the documented changed+deleted mixed response shape.
**Integration-test requirements.** Authorized sandbox table with change tracking enabled and a real deletion performed, to observe the deleted-row shape live - not available this pass.
**Acceptance criteria.** Changed and deleted rows are both surfaced correctly from a synthetic fixture; the incompatible-query-option guard is enforced locally.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live tenant with change tracking enabled available this pass; exact server error shape/code for an expired delta token was not independently fetched this session (Section 28 open question).

<a id="dvt-013"></a>
### DVT-013 - ETags and optimistic concurrency

**Business purpose.** Prevent a caller from silently overwriting a row that changed since it was last read.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific; conceptually parallel to how every existing connector treats a write as never-auto-retried, extended here with an explicit vendor-native concurrency token.
**Functional requirements.** Every retrieved row carries `@odata.etag` [D3, D10]. `If-Match: <etag>` (or `If-Match: *`) on `PATCH`/`DELETE` enforces optimistic concurrency - a mismatch returns `412 Precondition Failed` with documented codes `ConcurrencyVersionMismatch`/`DuplicateRecord` [D10]. `If-None-Match: null` should be sent on every request to defeat caching of expanded collection-valued navigation properties - a documented Dataverse-specific caching quirk, not a generic OData convention [D10]. `If-None-Match: *` on upsert prevents an update, forcing create-only (already covered in DVT-008).
**Nonfunctional requirements.** None beyond explicit opt-in on writes, already covered by DVT-004/008.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task UpdateRowAsync(string entitySet, Guid id, JsonElement fields, string? ifMatchETag = null, bool writesEnabled = false, CancellationToken ct = default);
// A 412 response classifies to a dedicated DataverseConcurrencyException : Koras.Connect.Exceptions.ContractException
// rather than the generic classifier default, since 412 is not currently a mapped status in IntegrationExceptionClassifier.
```
**Official API endpoints, methods and versions (verified).** `@odata.etag`, `If-Match`/`If-None-Match` semantics, `412`/`ConcurrencyVersionMismatch` - all per [D3]/[D10].
**Authentication and authorization.** Unchanged.
**Required API permissions, scopes or licenses.** None beyond Write privilege, already covered by DVT-004.
**Pagination, throttling and retry behavior.** Not applicable; concurrency conflicts are never auto-retried (the caller must re-read and decide).
**Error handling and concurrency.** This is the feature's entire subject: a 412 must be distinguishable from every other 4xx so a caller can implement its own re-read-and-retry policy, rather than treating it as a generic failure.
**Data privacy and security.** ETags are opaque version tokens, not business data.
**Dependencies.** DVT-004, DVT-008.
**Unit-test scenarios.** `If-Match` header sent correctly when supplied; `If-None-Match: null` sent on reads involving expanded navigation properties; a 412 response classifies as the dedicated concurrency exception, not a generic `ContractException`.
**Contract-test scenarios.** Synthetic 412 fixture with the documented `ConcurrencyVersionMismatch` code.
**Integration-test requirements.** Authorized sandbox with two concurrent writers to the same row, to observe a real 412 - not available this pass.
**Acceptance criteria.** A 412 is classified distinctly and the caller is never left unable to tell a concurrency conflict apart from a generic contract failure.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The shared `IntegrationExceptionClassifier` does not currently map 412 to anything specific (its default arm returns `ContractException`) - whether to extend the shared classifier or wrap purely in the Dataverse connector is an open architecture decision (Section 28), leaning toward connector-local wrapping per Section 8's "no abstraction creep for one vendor" principle, since 412-as-concurrency-conflict is not yet proven needed by Salesforce or Graph.

<a id="dvt-014"></a>
### DVT-014 - Service protection limits and throttling

**Business purpose.** Respect Dataverse's documented per-user rate limits so the SDK itself never causes a tenant-wide lockout, and surface a throttled response as a recoverable, typed condition.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException(RetryAfter)` mapping reused unchanged (Section 8.1); `TransportOptions`'s existing bounded-backoff/jitter GET-retry policy reused unchanged.
**Functional requirements.** Document, without hardcoding as a guaranteed SLA, the three independent service-protection facets Microsoft publishes as current defaults, each evaluated per web server per authenticated user over a 5-minute sliding window [D2]: (1) **6,000 requests** per 300 seconds (error `0x80072322`); (2) **1,200,000 ms (20 minutes) combined execution time** per 300 seconds (error `0x80072321`); (3) **52 (or higher) concurrent requests** (error `0x80072326`) [D2]. A breach returns `429 Too Many Requests` with a `Retry-After` header in seconds [D2]. Debug-only headers `x-ms-ratelimit-burst-remaining-xrm-requests`/`x-ms-ratelimit-time-remaining-xrm-requests` exist but are explicitly documented as **not** to be used for throttling decision logic [D2] - the connector must not build retry logic around them. Limits do not apply to plug-in/sandbox-originated calls, but the execution time of triggered server-side logic counts against the *originating* request [D2]. The `$batch` 1,000-request cap (DVT-007) is a separate limit from entitlement/license-based quotas - batching is explicitly documented as not a way to evade license-based limits [D2]. **Microsoft explicitly states these numeric defaults "can change and might vary between environments"** [D2] - the SDK must treat them as documentary context for callers, never as compile-time constants gating behavior.
**Nonfunctional requirements.** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged; writes are never auto-retried on 429, matching the universal "never replay an uncertain write" policy.
**Public C# interfaces/model contracts (proposed).** None new - fully covered by the existing `ThrottleException`/`TransportOptions`.
**Official API endpoints, methods and versions (verified).** 429 + `Retry-After`, the three numeric defaults, debug-header caveat - all per [D2].
**Authentication and authorization.** Not applicable.
**Required API permissions, scopes or licenses.** None.
**Pagination, throttling and retry behavior.** This feature *is* the throttling/retry behavior for the connector; see Functional requirements.
**Error handling and concurrency.** 429 classifies via the unchanged shared classifier.
**Data privacy and security.** Not applicable.
**Dependencies.** DVT-003.
**Unit-test scenarios.** 429 + `Retry-After` classifies as `ThrottleException` with the correct `RetryAfter` value; an excessive `Retry-After` beyond `TransportOptions.MaxDelay` fails fast rather than waiting (matching the existing SAP/Qualtrics-proven pattern); a malformed `Retry-After` falls back to bounded backoff rather than hanging; writes are never retried on 429.
**Contract-test scenarios.** Synthetic 429 fixtures with valid, excessive, and malformed `Retry-After` values.
**Integration-test requirements.** Deliberately triggering a real 6,000-request/300s breach against a live tenant is both infeasible and inadvisable for a documentation-only test harness; not attempted.
**Acceptance criteria.** 429 responses are classified and retried (GET) or surfaced without retry (writes) correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The three numeric defaults are Microsoft's own documented defaults, explicitly subject to per-environment variation - do not hardcode them as assertions against a live tenant's actual behavior.

<a id="dvt-015"></a>
### DVT-015 - Actions and functions

**Business purpose.** Invoke Dataverse's custom and built-in server-side business logic (actions) and computed queries (functions) beyond plain CRUD.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** Parallels ADP's explicit-allowlist `AdpOperationCatalog` discipline (never invent an operation name) and QLT-007's "verify shape before implementing" precedent.
**Functional requirements.** Unbound action: `POST {environment}/api/data/v9.2/{ActionName}` with a JSON body of named parameters (documented example: `Merge`) [D14]. Bound-to-entity action: `POST .../{entityset}(guid)/Microsoft.Dynamics.CRM.{ActionName}` - must use the fully-qualified name including namespace [D14]. Bound-to-collection action: `POST .../{entityset}/Microsoft.Dynamics.CRM.{ActionName}` [D14]. Entity-typed parameters with an ambiguous base type require an explicit `"@odata.type": "Microsoft.Dynamics.CRM.{logicalname}"` annotation in the body [D14]. Functions use `GET` with parameter-alias syntax `Fn(Param=@p1)?@p1=...` to stay under the 260-character URL-segment limit (DVT-003) - the companion official page for functions specifically was referenced consistently across fetched pages but not independently re-fetched this session (vendor-research gap, carried forward honestly).
**Nonfunctional requirements.** Explicit allowlist of action/function names the connector exposes typed wrappers for - never dynamically invoke an arbitrary caller-supplied name without the same `EndpointSafety.Segment`-style validation every other path segment gets.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<JsonDocument> InvokeUnboundActionAsync(string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> InvokeBoundActionAsync(string entitySet, Guid id, string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> InvokeFunctionAsync(string functionName, IReadOnlyDictionary<string,string> parameters, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Action/function invocation syntax per [D14]; the functions-specific companion page was not independently re-fetched this session.
**Authentication and authorization.** Each action/function has its own, potentially distinct, security-privilege requirement - entirely action-specific and not enumerable generically.
**Required API permissions, scopes or licenses.** Action/function-specific; unknown until a specific action is targeted and its tenant-side privilege requirement confirmed.
**Pagination, throttling and retry behavior.** Actions (writes) are never auto-retried; functions (reads via GET) reuse the shared GET-retry policy.
**Error handling and concurrency.** An action name not recognized by the tenant returns a documented 4xx the connector classifies, not a crash.
**Data privacy and security.** Action/function parameters may be sensitive business data.
**Dependencies.** DVT-003, DVT-004.
**Unit-test scenarios.** Unbound/bound action URL composition matches the documented fully-qualified-name requirement; `@odata.type` annotation included when the parameter type is ambiguous; function parameter-alias URL composition stays under the 260-character segment limit.
**Contract-test scenarios.** Synthetic fixture for the documented `Merge` action example.
**Integration-test requirements.** Authorized sandbox with a real custom action configured - not available this pass, and inherently tenant-specific even when available.
**Acceptance criteria.** The three documented invocation shapes (unbound action, bound action, function) compose correctly.
**Definition of Done.** Standard template; per Section 28, no action/function beyond the documented examples is implemented as a named typed wrapper until its tenant-specific contract is independently verified - mirroring ADP-021's "no invented operation name" discipline exactly.
**Known limitations and blockers.** The functions-specific official page was not independently re-fetched this session; which custom actions exist on any given tenant is entirely tenant-specific and cannot be enumerated from generic documentation.

<a id="dvt-016"></a>
### DVT-016 - File and image columns

**Business purpose.** Upload and download binary content stored directly in a Dataverse file/image column (not a related Note/attachment row).
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationTransport.CopyToAsync` reused for the download-streaming primitive; the chunked-upload session concept has no Phase 1 precedent (closest analog is ADP's event-notification polling, a different domain).
**Functional requirements.** Single-request upload/download for files **under 128 MB** [D15]: `PATCH {entityset}(guid)/{filecolumn}` with `Content-Type: application/octet-stream` + `x-ms-file-name: <name>` header and raw bytes as the body; download via `GET .../{filecolumn}/$value`, returning `x-ms-file-size`/`x-ms-file-name`/`mimetype` response headers [D15]. Chunked upload: initiate with `PATCH .../{filecolumn}?x-ms-file-name=<name>` + `x-ms-transfer-mode: chunked`, receiving a `Location` header (with a `sessiontoken`) and a recommended `x-ms-chunk-size` (4 MiB) [D15]; subsequent `PATCH` requests to that `Location` carry `Content-Range: bytes <start>-<end>/<total>`, returning `206 Partial Content` until the final chunk returns `204` [D15]. Chunked download uses the `Range` header against `.../{filecolumn}/$value`, with the server responding `206` and `x-ms-chunk-size` [D15]. An action-based alternative exists for both SDK and Web API: `InitializeFileBlocksUpload` → repeated `UploadBlock` (Base64 `BlockData`, ≤4 MB/block) → `CommitFileBlocksUpload`; mirrored by `InitializeFileBlocksDownload`/`DownloadBlock` for download; `DeleteFile` action or a plain `DELETE` on the column URL removes content [D15]. Column max size is exposed via `FileAttributeMetadata.MaxSizeInKB` (DVT-009's metadata surface) [D15]. On-premises/BYOK environments may disable chunking entirely (`IsChunkingSupported=false`) [D15] - the connector must check this rather than assume chunking is always available.
**Nonfunctional requirements.** Streamed upload/download (no whole-file buffering for large files); cancellation-aware mid-transfer; explicit write opt-in on upload/delete.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task UploadFileColumnAsync(string entitySet, Guid id, string column, Stream content, string fileName, bool writesEnabled = false, CancellationToken ct = default);
public async Task DownloadFileColumnAsync(string entitySet, Guid id, string column, Stream destination, CancellationToken ct = default);
public async Task DeleteFileColumnAsync(string entitySet, Guid id, string column, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Single-request and chunked upload/download shapes, action-based alternative, `IsChunkingSupported` caveat - all per [D15].
**Authentication and authorization.** Write privilege on the table for upload/delete; Read for download.
**Required API permissions, scopes or licenses.** Standard table-level security privileges; `FileAttributeMetadata.MaxSizeInKB` is a tenant-configured column limit, not a license gate.
**Pagination, throttling and retry behavior.** Chunked transfer is its own sequencing mechanism, not reusing `Page<T>`; chunk-level retry is a connector-local concern (a failed chunk PATCH is not automatically retried by the shared GET-only transport policy, since PATCH is a write).
**Error handling and concurrency.** A file exceeding `MaxSizeInKB` should be rejected locally once metadata is known, rather than only discovered via a server error.
**Data privacy and security.** File content is caller-owned, potentially highly sensitive binary data - never buffered into a log or diagnostic payload.
**Dependencies.** DVT-004, DVT-009.
**Unit-test scenarios.** Small-file single-request upload/download round-trips; chunked upload sequencing (session initiation, sequential chunk PATCH, final-chunk 204) matches the documented protocol; chunked download `Range`/`206` handling matches; interrupted mid-transfer preserves partial bytes without corrupting the destination stream (matching the existing Qualtrics/SAP "partial bytes" test pattern).
**Contract-test scenarios.** Synthetic fixtures for both single-request and chunked transfer shapes.
**Integration-test requirements.** Authorized sandbox table with a real file column, including a file large enough to require chunking - not available this pass.
**Acceptance criteria.** Both transfer modes round-trip correctly against synthetic fixtures; a file exceeding the known column limit is rejected before any upload is attempted.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live tenant with a file column available this pass; exact behavior when `IsChunkingSupported=false` is encountered mid-transfer (vs. known in advance) was not independently tested.

<a id="dvt-017"></a>
### DVT-017 - Attachments and notes

**Business purpose.** Read and write the legacy `annotation` (Note) entity's attached-file content, distinct from DVT-016's native file-column mechanism.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific; reuses DVT-004's generic CRUD for the `annotation` entity set itself.
**Functional requirements.** The `annotation` entity carries `documentbody` (a Base64-encoded string of the attached file content), `filename`, `mimetype`, `isdocument` - corroborated via search of the official attachment-annotation-files documentation and the `annotation` entity reference page, but **not independently verified via a full page fetch this session** (explicit vendor-research gap, carried forward honestly) [vendor research]. The legacy `activitymimeattachment.body` field serves the equivalent role specifically for email/activity attachments [vendor research].
**Nonfunctional requirements.** Base64 encode/decode handled transparently by the typed model; explicit write opt-in on create.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class DataverseAnnotation
{
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? DocumentBodyBase64 { get; set; } // caller decodes/encodes explicitly; the SDK never auto-decodes into a Stream
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
```
**Official API endpoints, methods and versions (verified).** Uses DVT-004's generic CRUD against the `annotations` entity set; the `documentbody`/`filename`/`mimetype` field names are documented but not independently re-fetched this session.
**Authentication and authorization.** Standard table-level Create/Read/Write/Delete privilege on `annotation`.
**Required API permissions, scopes or licenses.** Standard security-role privilege; no distinct license requirement documented.
**Pagination, throttling and retry behavior.** Reuses DVT-004/DVT-006 unchanged.
**Error handling and concurrency.** Standard CRUD error classification; no annotation-specific behavior documented beyond the generic entity CRUD contract.
**Data privacy and security.** `documentbody` can carry arbitrarily sensitive attached-file content; never logged or included in diagnostics by default.
**Dependencies.** DVT-004.
**Unit-test scenarios.** `DataverseAnnotation` round-trips `documentbody`/`filename`/`mimetype`/`isdocument` without data loss; unknown fields preserved via `AdditionalData`.
**Contract-test scenarios.** Synthetic `annotation` row fixture.
**Integration-test requirements.** Authorized sandbox with a real annotation row - not available this pass; field-name facts should be re-verified against a live tenant or a full documentation fetch before this feature ships as `implemented`.
**Acceptance criteria.** The documented field names round-trip correctly in a synthetic fixture, with the open gap explicitly recorded, not silently resolved.
**Definition of Done.** Standard template; the field-name verification gap above is a release blocker for calling this feature `implemented` with `verification: verified`.
**Known limitations and blockers.** `documentbody`/`filename`/`mimetype`/`isdocument` field names need one more independent confirmation (full-page fetch or live metadata query) before release (Section 28).

<a id="dvt-018"></a>
### DVT-018 - Dynamics 365 specialization

**Business purpose.** Document that Dynamics 365 first-party app tables (`account`, `contact`, `opportunity`, `lead`, `task`, etc.) are reached through exactly the same generic Dataverse mechanism as any custom table - there is no separate "Dynamics 365 API."
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None Dataverse-specific; this feature is primarily a documentation/typed-convenience-wrapper feature over DVT-004 through DVT-013, not a new protocol.
**Functional requirements.** Provide optional, pre-built `DataverseQuery`/typed-model convenience wrappers for the handful of first-party Dynamics 365 Sales tables most hosts actually touch (`account`, `contact`, `opportunity`) as thin, documented pass-throughs over the already-specified generic CRUD/query surface - mirroring exactly how ADP-012/SAP-010's "thin typed wrapper over an already-verified primitive" pattern works, not a new API family. **No official page draws a crisp architectural line between "core Dataverse tables" and "Dynamics-365-app-specific tables" beyond the shared `EntitySet`/entity-type-reference mechanism** - this is confirmed by consistent documentation usage across every fetched Dataverse page (DVT-004's `CreateRowAsync`/DVT-010's `AssociateDisassociateTableRows` examples all use `account`/`contact` as illustrative generic-mechanism examples), not an explicit policy statement, and is recorded here as such rather than invented.
**Nonfunctional requirements.** None beyond the underlying CRUD/query features'.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class DataverseAccount { public string? Name { get; set; } /* ... */ public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<DataverseAccount> EnumerateAccountsAsync(DataverseQuery? query = null, CancellationToken ct = default);
// Equivalent thin wrappers proposed for contact/opportunity; exact field lists require metadata verification (DVT-009)
// against a live tenant before any field beyond the documented examples is hardcoded into a typed model.
```
**Official API endpoints, methods and versions (verified).** Uses the generic `EntitySet` mechanism (DVT-004) unchanged; `account`/`contact`/`opportunity` are the documented illustrative examples across multiple fetched pages.
**Authentication and authorization.** Identical to any other table (DVT-004).
**Required API permissions, scopes or licenses.** Standard security-role privilege on the specific table; Dynamics 365 Sales app licensing is a separate, tenant-side concern the SDK does not manage or infer.
**Pagination, throttling and retry behavior.** Unchanged from DVT-006/014.
**Error handling and concurrency.** Unchanged from DVT-004.
**Data privacy and security.** Account/contact/opportunity data is frequently customer PII/commercially sensitive - no special handling beyond the SDK's general row-data policy, which already treats all row data as sensitive by default.
**Dependencies.** DVT-004, DVT-005, DVT-009, DVT-021.
**Unit-test scenarios.** Typed wrapper composes the identical generic query/CRUD calls a hand-written caller would; unknown fields preserved via `AdditionalData`.
**Contract-test scenarios.** Synthetic fixtures using the documented `account`/`contact` example shapes from DVT-004/DVT-010's own fetched pages.
**Integration-test requirements.** Authorized sandbox with a real Dynamics 365 Sales app provisioned - not available this pass.
**Acceptance criteria.** The typed convenience wrappers are proven to be pure pass-throughs with no invented field beyond what DVT-009's metadata discovery confirms.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact field lists for `account`/`contact`/`opportunity` must come from DVT-009's live metadata discovery, not from this document's necessarily-illustrative examples; no field beyond `Name`-style documented examples should be hardcoded until independently confirmed.

<a id="dvt-019"></a>
### DVT-019 - Solution and customization metadata

**Business purpose.** Discover which customization solution/publisher a component belongs to, and attach newly created components to a specific unmanaged solution during development/ALM workflows.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P2. **Milestone.** P2-D7.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** Every solution has a **Publisher** with a customization prefix applied to every component's schema name to avoid cross-solution naming collisions (e.g. `new_`, `contoso_`) - corroborated by search of the official solution-concepts-ALM documentation, not independently fetched in full this session [vendor research gap]. The Web API exposes a `solutions` entity set (directly confirmed: the fetched actions-and-functions page uses `POST .../solutions/Microsoft.Dynamics.CRM.ExportTranslation` as a worked example) [D14]. The header `MSCRM.SolutionUniqueName` attaches a newly created component to a specific unmanaged solution rather than the tenant's default solution - directly confirmed on the headers table of the compose-HTTP-requests page [D3]. Solution import/export action signatures (`ImportSolution`/`ExportSolution`) were **not independently fetched this session** [vendor research gap].
**Nonfunctional requirements.** None beyond standard metadata-read behavior.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<Guid> CreateRowInSolutionAsync(string entitySet, JsonElement fields, string solutionUniqueName, bool writesEnabled = false, CancellationToken ct = default);
// Sends the MSCRM.SolutionUniqueName header alongside DVT-004's CreateRowAsync.
```
**Official API endpoints, methods and versions (verified).** `MSCRM.SolutionUniqueName` header [D3]; `solutions` entity set existence [D14]; publisher/prefix concept and import/export actions not independently re-verified this session.
**Authentication and authorization.** Solution-component operations typically require System Customizer/System Administrator-level security roles, not ordinary table-level privilege.
**Required API permissions, scopes or licenses.** Elevated customization-level security role; this is a genuinely higher entitlement bar than any other Dataverse feature in this catalog and should be documented as such to callers.
**Pagination, throttling and retry behavior.** Reuses DVT-004/006 unchanged for the `solutions` entity set itself.
**Error handling and concurrency.** Unchanged generic classification.
**Data privacy and security.** Solution/publisher metadata is not row-level business data.
**Dependencies.** DVT-004, DVT-009.
**Unit-test scenarios.** `MSCRM.SolutionUniqueName` header sent correctly on a create request; `solutions` entity set CRUD via the generic DVT-004 primitives.
**Contract-test scenarios.** Synthetic fixture for a header-qualified create request.
**Integration-test requirements.** Authorized sandbox with System Customizer privilege and a real unmanaged solution - not available this pass.
**Acceptance criteria.** The `MSCRM.SolutionUniqueName` header mechanism works against a synthetic fixture; import/export actions are explicitly deferred, not guessed.
**Definition of Done.** Standard template; `ImportSolution`/`ExportSolution` action signatures remain a documented open gap (Section 28), not implemented until independently verified.
**Known limitations and blockers.** Publisher/prefix mechanics and solution import/export action signatures were not independently re-fetched this session.

<a id="dvt-020"></a>
### DVT-020 - Virtual table capability discovery

**Business purpose.** Detect when a table is a virtual table (backed by an external data source rather than native Dataverse storage) so a caller does not assume full OData feature parity (writes, certain query options) that virtual tables may not support.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P2. **Milestone.** P2-D7.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** A virtual table is composed of a data provider, a data-source row, and the virtual-table definition itself; built-in providers include SQL Server, SharePoint, Fabric, and a generic OData v4 provider - from the Web API's perspective a virtual table still appears as an ordinary `EntitySet` [vendor research, architecture confirmed via search, not independently fetched in full this session]. **Exact CRUD/query-option constraints specific to virtual tables (e.g., which OData features are unsupported) were not independently fetched this session** - this is a genuine, explicitly flagged gap, not an assumed "works exactly like a native table" claim.
**Nonfunctional requirements.** None beyond standard metadata-read behavior.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseVirtualTableInfo(string EntityLogicalName, bool IsVirtual, string? DataProviderName);
public Task<DataverseVirtualTableInfo> GetVirtualTableInfoAsync(string entitySet, CancellationToken ct = default); // reads from EntityDefinitions (DVT-009)
```
**Official API endpoints, methods and versions (verified).** Builds on DVT-009's `EntityDefinitions` metadata surface; virtual-table-specific capability constraints are an open vendor-research gap.
**Authentication and authorization.** Unchanged from DVT-009.
**Required API permissions, scopes or licenses.** None beyond standard metadata-read.
**Pagination, throttling and retry behavior.** Unchanged from DVT-006/009.
**Error handling and concurrency.** A write attempted against a read-only virtual table should surface the server's own documented rejection rather than the SDK silently assuming the write will succeed - but the exact rejection shape is part of the open gap above.
**Data privacy and security.** Not applicable; metadata only.
**Dependencies.** DVT-009.
**Unit-test scenarios.** `EntityDefinitions` parsing correctly identifies a virtual-table flag/provider name from a synthetic fixture matching the documented metadata shape, once that exact metadata field name is confirmed (currently an open gap - tests cannot be written against an unverified field name without risking asserting a guessed shape).
**Contract-test scenarios.** Deferred until the metadata field name is independently confirmed.
**Integration-test requirements.** Authorized sandbox with a real virtual table configured - not available this pass.
**Acceptance criteria.** Deferred - this feature cannot be marked beyond "Not started"/"Planned" until the specific metadata field distinguishing a virtual table is independently verified.
**Definition of Done.** Not applicable until the vendor-research gap closes; do not implement a guessed field name.
**Known limitations and blockers.** Exact virtual-table capability constraints and the specific metadata field distinguishing a virtual table were not independently fetched this session - this is the clearest Dataverse instance of "deliberately deferred over risk of inventing an unverified contract," the same category QLT-007/SAP-007 occupied before their own resolutions elsewhere in this SDK's history (Section 28).

<a id="dvt-021"></a>
### DVT-021 - Typed models and mappings

**Business purpose.** Provide extension-data-preserving typed C# models for the Dataverse rows/metadata this specification covers, and neutral `IRecordMapper` projections, matching every Phase 1 connector's established discipline.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2.
**Current implementation status.** Not started.
**Repository evidence.** Directly parallels `Koras.Connect.Abstractions.IRecordMapper<TSource,TTarget>`/`MappingResult<TTarget>` (reused unchanged) and the `[JsonExtensionData]`-bag convention every existing typed DTO (`Worker`, `PerPerson`, `QualtricsContact`) already follows.
**Functional requirements.** No official first-party OpenAPI/Swagger document exists for the Dataverse Web API [vendor research, confirmed via search]; the only official codegen path is `pac modelbuilder build` (Power Platform CLI) for the *organization-service* (SDK-for-.NET) side, the documented successor to the legacy `CrmSvcUtil.exe` - **neither targets the Web API/JSON contract this connector uses**, so the connector's typed models are hand-written against the CSDL `$metadata` document (DVT-009) and the documented row-shape examples, not generated from an official Web-API-specific tool. Every typed model preserves unknown fields via a `[JsonExtensionData]` dictionary, matching every existing Phase 1 DTO.
**Nonfunctional requirements.** Models are plain POCOs with `System.Text.Json` attributes, matching the existing connectors' style exactly (no third-party serialization library).
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record DataverseRecordProjection(Guid Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class DataverseRowMapper<TTarget> : IRecordMapper<JsonElement, TTarget> { /* caller-supplied projection delegate, mirroring PerPersonMapper's explicit field-by-field approach rather than reflection-based binding */ }
```
**Official API endpoints, methods and versions (verified).** No official Web-API-specific codegen tool exists [vendor research]; `pac modelbuilder build`/`CrmSvcUtil.exe` target the organization-service SDK, a distinct surface.
**Authentication and authorization.** Not applicable (pure data-shape concern).
**Required API permissions, scopes or licenses.** None.
**Pagination, throttling and retry behavior.** Not applicable.
**Error handling and concurrency.** A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws, matching every existing mapper.
**Data privacy and security.** Mapped/omitted/error states never leak raw field values into log output by default.
**Dependencies.** DVT-004, DVT-009.
**Unit-test scenarios.** A record missing a required identifier field is `Omitted`, not mapped with a null/default key (matching `PerPersonMapper`'s precedent exactly); unknown fields preserved via extension data; an unparseable date-like field surfaces as a field-level error, not a silent null (matching `SapDates`'s policy).
**Contract-test scenarios.** Golden-fixture tests against the documented `account`/`contact` example row shapes.
**Integration-test requirements.** None beyond the underlying CRUD/metadata features'.
**Acceptance criteria.** Golden mappings are deterministic; omitted/error fields are visible; original source data remains intact and inspectable.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No official Web-API-specific typed-client generator exists to validate against; hand-written models carry the same inherent "must be re-verified against live metadata before release" caveat every field-name claim in this catalog carries.

<a id="dvt-022"></a>
### DVT-022 - Contract tests, diagnostics and examples

**Business purpose.** Prove the Dataverse connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every Phase 1 connector's established testing/sample discipline exactly.
**Owning package.** `Koras.Connect.Dataverse`.
**Priority.** P0. **Milestone.** P2-D2 (unit/contract tests) / P2-D6 (sample, package validation).
**Current implementation status.** Not started.
**Repository evidence.** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline, credential-free console-sample convention exactly; also requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Dataverse.snapshot.txt` entry wired into the existing `ApiSnapshotTests.cs` reflection-based baseline (SDK-017).
**Functional requirements.** Offline unit/contract tests for every DVT-001 through DVT-021 P0/P1 feature, using a synthetic `HttpMessageHandler`, no live network access, running on both Linux and Windows via the existing CI matrix; at least one standalone `samples/Dataverse.*` console project demonstrating authentication → CRUD → query → pagination → metadata, entirely offline, matching `samples/SuccessFactors.PersonAndEmploymentSync`'s structure; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity stages with explicit Unknown/Skipped/Failed/Passed outcomes, never issuing a mutating request, matching every existing connector's diagnostic precedent exactly.
**Nonfunctional requirements.** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
**Public C# interfaces/model contracts (proposed).** `DataverseConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what DVT-001 through DVT-021 already define.
**Official API endpoints, methods and versions (verified).** `DiagnoseAsync`'s connectivity stage uses DVT-002's `WhoAmI` function as its one permitted minimal, non-mutating probe call.
**Authentication and authorization.** `DiagnoseAsync`'s Authentication stage issues exactly one minimal read (`WhoAmI`), never a mutating request, matching the Qualtrics/ADP connector-diagnostic precedent exactly.
**Required API permissions, scopes or licenses.** None beyond whatever the underlying probed feature already requires.
**Pagination, throttling and retry behavior.** Not applicable to this feature itself.
**Error handling and concurrency.** `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary, matching the shared `ConnectionDiagnostic` contract exactly.
**Data privacy and security.** Diagnostic output contains no secrets, tokens, or row payloads, matching `ConnectionDiagnostic`'s existing design intent.
**Dependencies.** Every other DVT-0NN feature this covers tests for.
**Unit-test scenarios.** Every unit-test scenario enumerated in DVT-001 through DVT-021 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed (matching the Qualtrics precedent: `DiagnoseAsyncSkipsAuthenticationStageWhenConfigurationIsInvalid`-style behavior).
**Contract-test scenarios.** Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus per the Phase 1 master's existing testing-strategy template.
**Integration-test requirements.** Every feature's integration-test requirement above remains individually deferred/blocked pending a live sandbox; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live tenant.
**Acceptance criteria.** The sample project builds and runs on a clean checkout with `dotnet build`/`dotnet run` and performs zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed.
**Definition of Done.** Matches the Phase 1 master's Section 26 Definition of Done template exactly: implemented public contract, numbered acceptance cases, deterministic unit/contract tests, applicable security/cancellation/resource checks, package consumer example, current official schema citation for every external operation claimed, required sandbox outcomes recorded where available (none yet), revision-linked CI artifacts, reviewed docs/limitations.
**Known limitations and blockers.** No live Dataverse sandbox credentials are available in this documentation pass; every integration-test requirement listed in DVT-001 through DVT-021 remains explicitly blocked, not silently skipped, pending an authorized tenant (Section 28).
## 11. Salesforce Feature Matrix

**Research method and confidence levels (read before trusting any field below).** Direct `WebFetch` against `developer.salesforce.com` and `help.salesforce.com` returned **HTTP 403 on every attempt** (bot-blocked) during this research pass. Two source tiers were used instead, and every fact below is tagged accordingly: **[PDF]** = directly read from the official Salesforce-hosted PDF `https://resources.docs.salesforce.com/latest/latest/en-us/sfdc/pdf/api_rest.pdf` ("REST API Developer Guide," **v68.0, Winter '27**, last updated 2026-10-09) or its companion SOQL/SOSL PDF - high confidence. **[snippet]** = WebSearch result snippets quoting the blocked official HTML pages, consistent across independent queries but not independently read in full - medium confidence, flagged per-item for a human re-check before being treated as a binding contract. Facts with no official corroboration at all are marked **[unverified]** and are not used to justify any proposed C# signature beyond a placeholder. Current REST API version: **v68.0** [PDF].

| ID | Feature | Priority | Milestone | Status |
|---|---|---|---|---|
| [SFD-001](#sfd-001) | OAuth authentication | P0 | P2-D3 | Not started |
| [SFD-002](#sfd-002) | Instance discovery and API versioning | P0 | P2-D3 | Not started |
| [SFD-003](#sfd-003) | REST API client | P0 | P2-D3 | Not started |
| [SFD-004](#sfd-004) | sObject CRUD | P0 | P2-D3 | Not started |
| [SFD-005](#sfd-005) | SOQL | P0 | P2-D3 | Not started |
| [SFD-006](#sfd-006) | Query pagination | P0 | P2-D3 | Not started |
| [SFD-007](#sfd-007) | Describe and metadata | P0 | P2-D3 | Not started |
| [SFD-008](#sfd-008) | External IDs and upsert | P0 | P2-D3 | Not started |
| [SFD-009](#sfd-009) | Composite API | P0 | P2-D3 | Not started |
| [SFD-010](#sfd-010) | Bulk API 2.0 ingest | P1 | P2-D5 | Not started |
| [SFD-011](#sfd-011) | Bulk API 2.0 query | P1 | P2-D5 | Not started |
| [SFD-012](#sfd-012) | Change Data Capture | P1 | P2-D5 | Not started |
| [SFD-013](#sfd-013) | Platform Events | P1 | P2-D5 | Not started |
| [SFD-014](#sfd-014) | Pub/Sub API | P1 | P2-D5 | Not started |
| [SFD-015](#sfd-015) | Relationships and references | P0 | P2-D3 | Not started |
| [SFD-016](#sfd-016) | Files and ContentVersion | P1 | P2-D5 | Not started |
| [SFD-017](#sfd-017) | Limits API and throttling | P0 | P2-D3 | Not started |
| [SFD-018](#sfd-018) | Concurrency and conditional requests | P0 | P2-D3 | Not started |
| [SFD-019](#sfd-019) | Error normalization and partial successes | P0 | P2-D3 | Not started |
| [SFD-020](#sfd-020) | Typed CRM/custom object models | P0 | P2-D3 | Not started |
| [SFD-021](#sfd-021) | Incremental extraction and checkpoints | P1 | P2-D5 | Not started |
| [SFD-022](#sfd-022) | Contract tests, diagnostics and examples | P0 | P2-D3/P2-D6 | Not started |

## 12. Salesforce Detailed Feature Specifications

<a id="sfd-001"></a>
### SFD-001 - OAuth authentication

**Business purpose.** Authenticate against a Salesforce org as either an unattended service (Client Credentials) or on behalf of a signed-in user, without the SDK generating or persisting long-lived secrets.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`RedactedCredential<T>` unchanged where the grant type matches; the JWT Bearer flow (certificate-based, no interactive step, closest to Salesforce's recommended unattended pattern) is new connector-owned code, mirroring DVT-001's certificate-assertion decision.
**Functional requirements.** Token endpoint: `POST https://{host}/services/oauth2/token`, where `{host}` is `login.salesforce.com`, `test.salesforce.com` (sandbox), or the org's My Domain host `https://{MyDomainName}.my.salesforce.com` [PDF p.6, snippet for host variants]. Support, in priority order: (1) Client Credentials flow for server-to-server; (2) JWT Bearer flow (certificate-signed assertion) for unattended access without a stored secret; (3) a caller-supplied delegated token source (authorization-code flow is explicitly out of scope for the SDK to implement itself - it requires an interactive browser redirect). **[PDF, directly confirmed, p.6]**: "Creating connected apps is restricted as of Spring '26... we recommend using **External Client Apps (ECA)** instead" - the connector's documentation must default callers toward ECA, with legacy Connected App support retained since many existing orgs still use it. The token response includes `access_token` and `instance_url` together (SFD-002) - **[snippet]**, consistent with long-standing, uncontroversial Salesforce behavior but not independently re-read this pass.
**Nonfunctional requirements.** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware; secret/certificate never persisted by the SDK.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceClientCredentialsOptions(Uri TokenEndpoint, string ClientId, string ClientSecret) : IValidatableOptions;
public sealed record SalesforceJwtBearerOptions(Uri TokenEndpoint, string ClientId, string Subject,
    Func<CancellationToken, ValueTask<X509Certificate2>> CertificateSource) : IValidatableOptions;
```
**Official API endpoints, methods and versions (verified).** `/services/oauth2/token` [PDF p.6]; ECA guidance directly confirmed [PDF p.6]; specific flow parameter names (`grant_type=client_credentials` etc.) are standard OAuth2 and not independently re-verified against the Salesforce-specific page this pass.
**Authentication and authorization.** Client Credentials / JWT Bearer are both app-only; no interactive browser step.
**Required API permissions, scopes or licenses.** An External Client App (or legacy Connected App) with OAuth scopes including `api` and, for JWT Bearer, a pre-registered certificate and a pre-authorized running-user; "Username-Password" flow is explicitly being retired per vendor guidance and is not implemented.
**Pagination, throttling and retry behavior.** Not applicable to token acquisition; reuses `ClientCredentialsTokenSource`'s cached-with-expiry-margin behavior for the Client Credentials case.
**Error handling and concurrency.** Non-2xx token response classifies as `AuthenticationException`; concurrent callers share one in-flight acquisition.
**Data privacy and security.** Client secret/certificate never logged; `RedactedCredential<T>` reused.
**Dependencies.** None (first Salesforce feature).
**Unit-test scenarios.** Expired cache triggers reacquisition; JWT assertion correctly signed and time-bounded; invalid certificate rejected before any network call; non-2xx response classifies as `AuthenticationException`.
**Contract-test scenarios.** Synthetic token-endpoint fixture for both Client Credentials and JWT Bearer grant shapes.
**Integration-test requirements.** Requires a real Salesforce org with an authorized External Client App/Connected App; not available this pass.
**Acceptance criteria.** A caller obtains a bearer token via Client Credentials or JWT Bearer with no interactive prompt.
**Definition of Done.** Standard template; live sandbox gate explicitly deferred (Section 28).
**Known limitations and blockers.** Authorization-code/device flows are deliberately out of scope for SDK-managed acquisition (require a browser); exact JWT claim set was snippet-only, not independently re-verified - a release blocker for calling this `implemented`/`verified` rather than `implemented`/`partial`.

<a id="sfd-002"></a>
### SFD-002 - Instance discovery and API versioning

**Business purpose.** Resolve which Salesforce instance/API version to call after authenticating, since `instance_url` is dynamic per org and the REST API itself is versioned.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `EndpointSafety.ValidateRoot` reused unchanged for the resolved `instance_url`.
**Functional requirements.** `instance_url` is returned in the OAuth token JSON response alongside `access_token` **[snippet, long-standing uncontroversial behavior, not independently re-read this pass]**. `GET /services/data/` requires **no authentication** and returns a JSON array of `{label, url, version}` per available API version (e.g. `{"label":"Summer '14","url":"/services/data/v31.0","version":"31.0"}`); `/services/data/vXX.X/` (no trailing resource) itself, once a version is chosen, returns the map of named resources (SFD-003) **[PDF p.30/138, directly confirmed]**. Current version at research time: **v68.0** [PDF cover page]. The connector targets a configurable version with `v68.0` as the default, isolated to one constant.
**Nonfunctional requirements.** Cancellation-aware; bounded timeout.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceApiVersion(string Label, string Url, string Version);
public Task<IReadOnlyList<SalesforceApiVersion>> ListApiVersionsAsync(CancellationToken ct = default); // unauthenticated
```
**Official API endpoints, methods and versions (verified).** `GET /services/data/` per [PDF p.30].
**Authentication and authorization.** Version discovery itself requires no token; every subsequent call uses the bearer token + resolved `instance_url`.
**Required API permissions, scopes or licenses.** None for version discovery.
**Pagination, throttling and retry behavior.** Single read, not paginated.
**Error handling and concurrency.** Unreachable/malformed host surfaces as a transport failure, not a silent empty list.
**Data privacy and security.** `instance_url` is not itself sensitive but is treated with the same redaction discipline as any configuration value.
**Dependencies.** SFD-001.
**Unit-test scenarios.** Version list parses correctly; unauthenticated call sends no `Authorization` header; a non-existent/unreachable host surfaces a transport exception.
**Contract-test scenarios.** Synthetic fixture matching the documented version-list shape.
**Integration-test requirements.** None beyond SFD-001's.
**Acceptance criteria.** A caller can discover available API versions without a token, and every subsequent call correctly targets the resolved `instance_url` + chosen version.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond SFD-001's.

<a id="sfd-003"></a>
### SFD-003 - REST API client

**Business purpose.** Provide the single HTTP client foundation every other Salesforce feature builds on.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged (Section 8.1).
**Functional requirements.** Base path `{instance_url}/services/data/v{XX.X}/`; `GET` on exactly that path ("Resources by Version") returns a JSON map of resource name → relative URI, confirmed resource keys include `sobjects`, `tooling`, `query`, `queryAll`, `search`, `chatter`, `composite`, `limits`, `theme` [PDF p.38, directly confirmed]. Every request: `Authorization: Bearer {token}`, `Content-Type: application/json` (XML also officially supported but out of scope - this SDK is JSON-only, matching every other Phase 1/2 connector's convention).
**Nonfunctional requirements.** net10.0/Linux/Windows; HTTPS-only root.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceOptions(Uri InstanceRoot, string ApiVersion = "v68.0") : IValidatableOptions;
public sealed partial class SalesforceClient { public SalesforceClient(IntegrationTransport transport, SalesforceOptions options); }
```
**Official API endpoints, methods and versions (verified).** Resource-map shape per [PDF p.38].
**Authentication and authorization.** Delegates to SFD-001's token source via the shared `IRequestAuthenticator`.
**Required API permissions, scopes or licenses.** None beyond SFD-001.
**Pagination, throttling and retry behavior.** Delegated to SFD-006/SFD-017.
**Error handling and concurrency.** `SalesforceApiException.FromTransportFailure` wraps single-resource reads, matching the ADP/SAP/Qualtrics/Dataverse read-vs-write split (Section 6.1).
**Data privacy and security.** No credentials or record data logged by the client skeleton.
**Dependencies.** SFD-001, SFD-002.
**Unit-test scenarios.** Required headers present on every request; resource-map parsing matches the documented shape; non-HTTPS root rejected at construction.
**Contract-test scenarios.** Synthetic fixture for the resource-map response.
**Integration-test requirements.** None beyond SFD-002's.
**Acceptance criteria.** Every Salesforce request carries the correct headers against the resolved instance root and API version.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None.

<a id="sfd-004"></a>
### SFD-004 - sObject CRUD

**Business purpose.** Create, read, update and delete individual Salesforce records (sObjects).
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** Follows the same CRUD-wrapping pattern as DVT-004, adapted to Salesforce's distinct response shapes.
**Functional requirements.** `POST /services/data/vXX.X/sobjects/{sObject}/` creates; response body `{"id":"...","errors":[],"success":true}` [PDF pp.42-47, directly confirmed]. `GET /services/data/vXX.X/sobjects/{sObject}/{id}/` reads, with an optional `fields` query param and support for `If-Match`/`If-None-Match`/`If-Modified-Since`/`If-Unmodified-Since` [PDF, directly confirmed]. `PATCH /services/data/vXX.X/sobjects/{sObject}/{id}/` updates, returning `204 No Content` on success. `DELETE /services/data/vXX.X/sobjects/{sObject}/{id}/` deletes. Documented status-code table (directly confirmed, verbatim): **200** (GET/HEAD/some PATCH), **201** (POST/some PATCH), **204** (DELETE/some PATCH), **300** (ambiguous external-ID match), **304**, **400**, **401**, **403**, **404** [PDF pp.144-154].
**Nonfunctional requirements.** Cancellation-aware; explicit write opt-in gate on create/update/delete, matching every other connector's convention.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<string> CreateRecordAsync(string sObject, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> GetRecordAsync(string sObject, string id, IReadOnlyList<string>? fields = null, CancellationToken ct = default);
public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public async Task DeleteRecordAsync(string sObject, string id, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** All four verbs and the status-code table are directly confirmed [PDF pp.42-47, 144-154].
**Authentication and authorization.** Bearer token from SFD-001; the specific object + operation must additionally be covered by the running user's object/field-level security (profile/permission set) - a 403 here is a genuine entitlement denial.
**Required API permissions, scopes or licenses.** Object-level CRUD permission and field-level security on the target sObject/fields.
**Pagination, throttling and retry behavior.** Single-record operations are not paginated; GET reuses the shared GET-retry policy; writes are never auto-retried.
**Error handling and concurrency.** 404 on a missing/inaccessible record; 300 on an ambiguous external-ID upsert target (SFD-008); `SalesforceApiException` wraps single-record reads.
**Data privacy and security.** Record field values are caller-owned business data; never logged by default.
**Dependencies.** SFD-001, SFD-002, SFD-003.
**Unit-test scenarios.** Create without `writesEnabled` throws before any HTTP call; create parses `id`/`success`/`errors`; 404 on a missing record surfaces correctly; 300 on an ambiguous match is distinguished from a generic failure.
**Contract-test scenarios.** Synthetic fixtures for each documented status-code case.
**Integration-test requirements.** Authorized sandbox org + disposable test records; not available this pass.
**Acceptance criteria.** All documented request/response shapes round-trip correctly against synthetic fixtures; no write is sent without explicit opt-in.
**Definition of Done.** Standard template; live sandbox CRUD evidence deferred.
**Known limitations and blockers.** No live Salesforce org available this pass; a standalone POST `Location` response header was referenced only inside Composite subrequest examples in the fetched PDF, not for a plain top-level POST - not assumed present until independently confirmed (Section 28).

<a id="sfd-005"></a>
### SFD-005 - SOQL

**Business purpose.** Query Salesforce records with a SQL-like language supporting filtering, relationships, and aggregation.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** Follows the same safe-query-composition discipline as DVT-005/every existing connector's filter builder.
**Functional requirements.** `GET /services/data/vXX.X/query?q={SOQL}` (URL-encoded `q` parameter); sibling `GET /services/data/vXX.X/queryAll?q=...` additionally includes soft-deleted/archived records [PDF/SOQL-SOSL PDF, directly confirmed]. Syntax: `SELECT fieldList[,subquery] FROM object [WHERE ...] [GROUP BY ...] [ORDER BY ...] [LIMIT n] [OFFSET n]` [directly confirmed]. Child-to-parent traversal via dot notation (`SELECT Id, Account.Name FROM Contact`), up to 5 levels, max 55 relationships per query; parent-to-child via a subquery naming the plural relationship (`SELECT Name, (SELECT LastName FROM Contacts) FROM Account`), up to 5 levels (v58.0+), max 20 parent-to-child relationships per query [directly confirmed in the SOQL/SOSL PDF].
**Nonfunctional requirements.** Query strings built via a safe-composition helper, never raw string interpolation of caller-supplied filter text without escaping, matching every existing connector's discipline.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<SalesforceQueryResult<T>> QueryAsync<T>(string soql, bool includeDeleted = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/query`, `/queryAll`, SOQL syntax and relationship-traversal limits - all directly confirmed in the fetched PDFs.
**Authentication and authorization.** Standard object/field-level security applies per queried field.
**Required API permissions, scopes or licenses.** Read access on every queried object/field.
**Pagination, throttling and retry behavior.** Delegated to SFD-006.
**Error handling and concurrency.** Malformed SOQL returns a documented 400 with an `errorCode`, classified via SFD-019's shared error shape.
**Data privacy and security.** SOQL text itself may reveal sensitive filter/field semantics; redacted from default logging exactly as every other connector's query-string composition.
**Dependencies.** SFD-003.
**Unit-test scenarios.** Query-string composition escapes special characters correctly; child-to-parent/parent-to-child syntax composes as documented; `queryAll` sets the correct resource path.
**Contract-test scenarios.** Synthetic fixtures matching the documented query-result shape, including a relationship subquery.
**Integration-test requirements.** Authorized sandbox org with related records; not available this pass.
**Acceptance criteria.** SOQL composition and execution round-trip correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-org caveat.

<a id="sfd-006"></a>
### SFD-006 - Query pagination

**Business purpose.** Traverse a SOQL result set larger than one page without the caller re-implementing Salesforce's specific `nextRecordsUrl` mechanics.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `Page<T>`/`ContinuationToken`/`EndpointSafety.Continuation` reused unchanged (Section 8.1).
**Functional requirements.** Query response fields: `totalSize`, `done`, `records[]`, and `nextRecordsUrl` (present only when `done=false`), with the pattern `/services/data/vXX.X/query/{queryLocator}` [PDF, directly confirmed]. Default batch size **2,000** records; the `Sforce-Query-Options: batchSize=N` request header overrides it, confirmed bounds **min 200 / max 2,000**, with the documentation explicitly stating "no guarantee the requested size is the actual size" [PDF, directly confirmed]. The Global Describe response separately reports an org-level `maxBatchSize` (sample value 200 seen in documentation) whose relationship to the per-request `Sforce-Query-Options` override was **not reconciled in any fetched text** - this is an open, explicitly flagged gap, not resolved by assumption.
**Nonfunctional requirements.** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
**Public C# interfaces/model contracts (proposed).**
```csharp
public IAsyncEnumerable<T> EnumerateQueryAsync<T>(string soql, int? batchSize = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `nextRecordsUrl`/`queryLocator`, `Sforce-Query-Options` header and bounds - directly confirmed [PDF].
**Authentication and authorization.** Unchanged from SFD-005.
**Required API permissions, scopes or licenses.** None beyond SFD-005's.
**Pagination, throttling and retry behavior.** Each page is a GET, reusing the shared GET-retry policy; `queryLocator` is treated as opaque and forwarded unmodified via `EndpointSafety.Continuation`.
**Error handling and concurrency.** Cycle/max-page-count guard throws `InvalidDataException`; a `batchSize` outside [200, 2000] is rejected locally before send.
**Data privacy and security.** `queryLocator` treated as a sensitive opaque value, never logged.
**Dependencies.** SFD-005.
**Unit-test scenarios.** `batchSize` outside documented bounds rejected locally; multi-page enumeration follows `nextRecordsUrl` correctly; an empty page with `done=false` is still followed (matching every existing connector's "empty page with continuation" test pattern).
**Contract-test scenarios.** Synthetic multi-page fixture.
**Integration-test requirements.** Authorized sandbox org with more than 2,000 matching records; not available this pass.
**Acceptance criteria.** A caller can enumerate an arbitrarily large query result via `IAsyncEnumerable<T>` without manually handling `nextRecordsUrl`.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The relationship between the org-level `maxBatchSize` (from Global Describe) and the per-request `Sforce-Query-Options` override was not reconciled in any fetched official text (Section 28 open question).

<a id="sfd-007"></a>
### SFD-007 - Describe and metadata

**Business purpose.** Discover an sObject's actual current schema (fields, types, permissions) rather than assuming a hardcoded shape.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** Parallels DVT-009/SAP SuccessFactors's metadata-driven discipline, adapted to Salesforce's JSON (not CSDL/XML) describe format.
**Functional requirements.** Object describe: `GET /services/data/vXX.X/sobjects/{sObject}/describe/`, supporting `If-Modified-Since`/`If-Unmodified-Since` with a `304` response when unchanged [PDF, directly confirmed]. Global describe: `GET /services/data/vXX.X/sobjects/` → `{encoding, maxBatchSize, sobjects[]}` [PDF, directly confirmed]. `sobjects[]` entry fields (directly confirmed, verbatim): `name, label, custom, keyPrefix, updateable, searchable, labelPlural, layoutable, activateable, urls, createable, customSetting, deletable, deprecatedAndHidden, feedEnabled, mergeable, queryable, replicateable, retrieveable, undeletable, triggerable`. Per-field metadata (directly confirmed, verbatim): `name, label, type, length, nillable, createable, updateable, picklistValues, referenceTo, relationshipName, custom, unique, externalId, defaultValue, calculated, precision, scale, soapType, filterable, sortable, autoNumber, idLookup`.
**Nonfunctional requirements.** Content-hash-keyed reparse cache so an unchanged schema is not reparsed on every call, matching the Dataverse/SAP metadata pattern.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceFieldDescriptor(string Name, string Type, bool Createable, bool Updateable, bool Nillable,
    bool Unique, bool ExternalId, IReadOnlyList<string>? ReferenceTo, string? RelationshipName);
public sealed record SalesforceObjectDescriptor(string Name, string Label, bool Createable, bool Updateable, bool Deletable,
    bool Queryable, string? KeyPrefix, IReadOnlyList<SalesforceFieldDescriptor> Fields);
public Task<SalesforceObjectDescriptor> DescribeObjectAsync(string sObject, CancellationToken ct = default);
public Task<IReadOnlyList<SalesforceObjectDescriptor>> DescribeGlobalAsync(CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Both describe endpoints and every listed field name are directly confirmed in the fetched PDF.
**Authentication and authorization.** Describe generally requires no object-specific data permission beyond read access to metadata, but field-level security can still hide specific field metadata on some orgs.
**Required API permissions, scopes or licenses.** None beyond a valid session.
**Pagination, throttling and retry behavior.** Single-document fetches, not paginated; reuses the shared GET-retry policy and the `304`-on-unchanged caching behavior.
**Error handling and concurrency.** Oversized/malformed describe payload rejected before parsing, matching the Dataverse/SAP metadata-size-cap pattern.
**Data privacy and security.** Metadata is schema information, not row-level business data, but still excluded from aggressive default logging.
**Dependencies.** SFD-003.
**Unit-test scenarios.** Object/global describe parse correctly against the documented field list; a `304 Not Modified` response is handled as "unchanged," not a parse failure; an unknown/removed field in a future describe response is preserved, not dropped.
**Contract-test scenarios.** Synthetic describe fixtures for a representative standard object and a representative custom object (`__c` suffix).
**Integration-test requirements.** Authorized sandbox org's real describe response; not available this pass.
**Acceptance criteria.** Field/permission discovery works against both a minimal synthetic fixture and (once available) a real org's describe response.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live org describe response available this pass.

<a id="sfd-008"></a>
### SFD-008 - External IDs and upsert

**Business purpose.** Create-or-update a record keyed by a business-meaningful external ID field instead of the Salesforce-internal 18-character record ID.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** Parallels DVT-008/SAP SuccessFactors's upsert-gating pattern.
**Functional requirements.** `PATCH /services/data/vXX.X/sobjects/{sObject}/{fieldName}/{fieldValue}` where `{fieldName}` must be a custom field flagged as an External ID; optional `updateOnly` parameter restricts to update-only semantics [PDF, directly confirmed]. Permissions: External ID + Unique together need no special permission; External ID without Unique requires "View All Data" [PDF, directly confirmed]. Response status: **201** = record inserted, **204** = record updated, **300** = duplicate external-ID value (response body contains the list of matching records) [PDF, directly confirmed].
**Nonfunctional requirements.** Explicit write opt-in gate, matching every other write operation in this specification.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceUpsertResult(string? RecordId, bool Created, IReadOnlyList<string>? AmbiguousMatchIds);
public async Task<SalesforceUpsertResult> UpsertByExternalIdAsync(string sObject, string externalIdField, string externalIdValue,
    JsonElement fields, bool updateOnly = false, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Upsert URL syntax, permission rules, and all three status codes - directly confirmed [PDF].
**Authentication and authorization.** Create + Update object permission; field-level security on the External ID field.
**Required API permissions, scopes or licenses.** "View All Data" specifically required only for an External-ID-without-Unique field, per the documented rule above.
**Pagination, throttling and retry behavior.** Single-record operation; never auto-retried (it is a write).
**Error handling and concurrency.** A 300 ambiguous-match response is surfaced distinctly (with the matching-record list), not treated as a generic failure.
**Data privacy and security.** External ID values and record fields are caller-owned business data.
**Dependencies.** SFD-004, SFD-007.
**Unit-test scenarios.** Create-vs-update outcome distinguished by status code; 300 ambiguous-match parses the matching-record list; upsert without `writesEnabled` throws before any HTTP call; `updateOnly` sends the documented query parameter.
**Contract-test scenarios.** Synthetic fixtures for all three documented response shapes (201/204/300).
**Integration-test requirements.** Authorized sandbox org with a real External ID field configured; not available this pass.
**Acceptance criteria.** All three documented outcomes are distinguished correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Which fields are flagged as External ID on any given org is entirely org-specific.

<a id="sfd-009"></a>
### SFD-009 - Composite API

**Business purpose.** Submit multiple dependent or independent operations in one HTTP round trip, trading off cross-referencing capability against batch size depending on which Composite variant is used.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `BatchItemResult<T>` (Section 8.2, item 2) reused for the mixed-outcome `/composite/batch` response shape; the multi-request envelope itself is new, connector-owned code.
**Functional requirements. [snippet-only for exact field names below - not independently read from a live page; flagged accordingly, not treated as fully verified.]** `POST /composite`: body `{allOrNone, compositeRequest:[{method,url,referenceId,body}]}`, response `{compositeResponse:[{body,httpHeaders,httpStatusCode,referenceId}]}`, supporting `@{referenceId.field}` cross-referencing between subrequests, **max 25 subrequests**, counted as **1 API call** [snippet]. `POST /composite/batch`: `{batchRequests:[{method,url,richInput}]}` → `{hasErrors, results:[{statusCode,result}]}`, **no cross-referencing** between subrequests, also max 25 subrequests [snippet, field names not independently re-verified]. `POST /composite/graph`: `{graphs:[{graphId, compositeRequest:[...]}]}`, with limits **max 75 graphs/payload, max 500 total nodes/payload, max 500 nodes/single graph, max depth 15, max 15 distinct node types/payload, stops after 14 graph failures** [snippet].
**Nonfunctional requirements.** Bounded subrequest-count validation locally (reject >25 for `/composite` and `/composite/batch`, >75 graphs for `/composite/graph`) before send, even though the exact field names carry only snippet-level confidence - the numeric limits are consistently repeated across independent queries and are treated as higher-confidence than the field-name spelling.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceCompositeSubrequest(HttpMethod Method, string Url, string ReferenceId, JsonElement? Body = null);
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteCompositeAsync(
    IReadOnlyList<SalesforceCompositeSubrequest> requests, bool allOrNone = false, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** All three Composite variants and their numeric limits are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/{requests-composite,using-composite-resources,resources-composite-graph,resources-composite-graph-limits}.html` - a direct re-fetch is required before this feature ships as `verified`.
**Authentication and authorization.** Each subrequest is evaluated against the same object/field-level security as if sent standalone.
**Required API permissions, scopes or licenses.** Union of whatever the contained subrequests individually require.
**Pagination, throttling and retry behavior.** The whole Composite call counts as a single API call against the daily limit (SFD-017); never auto-retried as a whole (it may contain writes).
**Error handling and concurrency.** `/composite` with `allOrNone=true` rolls back every subrequest on any failure; `/composite/batch` always reports partial results via `hasErrors`/per-item `statusCode`, with the outer HTTP status remaining 200 regardless (SFD-019).
**Data privacy and security.** Composite bodies may contain business data across many records at once; never logged by default.
**Dependencies.** SFD-003, SFD-004.
**Unit-test scenarios.** >25 subrequests rejected locally for `/composite`/`/composite/batch`; `@{referenceId.field}` cross-reference substitution round-trips for `/composite`; `allOrNone` rollback vs. `/composite/batch`'s always-partial behavior are both exercised.
**Contract-test scenarios.** Synthetic fixtures for all three Composite variants, explicitly labeled as built from snippet-confidence field names pending a direct re-fetch.
**Integration-test requirements.** Authorized sandbox org; not available this pass.
**Acceptance criteria.** The three Composite shapes compose correctly against synthetic fixtures; a human re-verifies the exact field spelling against a live page or direct fetch before this feature is marked `verified` rather than `partial`.
**Definition of Done.** Standard template; field-name re-verification is an explicit release blocker (Section 28).
**Known limitations and blockers.** Exact JSON field names for all three Composite variants are snippet-only, not independently read from the official page this pass - the single largest confidence gap in the Salesforce catalog's P0 feature set.

<a id="sfd-010"></a>
### SFD-010 - Bulk API 2.0 ingest

**Business purpose.** Insert, update, upsert, or delete very large record volumes efficiently via CSV-based asynchronous jobs, instead of one-record-at-a-time REST calls.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None Phase-1-reusable; this is a fundamentally different (async job + CSV) protocol from every existing connector's synchronous-request model.
**Functional requirements.** Create job: `POST /services/data/vXX.X/jobs/ingest` with `object`, `operation` (`insert`/`update`/`upsert`/`delete`/`hardDelete`), optional `externalIdFieldName` (required for upsert), `contentType`, `lineEnding`, `columnDelimiter` [snippet]. Upload: `PUT /services/data/vXX.X/jobs/ingest/{jobId}/batches`, `Content-Type: text/csv`, raw CSV body [snippet]. Close: `PATCH /services/data/vXX.X/jobs/ingest/{jobId}` with `{"state":"UploadComplete"}` (or `"Aborted"` to cancel) [snippet]. Job states: `Open → UploadComplete → InProgress → JobComplete/Failed/Aborted` [snippet]. Results: `GET .../jobs/ingest/{jobId}/successfulResults|failedResults|unprocessedrecords` (CSV) [snippet]. Limits, with one item independently corroborated via a help.salesforce.com article (id 000382576): **15,000 batches per rolling 24 hours** (shared with legacy Bulk API 1.0) **[corroborated]**; **150 MB file size cap per job**, **max 10,000 records per batch**, theoretical **150,000,000 records/24h** ceiling **[snippet, not independently corroborated]**.
**Nonfunctional requirements.** Streamed CSV upload (no whole-file buffering); cancellation-aware polling for job completion; explicit write opt-in gate.
**Public C# interfaces/model contracts (proposed).**
```csharp
public enum SalesforceBulkIngestOperation { Insert, Update, Upsert, Delete, HardDelete }
public sealed record SalesforceBulkJobHandle(string JobId, SalesforceBulkIngestOperation Operation);
public async Task<SalesforceBulkJobHandle> CreateIngestJobAsync(string sObject, SalesforceBulkIngestOperation operation,
    string? externalIdFieldName = null, bool writesEnabled = false, CancellationToken ct = default);
public async Task UploadIngestBatchAsync(SalesforceBulkJobHandle job, Stream csv, bool writesEnabled = false, CancellationToken ct = default);
public async Task CloseIngestJobAsync(SalesforceBulkJobHandle job, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** All endpoint shapes above are snippet-sourced from `developer.salesforce.com/docs/platform/api-asynch/guide/{close-job,bulk-api-2-job-states,bulk-api-2-0-ingest}.html`; the 15,000-batches/24h figure is independently corroborated via a help.salesforce.com article, the remaining numeric limits are not.
**Authentication and authorization.** Same bearer token as REST; object-level Create/Update/Delete permission per the chosen operation.
**Required API permissions, scopes or licenses.** Bulk API must be enabled on the org edition (SFD-022); standard object permissions for the chosen operation.
**Pagination, throttling and retry behavior.** Job-state polling, not request pagination; failed/unprocessed result retrieval is itself a CSV stream, not a JSON page.
**Error handling and concurrency.** A job in `Failed` state surfaces distinctly from `JobComplete`-with-some-failed-records (the latter requires checking `failedResults` even though the job itself "completed").
**Data privacy and security.** CSV payloads carry business data in bulk; never logged by default; streamed, not buffered, to bound memory.
**Dependencies.** SFD-003, SFD-007 (to validate field names before an expensive bulk job).
**Unit-test scenarios.** Job creation/upload/close URL and body composition match the documented shapes; state-machine transitions handled correctly (including `Aborted`); a job reporting `JobComplete` with nonzero `failedResults` is not treated as fully successful.
**Contract-test scenarios.** Synthetic fixtures for the full job lifecycle, explicitly labeled snippet-confidence pending direct re-fetch.
**Integration-test requirements.** Authorized sandbox org with Bulk API enabled; not available this pass.
**Acceptance criteria.** The full create→upload→close→poll→results lifecycle round-trips against synthetic fixtures.
**Definition of Done.** Standard template; exact batch-count-vs-filesize interaction (whether a single `/batches` PUT is itself capped at 10,000 rows independent of the 150MB cap) remains an explicit open gap (Section 28).
**Known limitations and blockers.** Most of this feature's numeric limits and field names are snippet-only; re-verify before marking `verified`.

<a id="sfd-011"></a>
### SFD-011 - Bulk API 2.0 query

**Business purpose.** Extract very large query result sets asynchronously as CSV, avoiding the per-page overhead of SFD-006's synchronous REST pagination for bulk extraction workloads.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None Phase-1-reusable; shares SFD-010's async-job pattern.
**Functional requirements.** Create job: `POST /services/data/vXX.X/jobs/query` with `{"operation":"query"|"queryAll","query":"<SOQL>"}` (`queryAll` includes deleted/archived rows) [snippet]. Status: `GET /services/data/vXX.X/jobs/query/{jobId}` [snippet]. Results: `GET .../jobs/query/{jobId}/results` → CSV; response header `Sforce-Locator` (literal string `"null"` signals no more pages); subsequent pages pass `locator` and `maxRecords` query parameters [snippet].
**Nonfunctional requirements.** Streamed CSV download; cancellation-aware polling; bounded max-pages guard matching every other connector's pagination safety net.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<SalesforceBulkJobHandle> CreateQueryJobAsync(string soql, bool includeDeleted = false, CancellationToken ct = default);
public async IAsyncEnumerable<Stream> EnumerateQueryResultsAsync(SalesforceBulkJobHandle job, int? maxRecords = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** All endpoint shapes above are snippet-sourced from `developer.salesforce.com/docs/platform/api-asynch/guide/{query-get-job-results,query-bulk-api-2-0}.html`; not independently re-fetched.
**Authentication and authorization.** Read access on every queried object/field.
**Required API permissions, scopes or licenses.** Bulk API enabled on the org edition.
**Pagination, throttling and retry behavior.** `Sforce-Locator`-based paging, treated as opaque via `ContinuationToken` (Section 8.2, item 3) since it is a genuinely opaque server-issued cursor, matching Dataverse's `$deltatoken` reuse justification.
**Error handling and concurrency.** A job in `Failed` state surfaces distinctly; `Sforce-Locator: "null"` (the literal string, not an absent header) must be checked explicitly to avoid an off-by-one infinite-loop bug.
**Data privacy and security.** Query results stream CSV business data; never buffered into logs.
**Dependencies.** SFD-005.
**Unit-test scenarios.** `Sforce-Locator` literal-string-`"null"` termination handled correctly (not confused with an actually-absent header); `maxRecords` default behavior documented as unconfirmed (commonly cited as 10,000 elsewhere but not confirmed official - the test suite must not assert an unconfirmed default).
**Contract-test scenarios.** Synthetic multi-page fixture using the documented locator mechanism.
**Integration-test requirements.** Authorized sandbox org with a large result set; not available this pass.
**Acceptance criteria.** Multi-page CSV result retrieval round-trips correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Default `maxRecords` value is unconfirmed; do not hardcode an assumed default into tests or documentation as if verified.

<a id="sfd-012"></a>
### SFD-012 - Change Data Capture

**Business purpose.** Receive a durable, ordered stream of create/update/delete/undelete events for subscribed objects, for near-real-time downstream synchronization.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None; this is an event-streaming capability with no Phase 1 analog.
**Functional requirements.** Two transports exist: legacy CometD/Bayeux Streaming API channels (`/data/ChangeEvents`, `/data/{Object}ChangeEvent`, `/data/{Custom}__ChangeEvent`, or a custom channel `/data/{Channel}__chn`), and the Pub/Sub API (SFD-014) - **[snippet]** describes CometD as "not formally deprecated" relative to Pub/Sub, but this framing itself was not independently confirmed and should not be stated as settled vendor guidance. `ChangeEventHeader` fields (snippet, from the Apex reference page): `entityName, changeType (CREATE/UPDATE/DELETE/UNDELETE), changeOrigin, transactionKey, commitTimestamp, recordIds, changedFields, nulledFields`. Supported editions: Enterprise, Unlimited, Performance, Developer [snippet]. CometD concurrent-client caps (snippet): Performance/Unlimited 2,000; Enterprise 1,000; Developer 20.
**Nonfunctional requirements.** Long-lived connection management (CometD long-polling or Pub/Sub gRPC stream, per SFD-014's transport decision - Section 6/ADR-P201); reconnect-with-replay-id resilience.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceChangeEvent(string EntityName, string ChangeType, string TransactionKey,
    DateTimeOffset CommitTimestamp, IReadOnlyList<string> RecordIds, IReadOnlyList<string>? ChangedFields);
public IAsyncEnumerable<SalesforceChangeEvent> SubscribeToChangeEventsAsync(string channel, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Channel naming conventions and `ChangeEventHeader` field list are snippet-sourced from `developer.salesforce.com/docs/platform/change-data-capture/guide/{cdc-subscribe-channels,cdc-event-fields-header,cdc-allocations,cdc-intro}.html` and the Apex reference `ChangeEventHeader` class page; not independently re-fetched.
**Authentication and authorization.** Subscriber needs "API Enabled" permission and CDC enabled for the specific object; CometD uses the same bearer token as REST over a different (Bayeux) protocol.
**Required API permissions, scopes or licenses.** CDC enablement is per-object, admin-configured, and edition-gated (SFD-022); free tier limited to 5 objects per the researched snippet.
**Pagination, throttling and retry behavior.** Replay-ID-based resume on reconnect (mechanism details not independently verified this pass - flagged for the same treatment as Dataverse's delta-token-expiry open question).
**Error handling and concurrency.** Connection drop requires reconnect-with-last-known-replay-ID, not a full resync, assuming the replay window has not expired - exact replay-window duration not independently confirmed this pass.
**Data privacy and security.** Change events carry record IDs and changed-field names, and depending on configuration can carry field values - treated as business data, never logged by default.
**Dependencies.** SFD-003, SFD-014 (shares transport-decision context).
**Unit-test scenarios.** `ChangeEventHeader` parsing matches the documented field list; reconnect-with-replay-ID resumes without duplicate delivery assumptions beyond what's independently confirmed.
**Contract-test scenarios.** Synthetic CDC event fixture matching the documented shape, explicitly labeled snippet-confidence.
**Integration-test requirements.** Authorized sandbox org with CDC enabled on a test object; not available this pass.
**Acceptance criteria.** Event parsing and reconnect logic work against synthetic fixtures; the CometD-vs-Pub/Sub transport choice is documented as a connector configuration option, not hardcoded.
**Definition of Done.** Standard template.
**Known limitations and blockers.** CometD deprecation status, exact replay-window duration, and per-edition event-volume limits are all snippet-only or partially corroborated - re-verify before release (Section 28).

<a id="sfd-013"></a>
### SFD-013 - Platform Events

**Business purpose.** Publish and subscribe to custom, schema-defined business events (distinct from CDC's automatic record-change events).
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None; shares SFD-012's event-streaming foundation.
**Functional requirements.** Custom Platform Events are defined in Setup/Metadata API and auto-suffixed `__e` [snippet]. Publish: `POST /services/data/vXX.X/sobjects/{EventName}__e/` - i.e., publishing reuses the sObject-create mechanism (SFD-004) rather than a distinct endpoint [snippet, consistent with the sObject model]. Subscribe: CometD channel `/event/{EventName}__e`, or the Pub/Sub API (SFD-014) [snippet]. Two publish modes (snippet): **Publish After Commit** (transactional, counts against the Apex DML governor limit) vs. **Publish Immediately** (non-transactional, counts against a separate 150-call-per-transaction `EventBus.publish()` limit; `allOrNone` is ignored in this mode).
**Nonfunctional requirements.** Publish reuses SFD-004's write-opt-in gate; subscribe reuses SFD-012's connection-management approach.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<string> PublishPlatformEventAsync(string eventApiName, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
public IAsyncEnumerable<JsonDocument> SubscribeToPlatformEventAsync(string eventApiName, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Publish-via-sObject-create and both channel forms are snippet-sourced from `developer.salesforce.com/docs/platform/platform-events/guide/{platform-events-publish-api,platform-event-extras,platform-events-publish-apex}.html`; not independently re-fetched.
**Authentication and authorization.** Publish requires Create permission on the event's underlying object; subscribe requires the standard CometD/Pub/Sub entitlement.
**Required API permissions, scopes or licenses.** Edition-gated daily event-delivery allocation (SFD-022).
**Pagination, throttling and retry behavior.** Not paginated; publish is a single write per event, never auto-retried; the 150-call `EventBus.publish()` limit is a per-transaction Apex-side governor limit, not directly relevant to this SDK's own REST-based publish path, and is documented here only for completeness.
**Error handling and concurrency.** A publish failure surfaces via the standard sObject-create error shape (SFD-019), since the mechanism is literally an sObject create against the `__e` object.
**Data privacy and security.** Event payloads are caller-defined business data; never logged by default.
**Dependencies.** SFD-004, SFD-012 (shares the subscribe transport).
**Unit-test scenarios.** Publish composes the correct `{EventName}__e` create request; subscribe parses event payloads against a documented custom-event schema fixture.
**Contract-test scenarios.** Synthetic publish/subscribe fixtures, explicitly labeled snippet-confidence.
**Integration-test requirements.** Authorized sandbox org with a custom Platform Event defined; not available this pass.
**Acceptance criteria.** Publish and subscribe both work against synthetic fixtures using the sObject-create mechanism correctly.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact allocation/limit numbers are snippet-only (SFD-022 carries the detail); re-verify before release.

<a id="sfd-014"></a>
### SFD-014 - Pub/Sub API

**Business purpose.** Provide a single, modern gRPC-based transport for both Platform Events and Change Data Capture, as Salesforce's current recommended event-streaming path.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None; this is the one Phase 2 feature across all three connectors that requires a gRPC/Protocol-Buffers dependency rather than plain HTTP+JSON (Section 6, ADR-P201).
**Functional requirements.** Transport: gRPC over HTTP/2; RPC envelope uses Protocol Buffers; event payloads are encoded in **Apache Avro**, with a `GetSchema` RPC resolving the Avro schema by schema ID (a schema-registry pattern) [snippet]. Endpoint: `api.pubsub.salesforce.com:7443` - **[snippet, flagged unverified]**: one source suggested port 443 may also work; this must be independently re-checked, not assumed. RPCs (snippet): `Subscribe` (bidirectional stream, pull/credit-based flow control), `ManagedSubscribe`/`ManagedFetchRequest` (beta, server-stored Replay ID), `Publish`/`PublishStream`. Auth metadata on every gRPC call (snippet): `accesstoken`, `instanceurl`, `tenantid` headers - exact header-name case-sensitivity flagged unverified.
**Nonfunctional requirements.** Depends on `Grpc.Net.Client` + `Google.Protobuf` (Section 6's explicit, justified exception to the "no non-HTTP-client dependencies" default); Avro deserialization requires either a hand-rolled minimal reader or an additional Avro library dependency - **not yet decided, an open architecture question** (Section 28), since adding a second new binary-protocol dependency for one connector's one P1 feature needs its own justification separate from the already-justified gRPC dependency.
**Public C# interfaces/model contracts (proposed).**
```csharp
public interface ISalesforcePubSubClient
{
    IAsyncEnumerable<SalesforcePubSubEvent> SubscribeAsync(string topic, string? replayId = null, CancellationToken ct = default);
    Task PublishAsync(string topic, ReadOnlyMemory<byte> avroPayload, CancellationToken ct = default);
}
public sealed record SalesforcePubSubEvent(string ReplayId, ReadOnlyMemory<byte> AvroPayload, string SchemaId);
```
**Official API endpoints, methods and versions (verified).** Every item above is snippet-sourced (the overview page itself returned 403 on direct fetch); sourced from `developer.salesforce.com/docs/platform/pub-sub-api/guide/{supported-auth,rpc-method-headers}.html`, `.../references/methods/{subscribe-rpc,managedsubscribe-rpc}.html`, `.../platform-events/guide/platform-events-event-schema-pubsub-api.html`. This is the lowest-confidence feature in the entire Salesforce catalog.
**Authentication and authorization.** Same OAuth bearer token as REST, passed via gRPC call metadata rather than an HTTP header, per the documented `accesstoken`/`instanceurl`/`tenantid` triple.
**Required API permissions, scopes or licenses.** Same per-object CDC/Platform-Event entitlements as SFD-012/SFD-013, since Pub/Sub is a transport for the same underlying event types.
**Pagination, throttling and retry behavior.** Pull-based credit flow control (client requests N events at a time) rather than push; reconnect uses the Replay ID mechanism, exact resume-window duration not independently confirmed.
**Error handling and concurrency.** gRPC status codes map to the shared `IntegrationExceptionClassifier`'s philosophy but require a new gRPC-specific adapter, since the classifier's current implementation is HTTP-status-code-based only (Core's `IntegrationTransport` is HTTP-only) - documented here as a connector-local extension, not a shared-abstraction change (Section 8.2's "no abstraction creep for one vendor" principle).
**Data privacy and security.** Event payloads may carry sensitive business data in Avro-encoded form; never logged in raw or decoded form by default.
**Dependencies.** SFD-001 (token reuse across transports), SFD-012, SFD-013.
**Unit-test scenarios.** Deferred pending the Avro-library decision above; a gRPC-channel construction unit test (metadata headers attached correctly) is feasible without a live server using a synthetic gRPC test server.
**Contract-test scenarios.** Synthetic gRPC service double implementing `Subscribe`/`Publish`, explicitly labeled snippet-confidence for wire-format details.
**Integration-test requirements.** Authorized sandbox org with Pub/Sub API access; not available this pass.
**Acceptance criteria.** Deferred beyond basic channel/metadata construction until the Avro-dependency decision is made and the endpoint/port/header details are independently re-verified.
**Definition of Done.** Not applicable beyond a design-only state until the open gaps above close; do not implement against unverified wire-format details.
**Known limitations and blockers.** This is the single most speculative feature in the entire 70-feature catalog: endpoint port, RPC method names, and Avro-vs-schema-registry mechanics are all snippet-only with an explicitly flagged internal inconsistency (port 7443 vs. 443). Recommend this be the first Salesforce P1 feature re-verified against a live Salesforce Pub/Sub API session or a freshly-fetched official page before any implementation work begins (Section 28).

<a id="sfd-015"></a>
### SFD-015 - Relationships and references

**Business purpose.** Traverse and query parent/child and polymorphic relationships between sObjects.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** None Salesforce-specific; extends SFD-005's query surface.
**Functional requirements.** Child-to-parent: dot notation, max 5 levels, max 55 relationships per query [snippet, consistent with the directly-confirmed SOQL basics in SFD-005]. Parent-to-child: subquery using the plural relationship name (discoverable via `describe`'s `relationshipName` field, SFD-007), max 5 levels (v58.0+) [snippet]. Polymorphic relationships (e.g., Task/Event's `Who`/`What` fields): `TYPEOF field WHEN Type1 THEN f1,f2 WHEN Type2 THEN f3 ELSE f4 END` syntax, available v46.0+ [snippet].
**Nonfunctional requirements.** None beyond SFD-005's query-composition safety.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceTypeOfClause(string Field, IReadOnlyDictionary<string, IReadOnlyList<string>> WhenFields, IReadOnlyList<string>? ElseFields);
```
**Official API endpoints, methods and versions (verified).** Relationship-traversal limits and `TYPEOF` syntax are snippet-sourced from `developer.salesforce.com/docs/platform/salesforce-soql-sosl/guide/{...-relationships-query-using,...-relationships-query-limits}.html`; relationship-traversal basics corroborate SFD-005's directly-confirmed SOQL/SOSL PDF content.
**Authentication and authorization.** Read access on every traversed object/field.
**Required API permissions, scopes or licenses.** None beyond SFD-005's.
**Pagination, throttling and retry behavior.** Unchanged from SFD-006.
**Error handling and concurrency.** A relationship-traversal depth/count violation returns a documented 400, classified via SFD-019.
**Data privacy and security.** No additional concern beyond SFD-005's.
**Dependencies.** SFD-005, SFD-007.
**Unit-test scenarios.** `TYPEOF` clause composition matches the documented syntax; relationship-depth/count limits enforced locally before send.
**Contract-test scenarios.** Synthetic fixture for a polymorphic `Who`/`What` query.
**Integration-test requirements.** Authorized sandbox org with Task/Event records; not available this pass.
**Acceptance criteria.** Relationship traversal and polymorphic queries compose correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact numeric limits are snippet-only; re-verify before release.

<a id="sfd-016"></a>
### SFD-016 - Files and ContentVersion

**Business purpose.** Upload, link, and retrieve file attachments stored as Salesforce Content (the modern file-storage model, superseding legacy Attachments).
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** Follows SFD-004's generic sObject-create pattern for `ContentVersion`.
**Functional requirements.** sObjects involved: `ContentVersion`, `ContentDocument`, `ContentDocumentLink` [snippet]. Upload: create a `ContentVersion` with `{Title (string, max 255), PathOnClient (string, max 500), VersionData (Base64-encoded file content)}` via the standard sObject-create mechanism (SFD-004) [snippet]. Linking: the created `ContentVersion.ContentDocumentId` is used to create a `ContentDocumentLink{ContentDocumentId, LinkedEntityId, ShareType, Visibility}` record associating the file with any other record [snippet].
**Nonfunctional requirements.** Streamed Base64 encoding for large files to avoid full in-memory buffering; explicit write opt-in gate, matching SFD-004.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task<string> UploadContentVersionAsync(string title, string pathOnClient, Stream content, bool writesEnabled = false, CancellationToken ct = default);
public async Task LinkContentDocumentAsync(string contentDocumentId, string linkedEntityId, string shareType, string visibility, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `ContentVersion`/`ContentDocument`/`ContentDocumentLink` field shapes are snippet-sourced from the object-reference documentation pages for each; the upload mechanism (reusing sObject-create) is a direct, low-risk inference from the directly-confirmed SFD-004 mechanism, not independently re-confirmed for this specific object.
**Authentication and authorization.** Create permission on `ContentVersion`/`ContentDocumentLink`; sharing-model-dependent visibility on the linked record.
**Required API permissions, scopes or licenses.** Standard Salesforce Files feature entitlement (included in all editions that support the modern Content model).
**Pagination, throttling and retry behavior.** Single-record writes; never auto-retried.
**Error handling and concurrency.** Standard sObject-create error shape (SFD-019).
**Data privacy and security.** File content is caller-owned, potentially highly sensitive binary data; Base64-encoded in transit, never logged.
**Dependencies.** SFD-004.
**Unit-test scenarios.** `VersionData` Base64 encoding round-trips without corruption; `ContentDocumentLink` composition matches the documented field shape.
**Contract-test scenarios.** Synthetic fixtures for both the upload and link steps.
**Integration-test requirements.** Authorized sandbox org; not available this pass.
**Acceptance criteria.** Upload-then-link round-trips correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** A REST-specific file-size cap was not found; only a SOAP API figure (50MB) was located, and it is explicitly **not** assumed to apply to the REST path without independent confirmation (Section 28).

<a id="sfd-017"></a>
### SFD-017 - Limits API and throttling

**Business purpose.** Discover an org's current API usage against its daily allocation, and classify a throttled response as a recoverable, typed condition.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException` mapping reused unchanged; `TransportOptions`'s bounded-backoff GET-retry policy reused unchanged.
**Functional requirements.** `GET /services/data/vXX.X/limits/` (available since API v29.0, requires "View Setup and Configuration" permission) [snippet]. Confirmed field names (snippet, from the resource-reference page): `DailyApiRequests, DailyBulkApiBatches, DailyBulkV2QueryJobs, DailyBulkV2QueryFileStorageMB, ConcurrentAsyncGetReportInstances, DataStorageMB, FileStorageMB, StreamingApiConcurrentClients` plus roughly 20 more (e.g. `DailyAsyncApexExecutions`), each typically shaped `{Max, Remaining}`. `DailyBulkV2IngestJobs` field name was **not found** in any fetched source - not assumed to exist. No `/limits/recordCount` sub-resource was found either - not assumed to exist.
**Nonfunctional requirements.** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceLimit(int Max, int Remaining);
public Task<IReadOnlyDictionary<string, SalesforceLimit>> GetLimitsAsync(CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Endpoint and the listed field names are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/resources-limits.html`; not independently re-fetched.
**Authentication and authorization.** Requires "View Setup and Configuration" permission on the calling user.
**Required API permissions, scopes or licenses.** Per-edition daily allocation (SFD-022).
**Pagination, throttling and retry behavior.** 429 + standard Salesforce error body (not a `Retry-After` header per se - Salesforce's REST API documents throttling via the standard error-code shape, `REQUEST_LIMIT_EXCEEDED`, rather than a dedicated header; this is a **notable divergence from Dataverse/Graph's `Retry-After` convention** and must be handled as a distinct code path, not assumed to carry a numeric retry delay).
**Error handling and concurrency.** `REQUEST_LIMIT_EXCEEDED` classifies as `ThrottleException` with no server-supplied delay, requiring the connector to apply its own bounded backoff rather than honoring a vendor-specified wait, unlike Dataverse/Graph.
**Data privacy and security.** Limit values are org-operational metadata, not business data.
**Dependencies.** SFD-003.
**Unit-test scenarios.** Dictionary-shaped limits parse correctly keyed by name; `REQUEST_LIMIT_EXCEEDED` classifies as `ThrottleException` without assuming a `Retry-After` value is present; an unrecognized limit key is preserved, not dropped.
**Contract-test scenarios.** Synthetic fixture matching the documented limits-response shape.
**Integration-test requirements.** Authorized sandbox org; not available this pass. Deliberately triggering a real daily-limit breach is infeasible and inadvisable for a documentation-only pass.
**Acceptance criteria.** Limits parse correctly; throttled responses classify correctly without assuming a header that Salesforce does not document providing.
**Definition of Done.** Standard template.
**Known limitations and blockers.** `DailyBulkV2IngestJobs` field name and a `/limits/recordCount` sub-resource were not found in any source and must not be assumed to exist; the absence of a `Retry-After`-style header (contrasted with Dataverse/Graph) is a deliberate per-vendor difference documented here, not an SDK oversight.

<a id="sfd-018"></a>
### SFD-018 - Concurrency and conditional requests

**Business purpose.** Prevent a caller from silently overwriting a record that changed since it was last read, and avoid unnecessary re-fetches of unchanged data.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** None Salesforce-specific; conceptually parallel to DVT-013's ETag handling, with materially different (and narrower) official scope.
**Functional requirements.** Salesforce documents a dedicated "Conditional Request Headers" mechanism [snippet], correcting an initial assumption that Salesforce lacks this entirely. Strong validation: `ETag` response header, `If-Match`, `If-None-Match` - **[snippet, flagged for independent re-verification]**: one source described this as scoped to **"Account records only"**, an unusually narrow scope that is inconsistent with how most REST APIs apply ETags uniformly, and must not be treated as settled fact until re-checked against a live page. Weak validation (broader scope, per snippet): `If-Modified-Since`/`If-Unmodified-Since` on sObject Rows, sObject Describe, Describe Global, and Invocable Actions. Documented behavior: a malformed conditional header → 400; `If-Match` mismatch → 412; `If-None-Match` match on GET → 304, on PATCH → 412; `If-Unmodified-Since` violated → 412; `If-Range` and DELETE are explicitly unsupported for conditional semantics.
**Nonfunctional requirements.** None beyond explicit write opt-in, already covered by SFD-004.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, string? ifUnmodifiedSince = null, bool writesEnabled = false, CancellationToken ct = default);
// A 412 response classifies to a dedicated SalesforceConcurrencyException : Koras.Connect.Exceptions.ContractException,
// mirroring DVT-013's decision not to extend the shared classifier for a single-vendor 412 case.
```
**Official API endpoints, methods and versions (verified).** All of the above is snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/intro_rest_conditional_requests.htm`; the "Account-only" ETag scoping claim in particular is explicitly flagged as unconfirmed and higher-risk than the rest of this feature's facts.
**Authentication and authorization.** Unchanged from SFD-004.
**Required API permissions, scopes or licenses.** None beyond SFD-004's.
**Pagination, throttling and retry behavior.** Not applicable; concurrency conflicts are never auto-retried.
**Error handling and concurrency.** This feature's entire subject is distinguishing 412/304/400 from other 4xx outcomes reliably.
**Data privacy and security.** ETags/timestamps are opaque version/time tokens, not business data.
**Dependencies.** SFD-004.
**Unit-test scenarios.** `If-Unmodified-Since` sent correctly when supplied; 412/304/400 each classify distinctly; the SDK does not assume `If-Match`/ETag support beyond `Account` until independently confirmed - meaning the SDK's own typed concurrency helper should be documented as broadly available only for the weak-validation (`If-Modified-Since`/`If-Unmodified-Since`) path unless/until the strong-validation scope is confirmed wider than Account.
**Contract-test scenarios.** Synthetic fixtures for 412/304/400.
**Integration-test requirements.** Authorized sandbox org with both an Account record and a non-Account record, to directly test the flagged ETag-scope claim; not available this pass.
**Acceptance criteria.** 412/304/400 are classified correctly; the Account-only ETag scope claim is either confirmed or corrected before this feature ships as `verified`.
**Definition of Done.** Standard template; the ETag-scope re-verification is an explicit release blocker (Section 28).
**Known limitations and blockers.** The "Account records only" ETag-scoping claim is the highest-priority re-verification item in this feature.

<a id="sfd-019"></a>
### SFD-019 - Error normalization and partial successes

**Business purpose.** Present every Salesforce failure - single-record or composite/batch partial failure - through the SDK's own typed exception hierarchy rather than forcing every caller to parse raw Salesforce JSON error arrays themselves.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationExceptionClassifier`/`BatchItemResult<T>` reused (Section 8.1/8.2).
**Functional requirements.** Standard error body: a JSON array of `{message, errorCode, fields[]}` [snippet]. Observed `errorCode` values (snippet): `REQUIRED_FIELD_MISSING, INVALID_FIELD, MALFORMED_ID, DUPLICATE_VALUE, FIELD_INTEGRITY_EXCEPTION, INSUFFICIENT_ACCESS, INVALID_SESSION_ID, RECORD_NOT_FOUND, ENTITY_IS_DELETED, ENTITY_IS_LOCKED, STORAGE_LIMIT_EXCEEDED, REQUEST_LIMIT_EXCEEDED, INVALID_CROSS_REFERENCE_KEY`. Composite-batch partial-success behavior (snippet, corroborating SFD-009): the outer HTTP status remains 200 even when some subrequests fail; the body carries `hasErrors`+`results[]`, each with its own `statusCode`/`result` - callers must inspect per-subrequest status, never just the outer code.
**Nonfunctional requirements.** None beyond standard exception-mapping discipline.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class SalesforceApiException : IntegrationException
{
    public static SalesforceApiException Classify(string errorCode, string message, IReadOnlyList<string>? fields);
    public static SalesforceApiException FromTransportFailure(IntegrationRequestException inner);
}
```
**Official API endpoints, methods and versions (verified).** Error body shape and errorCode list are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/errorcodes.html`; composite-batch partial-success corroborates SFD-009's own snippet-sourced shape. No verbatim raw JSON error sample was independently retrieved this pass.
**Authentication and authorization.** `INVALID_SESSION_ID`/`INSUFFICIENT_ACCESS` map to `AuthenticationException`/`AuthorizationException` respectively.
**Required API permissions, scopes or licenses.** Not applicable (this is error-handling infrastructure).
**Pagination, throttling and retry behavior.** `REQUEST_LIMIT_EXCEEDED` maps to `ThrottleException` (SFD-017's no-`Retry-After` caveat applies here too).
**Error handling and concurrency.** Every documented `errorCode` maps to a specific exception type or a reasoned default (`ContractException`) for unrecognized codes - matching every existing connector's classifier-default discipline.
**Data privacy and security.** Error messages may echo back submitted field values in some cases; treated as potentially sensitive and not logged verbatim by default, matching SAP's `message.value`-is-tenant-specific redaction precedent exactly.
**Dependencies.** SFD-004, SFD-009.
**Unit-test scenarios.** Every listed `errorCode` maps to the correct exception type; an unrecognized `errorCode` falls back to `ContractException` rather than throwing an unrelated error; composite-batch partial failure surfaces per-subrequest status via `BatchItemResult<T>`, not a single aggregate exception.
**Contract-test scenarios.** Synthetic fixtures for each documented `errorCode`, explicitly labeled snippet-confidence pending a verbatim raw-JSON sample.
**Integration-test requirements.** Authorized sandbox org to capture at least one real raw error body; not available this pass.
**Acceptance criteria.** Every documented `errorCode` classifies correctly; composite-batch partial success is never misreported as either "fully succeeded" or "fully failed."
**Definition of Done.** Standard template.
**Known limitations and blockers.** No verbatim raw JSON error sample was independently retrieved; re-verify exact body shape (array vs. object wrapper, field casing) before release.

<a id="sfd-020"></a>
### SFD-020 - Typed CRM/custom object models

**Business purpose.** Provide extension-data-preserving typed C# models for standard and custom Salesforce objects, matching every other connector's established discipline.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3.
**Current implementation status.** Not started.
**Repository evidence.** `IRecordMapper<TSource,TTarget>`/`MappingResult<T>` reused unchanged (Section 8.1).
**Functional requirements.** Two legacy typed-codegen paths exist but neither targets this connector's REST+JSON surface: the **Enterprise WSDL** (strongly-typed, per-org, every exposed object/field) and the **Partner WSDL** (generic/loosely-typed, org-agnostic) - both SOAP-API artifacts, downloaded from Setup → API page [snippet]. No full official OpenAPI specification exists for the whole REST API; there is an official **Beta**, "Generating an OpenAPI 3.0 Document for sObjects REST API," scoped only to sObjects CRUD, via `POST https://{MyDomainName}.my.salesforce.com/services/data/vXX.X/async/specifications/oas3` [snippet] - noted here as a potential future codegen source once it leaves beta, not used as an implementation dependency today per the same reasoning as DVT-021's "no official Web-API-specific generator" conclusion. Models are hand-written against SFD-007's describe metadata and the documented row-shape examples, preserving unknown fields via a `[JsonExtensionData]` dictionary, matching every existing Phase 1 DTO.
**Nonfunctional requirements.** Plain POCOs with `System.Text.Json` attributes, no third-party serialization library, matching every existing connector's style.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class SalesforceRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
```
**Official API endpoints, methods and versions (verified).** Enterprise/Partner WSDL existence and the OpenAPI 3.0 beta endpoint are snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/openapi_beta.htm` and general SOAP API documentation references; not independently re-fetched.
**Authentication and authorization.** Not applicable (pure data-shape concern).
**Required API permissions, scopes or licenses.** None.
**Pagination, throttling and retry behavior.** Not applicable.
**Error handling and concurrency.** A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws, matching every existing mapper.
**Data privacy and security.** Mapped/omitted/error states never leak raw field values into log output by default.
**Dependencies.** SFD-004, SFD-007.
**Unit-test scenarios.** A record missing a required identifier field is `Omitted`, not mapped with a null/default key; unknown fields preserved via extension data; a custom (`__c`-suffixed) field round-trips identically to a standard field.
**Contract-test scenarios.** Golden-fixture tests against documented standard-object (Account/Contact) and representative custom-object row shapes.
**Integration-test requirements.** None beyond the underlying CRUD/describe features'.
**Acceptance criteria.** Golden mappings are deterministic; omitted/error fields are visible; original source data remains intact.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The OpenAPI 3.0 beta generator is explicitly a beta feature with no guaranteed stability - not used as a hard dependency, consistent with the master Section 6 principle of only depending on GA vendor surfaces.

<a id="sfd-021"></a>
### SFD-021 - Incremental extraction and checkpoints

**Business purpose.** Extract only records that changed since a prior checkpoint, using Salesforce's timestamp- and SOAP-call-based mechanisms (there is no REST-native delta-token equivalent to Dataverse's `$deltatoken`).
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** `SapDeltaCheckpoint`/`SapDeltaFilters`'s filter-based (not opaque-token-based) shape is the proof-of-concept precedent cited in Section 8.2, item 3 - Salesforce's mechanism is the same category, a filterable timestamp, not an opaque server-issued cursor.
**Functional requirements.** `SystemModstamp`: present on most standard and custom objects, auto-maintained, read-only; documentation notes cross-object rollup fields do not always propagate it reliably [snippet]. `queryAll` (SFD-005/011) includes soft-deleted/archived rows; combined with an explicit `isDeleted = TRUE` filter this covers deletions, unlike a plain `query` [snippet]. Two SOAP-only legacy calls provide an alternative incremental mechanism, each with a materially different lookback ceiling than the other: `getUpdated(sObjectType, startDate, endDate)` - max **30-day** lookback, result capped at **600,000 IDs** (`EXCEEDED_ID_LIMIT` beyond that) [snippet]; `getDeleted(sObjectType, startDate, endDate)` - max **15-day** lookback, notably shorter than `getUpdated`'s 30 days [snippet]. Since this connector is REST/JSON-only (Section 6 architecture decision, matching every other Phase 2 connector), `getUpdated`/`getDeleted` are documented here as a known vendor mechanism but are **not** implemented unless a future SOAP-interop exception is explicitly justified - the primary implementation path is `SystemModstamp`-filtered SOQL plus `queryAll`+`isDeleted` for deletions.
**Nonfunctional requirements.** Checkpoint persistence is caller-owned (via a connector-specific checkpoint record, not `ContinuationToken`, since `SystemModstamp` is a queryable filter value, not an opaque cursor - Section 8.2, item 3's reasoning applied concretely).
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record SalesforceDeltaCheckpoint(DateTimeOffset SinceSystemModstamp);
public IAsyncEnumerable<T> EnumerateChangesAsync<T>(string sObject, SalesforceDeltaCheckpoint checkpoint, IReadOnlyList<string> fields, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `SystemModstamp`, `queryAll`+`isDeleted`, and the `getUpdated`/`getDeleted` SOAP calls (including their differing 30-day/15-day lookback ceilings) are snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/resources_queryall.htm`, `api.meta/api/sforce_api_calls_getupdated.htm`, and `platform/api/guide/sforce-api-calls-getdeleted.html`; not independently re-fetched.
**Authentication and authorization.** Unchanged from SFD-005.
**Required API permissions, scopes or licenses.** Read access on the queried object/fields, including `SystemModstamp`/`IsDeleted`.
**Pagination, throttling and retry behavior.** Reuses SFD-006 for the underlying SOQL pagination.
**Error handling and concurrency.** A checkpoint older than the data retention window (exact Salesforce-side retention for `queryAll`-visible soft-deleted rows not independently confirmed this pass) should be treated as a condition requiring a documented fallback (full resync), matching Dataverse's 7-day change-tracking-retention caveat in spirit, even though the exact Salesforce retention number is unconfirmed.
**Data privacy and security.** Checkpoint timestamps are not business data; record payloads returned by the delta query are.
**Dependencies.** SFD-005, SFD-006, SFD-007.
**Unit-test scenarios.** `SystemModstamp`-filtered query composition is correct; `queryAll`+`isDeleted` correctly surfaces soft-deleted rows; the SOAP-only `getUpdated`/`getDeleted` mechanism is documented but has no corresponding implementation to test, by design.
**Contract-test scenarios.** Synthetic fixture for a `SystemModstamp`-filtered query including both changed and soft-deleted rows.
**Integration-test requirements.** Authorized sandbox org with a real deletion and a real cross-object rollup field, to confirm the `SystemModstamp` propagation caveat; not available this pass.
**Acceptance criteria.** Checkpoint-based extraction correctly surfaces both changed and deleted rows via the REST-only mechanism; the SOAP-only alternative remains documented, not implemented.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact soft-deleted-row retention window for `queryAll` visibility is unconfirmed; `SystemModstamp` propagation reliability for rollup fields is a documented, not independently tested, caveat.

<a id="sfd-022"></a>
### SFD-022 - Contract tests, diagnostics and examples

**Business purpose.** Prove the Salesforce connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
**Owning package.** `Koras.Connect.Salesforce`.
**Priority.** P0. **Milestone.** P2-D3 (unit/contract tests) / P2-D6 (sample, package validation).
**Current implementation status.** Not started.
**Repository evidence.** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline sample convention; requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Salesforce.snapshot.txt` entry (SDK-017).
**Functional requirements.** Offline unit/contract tests for every SFD-001 through SFD-021 P0/P1 feature, using a synthetic `HttpMessageHandler` (and, for SFD-014's gRPC surface, a synthetic gRPC test double), no live network access, running on both Linux and Windows; at least one standalone `samples/Salesforce.*` console project demonstrating authentication → CRUD → SOQL → pagination → describe, entirely offline; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity, never issuing a mutating request. **This feature carries a distinct, explicit documentation obligation beyond every other connector's SFD-022/DVT-022/MGR-026 equivalent**: every per-feature specification above that is marked snippet-only or flagged with an explicit "not independently re-verified" caveat must have that caveat carried into the sample/test documentation verbatim, not silently upgraded to "confirmed" - per-edition numeric limits (SFD-022's own research findings) are the most consequential category of this, since they vary by org edition and were the least-corroborated figures in the whole Salesforce research pass.
**Nonfunctional requirements.** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
**Public C# interfaces/model contracts (proposed).** `SalesforceConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what SFD-001 through SFD-021 already define.
**Official API endpoints, methods and versions (verified).** `DiagnoseAsync`'s connectivity stage uses SFD-002's unauthenticated `GET /services/data/` as its one permitted minimal, non-mutating probe call before a token is even required, paralleling DVT-022's `WhoAmI` choice but even lighter-weight since no auth is needed for this specific check.
**Authentication and authorization.** `DiagnoseAsync`'s Authentication stage issues exactly one minimal read (e.g. `GET /services/data/vXX.X/limits/` or an equivalent lightweight authenticated GET), never a mutating request.
**Required API permissions, scopes or licenses.** None beyond whatever the underlying probed feature already requires.
**Pagination, throttling and retry behavior.** Not applicable to this feature itself.
**Error handling and concurrency.** `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary.
**Data privacy and security.** Diagnostic output contains no secrets, tokens, or record payloads.
**Dependencies.** Every other SFD-0NN feature this covers tests for.
**Unit-test scenarios.** Every unit-test scenario enumerated in SFD-001 through SFD-021 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed.
**Contract-test scenarios.** Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus, each fixture's confidence tier (PDF/snippet/unverified) preserved in an accompanying comment or doc annotation so a future maintainer knows which fixtures need re-verification against a live org before being trusted at face value.
**Integration-test requirements.** Every feature's integration-test requirement above remains individually deferred/blocked pending a live sandbox; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live org.
**Acceptance criteria.** The sample project builds and runs on a clean checkout with zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed; every snippet-only fact used anywhere in the Salesforce test/sample corpus is traceable to its confidence tier.
**Definition of Done.** Matches the Phase 1 master's Section 26 Definition of Done template, with the added explicit condition that no snippet-only Salesforce fact is presented in sample code or documentation as if it were PDF-confirmed.
**Known limitations and blockers.** No live Salesforce org is available in this documentation pass; the Composite API (SFD-009) and Pub/Sub API (SFD-014) field-name/wire-format details are explicitly the highest-priority re-verification items before any Salesforce connector implementation begins (Section 28).
## 13. Microsoft Graph Feature Matrix

Official sources checked November 13, 2026 (all `learn.microsoft.com`, high confidence - fetched directly, not snippet-sourced): [G1: Graph overview](https://learn.microsoft.com/en-us/graph/overview), [G2: Permissions overview](https://learn.microsoft.com/en-us/graph/permissions-overview), [G3: Throttling guidance](https://learn.microsoft.com/en-us/graph/throttling), [G4: Auth concepts/flows](https://learn.microsoft.com/en-us/graph/auth/auth-concepts), [G5: Paging](https://learn.microsoft.com/en-us/graph/paging), [G6: Delta query](https://learn.microsoft.com/en-us/graph/delta-query-overview), [G7: JSON batching](https://learn.microsoft.com/en-us/graph/json-batching), [G8: Webhooks/change notifications](https://learn.microsoft.com/en-us/graph/webhooks), [G9: Large file upload sessions](https://learn.microsoft.com/en-us/graph/sdks/large-file-upload), [G10: Known issues/versioning](https://learn.microsoft.com/en-us/graph/support), [G11: Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference). One flagged inconsistency carried forward honestly rather than silently resolved: the error-response schema shows both `innerError` and `innererror` casing across different official pages.

| ID | Feature | Priority | Milestone | Status |
|---|---|---|---|---|
| [MGR-001](#mgr-001) | Delegated and app-only authentication | P0 | P2-D4 | Not started |
| [MGR-002](#mgr-002) | Tenant and permission configuration | P0 | P2-D4 | Not started |
| [MGR-003](#mgr-003) | Graph HTTP client | P0 | P2-D4 | Not started |
| [MGR-004](#mgr-004) | Users and profiles | P0 | P2-D4 | Not started |
| [MGR-005](#mgr-005) | Groups and memberships | P0 | P2-D4 | Not started |
| [MGR-006](#mgr-006) | Directory roles and admin units | P1 | P2-D5 | Not started |
| [MGR-007](#mgr-007) | App registrations and service principals | P1 | P2-D5 | Not started |
| [MGR-008](#mgr-008) | Organization and tenant info | P0 | P2-D4 | Not started |
| [MGR-009](#mgr-009) | OneDrive drives and items | P0 | P2-D4 | Not started |
| [MGR-010](#mgr-010) | File upload, download and sessions | P0 | P2-D4 | Not started |
| [MGR-011](#mgr-011) | SharePoint sites and document libraries | P0 | P2-D4 | Not started |
| [MGR-012](#mgr-012) | SharePoint lists and items | P0 | P2-D4 | Not started |
| [MGR-013](#mgr-013) | Outlook mail | P1 | P2-D5 | Not started |
| [MGR-014](#mgr-014) | Outlook calendar | P1 | P2-D5 | Not started |
| [MGR-015](#mgr-015) | Teams and channels | P1 | P2-D5 | Not started |
| [MGR-016](#mgr-016) | Teams messages and chats | P1 | P2-D5 | Not started |
| [MGR-017](#mgr-017) | Planner | P2 | P2-D7 | Not started |
| [MGR-018](#mgr-018) | Microsoft To Do | P2 | P2-D7 | Not started |
| [MGR-019](#mgr-019) | Pagination and delta queries | P0 | P2-D4 | Not started |
| [MGR-020](#mgr-020) | Change notifications and subscriptions | P1 | P2-D5 | Not started |
| [MGR-021](#mgr-021) | Batch requests | P0 | P2-D4 | Not started |
| [MGR-022](#mgr-022) | Throttling and Retry-After | P0 | P2-D4 | Not started |
| [MGR-023](#mgr-023) | Large file transfer and resumability | P1 | P2-D5 | Not started |
| [MGR-024](#mgr-024) | Permission and consent diagnostics | P0 | P2-D4 | Not started |
| [MGR-025](#mgr-025) | Typed models and extensibility | P0 | P2-D4 | Not started |
| [MGR-026](#mgr-026) | Contract tests, diagnostics and examples | P0 | P2-D4/P2-D6 | Not started |

## 14. Microsoft Graph Detailed Feature Specifications

<a id="mgr-001"></a>
### MGR-001 - Delegated and app-only authentication

**Business purpose.** Authenticate against Microsoft Graph as either a signed-in user (delegated) or an unattended daemon (app-only), matching Graph's fundamental permission-model split.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** Reuses `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`CertificateCredentialProvider` unchanged for the app-only (client-credentials) case; delegated flows accept a caller-supplied `IAccessTokenSource` rather than the SDK implementing an interactive browser step, matching DVT-001/SFD-001's identical decision.
**Functional requirements.** App-only (client-credentials): `POST https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token`, `scope=https://graph.microsoft.com/.default` (URL-encoded), `grant_type=client_credentials`; **no `refresh_token` is issued** for this grant [G4]. Delegated (authorization-code): `https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize` then `.../token`, out of scope for SDK-managed acquisition (requires a browser) - the SDK accepts a caller-supplied delegated token source. `{tenant}` must be a GUID, verified domain, or `organizations`/`consumers`/`common` depending on the app's supported-account-types configuration - `common` is invalid for any call requiring app-only consent.
**Nonfunctional requirements.** net10.0/Linux/Windows; bounded acquisition timeout; cancellation-aware.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphClientCredentialsOptions(string TenantId, string ClientId, string? ClientSecret = null,
    Func<CancellationToken, ValueTask<X509Certificate2>>? CertificateSource = null) : IValidatableOptions;
```
**Official API endpoints, methods and versions (verified).** Token endpoint, `.default` scope convention, no-refresh-token-for-client-credentials - all per [G4].
**Authentication and authorization.** App-only via client secret or certificate; delegated via caller-supplied token (device code, auth code, or interactive, outside SDK scope).
**Required API permissions, scopes or licenses.** An Entra app registration with the specific Graph application permissions each downstream MGR feature declares (MGR-002), admin-consented.
**Pagination, throttling and retry behavior.** Not applicable to token acquisition; reuses `ClientCredentialsTokenSource`'s cached-with-expiry-margin behavior.
**Error handling and concurrency.** Non-2xx token response classifies as `AuthenticationException`; concurrent callers share one in-flight acquisition.
**Data privacy and security.** Client secret/certificate never logged; `RedactedCredential<T>` reused.
**Dependencies.** None (first Graph feature).
**Unit-test scenarios.** Expired cache triggers reacquisition; `.default` scope composed correctly; invalid certificate rejected before any network call; non-2xx classifies as `AuthenticationException`.
**Contract-test scenarios.** Synthetic token-endpoint fixture for the client-credentials grant.
**Integration-test requirements.** Requires a real Entra app registration with admin-consented application permissions; not available this pass.
**Acceptance criteria.** App-only token acquisition works without any interactive step; delegated scenarios correctly delegate to a caller-supplied source rather than the SDK attempting browser interaction.
**Definition of Done.** Standard template; live tenant gate deferred (Section 28).
**Known limitations and blockers.** No live Entra tenant/app registration available this pass.

<a id="mgr-002"></a>
### MGR-002 - Tenant and permission configuration

**Business purpose.** Document and validate which Graph permissions (delegated vs. application) a caller's app registration actually holds, before any downstream feature assumes access it doesn't have.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None Graph-specific; this feature is primarily configuration/documentation over MGR-001/MGR-024.
**Functional requirements.** Delegated permissions require a signed-in user and are represented as `oauth2PermissionGrant` records; application permissions require tenant-admin consent and are represented as `appRoleAssignment` records [G2]. Granting an application permission for Graph specifically requires the **Privileged Role Administrator** role (or Global Administrator); granting most other delegated/admin-consent scenarios needs Cloud Application Administrator or Application Administrator [G2]. Per-tenant permission-request limits vary by audience: observed figures of roughly 400/155/300/30 requested permissions depending on app registration type [G2] - the SDK must document this as a configuration constraint a caller may hit when requesting many scopes, not something the SDK can work around.
**Nonfunctional requirements.** None beyond documentation/validation.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphPermissionRequirement(string Permission, bool IsApplicationPermission, string Reason);
public sealed record GraphConnectionOptions(string TenantId, string ClientId, IReadOnlyList<GraphPermissionRequirement> RequiredPermissions) : IValidatableOptions;
```
**Official API endpoints, methods and versions (verified).** Permission-grant representations and admin-role requirements per [G2].
**Authentication and authorization.** Builds on MGR-001.
**Required API permissions, scopes or licenses.** Declared per downstream feature; this feature's job is to make that declaration explicit and checkable (MGR-024).
**Pagination, throttling and retry behavior.** Not applicable.
**Error handling and concurrency.** A caller declaring a required permission the app registration does not hold should fail fast and clearly at configuration-validation time, not as a confusing 403 deep inside a downstream call.
**Data privacy and security.** Permission scope strings are configuration metadata, not sensitive business data.
**Dependencies.** MGR-001.
**Unit-test scenarios.** `GraphConnectionOptions` validation rejects a configuration declaring zero required permissions when a feature mandates at least one; delegated-vs-application permission distinction is preserved through the options model.
**Contract-test scenarios.** None beyond MGR-024's diagnostic probe.
**Integration-test requirements.** Authorized Entra tenant to confirm the per-audience permission-request-count limits empirically; not available this pass.
**Acceptance criteria.** Every downstream MGR feature's required permissions are declared through this one typed mechanism, not scattered ad hoc strings.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact per-audience permission-request-count limits were documented with some uncertainty in the source research; treat as approximate until independently reconfirmed.

<a id="mgr-003"></a>
### MGR-003 - Graph HTTP client

**Business purpose.** Provide the single HTTP client foundation every other Graph feature builds on, including the v1.0/beta version split and Graph's specific error-body shape.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationTransport`/`EndpointSafety.ValidateRoot` reused unchanged.
**Functional requirements.** Base URLs: `https://graph.microsoft.com/v1.0/` (GA, default) and `https://graph.microsoft.com/beta/` (explicit opt-in only, per MGR-026) [G1]. Standard error body: `{"error":{"code","message","innerError",...}}` [G1] - **flagged inconsistency, carried forward honestly**: some official pages render the nested field as `innerError` and others as `innererror`; the connector's error model must accept either casing rather than assuming one. Standard HTTP status codes apply, including 429 (MGR-022). `ConsistencyLevel: eventual` header is required for certain advanced query scenarios (e.g., `$count`, some `$search`/`$filter` combinations on directory objects) [G1] - the connector must document which operations need it rather than sending it unconditionally.
**Nonfunctional requirements.** net10.0/Linux/Windows; HTTPS-only root.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphOptions(bool UseBetaEndpoint = false) : IValidatableOptions;
public sealed partial class GraphClient { public GraphClient(IntegrationTransport transport, GraphOptions options); }
```
**Official API endpoints, methods and versions (verified).** v1.0/beta base URLs, error-body shape (with the casing caveat), `ConsistencyLevel` header - all per [G1].
**Authentication and authorization.** Delegates to MGR-001's token source.
**Required API permissions, scopes or licenses.** None beyond MGR-001.
**Pagination, throttling and retry behavior.** Delegated to MGR-019/022.
**Error handling and concurrency.** `GraphApiException.FromTransportFailure` wraps single-resource reads, accepting both `innerError`/`innererror` casings when parsing.
**Data privacy and security.** No credentials or tenant data logged by the client skeleton.
**Dependencies.** MGR-001, MGR-002.
**Unit-test scenarios.** v1.0 vs. beta base URL selection; error body parses correctly under both casing variants; `ConsistencyLevel` header sent only for the documented operations that need it.
**Contract-test scenarios.** Synthetic fixture for both error-casing variants.
**Integration-test requirements.** None beyond MGR-001's.
**Acceptance criteria.** Every Graph request targets the correct version root and parses errors correctly regardless of casing.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The `innerError`/`innererror` casing inconsistency is a genuine, unresolved vendor-documentation inconsistency, not an SDK defect - documented as such.

<a id="mgr-004"></a>
### MGR-004 - Users and profiles

**Business purpose.** Read and manage directory user profiles.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None Graph-specific.
**Functional requirements.** `GET /users`, `GET /users/{id|userPrincipalName}`, `PATCH /users/{id}`, `POST /users` [G1, confirmed resource existence via overview/permissions pages]. `/me` is a convenience alias for the signed-in user and is **delegated-only** - application permissions are explicitly not supported for `/me`, since there is no signed-in user context in an app-only call [verified via Graph research]. Standard query options (`$select`/`$filter`/`$orderby`/`$expand`/`$search`/`$count`) apply per MGR-019's pagination/delta mechanics.
**Nonfunctional requirements.** Explicit write opt-in gate on create/update.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphUser { public string? Id { get; set; } public string? DisplayName { get; set; } public string? UserPrincipalName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphUser> EnumerateUsersAsync(GraphQuery? query = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/users` CRUD confirmed via [G1]/[G2]; `/me` delegated-only restriction independently confirmed in the Graph research pass.
**Authentication and authorization.** `User.Read.All`/`User.ReadWrite.All` (application) or `User.Read`/`User.ReadWrite` (delegated), per [G2]/[G11].
**Required API permissions, scopes or licenses.** Admin-consented application permission for app-only access; a signed-in user for `/me`.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** Calling `/me` with an app-only token should fail fast with a clear, classified error rather than a confusing generic 400/403.
**Data privacy and security.** User profile data is PII; never logged by default.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** `/me` rejected locally (or clearly classified) when only an app-only token source is configured; user CRUD composes correctly; pagination/delta reuse verified.
**Contract-test scenarios.** Synthetic fixture for a user list/page.
**Integration-test requirements.** Authorized tenant with real users; not available this pass.
**Acceptance criteria.** User CRUD and the `/me` delegated-only restriction both behave correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-005"></a>
### MGR-005 - Groups and memberships

**Business purpose.** Read and manage directory groups and their membership.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None Graph-specific.
**Functional requirements.** `GET /groups`, `GET /groups/{id}/members`, `POST /groups/{id}/members/$ref`, `DELETE /groups/{id}/members/{id}/$ref` [G1/G2, resource existence confirmed]. Personal Microsoft accounts (MSA) are **not supported** for groups operations - tenant/organizational accounts only [confirmed in Graph research]. **Flagged documentation anomaly, not independently resolved**: one permissions table lists `Group-NestingSupport.ReadWrite.All` as the "least privileged" option for what is described as a read operation - an unusual pairing (a ReadWrite-named scope as the minimum for a read) that was not independently reconciled against a second source and is carried forward as an open item rather than silently corrected.
**Nonfunctional requirements.** Explicit write opt-in gate on membership add/remove.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphGroup { public string? Id { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphUser> EnumerateGroupMembersAsync(string groupId, GraphQuery? query = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/groups`, `/groups/{id}/members`, `$ref` add/remove per [G1]/[G2].
**Authentication and authorization.** `Group.Read.All`/`GroupMember.ReadWrite.All` (application) or delegated equivalents [G11].
**Required API permissions, scopes or licenses.** Admin-consented application permission; MSA accounts unsupported.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** A membership operation attempted against an MSA-only context should fail clearly, not silently no-op.
**Data privacy and security.** Group membership can reveal organizational structure; treated as business data.
**Dependencies.** MGR-001, MGR-003, MGR-004 (members are users).
**Unit-test scenarios.** Membership add/remove `$ref` composition; member enumeration pagination.
**Contract-test scenarios.** Synthetic fixture for group + membership list.
**Integration-test requirements.** Authorized tenant with real groups; not available this pass.
**Acceptance criteria.** Group read and membership mutation both work correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The `Group-NestingSupport.ReadWrite.All` "least privileged for a read" anomaly is an unresolved documentation artifact, not confirmed as the SDK's actual minimum-permission recommendation (Section 28).

<a id="mgr-006"></a>
### MGR-006 - Directory roles and admin units

**Business purpose.** Read directory role assignments and administrative unit scoping, for governance/reporting integrations.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /directoryRoles` for role enumeration. **Correction to an initial assumption carried from the task's own feature description**: the actual path for administrative units is the nested `GET /directory/administrativeUnits`, not a flat top-level `/administrativeUnits` - independently confirmed in the Graph research pass. Permission: `RoleManagement.Read.Directory` (read) / `RoleManagement.ReadWrite.Directory` (write) [G11].
**Nonfunctional requirements.** Read-only scope for this P1 milestone; writes deferred to a later milestone if ever justified.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphDirectoryRole { public string? Id { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphDirectoryRole> EnumerateDirectoryRolesAsync(CancellationToken ct = default);
public IAsyncEnumerable<JsonDocument> EnumerateAdministrativeUnitsAsync(CancellationToken ct = default); // GET /directory/administrativeUnits
```
**Official API endpoints, methods and versions (verified).** `/directoryRoles` and the corrected `/directory/administrativeUnits` path, independently confirmed.
**Authentication and authorization.** `RoleManagement.Read.Directory` (application), admin-consented.
**Required API permissions, scopes or licenses.** Privileged Role Administrator or Global Administrator typically grants the underlying permission.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** Standard classification.
**Data privacy and security.** Role-assignment data is sensitive governance metadata.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** `/directory/administrativeUnits` path composed correctly (not the incorrect flat path); role enumeration pagination.
**Contract-test scenarios.** Synthetic fixture for both resources.
**Integration-test requirements.** Authorized tenant with configured admin units; not available this pass.
**Acceptance criteria.** Both resources are read correctly using the verified paths.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-007"></a>
### MGR-007 - App registrations and service principals

**Business purpose.** Read and manage Entra app registrations and their corresponding service principals, for tenant-automation scenarios.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET/POST /applications`, `GET/POST /servicePrincipals` [confirmed resource existence]. Permissions: `Application.Read.All`/`Application.ReadWrite.All`/`Application.ReadWrite.OwnedBy` [confirmed]; creating a new app registration additionally requires `AppRegistration.Create` or equivalent delegated/admin capability [confirmed].
**Nonfunctional requirements.** Explicit write opt-in gate on create/update; this feature is inherently privileged and should default to read-only usage in samples.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphApplication { public string? Id { get; set; } public string? AppId { get; set; } public string? DisplayName { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<GraphApplication> GetApplicationAsync(string id, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/applications`, `/servicePrincipals`, and the three permission variants, confirmed.
**Authentication and authorization.** `Application.ReadWrite.OwnedBy` is the least-privileged option for an app managing only its own registration.
**Required API permissions, scopes or licenses.** Admin-consented application permission; app-registration creation specifically may require an elevated role beyond plain API permission.
**Pagination, throttling and retry behavior.** Reuses MGR-019/MGR-021.
**Error handling and concurrency.** Standard classification.
**Data privacy and security.** App registration metadata can include client secrets' hashes/expiry (not the secret itself) - treated as sensitive configuration metadata.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** Read/create composition for both resources; `OwnedBy`-scoped permission distinguished from tenant-wide in documentation/tests.
**Contract-test scenarios.** Synthetic fixtures for application/service-principal shapes.
**Integration-test requirements.** Authorized tenant; not available this pass.
**Acceptance criteria.** Both resources read/create correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-008"></a>
### MGR-008 - Organization and tenant info

**Business purpose.** Read basic tenant-level organization metadata (verified domains, display name) for diagnostics and multi-tenant routing.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /organization`, `GET /organization/{id}` [confirmed]. **Important caveat, independently confirmed**: a caller holding only the `User.Read` permission sees a sharply restricted response containing only `id`, `displayName`, and `verifiedDomains` - every other field (e.g. licensing, technical contacts) is returned `null`, not omitted - this is a documented least-privilege response shape, not a bug, and the SDK's typed model must treat those fields as nullable rather than assuming they are always populated.
**Nonfunctional requirements.** Read-only.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphOrganization(string Id, string? DisplayName, IReadOnlyList<string>? VerifiedDomains);
public async Task<GraphOrganization> GetOrganizationAsync(CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/organization`, confirmed; the `User.Read`-only restricted-field caveat independently confirmed.
**Authentication and authorization.** `Organization.Read.All` (full detail) or just `User.Read` (restricted detail) [G11].
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Single read, not paginated.
**Error handling and concurrency.** Standard classification.
**Data privacy and security.** Organization metadata is low-sensitivity tenant configuration, with licensing details being the more sensitive subset gated behind the higher permission.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** Restricted-permission response correctly maps unpopulated fields to `null` rather than throwing or defaulting incorrectly.
**Contract-test scenarios.** Synthetic fixtures for both the full-detail and restricted-detail response shapes.
**Integration-test requirements.** Authorized tenant; not available this pass.
**Acceptance criteria.** Both permission-levels' response shapes are handled correctly.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-009"></a>
### MGR-009 - OneDrive drives and items

**Business purpose.** Enumerate and navigate a user's or site's OneDrive drive and its items.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /me/drive`, `GET /users/{id}/drive`, `GET /drives/{id}`, `GET /drives` [confirmed]. **Important, independently confirmed constraint**: application (app-only) permissions are **not supported** for a single `GET /me/drive` or `GET /users/{id}/drive` call - app-only access must go through `GET /drives` (the list form) instead; this asymmetry (list works app-only, single-resource shortcut doesn't) must be documented, not treated as a bug. A `404` on `/me/drive` can trigger automatic OneDrive provisioning for the user, but this behavior is **delegated-only** [confirmed].
**Nonfunctional requirements.** Read-only for this feature; writes are MGR-010's.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphDriveItem { public string? Id { get; set; } public string? Name { get; set; } public long? Size { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphDriveItem> EnumerateDriveItemsAsync(string driveId, string? itemId = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/me/drive`, `/users/{id}/drive`, `/drives/{id}`, `/drives` and the app-only-vs-delegated asymmetry, all independently confirmed.
**Authentication and authorization.** `Files.Read.All` (application, via `/drives` only) or delegated `Files.Read`/`Files.Read.All` (via `/me/drive` too) [G11].
**Required API permissions, scopes or licenses.** As above; app-only callers must target `/drives/{id}` rather than the `/me` or `/users/{id}/drive` shortcuts.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** An app-only call against `/me/drive` should fail fast and clearly, pointing the caller at `/drives`, rather than a confusing generic 400.
**Data privacy and security.** Drive item metadata (names, sizes) may reveal sensitive file organization; not specially redacted beyond the SDK's general policy.
**Dependencies.** MGR-001, MGR-003, MGR-004 (for `/users/{id}/drive`).
**Unit-test scenarios.** App-only call against `/me/drive` is rejected locally with a clear error pointing at `/drives`; `/drives` list enumeration works app-only; delegated `/me/drive` 404-triggers-provisioning behavior documented but not independently tested (requires a live, un-provisioned user).
**Contract-test scenarios.** Synthetic fixtures for `/drives` list and a single drive's items.
**Integration-test requirements.** Authorized tenant with OneDrive provisioned; not available this pass.
**Acceptance criteria.** The app-only/delegated asymmetry is enforced locally, preventing a confusing live failure.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-010"></a>
### MGR-010 - File upload, download and sessions

**Business purpose.** Upload and download file content to/from OneDrive/SharePoint drives, including large files via resumable upload sessions.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationTransport.CopyToAsync` reused for streaming primitives; the upload-session chunking protocol parallels DVT-016's chunked-upload pattern conceptually, though the exact byte-range rules differ (MGR-023).
**Functional requirements. [Flagged conflicting official figures - carried forward honestly, not reconciled by guessing.]** Simple (single-request) upload size limit: the v1.0 API reference states a **250 MB** hard limit for simple upload via `PUT /drives/{id}/items/{id}/content` (or `:/path:/content`), while a separate, older OneDrive developer-center page states **<4 MB** - these two official figures conflict and were not reconciled during this research pass; the SDK must document both and default to the more conservative figure (4 MB) as the simple-upload ceiling until independently reconciled, falling back to upload sessions (MGR-023) above that threshold. Download: `GET /drives/{id}/items/{id}/content` (redirects to a pre-authenticated download URL in practice). App-only access to upload *sessions* specifically has only one permission option, `Sites.ReadWrite.All` - there is no lower-privilege app-only tier documented for this operation, unlike most other Graph writes. A file protected by a sensitivity label cannot be replaced via the content-replace endpoint while app-only - sensitivity-label-protected files require delegated access for content replacement.
**Nonfunctional requirements.** Streamed upload/download (no whole-file buffering); cancellation-aware mid-transfer; explicit write opt-in gate.
**Public C# interfaces/model contracts (proposed).**
```csharp
public async Task UploadSmallFileAsync(string driveId, string parentPath, string fileName, Stream content, bool writesEnabled = false, CancellationToken ct = default); // <= 4MB conservative ceiling
public async Task DownloadFileAsync(string driveId, string itemId, Stream destination, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Upload/download endpoints confirmed via [G1]/[G9]; the 250MB-vs-4MB conflicting figures and the sensitivity-label/app-only restriction are independently confirmed as documented facts (the conflict itself, not a resolved single number).
**Authentication and authorization.** `Files.ReadWrite.All` (delegated or application) for most operations; `Sites.ReadWrite.All` specifically for app-only upload sessions.
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Not paginated; large files use MGR-023's session mechanism instead.
**Error handling and concurrency.** A simple-upload request exceeding the conservative 4MB ceiling is routed to the upload-session path automatically rather than attempted and failed against the server's actual (unreconciled) limit.
**Data privacy and security.** File content is caller-owned, potentially sensitive; never buffered into logs; sensitivity-label-protected files are a documented access-restriction case, not an SDK bug when encountered.
**Dependencies.** MGR-009, MGR-023 (for files exceeding the simple-upload ceiling).
**Unit-test scenarios.** Files at/above the 4MB conservative threshold are routed to session-based upload, not attempted via simple upload; download streams correctly; app-only upload session correctly uses only `Sites.ReadWrite.All`.
**Contract-test scenarios.** Synthetic fixtures for both simple upload and the session hand-off.
**Integration-test requirements.** Authorized tenant with a file of known size to directly resolve the 250MB-vs-4MB conflict; not available this pass.
**Acceptance criteria.** The conservative ceiling is enforced until independently reconciled; download and simple upload work correctly against synthetic fixtures.
**Definition of Done.** Standard template; the simple-upload size-limit conflict is an explicit release blocker before this feature is marked `verified` (Section 28).
**Known limitations and blockers.** The 250MB-vs-4MB simple-upload limit conflict is the most consequential unresolved Graph documentation discrepancy in this catalog; sensitivity-label-protected files cannot be replaced app-only.

<a id="mgr-011"></a>
### MGR-011 - SharePoint sites and document libraries

**Business purpose.** Resolve and navigate SharePoint sites and their document libraries (which are themselves Drive resources, MGR-009).
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** Site ID is a 3-part composite string: `{hostname},{site-collection-guid},{site-guid}` [confirmed]. Resolution paths: `GET /sites/root` (tenant root site), `GET /sites/{hostname}` (e.g. `contoso.sharepoint.com`), `GET /sites/{hostname}:/{server-relative-path}` (a specific site by path) [confirmed]. A site's document libraries are exposed as Drive resources via `GET /sites/{id}/drives`, reusing MGR-009's Drive model.
**Nonfunctional requirements.** Read-only for this feature.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphSite(string Id, string? DisplayName, string? WebUrl);
public async Task<GraphSite> GetSiteByPathAsync(string hostname, string? serverRelativePath = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Composite site-ID shape and all three resolution paths, confirmed.
**Authentication and authorization.** `Sites.Read.All` (application or delegated) [G11].
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Single reads; drive-list reuses MGR-019.
**Error handling and concurrency.** Standard classification; a malformed composite site ID is rejected locally before send.
**Data privacy and security.** Site metadata is organizational, not row-level business data.
**Dependencies.** MGR-001, MGR-003, MGR-009.
**Unit-test scenarios.** All three resolution-path URL compositions match the documented syntax; composite site-ID parsing round-trips.
**Contract-test scenarios.** Synthetic fixture for a site resolved by path.
**Integration-test requirements.** Authorized tenant with SharePoint provisioned; not available this pass.
**Acceptance criteria.** Site resolution works correctly for all three documented path forms.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-012"></a>
### MGR-012 - SharePoint lists and items

**Business purpose.** Read and write SharePoint list items (distinct from document-library files).
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /sites/{id}/lists`, `GET /sites/{id}/lists/{id}/items`, `POST /sites/{id}/lists/{id}/items`, `PATCH /sites/{id}/lists/{id}/items/{id}/fields` [confirmed]. **Important, independently confirmed constraint**: lists are reached only through a site-scoped path - **there is no top-level `/lists/{id}` resource**; every list operation must be addressed via its owning site ID.
**Nonfunctional requirements.** Explicit write opt-in gate on item create/update.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphListItem(string Id, IReadOnlyDictionary<string, JsonElement> Fields);
public async Task<string> CreateListItemAsync(string siteId, string listId, JsonElement fields, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Site-scoped list/item paths and the no-top-level-resource constraint, confirmed.
**Authentication and authorization.** `Sites.ReadWrite.All` (application or delegated) for writes; `Sites.Read.All` for reads [G11].
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** A caller attempting a bare `/lists/{id}` path (without a site prefix) is rejected locally, since this is a connector-level API-shape constraint the SDK should enforce rather than let fail confusingly server-side.
**Data privacy and security.** List item field values are caller-owned business data.
**Dependencies.** MGR-011.
**Unit-test scenarios.** Site-scoped path composed correctly; item create/update respects the write-opt-in gate; field update targets `/fields` sub-resource correctly.
**Contract-test scenarios.** Synthetic fixture for a list + items page.
**Integration-test requirements.** Authorized tenant with a SharePoint list; not available this pass.
**Acceptance criteria.** List/item CRUD works correctly via the site-scoped path only.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-013"></a>
### MGR-013 - Outlook mail

**Business purpose.** Read and send mail via Outlook/Exchange Online.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /me/messages` / `GET /users/{id}/messages` - default page size **10**, `$top` max **1000** [confirmed]; message bodies are **HTML-only** in the documented response shape (no separate plain-text content type returned alongside) [confirmed]. `POST /me/sendMail` - returns **202 Accepted with no response body** [confirmed].
**Nonfunctional requirements.** Explicit write opt-in gate on send.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphMessage { public string? Id { get; set; } public string? Subject { get; set; } public string? BodyHtml { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task SendMailAsync(string subject, string bodyHtml, IReadOnlyList<string> toRecipients, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/messages` paging defaults/max, HTML-only body, `/sendMail`'s 202-no-body response - all confirmed.
**Authentication and authorization.** `Mail.Read`/`Mail.ReadWrite`/`Mail.Send` (application or delegated) [G11].
**Required API permissions, scopes or licenses.** As above; application-permission mail access is a notably sensitive grant and should be flagged prominently in documentation.
**Pagination, throttling and retry behavior.** Reuses MGR-019; default page size of 10 is small relative to other Graph resources and should be documented so callers don't assume a larger implicit batch.
**Error handling and concurrency.** `/sendMail`'s 202-no-body response must not be misinterpreted as "no confirmation the mail was queued" - it is the documented success shape, not an error.
**Data privacy and security.** Mail content/metadata is highly sensitive PII; never logged by default under any circumstance.
**Dependencies.** MGR-001, MGR-003, MGR-004.
**Unit-test scenarios.** Default page size of 10 handled correctly without assuming a larger batch; `$top` clamped to the documented max of 1000; `/sendMail` 202-no-body treated as success.
**Contract-test scenarios.** Synthetic fixture for a message list page and a sendMail call.
**Integration-test requirements.** Authorized tenant with Exchange Online mailboxes; not available this pass.
**Acceptance criteria.** Mail read/send work correctly against synthetic fixtures with the documented defaults respected.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-014"></a>
### MGR-014 - Outlook calendar

**Business purpose.** Read and manage Outlook calendar events.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /me/events`, `GET /users/{id}/events`, `POST .../events`, `PATCH`/`DELETE .../events/{id}` [confirmed]. **Notable, independently confirmed contrast with Teams/Planner (MGR-015 through MGR-017)**: calendar application permissions are **fully supported and symmetric** with delegated permissions - `Calendars.ReadBasic`/`Calendars.Read`/`Calendars.ReadWrite` are identical in capability across both delegated and application permission types, unlike Teams (RSC-gated) or Planner (app-only unsupported entirely).
**Nonfunctional requirements.** Explicit write opt-in gate on create/update/delete.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphEvent { public string? Id { get; set; } public string? Subject { get; set; } public DateTimeOffset? Start { get; set; } public DateTimeOffset? End { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<string> CreateEventAsync(string userId, GraphEvent @event, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/events` CRUD and the full app-only/delegated symmetry claim, confirmed.
**Authentication and authorization.** `Calendars.ReadBasic`/`Calendars.Read`/`Calendars.ReadWrite`, application or delegated, with identical capability.
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** Standard classification.
**Data privacy and security.** Calendar data can reveal sensitive scheduling/business information; not specially redacted beyond the SDK's general policy.
**Dependencies.** MGR-001, MGR-003, MGR-004.
**Unit-test scenarios.** Event CRUD composition; app-only vs. delegated symmetry is reflected correctly (no artificial restriction imposed where the vendor imposes none).
**Contract-test scenarios.** Synthetic fixture for an event list and a created event.
**Integration-test requirements.** Authorized tenant with Exchange Online calendars; not available this pass.
**Acceptance criteria.** Calendar CRUD works identically under app-only and delegated tokens, matching the documented symmetry.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-015"></a>
### MGR-015 - Teams and channels

**Business purpose.** Enumerate Microsoft Teams and their channels.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /teams`, `GET /teams/{id}/channels` [confirmed]. **Important, independently confirmed caveat**: `GET /teams` (the tenant-wide list) returns **only** `id`, `displayName`, `description`, `visibility` - every other property is returned `null`, not omitted, regardless of permission level; richer per-team detail requires a separate `GET /teams/{id}` call. Application permissions **are supported** for team/channel enumeration (unlike MGR-017's Planner), but every personal Microsoft account (MSA) scenario is unsupported for **any** Teams operation across this entire feature area [confirmed].
**Nonfunctional requirements.** Read-only for this feature.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphTeamSummary(string Id, string? DisplayName, string? Description, string? Visibility);
public IAsyncEnumerable<GraphTeamSummary> EnumerateTeamsAsync(CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/teams`, `/teams/{id}/channels`, the restricted-field-list-on-list-call caveat, and the MSA-unsupported constraint, all confirmed.
**Authentication and authorization.** `Team.ReadBasic.All`/`Channel.ReadBasic.All` (application supported) [G11].
**Required API permissions, scopes or licenses.** Admin-consented application permission; tenant (not MSA) accounts only.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** A caller expecting populated fields from the `/teams` list call must be redirected (in documentation, not a runtime exception) to the per-team detail call.
**Data privacy and security.** Team/channel metadata is organizational, low-sensitivity.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** List-call field restriction correctly reflected in the typed model (nullable fields, not assumed-populated); channel enumeration pagination.
**Contract-test scenarios.** Synthetic fixtures for both the restricted list-call shape and a full per-team detail shape.
**Integration-test requirements.** Authorized tenant with Teams provisioned; not available this pass.
**Acceptance criteria.** Team/channel enumeration works correctly with the documented field restrictions respected.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-016"></a>
### MGR-016 - Teams messages and chats

**Business purpose.** Read and send messages in Teams channels and chats.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /teams/{id}/channels/{id}/messages`, `GET /chats/{id}/messages`, `POST` to either to send [confirmed]. **Important, independently confirmed nuance**: application permissions for chat/channel messages **do exist**, but the least-privileged tier is **Resource-Specific Consent (RSC)** - `ChatMessage.Read.Chat`/`ChannelMessage.Read.Group` - which is granted **per-chat/per-channel via a Teams app installation**, not tenant-wide admin consent. Higher, tenant-wide application alternatives (`Chat.Read.All`, etc.) exist but require full tenant-wide admin consent and are a materially different, more privileged configuration path than RSC. **Personal Microsoft accounts are unsupported for every Teams operation**, including this one, consistent with MGR-015.
**Nonfunctional requirements.** Explicit write opt-in gate on send; RSC vs. tenant-wide permission choice must be an explicit configuration decision documented to the caller, not silently defaulted.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphChatMessage { public string? Id { get; set; } public string? Body { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<string> SendChannelMessageAsync(string teamId, string channelId, string body, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** Message read/send endpoints and the RSC-vs-tenant-wide application-permission distinction, confirmed.
**Authentication and authorization.** RSC (`ChatMessage.Read.Chat`/`ChannelMessage.Read.Group`, per-resource consent via app install) or tenant-wide (`Chat.Read.All`, full admin consent) [G11].
**Required API permissions, scopes or licenses.** One of the two paths above, explicitly chosen; MSA unsupported.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** A send attempted against a chat/channel the RSC-consented Teams app was never installed into fails with a distinct, documented authorization error, not a generic 403.
**Data privacy and security.** Chat/channel message content is highly sensitive communication data; never logged by default.
**Dependencies.** MGR-015.
**Unit-test scenarios.** RSC-scoped vs. tenant-wide permission configuration both compose correctly; message send/read work under either.
**Contract-test scenarios.** Synthetic fixtures for both permission configurations.
**Integration-test requirements.** Authorized tenant with a Teams app installed for RSC; not available this pass.
**Acceptance criteria.** Both the RSC and tenant-wide permission paths are documented and work correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** RSC's per-resource consent model is materially more complex to configure than a typical tenant-wide application permission and must be documented clearly to avoid caller confusion (Section 28).

<a id="mgr-017"></a>
### MGR-017 - Planner

**Business purpose.** Read and manage Microsoft Planner plans, buckets, and tasks.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P2. **Milestone.** P2-D7.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /me/planner/plans`, `GET /planner/plans/{id}/tasks`, etc. [confirmed resource existence]. **Critical, explicitly and independently confirmed constraint**: the official documentation states, verbatim in substance, that **application permissions are "Not supported"** for Planner - confirmed explicitly in the permissions table for both read and write operation tiers. **Planner is delegated-only; it cannot be driven by a pure unattended daemon app** - this is the single clearest "do not even attempt app-only" case in the entire 70-feature catalog and must be documented prominently, not discovered by a caller via a confusing runtime failure.
**Nonfunctional requirements.** Delegated-token-source required; the connector should fail configuration validation fast if only an app-only token source is wired in for this feature.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphPlannerTask { public string? Id { get; set; } public string? Title { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphPlannerTask> EnumeratePlanTasksAsync(string planId, CancellationToken ct = default); // delegated-only
```
**Official API endpoints, methods and versions (verified).** Plan/task resource shapes and the explicit app-only-unsupported constraint, confirmed.
**Authentication and authorization.** Delegated only - `Tasks.Read`/`Tasks.ReadWrite` (delegated).
**Required API permissions, scopes or licenses.** A signed-in user context; no application-permission path exists at all.
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** Configuration validation rejects an app-only-only setup for this feature before any call is attempted.
**Data privacy and security.** Task content is caller-owned business data.
**Dependencies.** MGR-001 (delegated path specifically).
**Unit-test scenarios.** App-only-only configuration is rejected locally with a clear, specific message naming Planner's delegated-only constraint - this is the primary test for this feature, more important than ordinary CRUD coverage.
**Contract-test scenarios.** Synthetic fixture for a plan/task list under a delegated token.
**Integration-test requirements.** Authorized tenant with a signed-in test user and Planner provisioned; not available this pass.
**Acceptance criteria.** The delegated-only constraint is enforced locally and documented prominently.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No application-permission path exists at all for Planner - this is a vendor constraint, not an SDK limitation to be worked around.

<a id="mgr-018"></a>
### MGR-018 - Microsoft To Do

**Business purpose.** Read and manage personal To Do task lists.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P2. **Milestone.** P2-D7.
**Current implementation status.** Not started.
**Repository evidence.** None.
**Functional requirements.** `GET /me/todo/lists`, `GET /me/todo/lists/{id}/tasks` [confirmed]. **Corrects a common misconception that To Do is delegated-only like Planner**: the official overview explicitly states "The API supports both delegated and application permissions" - independently confirmed, directly quoting the official overview. The application-permission read path uses `Tasks.Read.All`, with **no higher-tier application permission found** for read access beyond that single scope. **Not independently verified this pass**: whether a corresponding application-permission write scope exists with equivalent symmetry - only the read-side application permission was independently checked.
**Nonfunctional requirements.** Explicit write opt-in gate on task create/update.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed class GraphTodoTask { public string? Id { get; set; } public string? Title { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphTodoTask> EnumerateTodoTasksAsync(string listId, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/me/todo/lists`/`tasks` endpoints and the both-delegated-and-application-permission support, confirmed via direct quote of the official overview.
**Authentication and authorization.** `Tasks.Read.All` (application, read confirmed) or `Tasks.ReadWrite`/`Tasks.Read` (delegated).
**Required API permissions, scopes or licenses.** As above; write-side application-permission symmetry is unconfirmed (see below).
**Pagination, throttling and retry behavior.** Reuses MGR-019.
**Error handling and concurrency.** Standard classification.
**Data privacy and security.** Personal task content is PII; never logged by default.
**Dependencies.** MGR-001, MGR-003.
**Unit-test scenarios.** Application-permission read path (`Tasks.Read.All`) works correctly; a write attempted under an application-only token source is explicitly flagged as unverified in test documentation rather than silently assumed to work (since write-side app-permission symmetry was not independently confirmed).
**Contract-test scenarios.** Synthetic fixture for a task-list read under an application token.
**Integration-test requirements.** Authorized tenant with To Do provisioned, specifically to confirm write-side application-permission support; not available this pass.
**Acceptance criteria.** Read access works under both delegated and application tokens; write-side application-permission support is explicitly marked unconfirmed until independently verified.
**Definition of Done.** Standard template; write-side app-permission confirmation is an explicit release blocker for calling the write path `verified` (Section 28).
**Known limitations and blockers.** Write-side application-permission symmetry with the read path is unconfirmed.

<a id="mgr-019"></a>
### MGR-019 - Pagination and delta queries

**Business purpose.** Traverse large result sets page-by-page, and extract only changed/deleted resources since a prior checkpoint, across every delta-capable Graph resource.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `Page<T>`/`ContinuationToken` reused for `@odata.nextLink`; a new `ContinuationToken`-wrapped delta-link mechanism parallels DVT-012's `@odata.deltaLink` reuse decision exactly (Section 8.2, item 3).
**Functional requirements.** `@odata.nextLink` for ordinary paging; `@odata.deltaLink` for delta queries - **a single response carries either one or the other, never both** [G5, G6, confirmed]. `$deltatoken=latest` is a documented shortcut to start a fresh delta session without enumerating full history first [G6]. Token duration varies by resource family: directory/education objects have a **fixed 7-day** delta-token lifetime; Outlook entities have **no fixed limit**, instead relying on an LRU-cache-dependent server-side retention that can evict a token earlier under load [G6]. An expired/evicted delta token surfaces as **`410 Gone`** with error code `syncStateNotFound` [G6]. Approximately 26 resource types support delta queries (confirmed list includes `application`, `administrativeUnit`, `chatMessage`, `contact`, `device`, `directoryRole`, `driveItem`, several `education*` types, `event`, `group`, `listItem`, `mailFolder`, `message`, `orgContact`, `oAuth2PermissionGrant`, `plannerUser`, `site`, `servicePrincipal`, `todoTask`, `todoTaskList`, `user`) - **`plannerBucket` delta support is beta-only**, not available in v1.0 [G6]. Delta queries are **not supported in Entra External ID / Azure AD B2C tenants** [G6].
**Nonfunctional requirements.** Bounded max-pages guard and cycle detection, matching every existing connector's pagination safety net.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphDeltaPage<T>(IReadOnlyList<T> Items, ContinuationToken? DeltaLink);
public IAsyncEnumerable<T> EnumerateAsync<T>(string resourcePath, GraphQuery? query = null, CancellationToken ct = default);
public Task<GraphDeltaPage<T>> GetDeltaAsync<T>(string resourcePath, ContinuationToken? resumeFrom = null, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `@odata.nextLink`/`@odata.deltaLink` mechanics, token-duration distinctions, `410 Gone`/`syncStateNotFound`, the ~26-resource delta-capable list (with the `plannerBucket` beta-only caveat), and the Entra External ID/B2C unsupported note - all per [G6].
**Authentication and authorization.** Unchanged from the underlying resource's own requirement.
**Required API permissions, scopes or licenses.** None beyond the underlying resource's.
**Pagination, throttling and retry behavior.** GET pages reuse the shared GET-retry policy; delta tokens are treated as opaque via `ContinuationToken`.
**Error handling and concurrency.** `410 Gone`/`syncStateNotFound` classifies as a distinct, recoverable-via-`$deltatoken=latest`-restart condition, not a generic contract error.
**Data privacy and security.** Delta/next tokens treated as sensitive opaque values, never logged.
**Dependencies.** MGR-003.
**Unit-test scenarios.** A response carrying `@odata.nextLink` is distinguished from one carrying `@odata.deltaLink`, never both; `410`/`syncStateNotFound` classifies distinctly and the SDK surfaces a clear "restart via $deltatoken=latest" recovery path; a resource not in the delta-capable list is rejected locally for delta use (falls back to plain pagination only).
**Contract-test scenarios.** Synthetic fixtures for ordinary paging, a successful delta page, and an expired-token 410 response.
**Integration-test requirements.** Authorized tenant to directly observe token-expiry timing per resource family; not available this pass.
**Acceptance criteria.** Both pagination mechanisms are handled correctly and distinguished from one another; delta-capability is checked against the documented resource list before attempting a delta query.
**Definition of Done.** Standard template.
**Known limitations and blockers.** Exact LRU-eviction timing for Outlook-entity delta tokens cannot be predicted (by design, per the vendor documentation itself) and must be documented as "no fixed SLA," not estimated.

<a id="mgr-020"></a>
### MGR-020 - Change notifications and subscriptions

**Business purpose.** Receive webhook push notifications when a resource changes, instead of polling delta queries continuously.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** None; this is a webhook-receiver capability with no Phase 1 analog.
**Functional requirements.** Create: `POST /subscriptions` [G8]. Webhook validation handshake, exact timing confirmed: Graph POSTs the new subscription endpoint a request containing `?validationToken=`, and the endpoint must echo that token back as **plain text within 10 seconds** [G8]. Normal notification delivery must be acknowledged (2xx) within **3 seconds**; on failure to ack, Graph retries delivery for up to **4 hours**, each attempt with a **10-second** timeout [G8]. Maximum subscription expiration varies sharply by resource type: as short as **60 minutes** for `presence`, up to **43,200 minutes (~30 days)** for several Teams/security resource types [G8]; Outlook messages/events/contacts cap at **10,080 minutes (7 days)**, or just **1,440 minutes (1 day)** if `includeResourceData` is requested [G8]. Three lifecycle notification types exist: `reauthorizationRequired`, `subscriptionRemoved`, `missed` - lifecycle notifications must be acknowledged with **202** [G8].
**Nonfunctional requirements.** The webhook receiver endpoint itself is the caller's responsibility to host (this SDK is a client/subscription-manager, not a webhook host); subscription-renewal scheduling is a connector-provided helper given the wide variance in max expiration per resource type.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphSubscription(string Id, string Resource, string ChangeType, DateTimeOffset ExpirationDateTime, string NotificationUrl);
public async Task<GraphSubscription> CreateSubscriptionAsync(string resource, string changeType, Uri notificationUrl,
    DateTimeOffset expirationDateTime, bool includeResourceData = false, bool writesEnabled = false, CancellationToken ct = default);
public async Task RenewSubscriptionAsync(string subscriptionId, DateTimeOffset newExpirationDateTime, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/subscriptions`, the 10-second validation-echo window, the 3-second-ack/4-hour-retry/10-second-per-attempt delivery SLA, the per-resource-type expiration table, and the three lifecycle-notification types - all per [G8].
**Authentication and authorization.** Resource-specific read permission (e.g. `Mail.Read` to subscribe to message changes) plus, for lifecycle robustness, the ability to re-authorize.
**Required API permissions, scopes or licenses.** Matches whatever the subscribed resource itself requires for a read.
**Pagination, throttling and retry behavior.** Not paginated; subscription renewal must be scheduled proactively given the short expiration windows for some resource types (as short as 60 minutes).
**Error handling and concurrency.** A `reauthorizationRequired` lifecycle event must trigger a token refresh / re-consent flow, not be silently dropped; `subscriptionRemoved` means the subscription must be recreated, not renewed.
**Data privacy and security.** Notification payloads by default carry only resource identifiers, not resource content, unless `includeResourceData` is explicitly requested - a deliberately higher-sensitivity, shorter-expiration opt-in the SDK should surface as a distinct, clearly-labeled option.
**Dependencies.** MGR-003.
**Unit-test scenarios.** Validation-token echo composed correctly as plain text; renewal scheduling respects the per-resource-type max-expiration table; lifecycle-event handling distinguishes all three types correctly.
**Contract-test scenarios.** Synthetic fixtures for subscription creation, the validation handshake, and each lifecycle notification type.
**Integration-test requirements.** A publicly reachable webhook endpoint and an authorized tenant; not available in a documentation-only pass.
**Acceptance criteria.** Subscription creation, the validation handshake, and renewal scheduling all work correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No live tenant/public endpoint available this pass; the full per-resource-type max-expiration table was only partially captured in the underlying research and should be re-verified against the live documentation page before being treated as exhaustive.

<a id="mgr-021"></a>
### MGR-021 - Batch requests

**Business purpose.** Submit multiple independent or dependent Graph operations in a single HTTP round trip.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `BatchItemResult<T>` reused for the mixed-outcome response shape (Section 8.2, item 2).
**Functional requirements.** `POST /$batch` [G7]. **Confirmed exactly, verified twice in the fetched documentation text**: maximum **20 requests per batch**. `dependsOn` array on a sub-request enforces sequential ordering relative to named sibling requests; a broken dependency chain (a request depending on one that failed) returns **`424 Failed Dependency`** for the dependent request [G7]. Each sub-request is **independently throttle-evaluated** - a batch is not a way to evade per-request rate limits, each contained call counts individually against the caller's throttling budget (MGR-022) [G7].
**Nonfunctional requirements.** Bounded batch size (reject >20 locally before send); explicit write opt-in gate on any batch containing a write.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphBatchRequest(string Id, HttpMethod Method, string Url, JsonElement? Body = null, IReadOnlyList<string>? DependsOn = null);
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
    IReadOnlyList<GraphBatchRequest> requests, bool writesEnabled = false, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** `/$batch`, the 20-request max (confirmed twice), `dependsOn`/`424` semantics, and independent per-sub-request throttling - all per [G7].
**Authentication and authorization.** One bearer token covers the whole batch; individual sub-requests are still evaluated against the same permission requirements as if sent standalone.
**Required API permissions, scopes or licenses.** Union of whatever the contained sub-requests individually require.
**Pagination, throttling and retry behavior.** Each sub-request's 429 is independent and must be retried individually using its own `Retry-After`, not at the batch level (MGR-022).
**Error handling and concurrency.** A `dependsOn` chain broken by an earlier failure surfaces `424` for every downstream dependent request, distinguished from the original failure itself.
**Data privacy and security.** Batch bodies may contain business data across many resources at once; never logged by default.
**Dependencies.** MGR-003.
**Unit-test scenarios.** >20 requests rejected locally; `dependsOn` ordering respected; a broken dependency chain surfaces `424` for dependents, not a generic failure; per-sub-request 429 handled independently, not batch-wide.
**Contract-test scenarios.** Synthetic multi-request fixture including a `dependsOn` chain and a broken-dependency case.
**Integration-test requirements.** Authorized tenant; not available this pass.
**Acceptance criteria.** The 20-request cap, `dependsOn` ordering, and `424` propagation all behave correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-022"></a>
### MGR-022 - Throttling and Retry-After

**Business purpose.** Respect Graph's documented rate limits and recover automatically from 429 responses without the caller writing custom retry logic.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException(RetryAfter)` mapping and `TransportOptions`'s bounded-backoff GET-retry policy reused unchanged.
**Functional requirements.** `429 Too Many Requests` + `Retry-After` (seconds) [G3]. **Confirmed global default**: **130,000 requests per 10 seconds per app, across all tenants** [G3] - a tenant-agnostic, app-wide ceiling distinct from any per-tenant/per-resource limit. Per-service limits exist beyond the global default (sample figures captured for Bookings, Cloud Communications, Call Records, Excel, and Identity Protection) but the full per-service table was **only partially captured** in the underlying research and is **not exhaustive** - must not be treated as a complete reference. **Critical, explicitly confirmed caveat**: automatic SDK-level retry logic does **not** apply inside a batch (MGR-021) - each sub-request's 429 must be retried individually using its own `Retry-After`, since the outer batch response's own status does not itself carry a single aggregate retry delay.
**Nonfunctional requirements.** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged; writes are never auto-retried on 429.
**Public C# interfaces/model contracts (proposed).** None new - fully covered by the existing `ThrottleException`/`TransportOptions`.
**Official API endpoints, methods and versions (verified).** 429+`Retry-After`, the 130,000/10s global default, and the batch-retry caveat - all per [G3].
**Authentication and authorization.** Not applicable.
**Required API permissions, scopes or licenses.** None.
**Pagination, throttling and retry behavior.** This feature *is* the throttling/retry behavior; see Functional requirements.
**Error handling and concurrency.** 429 classifies via the unchanged shared classifier; a batch sub-request's individual 429 is retried at the sub-request level, not the batch level, which requires the connector's batch-execution code to re-issue only the failed sub-request(s), not the whole batch.
**Data privacy and security.** Not applicable.
**Dependencies.** MGR-003.
**Unit-test scenarios.** 429+`Retry-After` classifies as `ThrottleException`; a batch containing one throttled sub-request retries only that sub-request, not the entire batch; writes are never retried on 429.
**Contract-test scenarios.** Synthetic 429 fixtures for both a standalone request and a batch sub-request.
**Integration-test requirements.** Deliberately triggering a real 130,000-req/10s breach is infeasible for a documentation-only pass; not attempted.
**Acceptance criteria.** 429 responses are classified and retried (GET, standalone or per-sub-request) or surfaced without retry (writes) correctly against synthetic fixtures.
**Definition of Done.** Standard template.
**Known limitations and blockers.** The full per-service throttling-limit table is not exhaustive in the underlying research; only a representative sample of services was captured.

<a id="mgr-023"></a>
### MGR-023 - Large file transfer and resumability

**Business purpose.** Upload files too large for a single request via a resumable, chunked upload session.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P1. **Milestone.** P2-D5.
**Current implementation status.** Not started.
**Repository evidence.** Parallels DVT-016's chunked-upload pattern conceptually; the exact byte-range rule differs materially (320 KiB multiple here vs. Dataverse's 4 MiB recommended chunk with no multiple-of-N constraint).
**Functional requirements.** Create an upload session, then `PUT` sequential byte ranges to the returned `uploadUrl` [G9]. **Confirmed exactly**: each byte range (except the final one) **must be a multiple of 327,680 bytes (320 KiB)**; maximum **under 60 MiB** per individual PUT request; **5-10 MiB** is the documented recommended fragment size; ranges **must be uploaded in sequential order** - out-of-order fragments are not supported [G9]. `GET {uploadUrl}` returns `nextExpectedRanges` + `expirationDateTime` to resume an interrupted upload; **no fixed default session duration was found** - expiration is dynamic/sliding, not a documented constant [G9]. `DELETE {uploadUrl}` cancels the session, returning `204` [G9]. No absolute maximum file size ceiling was found in the fetched documentation for upload sessions generally (MGR-010's simple-upload ceiling is a separate, smaller-scale concern).
**Nonfunctional requirements.** Streamed chunked upload (no whole-file buffering); strict sequential-order enforcement; cancellation-aware mid-transfer.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphUploadSession(Uri UploadUrl, DateTimeOffset? ExpirationDateTime);
public async Task<GraphUploadSession> CreateUploadSessionAsync(string driveId, string parentPath, string fileName, long totalSize, bool writesEnabled = false, CancellationToken ct = default);
public async Task UploadSessionFragmentAsync(GraphUploadSession session, Stream fragment, long rangeStart, long rangeEnd, long totalSize, CancellationToken ct = default); // rangeEnd-rangeStart+1 must be a multiple of 327680, except the final fragment
public async Task CancelUploadSessionAsync(GraphUploadSession session, CancellationToken ct = default);
```
**Official API endpoints, methods and versions (verified).** The 327,680-byte multiple requirement, <60MiB-per-request cap, 5-10MiB recommendation, sequential-order requirement, `nextExpectedRanges`/`expirationDateTime`, and `DELETE`-cancels-with-204 - all confirmed per [G9].
**Authentication and authorization.** Same as MGR-010; app-only upload sessions specifically require `Sites.ReadWrite.All` with no lower tier.
**Required API permissions, scopes or licenses.** As per MGR-010.
**Pagination, throttling and retry behavior.** Not request-paginated; the session itself is the resumability mechanism - a dropped connection resumes via `GET {uploadUrl}`'s `nextExpectedRanges`, not a fresh session.
**Error handling and concurrency.** An out-of-order fragment is rejected locally before send (not merely left to the server to reject), since sequential order is a hard, documented requirement; a fragment size not a multiple of 327,680 bytes (except the final one) is rejected locally.
**Data privacy and security.** File content streamed, never buffered whole; sensitivity-label-protected files carry the same MGR-010 app-only-replace restriction.
**Dependencies.** MGR-009, MGR-010.
**Unit-test scenarios.** Fragment-size-not-a-multiple-of-320KiB rejected locally (except correctly allowing a smaller final fragment); out-of-order fragment rejected locally; interrupted upload resumes correctly via `nextExpectedRanges`; cancellation sends `DELETE` and expects `204`.
**Contract-test scenarios.** Synthetic multi-fragment upload-session fixture, including a resume-after-interruption scenario.
**Integration-test requirements.** Authorized tenant with a large file to upload; not available this pass.
**Acceptance criteria.** Chunked upload, resume, and cancellation all work correctly against synthetic fixtures with the exact byte-range rules enforced.
**Definition of Done.** Standard template.
**Known limitations and blockers.** No fixed session-expiration duration is documented (by design, it's dynamic) - the SDK must not assume or hardcode an expected lifetime.

<a id="mgr-024"></a>
### MGR-024 - Permission and consent diagnostics

**Business purpose.** Let a caller inspect which Graph permissions are actually granted/consented for their app registration, to diagnose "why did this call fail with 403" before it happens in production.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** None Graph-specific; conceptually parallel to every connector's `DiagnoseAsync`, extended here with a Graph-specific permission inspection step beyond the generic connectivity probe.
**Functional requirements.** `oAuth2PermissionGrant` resource shape (confirmed): `clientId`, `consentType`, `principalId`, `resourceId`, `scope` (a space-delimited string, **max 3,850 characters** - confirmed exact figure). Tenant-wide list: `GET /oauth2PermissionGrants` (requires `Directory.Read.All`); per-user: `GET /me/oauth2PermissionGrants` [confirmed]. Admin-consent redirect endpoint, confirmed exact shape: `https://login.microsoftonline.com/{tenant}/v2.0/adminconsent?client_id=...&scope=...&redirect_uri=...&state=...` - `{tenant}` here must be a GUID, a verified domain, or the literal `organizations`, and is **explicitly documented as never valid as `common`** for this specific endpoint [confirmed].
**Nonfunctional requirements.** Read-only diagnostic surface; never issues a mutating request.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphPermissionGrant(string ClientId, string ConsentType, string? PrincipalId, string ResourceId, string Scope);
public Task<IReadOnlyList<GraphPermissionGrant>> ListGrantsForCurrentAppAsync(CancellationToken ct = default);
public Uri BuildAdminConsentUrl(string tenant, string clientId, IReadOnlyList<string> scopes, Uri redirectUri, string? state = null);
```
**Official API endpoints, methods and versions (verified).** `oAuth2PermissionGrant` shape (including the exact 3,850-character scope-string limit), both list endpoints, and the admin-consent URL shape (including the `common`-never-valid constraint) - all confirmed.
**Authentication and authorization.** `Directory.Read.All` (application) for the tenant-wide list; a signed-in user for the per-user list.
**Required API permissions, scopes or licenses.** As above.
**Pagination, throttling and retry behavior.** Reuses MGR-019 for the list endpoints.
**Error handling and concurrency.** `BuildAdminConsentUrl` rejects `tenant == "common"` locally before constructing a URL, since the vendor documentation explicitly states this is never valid for this specific endpoint.
**Data privacy and security.** Permission-grant data reveals which scopes an app can exercise - treated as sensitive configuration/security metadata, not logged by default.
**Dependencies.** MGR-001, MGR-002, MGR-003.
**Unit-test scenarios.** `tenant == "common"` rejected locally in `BuildAdminConsentUrl`; grant-list parsing handles the 3,850-character scope-string boundary; admin-consent URL composition matches the documented query-parameter shape exactly.
**Contract-test scenarios.** Synthetic fixture for a permission-grant list.
**Integration-test requirements.** Authorized tenant to confirm the admin-consent redirect flow end-to-end; not available this pass.
**Acceptance criteria.** Permission-grant inspection and admin-consent URL construction both work correctly against synthetic fixtures, with the `common`-rejection rule enforced.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None beyond the standing no-live-tenant caveat.

<a id="mgr-025"></a>
### MGR-025 - Typed models and extensibility

**Business purpose.** Provide extension-data-preserving typed C# models for Graph resources, consistent with every other connector's established discipline, without taking a hard dependency on the official Microsoft Graph SDK.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4.
**Current implementation status.** Not started.
**Repository evidence.** `IRecordMapper<TSource,TTarget>`/`MappingResult<T>` reused unchanged (Section 8.1).
**Functional requirements.** `$metadata` CSDL documents exist at `https://graph.microsoft.com/v1.0/$metadata` and `https://graph.microsoft.com/beta/$metadata` [confirmed]. Official `Microsoft.Graph`/`Microsoft.Graph.Core`/`Microsoft.Graph.Beta` NuGet packages exist (github.com/microsoftgraph/msgraph-sdk-dotnet) [confirmed existence] - referenced here **only as a model-shape precedent for field naming conventions**, explicitly **not** a dependency of `Koras.Connect.Microsoft.Graph` per the Section 6 architecture decision to avoid the official SDK's own dependency weight and its divergent DI/auth conventions. Every typed model preserves unknown fields via `[JsonExtensionData]`, matching every existing Phase 1/Phase 2 DTO.
**Nonfunctional requirements.** Plain POCOs with `System.Text.Json` attributes, no third-party serialization or the official Graph SDK's object model.
**Public C# interfaces/model contracts (proposed).**
```csharp
public sealed record GraphRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class GraphRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
```
**Official API endpoints, methods and versions (verified).** `$metadata` URLs and official SDK package existence, confirmed; not used as an implementation dependency.
**Authentication and authorization.** Not applicable (pure data-shape concern).
**Required API permissions, scopes or licenses.** None.
**Pagination, throttling and retry behavior.** Not applicable.
**Error handling and concurrency.** A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws.
**Data privacy and security.** Mapped/omitted/error states never leak raw field values into log output by default.
**Dependencies.** MGR-003, MGR-004 (illustrative model).
**Unit-test scenarios.** A record missing a required identifier field is `Omitted`; unknown fields preserved via extension data; v1.0 and beta responses for the same logical resource (where field sets differ) both map without data loss.
**Contract-test scenarios.** Golden-fixture tests against documented user/group row shapes.
**Integration-test requirements.** None beyond the underlying CRUD features'.
**Acceptance criteria.** Golden mappings are deterministic; omitted/error fields are visible; the connector never takes a dependency on the official Microsoft Graph SDK.
**Definition of Done.** Standard template.
**Known limitations and blockers.** None.

<a id="mgr-026"></a>
### MGR-026 - Contract tests, diagnostics and examples

**Business purpose.** Prove the Graph connector's protocol-level behavior offline, and give adopters a runnable, credential-free starting point, matching every other connector's established testing/sample discipline.
**Owning package.** `Koras.Connect.Microsoft.Graph`.
**Priority.** P0. **Milestone.** P2-D4 (unit/contract tests) / P2-D6 (sample, package validation).
**Current implementation status.** Not started.
**Repository evidence.** Directly parallels `tests/Koras.Connect.Tests/{Sap,Qualtrics,Adp}/*.cs`'s synthetic-`HttpMessageHandler` test style and `samples/{SuccessFactors.*,Qualtrics.*,ADP.*}`'s offline sample convention; requires a new `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Microsoft.Graph.snapshot.txt` entry (SDK-017).
**Functional requirements.** Offline unit/contract tests for every MGR-001 through MGR-025 P0/P1 feature, using a synthetic `HttpMessageHandler`, no live network access, running on both Linux and Windows; at least one standalone `samples/MicrosoftGraph.*` console project demonstrating authentication → users/groups → OneDrive → pagination/delta, entirely offline; a `IConnector.DiagnoseAsync` implementation covering Configuration/Authentication/Connectivity/Permissions (the last stage specific to Graph, built on MGR-024's diagnostic surface), never issuing a mutating request. v1.0-vs-beta usage must be exercised in at least one test to confirm the version-selection mechanism (MGR-003) works correctly, with beta usage clearly labeled non-production per [G10]'s "beta is not supported in production" guidance - carried into the sample's own comments, not just this document.
**Nonfunctional requirements.** Tests run offline on both OSes in the existing CI matrix; no live credentials required for any test in this feature's scope.
**Public C# interfaces/model contracts (proposed).** `GraphConnector : IConnector` implementing the diagnostic probe; no other new public surface beyond what MGR-001 through MGR-025 already define.
**Official API endpoints, methods and versions (verified).** `DiagnoseAsync`'s connectivity stage uses MGR-008's `GET /organization` (restricted to `User.Read`-level detail, per MGR-008's own documented restricted-response shape) as its one permitted minimal, non-mutating probe call; the permissions stage uses MGR-024's grant-listing as a non-mutating diagnostic, not a live call against a protected resource.
**Authentication and authorization.** `DiagnoseAsync`'s Authentication stage issues exactly one minimal read, never a mutating request.
**Required API permissions, scopes or licenses.** None beyond whatever the underlying probed feature already requires.
**Pagination, throttling and retry behavior.** Not applicable to this feature itself.
**Error handling and concurrency.** `DiagnoseAsync` reports `Failed` with a sanitized code on any probe failure, never throws past the diagnostic boundary.
**Data privacy and security.** Diagnostic output contains no secrets, tokens, or tenant/resource payloads.
**Dependencies.** Every other MGR-0NN feature this covers tests for.
**Unit-test scenarios.** Every unit-test scenario enumerated in MGR-001 through MGR-025 above; `DiagnoseAsync` reports `Skipped` for a stage whose prerequisite already failed; the two documented conflicting-figures items (MGR-010's 250MB/4MB upload limit, and any other open gap) are represented in test documentation as explicitly unresolved, not silently resolved one way.
**Contract-test scenarios.** Every contract-test scenario enumerated above, consolidated into one versioned synthetic-fixture corpus.
**Integration-test requirements.** Every feature's integration-test requirement above remains individually deferred/blocked pending a live tenant; this feature's own integration-test requirement is the sample project actually compiling and running on a clean checkout, which requires no live tenant.
**Acceptance criteria.** The sample project builds and runs on a clean checkout with zero network access; the full synthetic test suite passes on both OSes in CI; the API snapshot baseline is committed; v1.0-vs-beta version selection is exercised at least once.
**Definition of Done.** Matches the Phase 1 master's Section 26 Definition of Done template exactly.
**Known limitations and blockers.** No live Entra tenant is available in this documentation pass; MGR-010's conflicting simple-upload-size figures and MGR-016's RSC-vs-tenant-wide permission choice are the highest-priority re-verification items before Graph connector implementation begins (Section 28).
## 15. Official API Capability Matrix

| Capability | Dataverse | Salesforce | Microsoft Graph |
|---|---|---|---|
| Protocol | OData v4.0 (not 4.01) REST+JSON | REST+JSON (SOAP legacy, out of scope); Pub/Sub is gRPC+Avro | REST+JSON (v1.0/beta) |
| App-only auth | Entra client-credentials/certificate, Application User + security role, no license | OAuth2 Client Credentials/JWT Bearer, ECA/Connected App | Entra client-credentials/certificate, `.default` scope, admin-consented app role |
| Delegated auth | Entra `user_impersonation` scope | Authorization Code (out of SDK scope to implement) | Entra delegated scopes (out of SDK scope to implement) |
| Pagination | `@odata.nextLink` + opaque `$skiptoken`, max 5,000/page | `nextRecordsUrl` + `queryLocator`, 200-2,000/page (default 2,000) | `@odata.nextLink`, default/max vary per resource (e.g. mail: default 10, max 1,000) |
| Delta/incremental | `@odata.deltaLink` + opaque `$deltatoken`, native deletes, 7-day default retention | `SystemModstamp` filter + `queryAll`/`isDeleted` (not opaque-token-based); legacy SOAP `getUpdated`/`getDeleted` out of scope | `@odata.deltaLink` + opaque token, ~26 resource types, 7-day fixed (directory) or LRU-variable (Outlook) |
| Batch | `$batch` multipart/mixed, max 1,000 requests, atomic changesets | `/composite` (25 max, cross-ref) / `/composite/batch` (25 max, no cross-ref) / `/composite/graph` (75 graphs/500 nodes) | `$batch` JSON, max 20 requests, `dependsOn` ordering, per-sub-request throttling |
| Throttling signal | `429` + `Retry-After` (seconds); 3 documented facets (6,000 req/300s, 1.2M ms exec/300s, 52 concurrent) | `REQUEST_LIMIT_EXCEEDED` error code, **no `Retry-After` header documented** | `429` + `Retry-After` (seconds); global default 130,000 req/10s/app |
| Concurrency control | `@odata.etag` + `If-Match`/`If-None-Match`, `412` | `ETag`/`If-Match`/`If-None-Match` (scope unconfirmed, possibly Account-only) + `If-Modified-Since`/`If-Unmodified-Since` (broader), `412`/`304` | No per-record ETag mechanism documented for most resources; delta query is the primary change-detection path |
| Official typed codegen | None for Web API; `pac modelbuilder build` targets a different (organization-service) surface | None for REST; Enterprise/Partner WSDL target SOAP; OpenAPI 3.0 generator is beta, sObjects-only | Official `Microsoft.Graph`/`.Core`/`.Beta` NuGet SDKs exist but are not a dependency of this connector |
| Event streaming | None (polling/delta only) | CometD Streaming API (legacy) + Pub/Sub API (gRPC, current) for CDC/Platform Events | Webhooks (`/subscriptions`), push-based, 10s validation / 3s ack / 4h retry |
| Error shape | OData error JSON, `412`/`429`/standard 4xx | `[{message, errorCode, fields[]}]` array | `{error:{code, message, innerError|innererror}}` (casing inconsistency across pages) |

## 16. Authentication and Permissions

All three connectors reuse `Koras.Connect.Authentication.ClientCredentialsTokenSource`/`CertificateCredentialProvider`/`RedactedCredential<T>` unchanged for the client-credentials/certificate case (Section 8.1); none implement an interactive browser flow themselves - delegated scenarios always accept a caller-supplied `IAccessTokenSource`. Per-vendor permission models differ structurally and must not be unified into one shared "permission" abstraction (Section 8.2): Dataverse grants access via a security role bound to an Application User (table/row/column-scoped, environment-local); Salesforce grants access via profile/permission-set object-and-field-level security plus a Connected App/ECA's OAuth scopes; Microsoft Graph grants access via delegated `oauth2PermissionGrant` or application `appRoleAssignment` records, tenant-wide and resource-type-scoped, with admin consent required for most application permissions. Each connector's `GraphPermissionRequirement`-style declaration (MGR-002) is connector-local, not shared, since the shape of "what permission does this operation need" differs too much across vendors to unify without losing precision (consistent with Section 8.2's "no abstraction creep" principle). Microsoft Graph uniquely exposes a first-class permission/consent diagnostic surface (MGR-024); Dataverse and Salesforce have no equivalent documented API for a caller to introspect its own granted entitlements - each connector's `DiagnoseAsync` (DVT-022/SFD-022/MGR-026) is the closest cross-vendor equivalent, probing a minimal non-mutating operation and surfacing a 403 as a classified, actionable diagnostic rather than a stack trace.

## 17. Data Models and Mapping

No vendor among the three provides an official, stable, REST-surface-specific typed-client generator (Section 10/12/14's DVT-021/SFD-020/MGR-025 findings are consistent on this point): Dataverse's `pac modelbuilder build` targets the organization-service SDK, not the Web API this connector uses; Salesforce's Enterprise/Partner WSDL target SOAP, and its OpenAPI 3.0 generator is beta and sObjects-only; Microsoft Graph's official NuGet SDKs exist but are deliberately not a dependency (Section 6/MGR-025). All three connectors therefore hand-write typed models against each vendor's describe/metadata surface (`$metadata` CSDL for Dataverse, `/sobjects/{x}/describe` for Salesforce, `$metadata`/documented JSON shapes for Graph), every model preserving unknown fields via `[JsonExtensionData]` and every mapper returning `IRecordMapper<TSource,TTarget>`/`MappingResult<TTarget>` (never throwing on a field-level issue), identical in spirit to every Phase 1 connector's established discipline (Section 8.1). `BatchItemResult<T>` (Section 8.2, item 2) is the one new shared Abstractions type proposed for Phase 2, justified by genuine 3-way reuse: Dataverse's `$batch` continue-on-error mode, Salesforce's `/composite/batch`, and Microsoft Graph's `$batch` all return a mixed array of per-item success/failure outcomes in a structurally equivalent shape.

## 18. Pagination, Batching and Change Tracking

`Page<T>`/`ContinuationToken` (Section 8.1) are reused unchanged by all three connectors for ordinary forward-only pagination, treating each vendor's cursor (`$skiptoken`, `queryLocator`, Graph's `@odata.nextLink` token) as opaque. Delta/incremental mechanisms diverge in kind, not just detail, and are deliberately not unified (Section 8.2, item 3): Dataverse's `$deltatoken` and Microsoft Graph's delta token are both genuinely opaque, server-issued cursors and are wrapped in `ContinuationToken` exactly like ordinary pagination; Salesforce's `SystemModstamp` is a plain, caller-constructed filter value, not an opaque cursor, and is represented by a connector-local `SalesforceDeltaCheckpoint` record instead - attempting to force it through `ContinuationToken` would misrepresent its actual semantics to callers. Batch size limits differ by an order of magnitude across vendors (1,000 for Dataverse `$batch`, 25 for Salesforce `/composite`/`/composite/batch`, 20 for Microsoft Graph `$batch`) and are enforced locally by each connector before send, never silently truncated or left for the server to reject. Native delete-detection in a delta feed exists only for Dataverse and Microsoft Graph (both represent deletions as a distinct entry type within the delta response); Salesforce requires the separate `queryAll`+`isDeleted` convention, and the Phase 1 `Reconciliation.DetectChangesAsync` fallback (Section 8.1) remains relevant as a last-resort mechanism for any resource on any connector that lacks a native delta/delete signal.

## 19. Resilience and Error Handling

Every connector wraps single-resource reads in a `{Connector}ApiException.FromTransportFailure`/`.Classify` pair layered over the shared `IntegrationExceptionClassifier`, matching the established ADP/SAP/Qualtrics read-vs-write/pagination split (Section 6.1) - pagination and batch/write paths stay on the raw `IntegrationRequestException`, never silently wrapped, so a caller enumerating a page never has to catch a connector-specific type just to detect a transport failure. 429 handling differs by vendor in a way the shared classifier already accommodates (`ThrottleException(RetryAfter)`) but that each connector must document distinctly: Dataverse and Microsoft Graph both supply `Retry-After` in seconds; **Salesforce does not document a `Retry-After` header at all**, only the `REQUEST_LIMIT_EXCEEDED` error code, so the Salesforce connector's `ThrottleException` is constructed with no server-supplied delay and must apply its own bounded backoff rather than honoring a vendor-specified wait (SFD-017/SFD-019). Two single-vendor exception-type extensions are proposed as connector-local, not shared, additions (Section 8.2's "no abstraction creep" principle applied concretely): `DataverseConcurrencyException`/`SalesforceConcurrencyException` for a classified `412` (since the shared classifier's current default for an unmapped status is a generic `ContractException`, and 412-as-concurrency-conflict is not yet proven needed by Microsoft Graph); and a gRPC-status-to-`IntegrationException` adapter local to the Salesforce Pub/Sub transport (SFD-014), since the shared `IntegrationTransport`/classifier are HTTP-status-code-based only and Pub/Sub's gRPC transport has no HTTP status codes to classify.

## 20. Observability

**Foundation gap, carried forward honestly from the repository audit (Section 3) rather than assumed away**: the Phase 1 master specification's scope statement names an `Observability` package as part of Phase 1, but no such package exists in the repository today - only `ILogger`-based logging (via `Microsoft.Extensions.Logging.Abstractions`, consumed by `Koras.Connect.Core`) and ad hoc redaction conventions (`RedactedCredential<T>.ToString()`, `ODataQuery.ToString()` overrides) currently provide any observability surface. Phase 2 connectors therefore inherit exactly what exists today - structured `ILogger` calls at the transport layer, with every connector's `{Connector}ApiException`/`BatchItemResult<T>`/diagnostic surface designed to carry a sanitized, loggable failure code separately from any raw vendor error body - and must not assume a richer tracing/correlation/metrics surface (distributed tracing spans, `Activity`/`ActivitySource` instrumentation, structured metrics) exists to build on, since it does not. A request-correlation-ID convention (attaching a caller-supplied or generated correlation identifier to every outbound request and surfacing it in every thrown exception) is proposed as a new, small, genuinely cross-connector capability for P2-D1 (Section 24), since all three vendors' support channels ask for a client-correlation header/value when diagnosing a production issue (Dataverse/Graph via `client-request-id`-style conventions referenced in their request/response header documentation; Salesforce has no equivalent documented request-ID echo mechanism and would rely on the SDK's own generated correlation ID alone). Building a full dedicated `Observability` package remains out of this specification's scope (it is a foundation-readiness item, not a Dataverse/Salesforce/Graph-specific one) and is flagged as a cross-cutting risk in Section 25.

## 21. Security and Privacy

Every connector's secret/certificate handling reuses `RedactedCredential<T>` unchanged - no connector-specific secret type is introduced. Every write operation across all 70 features is gated behind an explicit `writesEnabled: bool = false` parameter, matching the universal convention established by every Phase 1 connector; this specification does not relax that convention for any Phase 2 feature, including the higher-volume Bulk/Composite/Batch write paths. Field-level/row-level security enforcement is entirely vendor-side (Dataverse security roles, Salesforce field-level security/sharing rules, Graph delegated-vs-application permission scoping) - the SDK never attempts to second-guess or pre-filter based on an assumed entitlement, and a 403 from any of the three vendors is treated as a genuine, correctly-enforced denial, not a bug to work around. PII/sensitive-data handling is vendor-specific in degree but uniform in policy: row/record/resource field values (Dataverse rows, Salesforce records, Graph user/mail/calendar/chat content) are treated as sensitive by default and excluded from log output across every connector, matching the SAP SuccessFactors `message.value`-redaction precedent exactly. No credentials or production data may be committed to the repository at any point in this specification's execution (binding constraint, restated from the task's own instructions) - every code example and fixture in this document and in any future implementation must use synthetic values. Token/certificate rotation reuses the existing `ICredentialProvider<T>`/hot-reload pattern (where available, e.g. ADP-003's `IOptionsMonitor`-backed adapter) rather than introducing a new rotation mechanism per Phase 2 connector.

## 22. Test Strategy

Every connector follows the identical offline-first discipline already proven across ADP/SAP SuccessFactors/Qualtrics (557 tests passing today, 0 live-credential dependencies): unit tests exercise typed models/mappers/query builders in isolation; contract tests run against a synthetic `HttpMessageHandler` (or, uniquely for Salesforce's Pub/Sub API, a synthetic gRPC service double) reproducing each documented request/response shape; pagination/rate-limit/retry/auth/authz-negative/partial-failure/concurrency/file-upload-download scenarios are each explicitly enumerated per feature in Sections 10/12/14 above, not left implicit. The following test categories require a live, authorized vendor sandbox and are **not** achievable in this documentation-only pass or by any future offline-only implementation phase - each is recorded as a standing, explicit blocker rather than silently skipped: real security-role/profile/permission-set entitlement behavior (DVT-004/SFD-004/MGR-004 and every write feature); real service-protection-limit/throttling breach behavior at vendor-documented thresholds (DVT-014/SFD-017/MGR-022, deliberately not attempted even once live sandbox access exists, since triggering a real breach is operationally disruptive); real change-tracking/delta-token expiry timing (DVT-012/SFD-021/MGR-019); real webhook subscription validation against a publicly reachable endpoint (MGR-020); real large-file/chunked-upload behavior at vendor-documented size boundaries (DVT-016/MGR-010/MGR-023); and resolution of every explicitly flagged vendor-research confidence gap (Section 28) via either a live call or an independently re-fetched official page. No test may report a passing result without evidence actually produced in the commit that introduces it - this specification's "Not started" status for all 70 features reflects that no such evidence exists yet, consistent with Section 2's honesty requirement.

## 23. NuGet Packaging and Versioning

Package IDs: `Koras.Connect.Dataverse`, `Koras.Connect.Salesforce`, `Koras.Connect.Microsoft.Graph` - matching the existing `Koras.Connect.{Adp,Sap.SuccessFactors,Qualtrics}` naming convention exactly (Section 6). All three target `net10.0` only, inherit `Directory.Build.props`' shared settings (Nullable enable, TreatWarningsAsErrors, GenerateDocumentationFile), and start at the repository's current preview version scheme (`0.1.0-preview.2`-pattern, bumped at release time per the existing `eng/check_version_consistency.py` tooling - SDK-019). None of the three packages reference each other or any Phase 1 connector package (Section 7's acyclic dependency matrix); all three reference only `Koras.Connect.Abstractions`, `Koras.Connect.Core`, and `Koras.Connect.Authentication`, plus `Microsoft.Extensions.Http`/`Microsoft.Extensions.Options` (all three) and, for Salesforce only, `Grpc.Net.Client`/`Google.Protobuf` for the Pub/Sub transport (ADR-P201, Section 27). Each package requires a new `tests/Koras.Connect.Tests/ApiSnapshots/{PackageId}.snapshot.txt` baseline wired into the existing reflection-based `ApiSnapshotTests.cs` (SDK-002/SDK-017) from its very first implementation commit, matching every existing package's enforcement. The `Koras.Connect` facade package (type-forwarding only, Section 3.2) is **not** extended to forward Phase 2 connector types - the facade's existing scope is Phase 1 foundation + HR connectors only, and extending it to Phase 2 would require a deliberate, separately-justified decision not made by this specification. Each new package's `README.md` must document supported operations, the `writesEnabled` gate, and an explicit list of features not yet implemented, matching the SAP SuccessFactors README precedent (SAP-016).

## 24. Implementation Roadmap

| Milestone | Scope | Dependencies | Acceptance criteria |
|---|---|---|---|
| **P2-D0** | This documentation deliverable itself: architecture, 70-feature catalog, test strategy, roadmap. | None. | This document committed on a dedicated documentation branch; all 70 feature IDs validated (Section 29). |
| **P2-D1** | Shared foundation readiness: add `BatchItemResult<T>` to `Koras.Connect.Abstractions` (Section 8.2); extend `PackageGraphTests.cs` to cover the 3 new packages' acyclic dependency rule (Section 7); add the request-correlation-ID convention (Section 20) to `IntegrationTransport` if adopted. | P2-D0. | New shared types ship with unit tests and an API-snapshot entry; no existing connector's public API changes; `PackageGraphTests.cs` passes with the 3 new (not-yet-created) package names registered as expected-acyclic nodes. |
| **P2-D2** | Dataverse connector: all 14 P0 features (DVT-001 through DVT-014, DVT-021, DVT-022 - note DVT-021/022 are P0 but numbered after the P1 block in the original catalog ordering; see Section 9's table for the authoritative priority column). | P2-D1. | All Dataverse P0 features implemented with unit/contract tests passing offline on both OSes; sample project runs; API snapshot committed; live-sandbox-gated items explicitly recorded as blocked, not silently skipped. |
| **P2-D3** | Salesforce connector: all P0 features (SFD-001 through SFD-009, SFD-015, SFD-017 through SFD-020, SFD-022). | P2-D1. | Same acceptance pattern as P2-D2; the Composite API (SFD-009) field-name re-verification (Section 28) is resolved or explicitly re-flagged before this milestone closes. |
| **P2-D4** | Microsoft Graph connector: all P0 features (MGR-001 through MGR-005, MGR-008 through MGR-012, MGR-019, MGR-021, MGR-022, MGR-024, MGR-025, MGR-026). | P2-D1. | Same acceptance pattern; the MGR-010 upload-size-limit conflict (Section 28) is resolved or explicitly re-flagged. |
| **P2-D5** | Advanced capabilities (selected P1 features): Dataverse actions/files/attachments/D365 specialization (DVT-015 through DVT-018); Salesforce Bulk 2.0/CDC/Platform Events/Pub/Sub/Files (SFD-010 through SFD-014, SFD-016, SFD-021); Microsoft Graph admin units/app registrations/mail/calendar/Teams/subscriptions/large-file transfer (MGR-006, MGR-007, MGR-013 through MGR-016, MGR-020, MGR-023). | P2-D2, P2-D3, P2-D4 respectively (each connector's P1 work depends only on its own P0 foundation, not on the other two connectors - Section 7). | Each listed P1 feature meets its own Definition of Done (Section 26); SFD-014's Avro-dependency decision (Section 28) is made before any Pub/Sub implementation work starts. |
| **P2-D6** | Validation and release readiness across all three connectors: full offline test suite green on both OSes; all three API snapshots stable; `eng/check_vulnerabilities.py`/`validate_packages.py`/`check_version_consistency.py` (SDK-016 through SDK-019) pass for the 3 new packages; sample projects for all three connectors run on a clean checkout. | P2-D2, P2-D3, P2-D4, and whichever P2-D5 items are in scope for the release. | Clean-checkout `dotnet build`/`dotnet test`/`dotnet pack` succeed for all three new packages with zero regressions to the existing 557-test baseline. |
| **P2-D7** | Later expansion: P2-priority features (DVT-019, DVT-020, MGR-017, MGR-018) plus any features whose P2-D5/D6 release was explicitly deferred. | P2-D6. | Each feature meets its own Definition of Done; no feature in this tier blocks any earlier milestone. |

## 25. Dependencies and Risks

| Risk | Connector(s) | Mitigation |
|---|---|---|
| Composite API (SFD-009) and Pub/Sub API (SFD-014) field names/wire format are snippet-only, not independently read from a live page or direct fetch. | Salesforce | Re-verify against a live org or a successfully-fetched official page before implementation starts (Section 28); do not implement against unverified field names. |
| MGR-010's simple-upload size limit has two conflicting official figures (250MB vs. 4MB). | Microsoft Graph | Default to the conservative 4MB ceiling until reconciled; route anything larger through the upload-session path (MGR-023) regardless. |
| No Observability package exists despite Phase 1 scope naming one (Section 20). | All three (cross-cutting) | Phase 2 connectors ship with existing `ILogger`-only observability; a dedicated Observability package remains a distinct, unscheduled foundation initiative, not bundled into this roadmap. |
| Salesforce lacks a documented `Retry-After` header on throttling responses, unlike Dataverse/Graph. | Salesforce | Document the asymmetry explicitly (Section 19); apply the connector's own bounded backoff rather than assuming a vendor-supplied delay. |
| GlobalOptionSetDefinitions entity-set name (DVT-011) and `annotation` field names (DVT-017) were corroborated by search but not independently re-fetched. | Dataverse | Re-verify before marking either feature `verified`; do not implement against an unconfirmed entity/field name. |
| RSC (resource-specific consent) permission model for Teams messages (MGR-016) is materially more complex to configure than a typical tenant-wide application permission. | Microsoft Graph | Document both the RSC and tenant-wide paths explicitly as distinct configuration choices; do not default silently to one. |
| Planner (MGR-017) has no application-permission path at all; a caller misconfiguring an app-only setup would otherwise fail confusingly at runtime. | Microsoft Graph | Fail configuration validation fast and explicitly, naming the delegated-only constraint, before any call is attempted. |
| No official Web-API-specific/REST-specific typed-client generator exists for any of the three vendors (DVT-021/SFD-020/MGR-025). | All three | Hand-written models against each vendor's own describe/metadata surface, with `[JsonExtensionData]` preservation as the safety net against schema drift. |
| Grpc.Net.Client/Google.Protobuf dependency for Salesforce Pub/Sub (ADR-P201) is the first non-HTTP-client dependency introduced by any connector in this SDK. | Salesforce | Scoped strictly to the Pub/Sub transport internals; does not leak into the public API surface of any other feature; documented as an explicit, justified exception (Section 27). |
| No live vendor sandbox/tenant credentials are available for any of the three connectors in this documentation pass. | All three | Every integration-test requirement in Sections 10/12/14 is recorded as an explicit, standing blocker, not silently skipped; Section 22 names this as a structural limitation of the current pass, not a connector-specific gap. |

## 26. Acceptance Criteria and Definition of Done

Every feature specification in Sections 10, 12, and 14 above references a "standard template" Definition of Done, defined once here to avoid 70 repetitions: a feature is Done when (1) its public C# contract matches what is documented in this specification (or a reasoned, recorded deviation); (2) its numbered acceptance criteria are met and independently checkable; (3) its unit and contract tests pass deterministically, offline, on both Linux and Windows, using only synthetic fixtures; (4) applicable security (write opt-in gate, secret redaction), cancellation, and resource-ownership checks pass; (5) at least one package-consumer sample demonstrates the feature in a runnable, credential-free form; (6) every official API operation the feature claims to implement cites a current, specifically-dated official source consistent with this specification's own citations, with any snippet-only or unresolved fact carried forward explicitly rather than silently upgraded to "verified"; (7) required live-sandbox outcomes are recorded where available, or explicitly and specifically logged as a standing external blocker where not; (8) CI artifacts (test run, API snapshot) are linked to the implementing commit's revision; (9) documentation/limitations are reviewed and current as of the implementing commit. A feature currently documented as "Not started" in this specification has met none of these nine criteria yet, by definition - this specification's own completeness (Section 29) is a separate, narrower bar: that every feature is specified completely enough to be implemented against, not that any feature is implemented.

## 27. Architecture Decisions

| ID | Decision | Rationale | Alternatives considered |
|---|---|---|---|
| ADR-P201 | Take a `Grpc.Net.Client`/`Google.Protobuf` dependency, scoped to `Koras.Connect.Salesforce`'s Pub/Sub transport only, for SFD-014. | Pub/Sub is Salesforce's current recommended transport for both CDC (SFD-012) and Platform Events (SFD-013); it is natively gRPC+Avro, with no documented plain-HTTP equivalent offering the same capability. | Implement only the legacy CometD/Bayeux (HTTP long-polling) transport and skip Pub/Sub entirely - rejected because CometD's deprecation trajectory relative to Pub/Sub, while not fully confirmed (Section 28), is consistently described as the legacy path in every source found; skipping the vendor's current-recommended transport would make this connector's event-streaming story stale on arrival. |
| ADR-P202 | Do not depend on the official `Microsoft.Graph`/`Microsoft.Graph.Core` NuGet SDK for `Koras.Connect.Microsoft.Graph`; hand-roll a thin REST client over `Microsoft.Extensions.Http` instead. | Matches every other connector's pattern in this SDK (none wrap an official vendor SDK); avoids the official SDK's own large dependency graph, its distinct DI/auth conventions, and its request-builder object model, which would be a second, inconsistent way of calling HTTP APIs inside this codebase. | Depend on `Microsoft.Graph`/`Microsoft.Graph.Core` directly - rejected per the reasoning above; the official SDK's model shapes are still referenced as a naming precedent only (MGR-025), not a dependency. |
| ADR-P203 | Do not create a unified `IBatchRequestExecutor` abstraction in `Koras.Connect.Abstractions`; each connector defines its own `{Connector}BatchRequest`/`{Connector}BatchResponse` types. | Vendor batch semantics are too different to unify without losing precision: Dataverse changesets are all-or-nothing atomic; Salesforce's three Composite variants trade off cross-referencing against batch size in incompatible ways; Microsoft Graph's `dependsOn` ordering has no Dataverse/Salesforce equivalent. Forcing a shared interface would either lose these distinctions or grow leaky vendor-specific escape hatches. | A shared `IBatchRequestExecutor<TRequest,TResponse>` generic interface - rejected; see Section 8.2, item 1 for the full reasoning. |
| ADR-P204 | Add `BatchItemResult<T>(int Index, bool Succeeded, T? Value, ErrorDetails? Error)` to `Koras.Connect.Abstractions`. | All three vendors' batch/composite mechanisms return a mixed per-item success/failure array in a structurally equivalent shape; ADP/SAP SuccessFactors already hand-roll an equivalent `UpsertResult` shape today, establishing precedent that this reuse is genuine, not speculative. | Keep `BatchItemResult<T>` connector-local (3 duplicate types) - rejected as genuine, proven duplication rather than premature abstraction, unlike ADR-P203's rejected case. |
| ADR-P205 | Do not create a shared delta/checkpoint interface in `Koras.Connect.Abstractions`; reuse `ContinuationToken` where a vendor's delta mechanism is a genuinely opaque server-issued cursor (Dataverse `$deltatoken`, Microsoft Graph delta token), and define a connector-local checkpoint record where it is a queryable filter value instead (Salesforce `SalesforceDeltaCheckpoint` wrapping `SystemModstamp`). | SAP SuccessFactors's existing `SapDeltaCheckpoint`/`SapDeltaFilters` already establishes the filter-based shape as a proven, non-speculative pattern; forcing Salesforce's filter-based mechanism through the same `ContinuationToken` type used for genuinely opaque cursors would misrepresent its actual semantics (a filter value is inspectable/constructible by the caller; an opaque cursor is not). | A single shared `IDeltaCheckpoint` marker interface spanning both shapes - rejected; the two mechanisms don't share enough behavior to justify a shared interface beyond what `ContinuationToken` and plain records already provide independently. |
| ADR-P206 | All three Phase 2 connectors are JSON-only; none implement their vendor's XML/SOAP alternative surface (Dataverse's `application/xml` content-type option, Salesforce's SOAP WSDL-based API, Microsoft Graph's XML option where applicable). | Matches every Phase 1 connector's JSON-only convention; none of the vendors require XML/SOAP for feature parity on the operations this specification covers; adding a second serialization surface per connector would roughly double each connector's surface area for no documented capability gain. | Support both JSON and XML/SOAP per connector - rejected as scope inflation with no identified caller need. |

## 28. Open Questions and External Blockers

This section consolidates every explicitly flagged gap from Sections 9-14 into one list, so a future implementer has a single place to check before starting work on any given feature.

**External/live-environment blockers (apply to nearly every feature; not repeated per-item below):** no live Dataverse environment, Salesforce org, or Entra/Graph tenant with admin-consented permissions is available in this documentation-only pass. Every integration-test requirement in Sections 10, 12, and 14 is blocked on this and is recorded as such, not silently skipped.

**Dataverse:**
- DVT-011: `GlobalOptionSetDefinitions` entity-set name is corroborated by search, not independently re-fetched from a live page.
- DVT-013: whether to extend the shared `IntegrationExceptionClassifier` for 412-as-concurrency-conflict, or keep it connector-local (current lean: connector-local, per ADR-P203's reasoning extended to this case).
- DVT-015: the functions-specific official companion page was not independently re-fetched.
- DVT-017: `annotation` entity's `documentbody`/`filename`/`mimetype`/`isdocument` field names are corroborated by search, not independently re-fetched.
- DVT-019: solution import/export action signatures (`ImportSolution`/`ExportSolution`) were not independently fetched.
- DVT-020: the specific metadata field distinguishing a virtual table was not independently fetched - this feature is explicitly deferred past "Not started" to "cannot specify further" until that gap closes.
- DVT-022's own content inherits every gap above into its test/sample documentation.

**Salesforce:**
- SFD-001: exact JWT Bearer claim set was snippet-only.
- SFD-009: Composite/`composite-batch`/`composite-graph` exact JSON field names - the single highest-priority Salesforce re-verification item, since SFD-009 is P0.
- SFD-010/SFD-011: Bulk ingest batch-count-vs-filesize interaction and query `maxRecords` default are unconfirmed.
- SFD-012/SFD-013: CometD-vs-Pub/Sub deprecation framing, exact channel strings, and publish-mode limit numbers are snippet-only.
- SFD-014: endpoint port (7443 vs. 443) and gRPC metadata header case-sensitivity are explicitly flagged unverified; this is the single lowest-confidence feature in the entire 70-feature catalog and should be the first item independently re-verified before any Salesforce event-streaming implementation begins. The Avro-vs-schema-registry dependency decision is also unresolved.
- SFD-016: REST-specific ContentVersion file-size cap was not found (only a SOAP 50MB figure, not assumed to transfer).
- SFD-017: `DailyBulkV2IngestJobs` field name and a `/limits/recordCount` sub-resource were not found and must not be assumed to exist.
- SFD-018: the "Account records only" ETag-scoping claim is unconfirmed and is this feature's highest-priority re-verification item.
- SFD-019: no verbatim raw JSON error sample was independently retrieved.
- SFD-021: exact soft-deleted-row retention window for `queryAll` visibility is unconfirmed.
- SFD-022: per-edition numeric limits (daily API requests, Bulk/CDC/Platform Event allocations) are the least-corroborated figures in the whole Salesforce research pass and must not be published as binding spec language without re-verification.

**Microsoft Graph:**
- MGR-005: the `Group-NestingSupport.ReadWrite.All` "least privileged for a read" pairing is an unresolved documentation anomaly.
- MGR-010: the 250MB-vs-4MB simple-upload size-limit conflict is the single most consequential unresolved Graph documentation discrepancy in this catalog and should be the first Graph item independently re-verified.
- MGR-016: RSC's per-resource consent model needs clear caller-facing documentation distinguishing it from the tenant-wide alternative; not itself unverified, but flagged for implementation-time attention given its configuration complexity.
- MGR-018: write-side application-permission symmetry with the read path (`Tasks.Read.All`) was not independently confirmed.
- MGR-020: the full per-resource-type max-subscription-expiration table was only partially captured and is not exhaustive.
- MGR-022: the full per-service throttling-limit table beyond the confirmed 130,000/10s global default was only partially captured and is not exhaustive.
- Cross-cutting: the `innerError`/`innererror` casing inconsistency across official Graph documentation pages is a genuine, unresolved vendor-documentation artifact (MGR-003).

**Cross-cutting (not vendor-specific):**
- No `Koras.Connect.Observability` package exists despite being named in Phase 1's scope statement (Section 20) - whether and when to build it is a foundation-readiness decision outside this specification's authority to schedule.
- Whether to add a request-correlation-ID convention to the shared `IntegrationTransport` (Section 20) is proposed for P2-D1 but not yet decided.

## 29. Development Readiness Checklist

- [x] All 22 Dataverse features (DVT-001 through DVT-022) are specified with unique IDs, matching the mandatory catalog exactly.
- [x] All 22 Salesforce features (SFD-001 through SFD-022) are specified with unique IDs, matching the mandatory catalog exactly.
- [x] All 26 Microsoft Graph features (MGR-001 through MGR-026) are specified with unique IDs, matching the mandatory catalog exactly.
- [x] All 70 feature IDs are unique across the three catalogs (verified by construction: disjoint ID prefixes DVT/SFD/MGR, sequential numbering within each, no gaps, no duplicates - confirmed by direct enumeration in Section 30).
- [x] No feature's priority was silently altered from the mandatory specification (P0/P1/P2 assignments in Sections 9/11/13 match the task's own catalog verbatim).
- [x] Every feature declares an owning package, functional/nonfunctional requirements, dependencies, and acceptance criteria (Sections 10, 12, 14).
- [x] Every feature's "Current implementation status" is evidence-based: all 70 are `Not started`, correctly, since no Phase 2 connector code exists in the repository (confirmed via the repository audit, Section 3).
- [x] Official API endpoints, permissions, and limits are cited with source URLs and confidence levels (PDF/directly-fetched vs. snippet vs. unverified) rather than invented; every gap is recorded in Section 28 rather than silently resolved.
- [x] The target package dependency graph (Section 7) is acyclic by construction: all three new packages depend only on `{Abstractions, Core, Authentication}`, never on each other, and never on any Phase 1 connector.
- [x] All eight implementation milestones (P2-D0 through P2-D7) are defined with dependencies and acceptance criteria (Section 24).
- [x] Only this one documentation file is modified in the git history produced by this assignment (verified at commit time, Section 1/final report). This remained true through commit `eb063ba`; the merge into `develop` and the subsequent per-feature file split (Section 1, "Post-audit actions") were separate, explicitly user-directed actions outside this assignment's original scope, not a violation of it.
- [ ] Live-sandbox/tenant validation for any feature - explicitly and correctly **not** checked; no live credentials exist for this pass, and every such requirement is recorded as a standing blocker (Section 28), not silently marked done.

## 30. Master Feature Index

| ID | Name | Priority | Package | Status |
|---|---|---|---|---|
| DVT-001 | Microsoft Entra OAuth authentication | P0 | Koras.Connect.Dataverse | Not started |
| DVT-002 | Environment discovery and connection | P0 | Koras.Connect.Dataverse | Not started |
| DVT-003 | Dataverse Web API client | P0 | Koras.Connect.Dataverse | Not started |
| DVT-004 | Table and row CRUD | P0 | Koras.Connect.Dataverse | Not started |
| DVT-005 | OData filtering, selection and ordering | P0 | Koras.Connect.Dataverse | Not started |
| DVT-006 | Pagination and continuation links | P0 | Koras.Connect.Dataverse | Not started |
| DVT-007 | Batch and changeset operations | P0 | Koras.Connect.Dataverse | Not started |
| DVT-008 | Upsert and alternate keys | P0 | Koras.Connect.Dataverse | Not started |
| DVT-009 | Table and column metadata | P0 | Koras.Connect.Dataverse | Not started |
| DVT-010 | Lookups and relationships | P0 | Koras.Connect.Dataverse | Not started |
| DVT-011 | Choice, status and state metadata | P0 | Koras.Connect.Dataverse | Not started |
| DVT-012 | Change tracking and delta synchronization | P0 | Koras.Connect.Dataverse | Not started |
| DVT-013 | ETags and optimistic concurrency | P0 | Koras.Connect.Dataverse | Not started |
| DVT-014 | Service protection limits and throttling | P0 | Koras.Connect.Dataverse | Not started |
| DVT-015 | Actions and functions | P1 | Koras.Connect.Dataverse | Not started |
| DVT-016 | File and image columns | P1 | Koras.Connect.Dataverse | Not started |
| DVT-017 | Attachments and notes | P1 | Koras.Connect.Dataverse | Not started |
| DVT-018 | Dynamics 365 specialization | P1 | Koras.Connect.Dataverse | Not started |
| DVT-019 | Solution and customization metadata | P2 | Koras.Connect.Dataverse | Not started |
| DVT-020 | Virtual table capability discovery | P2 | Koras.Connect.Dataverse | Not started |
| DVT-021 | Typed models and mappings | P0 | Koras.Connect.Dataverse | Not started |
| DVT-022 | Contract tests, diagnostics and examples | P0 | Koras.Connect.Dataverse | Not started |
| SFD-001 | OAuth authentication | P0 | Koras.Connect.Salesforce | Not started |
| SFD-002 | Instance discovery and API versioning | P0 | Koras.Connect.Salesforce | Not started |
| SFD-003 | REST API client | P0 | Koras.Connect.Salesforce | Not started |
| SFD-004 | sObject CRUD | P0 | Koras.Connect.Salesforce | Not started |
| SFD-005 | SOQL | P0 | Koras.Connect.Salesforce | Not started |
| SFD-006 | Query pagination | P0 | Koras.Connect.Salesforce | Not started |
| SFD-007 | Describe and metadata | P0 | Koras.Connect.Salesforce | Not started |
| SFD-008 | External IDs and upsert | P0 | Koras.Connect.Salesforce | Not started |
| SFD-009 | Composite API | P0 | Koras.Connect.Salesforce | Not started |
| SFD-010 | Bulk API 2.0 ingest | P1 | Koras.Connect.Salesforce | Not started |
| SFD-011 | Bulk API 2.0 query | P1 | Koras.Connect.Salesforce | Not started |
| SFD-012 | Change Data Capture | P1 | Koras.Connect.Salesforce | Not started |
| SFD-013 | Platform Events | P1 | Koras.Connect.Salesforce | Not started |
| SFD-014 | Pub/Sub API | P1 | Koras.Connect.Salesforce | Not started |
| SFD-015 | Relationships and references | P0 | Koras.Connect.Salesforce | Not started |
| SFD-016 | Files and ContentVersion | P1 | Koras.Connect.Salesforce | Not started |
| SFD-017 | Limits API and throttling | P0 | Koras.Connect.Salesforce | Not started |
| SFD-018 | Concurrency and conditional requests | P0 | Koras.Connect.Salesforce | Not started |
| SFD-019 | Error normalization and partial successes | P0 | Koras.Connect.Salesforce | Not started |
| SFD-020 | Typed CRM/custom object models | P0 | Koras.Connect.Salesforce | Not started |
| SFD-021 | Incremental extraction and checkpoints | P1 | Koras.Connect.Salesforce | Not started |
| SFD-022 | Contract tests, diagnostics and examples | P0 | Koras.Connect.Salesforce | Not started |
| MGR-001 | Delegated and app-only authentication | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-002 | Tenant and permission configuration | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-003 | Graph HTTP client | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-004 | Users and profiles | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-005 | Groups and memberships | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-006 | Directory roles and admin units | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-007 | App registrations and service principals | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-008 | Organization and tenant info | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-009 | OneDrive drives and items | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-010 | File upload, download and sessions | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-011 | SharePoint sites and document libraries | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-012 | SharePoint lists and items | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-013 | Outlook mail | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-014 | Outlook calendar | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-015 | Teams and channels | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-016 | Teams messages and chats | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-017 | Planner | P2 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-018 | Microsoft To Do | P2 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-019 | Pagination and delta queries | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-020 | Change notifications and subscriptions | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-021 | Batch requests | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-022 | Throttling and Retry-After | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-023 | Large file transfer and resumability | P1 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-024 | Permission and consent diagnostics | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-025 | Typed models and extensibility | P0 | Koras.Connect.Microsoft.Graph | Not started |
| MGR-026 | Contract tests, diagnostics and examples | P0 | Koras.Connect.Microsoft.Graph | Not started |

**Totals: 22 Dataverse + 22 Salesforce + 26 Microsoft Graph = 70 features, all unique, all `Not started`.**

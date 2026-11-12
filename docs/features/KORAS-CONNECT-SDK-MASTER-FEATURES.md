# Koras Connect SDK - Master Feature Specification

## 1. Document Control and Version History

| Field | Value |
|---|---|
| Document | Master architecture and feature index, version 1.1 |
| Audit date | November 12, 2026, America/New_York |
| Assignment | P1, documentation only; stop before P2 |
| Repository | `C:\repos\koras-connect-sdk` |
| Origin | `https://github.com/kkora/koras-connect-sdk` |
| Baseline | `develop`, `15ce97f8b91c2321076a93e541238823ded0da9a` |
| Default branch | `develop`, confirmed through GitHub repository metadata and origin/HEAD |
| Initial working tree | Clean; no staged or unstaged work |
| Product owner | Repository owner; acceptance is not inferred from authorship or merging |
| Specification authority | Supplied requirements; this master for shared architecture; individual feature files for current requirements/status; historical plans remain evidence |
| Approval | P1 creation/validation/local commit authorized by the assignment; feature/release acceptance not granted |

Version 1.1 (November 12, 2026) follows the owner request to organize one feature per file. All 74 detailed specifications now live under `sdk/`, `adp/`, `sap-successfactors/` and `qualtrics/`. Their YAML `status` is the authoritative current tracking state; update evidence and status history in the same file. This master retains shared architecture, audit evidence and baseline summary tables; its status columns are the frozen November 12, 2026 audit snapshot, not a second live status registry. See the [feature tracker](README.md) for update instructions and all feature links. This authorized documentation reorganization supersedes the earlier single-file deliverable restriction; it does not authorize P2 implementation.

Version 1.0 consolidates the source audit, exact SDK/ADP requirements, independent connector catalogs, package migration and P1-P7 gates. The baseline SHA identifies implementation evidence, not this document's eventual commit. Git records the documentation commit separately so this file need not contain its own hash.

Status vocabulary applies to the **whole feature's defined scope**: **Not started** means no executable feature implementation found; **Planned** means a designed future scope with no implementation; **Partially implemented** means useful code exists but stated requirements remain; **Implemented but unverified** means source covers the bounded scope but execution evidence has not been independently established here; **Tested** requires recorded execution tied to a revision and test scope; **Accepted** requires explicit acceptance against all criteria; **Blocked** means a specific prerequisite prevents the next required action. Implementation status and external verification are separate: a partial feature can have blocked live validation. No feature is classified Accepted by P1.

Priority P0/P1/P2 is urgency; phase P1-P7 is a development grouping. They are separate axes. Proposed public symbols below are design commitments for later implementation, not claims that those symbols already exist.

## 2. Executive Summary

The current .NET 10 preview contains four packages and useful authentication, transport, ADP worker/operation, export, SAP OData and Qualtrics survey/export implementations. It does not yet implement the eight target package boundaries or a consistent connector, capability, credential, pagination, export-provider and structured-error contract family. Existing synthetic tests cover selected protocol behavior; neither tenant entitlement nor complete business-schema coverage follows from them.

The first release prioritizes Abstractions, Core, Authentication, Export and ADP. Preserve existing SAP/Qualtrics preview behavior during shared refactoring, then expand them in P5/P6. Observability integration and enterprise expansion follow in P7; safe baseline diagnostics remain P0 foundation work. Public publication, deployment, paid services and main promotion are separate actions, excluded from P1.

This catalog contains 20 SDK, 22 ADP, 16 SAP and 16 Qualtrics features, 74 total. Each linked feature file retains the same twenty mandatory specification fields and adds editable status metadata and completion evidence. Sections 16-26 supply binding common requirements; entries add capability-specific requirements rather than silently waiving common gates.

## 3. Repository Audit and Baseline

### Audit method and source inventory

Read all tracked C# files, all six project files, the solution, shared build configuration, release scripts/tests, sample NuGet configuration, three workflows, README, license-review record and every existing planning/architecture/feature/decision Markdown file. No `.sln` exists; `Koras.Connect.slnx` includes four runtime projects and one test project. The sixth project is the independent package-reference sample, outside the solution. No external reference implementation was read. Source inspection is static; P1 does not rerun application builds or report fresh runtime test results.

Evidence paths below are relative to repository root. These aliases resolve to exact source files and are reused in feature entries:

| Evidence key | Paths and audited behavior |
|---|---|
| E-BUILD | `Koras.Connect.slnx`, `global.json`, `Directory.Build.props`, all `src/*/*.csproj`, `tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`, `samples/PackageConsumer/PackageConsumer.csproj`: net10.0, SDK minimum 10.0.100/latestFeature, preview.2, nullable, deterministic build, warnings as errors, XML docs with CS1591 suppressed |
| E-AUTH | `src/Koras.Connect/Authentication/ClientCredentialsTokenSource.cs`: redacted options, HTTPS validation, Basic client credentials, optional scope, semaphore/cache/expiry renewal; `ClientCertificateTransport.cs`: PEM/PFX loading, private-key check, redirects disabled |
| E-TRANSPORT | `src/Koras.Connect/Transport/IntegrationTransport.cs`: IAccessTokenSource, TransportOptions, IntegrationRequestException, IntegrationJsonResponse, GET retry/deadline/JSON/XML/binary operations; `RequestAuthentication.cs`: IRequestAuthenticator and bearer/header implementations |
| E-SAFETY | `src/Koras.Connect/Transport/EndpointSafety.cs`: root, identifier and continuation boundary validation |
| E-DI | `src/Koras.Connect.Adp/AdpServiceCollectionExtensions.cs`: AddKorasAdp, named factory clients, mTLS, logger removal, duplicate registration rejection |
| E-WORKERS | `src/Koras.Connect.Adp/Workers/AdpWorkersClient.cs`: page/search/metadata/individual and typed/raw async enumeration; `Worker.cs`: Worker, Identifier, Person, LegalName, WorkAssignment and extension data |
| E-OPS | `src/Koras.Connect.Adp/AdpOperationsClient.cs`: explicit AdpOperationCatalog, opt-in allowlist, raw read/write/metadata, notification/ack, effective-date benefits query |
| E-EVENT | `src/Koras.Connect.Adp/Events/AdpWebhookAuthentication.cs`: publisher header check and delivery key, no receiver or subscription provisioning |
| E-SYNC | `src/Koras.Connect/Synchronization/Reconciliation.cs`: DetectChangesAsync, RecordChange and BatchAsync, no persistence/deletion inference |
| E-EXPORT | `src/Koras.Connect/Export/TabularExport.cs`: static CSV/JSON/XML/XLSX stream writers, width/Excel bounds and formula defenses |
| E-SAP | `src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.cs`: ODataQuery, metadata, generic/person enumeration, PerPerson/EmpEmployment/EmpJob, explicit upsert/results; `SamlBearerTokenSource.cs`: SAML assertion exchange/cache |
| E-QLT | `src/Koras.Connect.Qualtrics/QualtricsClient.cs`: token factory, typed survey/progress/options, list/get, start/poll/download exports |
| E-TEST | `tests/Koras.Connect.Tests/CoreTests.cs`, `WorkersTests.cs`, `AdpContractTests.cs`, `SecurityAndLifecycleTests.cs`, `ExpansionTests.cs`: synthetic in-memory handlers and inline payloads; no external fixture corpus or live sandbox harness found |
| E-RELEASE | `.github/workflows/ci.yml`, `release.yml`, `main-promotion.yml`; `eng/release_manifest.json`, `validate_packages.py`, `generate_sbom.py`, `release_checks.py`, `check_sources.py`; `tests/release/test_release_checks.py` |
| E-SAMPLE | `samples/PackageConsumer/Program.cs`, `NuGet.Config`, project; `README.md`: synthetic package consumer and inline provider usage |

Current runtime graph: `Koras.Connect.Adp`, `Koras.Connect.Sap.SuccessFactors` and `Koras.Connect.Qualtrics` reference `Koras.Connect`. Core has no third-party runtime package; ADP also references Microsoft.Extensions.Http 10.0.0. Tests use Microsoft.NET.Test.Sdk 17.14.1, xunit 2.9.3 and xunit.runner.visualstudio 3.1.4. Shared NuGet auditing is enabled for direct/transitive packages. No central package-version file, API compatibility baseline, dedicated documentation site, signing or build attestation automation was found. Sample NuGet.Config clears sources and selects local artifacts plus nuget.org. No root NuGet.Config was found.

### History and pull requests

Local history includes independent foundation `4de0693`, expanded ADP/core `5b3c74b`, G4-G6 implementation `415419e56188dba4b362d5950e8991096f7bf0eb`, expansion merge PR #2, legacy reconciliation PR #3, development policy PR #8, agent policy PR #9 and architecture reconciliation PR #10. Git ancestry confirms both historical tips `cd2f66f547fe485abc4f00d8f3adb9c311cce457` and `c968628e32abe4905e2cbdd7bbc1ce14482a0ffa` are ancestors of this baseline. Their local feature branches are absent; do not revive their prototype trees. Current local refs are develop and origin/develop/main.

GitHub open-PR query returned none at audit time. [PR #1](https://github.com/kkora/koras-connect-sdk/pull/1) and [PR #2](https://github.com/kkora/koras-connect-sdk/pull/2) are merged historical implementation evidence; [PR #10](https://github.com/kkora/koras-connect-sdk/pull/10) restores original SDK/ADP identifiers. `docs/planning/VERIFICATION.md` records 55 .NET and 11 Python tests at the prior expansion revision; those are historical reports, not fresh P1 executions or complete-feature acceptance. PR #2 identifies Linux/Windows run 43235741446 at `987965cee2948c05de1593220b1169e5117771e6`. No assertion is made about current CI or server-side branch protection.

GitHub metadata reports repository visibility **public**, conflicting with `LICENSE-REVIEW.md`'s historical private-repository sentence. Repository visibility does not grant a source-distribution license. No license grant or publication authorization is inferred. Record this discrepancy for later document maintenance; preserve the original file during P1.

### Existing documents and supersession

This file supersedes conflicting future feature requirements/phase labels in `docs/features/MASTER-FEATURE-PLAN.md`, all three provider feature matrices, `docs/architecture/SDK-ARCHITECTURE.md`, and the plans under `docs/planning/`. It preserves their historical implementation evidence. `ARCHITECTURE-AND-FEATURE-RECONCILIATION.md` remains the source of corrected SDK/ADP identity mapping. `BUILD-AND-MERGE-PLAN.md`, `INDEPENDENT-SDK-G0-G3.md`, `IMPLEMENTATION-ROADMAP.md`, `G4-G6-SPECIFICATION.md`, `DEPENDENCIES.md`, `VERIFICATION.md` and `RELEASE-RUNBOOK.md` are historical/operational references, not proof of current acceptance. All remain unchanged.

`docs/decisions/DEVELOPMENT-BRANCH-POLICY.md`, `APPROVAL-GATES.md` and `LEGACY-BRANCH-RECONCILIATION.md` preserve policy/history. The current P1 restriction takes precedence over older implementation/merge authorizations. Statements about retaining merged PR #1 as draft or future direct-main merges are historical and must not drive new actions.

The old master reused SDK IDs. Preserve it without treating those meanings as canonical:

| Old SDK ID / subject | Canonical coverage |
|---|---|
| 001 structure; 002 interfaces | SDK-001; SDK-002 |
| 003 HTTP/DI; 004 certificate/OAuth | SDK-003/004; SDK-005/006, ADP-001/002 |
| 005 pagination/cancellation; 006 retries/errors | SDK-007/010; SDK-008/009 |
| 007 CSV/JSON/XML; 008 Excel | SDK-013, ADP-008; SDK-013, ADP-009 |
| 009 logging; 010 packaging | SDK-011/012; SDK-016/019 |
| 011 security; 012 docs/examples | SDK-018; SDK-020, ADP-022 and provider documentation entries |
| 013 ADP; 014 SAP; 015 Qualtrics | ADP-001-022; SAP-001-016; QLT-001-016 |

SAP-001-007 retain their earlier broad catalog subjects; new IDs refine coverage. QLT-001-008 retain earlier subjects; old QLT-003b and QLT-003c are historical aliases for the polling/download acceptance scenarios within QLT-003, not additional features. No ADP ID is reassigned.

## 4. Product Vision and Scope

Provide independently installable, host-neutral .NET integration libraries with explicit vendor contracts, predictable resource ownership, safe defaults and honest capability reporting. Engineers can retrieve, map, reconcile and export authorized records without adopting a scheduler, database, cloud service or another provider.

SDK responsibilities: API clients, authentication, transport, API models, serialization, pagination, resilience, diagnostics, exports, reusable mapping and stateless synchronization primitives. Excluded: persistent scheduling, cross-system workflow orchestration, tenant administration, subscription billing, dashboards, cloud provisioning, production job hosting and customer onboarding. CLI/Cloud own those exclusions. A provider SDK can be used without any other provider package.

## 5. SDK Architectural Principles

Use independent engineering and only this repository plus official ADP, SAP, Qualtrics and Microsoft specifications. Never consult the prohibited reference repository. Keep dependencies acyclic and provider-neutral layers free of vendor models. Prefer explicit composition over service location; preserve constructor injection alongside DI helpers. Propagate cancellation through authentication, transport, enumeration and export. Do not infer remote entitlements from local configuration or operation enums. Preserve unknown fields and make lossy mapping intentional. Fail safely on invalid configuration before sending credentials. Separate source implementation, synthetic test evidence, authorized live validation and product acceptance.

## 6. Target Package Architecture

All targets initially use net10.0. No netstandard or older-runtime compatibility is promised. Coordinate the first five packages at one preview version, then one stable major/minor line; test the minimum supported dependencies. Add a new TFM only with a compatibility proposal and full package-consumer tests. Proposed dependencies below are package families, not approval to add or upgrade packages during P1.

| Exact target package ID | Responsibilities and public boundary | Proposed external dependencies | Release |
|---|---|---|---|
| Koras.Connect.Abstractions | Connector/capability, credential/token/request-auth, page/checkpoint, export/mapping/diagnostic contracts and small neutral DTOs; no HTTP implementation | .NET runtime only | First |
| Koras.Connect.Core | Transport, serialization, resilience, pagination utilities, neutral exceptions, safe baseline diagnostic hooks, DI builder | Microsoft.Extensions.Http/Options/Logging abstractions 10.x where needed; no exporter | First |
| Koras.Connect.Authentication | Credential/certificate adapters and OAuth/token lifecycle; implements abstractions | Microsoft.Extensions.Options/DI abstractions 10.x if required; no cloud vault SDK by default | First |
| Koras.Connect.Export | CSV/JSON/XML/XLSX providers, schema projection, stream ownership | Runtime initially; an Excel dependency requires separate license/performance justification | First |
| Koras.Connect.Observability | Optional OpenTelemetry registration, enrichment/filtering and advanced metrics | OpenTelemetry API/instrumentation packages, exact versions reviewed in P7; host selects exporter | Subsequent |
| Koras.Connect.ADP | Product-specific workers, models, entitlement-aware operations, metadata, diagnostics | Shared packages; Microsoft.Extensions.Http 10.x through reviewed direct needs | First |
| Koras.Connect.SAP.SuccessFactors | Independent OData/auth adapters, employee models, metadata/upsert | Shared packages; no ADP reference | Subsequent |
| Koras.Connect.Qualtrics | Independent directories/contacts/surveys/export API models/client | Shared packages; no SAP or ADP reference | Subsequent |

DI strategy: a proposed AddKorasConnect builder composes neutral services; provider AddKorasAdp/AddKorasSuccessFactors/AddKorasQualtrics extensions register named instances and validate on construction/start. New named registrations isolate tenant credentials and HTTP handlers; until implemented retain existing ADP duplicate-registration rejection and use separate providers. Do not imply named multi-tenant registration exists today. Token caches are per credential/tenant, never globally shared. Hosts own external HttpClient/certificates unless a factory explicitly creates them; owned resources have deterministic disposal. Do not capture replaceable factory clients indefinitely without a documented handler/DNS/certificate-rotation strategy; current singleton registration deserves lifecycle review.

Migration: snapshot existing public API/package metadata first; split neutral contracts, then move implementations preserving signatures where feasible. Keep a temporary Koras.Connect compatibility facade with type forwarders or obsolete wrappers only where CLR identity allows it; test consumers built against preview.2. No duplicate assembly may define the same public type. NuGet IDs are case-insensitive: ADP/Adp and SAP/Sap casing changes are not new identities. Do case-only filesystem changes through a temporary name in a later phase, never create parallel directories. A package display-case change does not solve assembly/namespace migration. Document the break and preview migration if forwarding cannot preserve compatibility. Facade is transitional, not a ninth permanent target package.

## 7. Package Dependency Matrix

An arrow means a direct permitted dependency; absence means prohibited unless a future decision updates this specification. Transitive shared dependencies are not extra direct requirements.

| Package | Direct internal dependencies | Must not depend on |
|---|---|---|
| Abstractions | None | Every implementation, host, vendor, DI container |
| Core | Abstractions | Authentication implementation, Export, Observability, providers |
| Authentication | Abstractions | Core transport implementation, providers, Export, Observability |
| Export | Abstractions | Core HTTP, auth, providers, Observability |
| Observability | Abstractions, Core | Provider implementations, host exporters |
| ADP | Abstractions, Core, Authentication | SAP, Qualtrics, Export, Observability |
| SAP.SuccessFactors | Abstractions, Core, Authentication | ADP, Qualtrics, Export, Observability |
| Qualtrics | Abstractions, Core, Authentication | ADP, SAP, Export, Observability |

Provider exports compose in the consuming application using neutral projections and Export; ADP does not require Export transitively. Core accepts IRequestAuthenticator from Abstractions; Authentication implements it without needing Core. Provider-specific authentication remains provider-owned. Baseline ActivitySource/Meter hooks stay in Core so optional Observability can be omitted. Build graph validation and isolated package-consumer tests must enforce this graph in P2/P4.

## 8. Public API and Contract Strategy

Current public interfaces are only IAccessTokenSource and IRequestAuthenticator. Keep existing methods documented as current while proposed signatures are reviewed through compiling consumer examples in P2. Proposed contracts:

```csharp
// Proposed design, not currently available or a complete compilable assembly.
public interface IConnector {
    ConnectorDescriptor Descriptor { get; }
    ValueTask<ConnectionDiagnostic> DiagnoseAsync(CancellationToken cancellationToken = default);
}
public interface ICredentialProvider<TCredential> {
    ValueTask<TCredential> GetAsync(CancellationToken cancellationToken = default);
}
public sealed record Page<T>(IReadOnlyList<T> Items, ContinuationToken? Next);
public sealed record ContinuationToken(string Provider, string QueryFingerprint, string OpaqueValue);
public interface IExportProvider {
    string Format { get; }
    Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records,
        ExportSchema<T> schema, CancellationToken cancellationToken = default);
}
public interface IRecordMapper<TSource, TTarget> {
    MappingResult<TTarget> Map(TSource source);
}
```

ConnectorDescriptor has provider/product/API version, static supported operations and runtime entitlement state (Unknown/Configured/Verified/Denied with timestamp/evidence reference); no boolean may conflate these. ConnectionDiagnostic has stage, outcome, sanitized code, correlation, timestamp and recommended next step; no secrets or payloads. Credential wrappers redact ToString and avoid diagnostic enumeration. Page tokens are opaque sensitive values bound to provider/query/tenant context and expiry where known; never contain raw credentials. MappingResult distinguishes valid output, explicit omission and field errors. ExportSchema defines ordered columns, projection and format/null/date policies. Freeze complete members and nullability with API baselines before stable release.

Use Task for asynchronous I/O, ValueTask only for cache/source boundaries, IAsyncEnumerable for streaming. Require optional CancellationToken last; preserve cancellation rather than wrapping it as a vendor error. Return disposable JSON only when ownership is explicit, prefer typed DTOs for stable operations, retain extension data. Distinguish absent/masked/null values and effective dates. Exceptions belong in Core; vendor exception subclasses depend inward. Capability contracts use no reflection-based claims about server permissions.

## 9. Shared SDK Master Feature Matrix

| ID | Feature | Priority | Phase | Audit baseline status |
|---|---|---|---|---|
| [SDK-001](sdk/SDK-001.md) | .NET 10 solution and package structure | P0 | P2 | Partially implemented |
| [SDK-002](sdk/SDK-002.md) | Connector interface contracts | P0 | P2 | Not started |
| [SDK-003](sdk/SDK-003.md) | Dependency injection extensions | P0 | P2 | Partially implemented |
| [SDK-004](sdk/SDK-004.md) | HttpClientFactory integration | P0 | P2 | Partially implemented |
| [SDK-005](sdk/SDK-005.md) | Authentication abstraction | P0 | P2 | Partially implemented |
| [SDK-006](sdk/SDK-006.md) | Secure credential providers | P0 | P2 | Partially implemented |
| [SDK-007](sdk/SDK-007.md) | Pagination abstractions | P0 | P2 | Partially implemented |
| [SDK-008](sdk/SDK-008.md) | Retry and resilience policies | P0 | P2 | Partially implemented |
| [SDK-009](sdk/SDK-009.md) | Structured exception hierarchy | P0 | P2 | Partially implemented |
| [SDK-010](sdk/SDK-010.md) | Cancellation and timeout support | P0 | P2 | Partially implemented |
| [SDK-011](sdk/SDK-011.md) | Logging and correlation IDs | P0 | P2 | Partially implemented |
| [SDK-012](sdk/SDK-012.md) | OpenTelemetry instrumentation | P1 | P7 | Partially implemented |
| [SDK-013](sdk/SDK-013.md) | Export provider interfaces | P0 | P3 | Partially implemented |
| [SDK-014](sdk/SDK-014.md) | Configuration validation | P0 | P2 | Partially implemented |
| [SDK-015](sdk/SDK-015.md) | Package signing and provenance | P1 | P7 | Not started |
| [SDK-016](sdk/SDK-016.md) | NuGet publishing workflow | P0 | P4 | Partially implemented |
| [SDK-017](sdk/SDK-017.md) | API compatibility checks | P0 | P2-P4 | Not started |
| [SDK-018](sdk/SDK-018.md) | Security scanning and SBOM | P0 | P4 | Partially implemented |
| [SDK-019](sdk/SDK-019.md) | Versioning and release notes | P0 | P4 | Partially implemented |
| [SDK-020](sdk/SDK-020.md) | SDK documentation website | P1 | P7 | Not started |

## 10. Shared SDK Detailed Feature Specifications

<a id="sdk-001"></a>
### SDK-001 - .NET 10 solution and package structure

The authoritative [feature specification and completion tracker](sdk/SDK-001.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-002"></a>
### SDK-002 - Connector interface contracts

The authoritative [feature specification and completion tracker](sdk/SDK-002.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-003"></a>
### SDK-003 - Dependency injection extensions

The authoritative [feature specification and completion tracker](sdk/SDK-003.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-004"></a>
### SDK-004 - HttpClientFactory integration

The authoritative [feature specification and completion tracker](sdk/SDK-004.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-005"></a>
### SDK-005 - Authentication abstraction

The authoritative [feature specification and completion tracker](sdk/SDK-005.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-006"></a>
### SDK-006 - Secure credential providers

The authoritative [feature specification and completion tracker](sdk/SDK-006.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-007"></a>
### SDK-007 - Pagination abstractions

The authoritative [feature specification and completion tracker](sdk/SDK-007.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-008"></a>
### SDK-008 - Retry and resilience policies

The authoritative [feature specification and completion tracker](sdk/SDK-008.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-009"></a>
### SDK-009 - Structured exception hierarchy

The authoritative [feature specification and completion tracker](sdk/SDK-009.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-010"></a>
### SDK-010 - Cancellation and timeout support

The authoritative [feature specification and completion tracker](sdk/SDK-010.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-011"></a>
### SDK-011 - Logging and correlation IDs

The authoritative [feature specification and completion tracker](sdk/SDK-011.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-012"></a>
### SDK-012 - OpenTelemetry instrumentation

The authoritative [feature specification and completion tracker](sdk/SDK-012.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-013"></a>
### SDK-013 - Export provider interfaces

The authoritative [feature specification and completion tracker](sdk/SDK-013.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-014"></a>
### SDK-014 - Configuration validation

The authoritative [feature specification and completion tracker](sdk/SDK-014.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-015"></a>
### SDK-015 - Package signing and provenance

The authoritative [feature specification and completion tracker](sdk/SDK-015.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-016"></a>
### SDK-016 - NuGet publishing workflow

The authoritative [feature specification and completion tracker](sdk/SDK-016.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-017"></a>
### SDK-017 - API compatibility checks

The authoritative [feature specification and completion tracker](sdk/SDK-017.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-018"></a>
### SDK-018 - Security scanning and SBOM

The authoritative [feature specification and completion tracker](sdk/SDK-018.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-019"></a>
### SDK-019 - Versioning and release notes

The authoritative [feature specification and completion tracker](sdk/SDK-019.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sdk-020"></a>
### SDK-020 - SDK documentation website

The authoritative [feature specification and completion tracker](sdk/SDK-020.md) is maintained in its own file. Shared requirements in this master document still apply.

## 11. ADP Master Feature Matrix

| ID | Feature | Priority | Phase | Audit baseline status |
|---|---|---|---|---|
| [ADP-001](adp/ADP-001.md) | Certificate and mTLS authentication | P0 | P2 | Implemented but unverified |
| [ADP-002](adp/ADP-002.md) | OAuth token acquisition and refresh | P0 | P2 | Implemented but unverified |
| [ADP-003](adp/ADP-003.md) | Secure client credentials configuration | P0 | P2 | Partially implemented |
| [ADP-004](adp/ADP-004.md) | Worker profile retrieval | P0 | P3 | Implemented but unverified |
| [ADP-005](adp/ADP-005.md) | Worker pagination and filtering | P0 | P3 | Partially implemented |
| [ADP-006](adp/ADP-006.md) | Worker changes and incremental sync | P0 | P3 | Partially implemented |
| [ADP-007](adp/ADP-007.md) | Worker schema mapping | P0 | P3 | Partially implemented |
| [ADP-008](adp/ADP-008.md) | CSV/JSON/XML export | P0 | P3 | Partially implemented |
| [ADP-009](adp/ADP-009.md) | Excel export | P1 | P3 | Partially implemented |
| [ADP-010](adp/ADP-010.md) | Structured errors and rate-limit handling | P0 | P3 | Partially implemented |
| [ADP-011](adp/ADP-011.md) | Retry, timeout, and cancellation | P0 | P3 | Implemented but unverified |
| [ADP-012](adp/ADP-012.md) | Connection diagnostics | P0 | P3 | Not started |
| [ADP-013](adp/ADP-013.md) | ADP organization and manager data | P1 | P3 extension | Partially implemented |
| [ADP-014](adp/ADP-014.md) | Employment status and lifecycle events | P1 | P3 extension | Partially implemented |
| [ADP-015](adp/ADP-015.md) | Payroll operations, where entitled | P1 | P3 extension | Partially implemented |
| [ADP-016](adp/ADP-016.md) | Time and attendance APIs | P2 | P7 | Partially implemented |
| [ADP-017](adp/ADP-017.md) | Benefits APIs | P2 | P7 | Partially implemented |
| [ADP-018](adp/ADP-018.md) | Webhook/event subscriptions | P2 | P3 primitives; P7 expansion | Partially implemented |
| [ADP-019](adp/ADP-019.md) | Typed API response models | P0 | P3 | Partially implemented |
| [ADP-020](adp/ADP-020.md) | Sandbox and contract test suite | P0 | P3-P4 | Partially implemented |
| [ADP-021](adp/ADP-021.md) | API capability/entitlement matrix | P0 | P1 baseline; P3 completion | Partially implemented |
| [ADP-022](adp/ADP-022.md) | Documentation and runnable examples | P0 | P3-P4 | Partially implemented |

## 12. ADP Detailed Feature Specifications

Baseline product scope is ADP U.S. Workforce Now. WFN Next Gen, RUN, Lyric, Vantage and international products are not assumed equivalent. The current worker/auth/retry code covers bounded operations, but acceptance also requires current schema and permitted sandbox verification. First-release P0 requirements are mandatory; existing P1/P2 dispatch code is preserved as an explicitly limited preview, not expanded during P1.

<a id="adp-001"></a>
### ADP-001 - Certificate and mTLS authentication

The authoritative [feature specification and completion tracker](adp/ADP-001.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-002"></a>
### ADP-002 - OAuth token acquisition and refresh

The authoritative [feature specification and completion tracker](adp/ADP-002.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-003"></a>
### ADP-003 - Secure client credentials configuration

The authoritative [feature specification and completion tracker](adp/ADP-003.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-004"></a>
### ADP-004 - Worker profile retrieval

The authoritative [feature specification and completion tracker](adp/ADP-004.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-005"></a>
### ADP-005 - Worker pagination and filtering

The authoritative [feature specification and completion tracker](adp/ADP-005.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-006"></a>
### ADP-006 - Worker changes and incremental sync

The authoritative [feature specification and completion tracker](adp/ADP-006.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-007"></a>
### ADP-007 - Worker schema mapping

The authoritative [feature specification and completion tracker](adp/ADP-007.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-008"></a>
### ADP-008 - CSV/JSON/XML export

The authoritative [feature specification and completion tracker](adp/ADP-008.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-009"></a>
### ADP-009 - Excel export

The authoritative [feature specification and completion tracker](adp/ADP-009.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-010"></a>
### ADP-010 - Structured errors and rate-limit handling

The authoritative [feature specification and completion tracker](adp/ADP-010.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-011"></a>
### ADP-011 - Retry, timeout, and cancellation

The authoritative [feature specification and completion tracker](adp/ADP-011.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-012"></a>
### ADP-012 - Connection diagnostics

The authoritative [feature specification and completion tracker](adp/ADP-012.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-013"></a>
### ADP-013 - ADP organization and manager data

The authoritative [feature specification and completion tracker](adp/ADP-013.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-014"></a>
### ADP-014 - Employment status and lifecycle events

The authoritative [feature specification and completion tracker](adp/ADP-014.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-015"></a>
### ADP-015 - Payroll operations, where entitled

The authoritative [feature specification and completion tracker](adp/ADP-015.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-016"></a>
### ADP-016 - Time and attendance APIs

The authoritative [feature specification and completion tracker](adp/ADP-016.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-017"></a>
### ADP-017 - Benefits APIs

The authoritative [feature specification and completion tracker](adp/ADP-017.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-018"></a>
### ADP-018 - Webhook/event subscriptions

The authoritative [feature specification and completion tracker](adp/ADP-018.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-019"></a>
### ADP-019 - Typed API response models

The authoritative [feature specification and completion tracker](adp/ADP-019.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-020"></a>
### ADP-020 - Sandbox and contract test suite

The authoritative [feature specification and completion tracker](adp/ADP-020.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-021"></a>
### ADP-021 - API capability/entitlement matrix

The authoritative [feature specification and completion tracker](adp/ADP-021.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="adp-022"></a>
### ADP-022 - Documentation and runnable examples

The authoritative [feature specification and completion tracker](adp/ADP-022.md) is maintained in its own file. Shared requirements in this master document still apply.

## 13. ADP API Capability and Entitlement Matrix

### Official reference register and retrieval limits

All retrieval attempts below occurred November 12, 2026. Readable official documentation confirms only the stated contract slice; it does not prove current tenant access. Older PDF revisions require current authorized metadata before release acceptance. These references document protocols only; no vendor SDK implementation was copied.

| Ref | Official reference | P1 retrieval / evidence scope |
|---|---|---|
| A1 | [WFN Worker Management guide](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Worker_Management_API_Guide_for_ADP_Workforce_Now) | Readable PDF, revised June 14, 2023; worker reads/query and product-specific behavior. Portal preview returned only a JavaScript shell. |
| A2 | [WFN Work Assignment guide](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Work_Assignment_API_Guide_for_ADP_Workforce_Now) | Readable PDF, revised April 26, 2023; assignment request/metadata and validation context. |
| A3 | [WFN spending-account enrollment guide](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Benefit_Spending_Account_Enrollment_API_Guide_%28HROpen%29_for_ADP_Workforce_Now) | Readable PDF, revised June 8, 2023; dated enrollment read and plan/onboarding restrictions; inconsistent route prose is unresolved. |
| A4 | [ADP event/webhook guide](https://marketplace-cdn.adp.com/dev-portal/pdf/Webhooks) | Readable PDF, revised October 31, 2025; notification GET/DELETE, subscription setup, static publisher header. |
| A5 | [ADP-published WFN request collection](https://raw.githubusercontent.com/adpllc/marketplace-sample-payloads/master/wfn/ADP%20WFN%20APIs.postman_collection.json) | Readable official protocol examples; methods/paths are not complete response schemas or current tenant grants. |
| A6 | [Getting started](https://developers.adp.com/guides/api-guides/adp-marketplace---getting-started) | Fetch failed; no new contract claim from this URL. |
| A7 | [WFN catalog](https://developers.adp.com/articles/guides/adp-workforce-now-api-catalog) and [termination guide](https://developers.adp.com/articles/preview/guide-worker-termination-api-guide-for-adp-workforce-now-3?chapter=2) | JavaScript shells; exact current termination contract not independently retrieved through these pages. |

### Documented grant candidates versus actual permission

The readable A2 guide identifies `/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.modify` for assignment modification and Practitioner context. A1 identifies `/hr/workerInformationManagement/workerManagement/workRelationshipManagement/worker.reportsTo.modify` for manager modification and `/hr/workerInformationManagement/workerManagement/workAssignmentManagement/worker.workAssignment.assignedOrganizationalUnits.modify` for allocation changes. Treat these as historical documented canonical candidates; actual current CAR approval, actor/population and product applicability still require authorized confirmation. Never synthesize remaining scope strings by concatenating event names.

### Operation inventory

All rows are WFN/U.S. candidates, not a cross-product ADP guarantee. **S** = implemented source observed; **D** = official method/path slice found; **U** = contract verification incomplete. Every row has **live=unverified**. Auth is registered client mTLS plus authorized bearer for resource APIs; token acquisition uses the exact configured security endpoint. The tenant-granted CAR scope, actor and population are **unknown until supplied by the customer's current application/metadata** for every row. Documented scope candidates below do not establish tenant grants; other exact scope strings remain unverified. Enabling an enum is local permission to attempt, never evidence of a remote grant. No numeric quota is verified; apply SDK-008 and retain documented server hints. Shared error behavior is ADP-010/011, not raw error-body logging.

| Feature coverage / family | Method and path (relative to configured root) | Request - response / current support | Documentation state; grant/limit follow-up |
|---|---|---|---|
| ADP-001/002, OAuth | POST `/auth/oauth/v2/token` | client_credentials + Basic credentials - access_token/expires_in; S | D A5; current host/mTLS registration and complete response contract U; no refresh-token promise |
| ADP-004/005/019, workers | GET `/hr/v2/workers` | top/skip/filter - workers; S | D A1; schema/filter entitlement U; SDK bound 1-100 is local |
| ADP-004/019, worker | GET `/hr/v2/workers/{associateOID}` | encoded OID - workers/Worker; S | D A1; population/field masking U |
| ADP-005/021, metadata | GET `/hr/v2/workers/meta` | no body - JSON metadata; S | D A1; current schema/version U |
| ADP-014, hire | POST `/events/hr/v1/worker.hire` | event JSON - raw JSON; S | D A5 example; full current schema/grant U |
| ADP-014, rehire | POST `/events/hr/v1/worker.rehire` | event JSON - raw JSON; S | D A5 example; dates/actor/current schema U |
| ADP-014, termination | POST `/events/hr/v1/worker.work-assignment.terminate` | event JSON - raw JSON; S | U: portal A7 unavailable; retain as source evidence, not newly verified contract |
| ADP-013/014, manager | POST `/events/hr/v1/worker.reports-to.modify` | event JSON - raw JSON; S | A1 historical guide; full product/current schema U |
| ADP-014, assignment | POST `/events/hr/v1/worker.work-assignment.modify` | SDK checks associateOID/workAssignmentID/workAssignment - raw JSON; S | D A2; metadata-required fields, codes, effectiveDateTime U |
| ADP-013/014, allocations | POST `/events/hr/v1/worker.work-assignment.assigned-organizational-units.modify` | event JSON - raw JSON; S | A1 historical guide; not a general home department replacement; current schema U |
| ADP-014, communication | POST `/events/hr/v1/worker.personal-communication.email.change` | event JSON - raw JSON; S | D A5 example; full payload/grant U |
| ADP-015, payroll input | POST `/events/payroll/v1/pay-data-input.modify` | event JSON - raw JSON; S | D A5 example; payroll-group/batch schema and write authorization U |
| ADP-015, instructions | GET `/payroll/v1/workers/{associateOID}/payroll-instructions` | OID - raw JSON; S | D A5; full schema/permission U |
| ADP-015, distributions | GET `/payroll/v2/workers/{associateOID}/pay-distributions` | OID - raw JSON; S | D A5; full schema/permission U |
| ADP-016, time-off | GET `/time/v2/workers/{associateOID}/time-off-details/time-off-balances` | OID - raw JSON; S | D A5; units/date/paging/permission U |
| ADP-016, requests | GET `/time/v2/workers/{associateOID}/time-off-details/time-off-requests` | OID - raw JSON; S | D A5; full schema/permission U; not all attendance |
| ADP-017, enrollments | GET `/benefits/v1/associates/{associateOID}/spending-account-enrollments` | OID + effective-date filter - raw JSON; S | D A3; plan setup/Benefits onboarding and exact tenant contract U |
| ADP-013, organizations | GET `/hr/v1/organization-departments` | no body - raw JSON; S | D A5; full schema/paging/permission U |
| ADP-006/018, notifications | GET `/core/v1/event-notification-messages` | no body - JSON/204 + adp-msg-msgid; S | D A4; actual event subscriptions/coverage U |
| ADP-006/018, acknowledgement | DELETE `/core/v1/event-notification-messages/{messageId}` | prior header ID - JSON/204; S | D A4; host explicitly calls after durable processing |
| ADP-018, webhooks | No management endpoint claimed | VerifyPublisherHeader/GetDeliveryKey host helpers; S | A4; receiver authentication/TLS/inbox are host responsibilities; provisioning is administrative |
| ADP-016/017, expansion | No additional endpoint verified | No attendance/timecard/general-benefits implementation asserted | Block expansion until independently retrieved official contracts/grants and tests exist |

The catalog also exposes metadata GET at event-path + `/meta` for hire, rehire, personal email, payroll input and assignment modify. This is **source evidence**, not confirmation that each current tenant supports the route. Check each metadata route independently; do not generate `/meta` for every enum. Raw dispatch structural checks do not validate the whole business schema. Official sample collection uses credential variants; never place secrets in query strings in new SDK APIs/examples even if a historical example does so.

Provider response contracts: workers use a workers array and extension-preserving partial models; operation responses remain caller-owned JSON; notification wrappers retain allowlisted IDs and map 204 to JSON null. Writes require metadata-derived required fields/codes and result parsing, still incomplete. Per-operation test sets must include 401/403, invalid input, malformed/empty responses, cancellation, rate hints and no automatic write retry; additional business cases are in ADP-013-019. No provider-wide paging algorithm is assumed for non-worker reads. Unsupported pagination/filter options must fail locally or remain absent until verified.

Worker guide A1 documents top/skip/filter and a separate asynchronous flow. Current code does not implement its 202/link polling; do not advertise that mode. A2 is assignment-specific and is not a generic worker update contract. A3 describes external-plan setup and required dated filtering; sensitive fields must remain explicitly selected. A4 requires subscribed notifications and recommends current-record re-read; static publisher HMAC is not payload integrity or replay prevention. No SDK scheduler, durable inbox or subscription-administration API is implied.

To close U: obtain current authorized metadata/schema and canonical CAR scope/actor/population/product version; record retrieval date and operation-specific limits; create synthetic fixtures from schemas without copying customer PII; run authorized sandbox scenarios; update matrix and capability descriptor together. Missing research is not proof that ADP forbids a capability.

## 14. SAP SuccessFactors Feature Catalog

This independent connector owns SAP semantics, not ADP-to-SAP orchestration. SAP-001-007 retain earlier catalog subjects; SAP-008-016 refine them. Preserve preview behavior during P2-P4; expansion/acceptance is P5.

Official sources checked November 12, 2026: [S1: SAML exchange](https://help.sap.com/docs/successfactors-platform/sap-successfactors-api-reference-guide-odata-v2/requesting-access-token?locale=en-US) (official search excerpt readable, page rendering limited), [S2: entity operations/upsert](https://userapps.support.sap.com/sap/support/knowledge/en/2670250), [S3: query/pagination/retry guidance](https://userapps.support.sap.com/sap/support/knowledge/E/2735876) (readable). The historical 2411 OData PDF redirected to page-not-found and is not new schema evidence. Current tenant metadata/entity references must close each entity contract. Permissions are entity-specific RBP/target population; do not invent OAuth scope strings or assume all entities support all verbs.

| ID | Feature | Priority | Phase | Audit baseline status |
|---|---|---|---|---|
| [SAP-001](sap-successfactors/SAP-001.md) | OAuth and supported authentication mechanisms | P0 | P5 | Partially implemented |
| [SAP-002](sap-successfactors/SAP-002.md) | OData API foundation and employee/person data | P0 | P5 | Partially implemented |
| [SAP-003](sap-successfactors/SAP-003.md) | Upsert operations | P0 | P5 | Partially implemented |
| [SAP-004](sap-successfactors/SAP-004.md) | Pagination and filtering | P0 | P5 | Partially implemented |
| [SAP-005](sap-successfactors/SAP-005.md) | Error handling and diagnostics | P0 | P5 | Partially implemented |
| [SAP-006](sap-successfactors/SAP-006.md) | Contract and sandbox testing | P0 | P5 | Partially implemented |
| [SAP-007](sap-successfactors/SAP-007.md) | Additional APIs and complete business-schema validation | P1 | P5 | Planned |
| [SAP-008](sap-successfactors/SAP-008.md) | Employment records | P0 | P5 | Partially implemented |
| [SAP-009](sap-successfactors/SAP-009.md) | Job information | P0 | P5 | Partially implemented |
| [SAP-010](sap-successfactors/SAP-010.md) | Organizational data | P1 | P5 | Not started |
| [SAP-011](sap-successfactors/SAP-011.md) | Manager relationships | P1 | P5 | Partially implemented |
| [SAP-012](sap-successfactors/SAP-012.md) | Employee create/update operations where authorized | P1 | P5 | Partially implemented |
| [SAP-013](sap-successfactors/SAP-013.md) | Delta and incremental synchronization | P0 | P5 | Partially implemented |
| [SAP-014](sap-successfactors/SAP-014.md) | Metadata and schema discovery | P0 | P5 | Partially implemented |
| [SAP-015](sap-successfactors/SAP-015.md) | Data mapping | P0 | P5 | Partially implemented |
| [SAP-016](sap-successfactors/SAP-016.md) | Configuration, documentation and examples | P0 | P5 | Partially implemented |

<a id="sap-001"></a>
### SAP-001 - OAuth and supported authentication mechanisms

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-001.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-002"></a>
### SAP-002 - OData API foundation and employee/person data

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-002.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-003"></a>
### SAP-003 - Upsert operations

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-003.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-004"></a>
### SAP-004 - Pagination and filtering

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-004.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-005"></a>
### SAP-005 - Error handling and diagnostics

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-005.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-006"></a>
### SAP-006 - Contract and sandbox testing

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-006.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-007"></a>
### SAP-007 - Additional APIs and complete business-schema validation

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-007.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-008"></a>
### SAP-008 - Employment records

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-008.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-009"></a>
### SAP-009 - Job information

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-009.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-010"></a>
### SAP-010 - Organizational data

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-010.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-011"></a>
### SAP-011 - Manager relationships

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-011.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-012"></a>
### SAP-012 - Employee create/update operations where authorized

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-012.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-013"></a>
### SAP-013 - Delta and incremental synchronization

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-013.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-014"></a>
### SAP-014 - Metadata and schema discovery

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-014.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-015"></a>
### SAP-015 - Data mapping

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-015.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="sap-016"></a>
### SAP-016 - Configuration, documentation and examples

The authoritative [feature specification and completion tracker](sap-successfactors/SAP-016.md) is maintained in its own file. Shared requirements in this master document still apply.

## 15. Qualtrics Feature Catalog

This connector is usable without ADP or SAP. Historical QLT-001-008 subjects remain; QLT-003b/c alias progress/download acceptance scenarios inside QLT-003. Preserve existing behavior during P2-P4; expansion is P6, eventing P7.

Official references checked November 12, 2026: [Q1: setup/token permission](https://www.qualtrics.com/support/integrations/api-integration/overview/), [Q2: survey list](https://developer-docs.qualtrics.com/docs/public-api/surveys/list-surveys/), [Q3: export start](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/create-export/), [Q4: export progress](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-progress/), [Q5: file download](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-file/), [Q6: contacts](https://developer-docs.qualtrics.com/docs/public-api/contacts/contacts/), [Q7: mailing lists](https://developer-docs.qualtrics.com/docs/public-api/mailingLists/mailing-lists/), [Q8: event subscriptions](https://developer-docs.qualtrics.com/docs/public-api/eventSubscriptions/event-subscriptions-api/). Survey/export routes were readable; family pages do not supply all operation schemas. Segment page fetch failed. Unverified directory/participant/batch/event routes remain unspecified until independently checked.

Verified sequence: GET /API/v3/surveys; POST /API/v3/surveys/{surveyId}/export-responses; GET its /{progressId}; then GET /{fileId}/file. Documented export formats/constraints apply; status complete and fileId, not percentComplete alone, determine readiness. Stream bytes as returned; host owns archive extraction. Exact account quotas and permissions remain unverified.

| ID | Feature | Priority | Phase | Audit baseline status |
|---|---|---|---|---|
| [QLT-001](qualtrics/QLT-001.md) | Authentication and API token configuration | P0 | P6 | Partially implemented |
| [QLT-002](qualtrics/QLT-002.md) | Survey APIs | P0 | P6 | Partially implemented |
| [QLT-003](qualtrics/QLT-003.md) | Response export APIs | P0 | P6 | Partially implemented |
| [QLT-004](qualtrics/QLT-004.md) | Pagination and transport resilience | P0 | P6 | Partially implemented |
| [QLT-005](qualtrics/QLT-005.md) | Typed responses, errors and diagnostics | P0 | P6 | Partially implemented |
| [QLT-006](qualtrics/QLT-006.md) | Directory and contact operations | P1 | P6 | Not started |
| [QLT-007](qualtrics/QLT-007.md) | Events and webhook capabilities | P2 | P7 | Not started |
| [QLT-008](qualtrics/QLT-008.md) | Contract and sandbox testing | P0 | P6 | Partially implemented |
| [QLT-009](qualtrics/QLT-009.md) | Mailing lists and segments | P1 | P6 | Not started |
| [QLT-010](qualtrics/QLT-010.md) | Participant management | P1 | P6 | Not started |
| [QLT-011](qualtrics/QLT-011.md) | Batch operations | P1 | P6 | Not started |
| [QLT-012](qualtrics/QLT-012.md) | Data mapping | P0 | P6 | Partially implemented |
| [QLT-013](qualtrics/QLT-013.md) | Rate-limit handling | P0 | P6 | Partially implemented |
| [QLT-014](qualtrics/QLT-014.md) | Documentation and examples | P0 | P6 | Partially implemented |
| [QLT-015](qualtrics/QLT-015.md) | Contact-specific typed schema and consent | P1 | P6 | Not started |
| [QLT-016](qualtrics/QLT-016.md) | API client foundation and configuration | P0 | P6 | Partially implemented |

<a id="qlt-001"></a>
### QLT-001 - Authentication and API token configuration

The authoritative [feature specification and completion tracker](qualtrics/QLT-001.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-002"></a>
### QLT-002 - Survey APIs

The authoritative [feature specification and completion tracker](qualtrics/QLT-002.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-003"></a>
### QLT-003 - Response export APIs

The authoritative [feature specification and completion tracker](qualtrics/QLT-003.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-004"></a>
### QLT-004 - Pagination and transport resilience

The authoritative [feature specification and completion tracker](qualtrics/QLT-004.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-005"></a>
### QLT-005 - Typed responses, errors and diagnostics

The authoritative [feature specification and completion tracker](qualtrics/QLT-005.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-006"></a>
### QLT-006 - Directory and contact operations

The authoritative [feature specification and completion tracker](qualtrics/QLT-006.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-007"></a>
### QLT-007 - Events and webhook capabilities

The authoritative [feature specification and completion tracker](qualtrics/QLT-007.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-008"></a>
### QLT-008 - Contract and sandbox testing

The authoritative [feature specification and completion tracker](qualtrics/QLT-008.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-009"></a>
### QLT-009 - Mailing lists and segments

The authoritative [feature specification and completion tracker](qualtrics/QLT-009.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-010"></a>
### QLT-010 - Participant management

The authoritative [feature specification and completion tracker](qualtrics/QLT-010.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-011"></a>
### QLT-011 - Batch operations

The authoritative [feature specification and completion tracker](qualtrics/QLT-011.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-012"></a>
### QLT-012 - Data mapping

The authoritative [feature specification and completion tracker](qualtrics/QLT-012.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-013"></a>
### QLT-013 - Rate-limit handling

The authoritative [feature specification and completion tracker](qualtrics/QLT-013.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-014"></a>
### QLT-014 - Documentation and examples

The authoritative [feature specification and completion tracker](qualtrics/QLT-014.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-015"></a>
### QLT-015 - Contact-specific typed schema and consent

The authoritative [feature specification and completion tracker](qualtrics/QLT-015.md) is maintained in its own file. Shared requirements in this master document still apply.

<a id="qlt-016"></a>
### QLT-016 - API client foundation and configuration

The authoritative [feature specification and completion tracker](qualtrics/QLT-016.md) is maintained in its own file. Shared requirements in this master document still apply.

## 16. Security and Credential Architecture

Credentials originate in host-supplied providers. The SDK must not require a cloud vault, persist secrets, generate production keys or inspect arbitrary host environment variables automatically. Prefer caller-loaded certificates/credentials, explicit configuration binding and redacted wrappers. A string secret cannot be reliably erased from managed memory; do not claim secure-memory guarantees from ToString redaction. Minimize copies/lifetime and isolate cache keys by provider, origin, tenant/client identity and credential generation. Rotation invalidates only that generation's token/certificate handlers, with host-controlled drain/disposal.

Require HTTPS, valid platform certificate verification and redirects disabled for token/API requests. Never disable server certificate validation. Validate configured roots, identifiers and returned links before attaching credentials. Token endpoints and resource hosts are separately configured. Do not follow a server-supplied URL across origins/API paths. Factory defaults should prevent cookie/header sharing across tenants; caller-owned HttpClient security requirements must be explicit. Certificate expiry/not-yet-valid checks and actionable safe diagnostics precede remote probes; revoked/rotated credentials are tested through host policy. PEM/PFX password/private material never enters logs, repository assets or examples.

Sensitive content includes HR/person/contact/response records, assertions, tokens, private keys, URLs/query filters, response messages, external IDs, checkpoints and embedded-data values. Default telemetry excludes them. Allowlist safe protocol identifiers, bound their size and strip/reject controls; do not copy arbitrary response headers. Export schemas select fields deliberately, neutralize spreadsheet formulas and preserve privacy/consent policies. Returned raw data is caller-owned sensitive data; this does not justify automatic logging. No default data retention, storage or unmasking is introduced.

Security review includes TLS/redirect and continuation attacks, XXE/deep or oversized payloads, header/query/identifier injection, CSV formulas, cross-tenant cache bleed, unsafe write replay, denial of service from pagination/polling and secret leakage via third-party instrumentation. Add explicit response/export size policies in P2/P3; source inspection found no comprehensive response-size guard. Scan runtime dependencies and build/test tools separately. Publication/license/vendor terms and account protections remain release prerequisites, not security settings changed by this document.

## 17. Error Handling and Resilience

Classify local invalid options before sending. Treat 401 as failed authentication, 403 as authorization/entitlement denial, 404 according to the documented operation (not generic empty success), 429 as throttling and selected 408/5xx/network failures as transient only for safe reads. Keep provider business-row failures distinct from HTTP success. Unknown response shapes are contract errors; unsupported codes remain preserved as safe opaque code fields when permitted. Public exceptions include operation name, sanitized code, HTTP status, attempts, safe correlation and optional retry guidance. They omit URI, credentials, raw body and employee identifiers. Cancellation stays OperationCanceledException with caller/deadline context, not a fake vendor error.

Current transport retries only GET: network HttpRequestException and configured transient HTTP statuses, bounded exponential delay with jitter, delta/date Retry-After and linked request timeout. A server delay beyond MaxDelay fails without retrying early. This is useful source evidence, not complete circuit/concurrency policy. RequestTimeout covers transport auth invocation/retries/body processing; separately created OAuth clients have their own HTTP timeout. Define the composite operation budget explicitly so a diagnostic or multi-page extraction cannot run indefinitely. Poll/page bounds supplement per-request deadlines.

Never automatically replay POST/DELETE or upsert merely because a connection timed out. A write may have succeeded remotely. Return unknown-outcome guidance and let the caller reconcile using documented provider mechanisms. No universal idempotency key/header is invented. Binary output is not retried after consumption has started; partial destination data is invalid unless a documented resume protocol says otherwise. Dispose requests/responses/parsers, keep caller streams/certificates alive per documented ownership, and do not stack independent retry handlers inadvertently.

## 18. Pagination, Data Mapping, and Exports

Neutral pages/checkpoints adapt, rather than replace, provider protocols: ADP offset/worker envelope; SAP d.results/__next and entity-specific effective dates; Qualtrics result.elements/nextPage and separate export progress. Continue valid server pages even when a page is short or empty with a next link. Reject cycles, unsafe destinations and exceeded limits. ADP's current iterator stops at empty workers with an offset safety bound; it detects consecutive identical page hashes, not every possible cycle. This is a specific gap to cover without claiming a robust universal cursor today.

Bind checkpoints to account/provider/query/schema identity, mark them sensitive/opaque and commit only after complete successful traversal and durable host delivery. SDK primitives never save them. E-SYNC fingerprints depend on JSON representation/property order; they can create harmless extra updates and are not a canonical semantic diff. Duplicate/empty record keys fail; unseen workers are not automatically deleted or terminated because visibility can change. Event notifications trigger re-read; host deduplicates and persists inbox/ack state.

Mapping defines ordered fields, presence/null/masked semantics, invariant numeric/date policy, effective-date interpretation and source-key provenance. Provider models remain separate from neutral projections; no global employee model erases vendor distinctions. SDK maps records, while CLI/Cloud decides cross-system mappings and delivery transactions. Export owns serialization only, with caller stream ownership, explicit schema/field selection and no implicit filesystem, Excel automation, SFTP or cloud upload.

Format requirements: CSV UTF-8/no BOM by default, explicit newline/delimiter policy and escaped quotes/newlines; formula/control-prefix protection is tested. JSON schema/extension handling is explicit and must be tested independently (current export test emphasizes CSV/XML/XLSX). XML uses safe names/escaping and a defined invalid-character policy. XLSX retains inline strings, sheet bounds and text limits; independent reader validation is required beyond checking ZIP XML. Support async streams in new providers, cancellation and backpressure; measure peak memory with increasing record counts and assert no whole-dataset buffering. Proposed P3 baseline workload is 100,000 synthetic rows with small fixed columns; set an evidence-backed budget before release instead of claiming a benchmark result here.

## 19. Observability and Diagnostics

Core supplies safe ActivitySource/Meter and optional logging abstractions without requiring OpenTelemetry/exporters. Existing source name is Koras.Connect, with method/status tags and request counter; no dedicated Observability package exists. Instrument request duration, retries, throttle outcomes, operation failures and cancellation with bounded provider/operation labels; never tag full host/URL/record ID or unbounded tenant names. Trace parentage uses host context; correlate vendor request IDs only after sanitizing/bounding. Document host HTTP-client automatic instrumentation because it may otherwise include URLs that SDK hooks omit.

Baseline diagnostics in P2/P3 expose stages (configuration, certificate/TLS, authentication, permitted minimal read, response schema) with explicit Unknown/Skipped/Failed/Succeeded outcomes. One successful read does not establish every grant. No diagnostic writes, full tenant enumeration or secret printing. P7 optional Observability registers sources/instruments and enriches safely; host chooses exporter, backend, retention and sampling. Tests capture complete logs/traces/metrics using canary sensitive values and verify opt-out behavior and bounded cardinality.

## 20. Testing and Quality Strategy

| Layer | Required evidence | Existing evidence and gaps |
|---|---|---|
| Unit | Deterministic cache, validation, mapping, projection, paging state, export formatting and safe error tests | E-TEST has selected auth/export/sync/validation cases; broaden only for real new contracts |
| Contract | Official versioned synthetic request/response/schema fixtures; positives and malformed/denied cases per operation | Inline synthetic handlers exist; dedicated schema-provenance corpus absent |
| Security | Redirect/continuation/header/query/XXE, token/PII canaries, tenant isolation, no unsafe replay | Selected focused tests exist, not full assessment |
| Consumer | Each independent nupkg, old/new API compatibility, minimal dependency graph and direct/DI construction | E-SAMPLE covers four current packages and selected synthetic types/exports |
| Sandbox | Opt-in permitted product/API/auth/population checks, exact revision and result | No live credentials/entitlements or harness found |
| Release | Build/test/pack, API validation, package assets, runtime SBOM/digests, scanner/license review and negative gates | Existing scripts/workflows implement subset; no signing/API baseline/comprehensive scanning |
| Performance | Measured synthetic paging/export/token concurrency and cancellation/resource behavior | No performance result claimed; define budgets after baseline measurements |

Test semantics matter more than folder names. Later organization may use Unit/Contract/Integration/Security folders/categories while preserving existing coverage. Synthetic tests must run offline on Linux/Windows and never require production secrets. Live tests default off, report a missing prerequisite as skipped/blocked with reason, and never convert missing access into pass. Approved write tests require sandbox-only explicit enablement, disposable records and a documented reconciliation/cleanup approach; no owner authorization for production writes is implied.

Historical verification numbers remain tied to their old revision. New phase completion requires fresh exact-head CI and retained artifacts/TRX, not copied prior counts. Contract tests generated from the same implementation path alone are insufficient: assertions must derive from independently reviewed official contracts. Validate each supported first-release operation and malformed schema, not merely one endpoint from each family. A green general build cannot accept a feature with unmet schema, entitlement, compatibility or security criteria.

## 21. NuGet Packaging and Release Strategy

Current source packs Koras.Connect, Koras.Connect.Adp, Koras.Connect.Sap.SuccessFactors and Koras.Connect.Qualtrics at 0.1.0-preview.2. The manifest/package validator/SBOM generator/sample assume those names and project paths; all must migrate together in later phases. Keep candidate archives immutable per version and cleanly separated by candidate revision. First-release dependency order: Abstractions, Core/Authentication/Export, ADP; optional later packages consume compatible shared versions. Publish ordering must follow dependencies and cannot be an unreviewed alphabetical glob when the graph changes.

Required candidate gates: successful Linux/Windows build/test/pack/consumer, verified public API baseline, exact package graph/TFM/README/XML docs/version, no private source/keys, validated runtime SBOM and archive hashes, reviewed dependency licenses/security findings, known-limitations release notes and required sandbox acceptance. Artifact hashes are integrity checks, not signatures or provenance. No SourceLink/signing/provenance or third-party certification is inferred solely from deterministic properties.

The current release workflow defaults to validation, publicationApproved=false. Existing publication code additionally checks approved manifest, matching vVERSION tag, approval/license record, exact archives and hashes, and a scoped NuGet secret in nuget-release. The environment's actual server-side protections were not verified; workflow text is not proof. Do not create approval records, tags, accounts, secrets or publish anything during P1. LICENSE-REVIEW.md remains pending; public repository visibility grants no distribution license.

Future signing/provenance design references [M3: NuGet signing](https://learn.microsoft.com/en-us/nuget/create-packages/sign-a-package) and [M4: trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), reviewed November 12, 2026. Signing, trusted publishing authentication and build attestation are distinct controls. Replacing long-lived API keys is a later reviewed workflow decision, not current implementation. Any paid certificate/service or actual public push requires separate existing authorization. Partial publication is possible; inspect exact remote package/version state before a retry and never overwrite or falsely report complete publication.

.NET implementation references: [M1: IHttpClientFactory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory), [M2: package/API validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview), checked November 12, 2026. Factory handler/cookie lifetimes and package-validation baselines inform the proposed requirements; they do not certify current code.

## 22. CLI and Cloud Integration Boundaries

| Concern | SDK owns | CLI / Integration Cloud owns |
|---|---|---|
| Retrieval | Authorized API calls, models, query/page adapters | Selecting tenants/products/populations and extraction schedule |
| Credentials | Provider interfaces, secure HTTP/token handling | Secret provisioning/storage/rotation policy and access administration |
| Sync | Stateless diff/checkpoint/event helpers | Durable state, deduplication/inbox, transaction/outbox, job retries |
| Mapping | Reusable field projections with explicit semantics | ADP-to-SAP/Qualtrics workflow rules and cross-system routing |
| Export | Stream serializers/providers | Files, destination authorization, uploads, retention and encryption at rest |
| Diagnostics | Safe per-operation hooks/read-only probes | Trace backend, alerting, log retention, operational support |
| Events | Parsed/authentication helpers for verified contracts | HTTP receiver, TLS, independent authentication, replay store and scheduling |
| Product services | None | Tenant admin, billing, dashboards, onboarding, provisioning and production hosting |

SDK packages must work from a plain console consumer without cloud accounts or infrastructure. Hosts may compose multiple connectors, but the SDK never packages an ADP-to-SAP workflow, persistent export poller or managed integration job. Bounded WaitForExportAsync is a caller-driven operation, not a hosted background service. Do not create or modify CLI/Cloud repositories in P1.

## 23. Existing Implementation Gap Analysis

Counts below describe whole master requirements, not individual method completeness. The four source-covered ADP slices are Implemented but unverified in this audit; their historical synthetic execution remains evidence, not new P1 test runs. All other entries explicitly identify missing contract/migration/validation work.

| Domain | Principal existing implementation | Material gaps |
|---|---|---|
| Shared architecture | Four net10.0 projects, two auth interfaces | Eight target boundaries, compatibility migration, connector/capability/credential/page/export/mapping/diagnostic contracts |
| Authentication/transport | Cached OAuth, certificate loaders, header/bearer adapters, bounded GET retries and total transport deadline | Rotating credential provider, consistent validation, factory lifecycle across providers, structured error hierarchy, bounded response sizes |
| ADP workers | Typed/raw profile/list/search/metadata and extension fields | Full schema/mapping, common resume/checkpoints, diagnostic API, official current grants/metadata and live validation |
| ADP expansion | Selected lifecycle/payroll/time-off/benefit JSON dispatch and event helpers | Typed business validation, precise entitlements/product versions, broad attendance/benefits contracts, subscription provisioning docs |
| Export | Four static stream serializers | Provider API, async schema projection, robust independent roundtrip/boundary/performance verification |
| SAP | OData/SAML, generic/person reads, three partial DTOs, opt-in upsert | Dedicated entity contracts/models, metadata descriptors, managers/orgs, delta/typed writes/mapping, DI/sandbox/examples |
| Qualtrics | Token header, survey and response export | Directories/contacts/consent, lists/segments, participant/batch/event contracts, per-family quotas, DI/sandbox/examples |
| Release | CI/package consumer, candidate workflow, hashes/runtime SBOM/focused source checks | API baselines, comprehensive scanning, license/acceptance evidence, release notes, signatures/attestations/site |

Do not resurrect old prototype projects simply to match target folder names. Their historical weak HTTPS/redaction defaults and case collisions were already reconciled. Current source is the implementation baseline; historical plans cannot reclassify missing APIs as implemented. State counts and complete feature lists appear in section 30.

## 24. Dependency-Driven Development Roadmap

The feature dependency field lists prerequisite contracts/work and related verification dependencies. Treat broad catalog/test entries as evidence consumers rather than forcing a test feature to precede the feature it verifies. The executable work order below disambiguates baseline capture, implementation and acceptance so there is no circular work gate.

1. P2 entry: separately authorize implementation; capture current public surface/package baseline (SDK-017 first slice), settle complete neutral signatures and resource ownership; no file moves before baseline capture.
2. P2 contracts: SDK-002/005/006/007/009/014 and first SDK-013 interface; define credential/request-auth contracts in Abstractions. Then split Abstractions/Core/Authentication and establish compatibility facade/graph for SDK-001. Freeze safe options/error semantics before DI.
3. P2 behavior: integrate SDK-004 factory handlers, SDK-003 builders, SDK-008/010 cancellation/resilience and SDK-011 safe correlation. Preserve ADP-001/002 and existing SAP/Qualtrics behavior through isolated consumers. Add the migration/API checks, not merely a folder reorganization.
4. P3: implement Export providers and worker projection/schema; finish ADP diagnostics/typed worker contracts/page/checkpoint/sync. Research current official capabilities and metadata in parallel with offline implementation only when not prerequisite for an API decision. Do not add speculative writes to work around missing grants.
5. P4: complete exact supported first-release P0 contract/sandbox tests, package graph/compatibility/security/SBOM/release notes/candidate checks. A blocked live case remains unaccepted while unrelated offline work proceeds. Release readiness does not automatically publish.
6. P5/P6: preserve and strengthen SAP/Qualtrics in their independent package order. Establish metadata/auth/error/paging foundations first, then typed reads, mapping, explicitly authorized writes/batches and examples, then acceptance tests. Directory/participant/event expansion waits for actual official product contracts.
7. P7: optional observability package, signing/provenance/site build and selected higher-priority provider extensions after their contracts/security gates. Signing/publication/deployment credentials are separate external prerequisites.

Feature priority does not change because some preview method exists. ADP-009 remains P1 despite existing XLSX; ADP-018 remains P2 while its P0 incremental-sync prerequisites use existing polling primitives in P3. SAP/Qualtrics P0 labels mean mandatory for those connector releases, not that they must ship in the first ADP release. No phase begins automatically from this P1 commit.

## 25. Phase P1-P7 Deliverables

| Phase | Entry dependencies | Deliverables | Exit evidence / exclusions |
|---|---|---|---|
| P1 Documentation and architecture | Repository identity/branch/history/source audit | This single canonical specification, 74 detailed features, package graph, gaps, roadmap and validation | Feature/field/priority/path/anchor validation; only this file committed on develop; stop, no runtime implementation |
| P2 Foundation | Separate authorization, P1 available | API baseline, Abstractions/Core/Authentication split, neutral contracts, DI/factory/validation/error/resilience/cancel/diagnostic hooks, compatibility consumers | Exact-head Linux/Windows synthetic/security/consumer checks; migration documented; no silent preview break |
| P3 Export and ADP | P2 contracts/transport/auth stable; official operation contracts for claimed APIs | Export package/providers; typed worker/API/schema/mapping; connection probes; checkpoint/event/reconciliation primitives; ADP fixtures/examples | All declared P0 offline acceptance criteria pass; required current metadata/live blockers explicitly recorded; P1/P2-priority expansion not silently promoted |
| P4 Validation and release readiness | P3 supported first-release scope fixed | API compatibility, security/SBOM/license review, packaging/notes/candidate workflow and supported ADP sandbox evidence | Release checklist met for selected scope; no publish unless separately authorized; main promotion requires its own PR/CI/protection compliance |
| P5 SAP SuccessFactors | Shared packages compatible, current tenant/entity/auth contracts | Independent SAP package, OData/models/entity reads/upsert/delta/mapping/errors/docs/tests | SAP P0 acceptance plus actual RBP/schema/sandbox evidence; no ADP-to-SAP orchestration |
| P6 Qualtrics | Shared packages compatible, current family/account contracts | Independent client; survey/export hardening, entitled directory/contact/list/participant/batch APIs and typed consent data | Qualtrics P0 plus selected P1 acceptance, safe quota/paging/write tests and permitted sandbox evidence; no unverified product assumptions |
| P7 Enterprise observability and expansion | Stable shared APIs and per-extension evidence | Optional Observability/OpenTelemetry, advanced diagnostics, signing/provenance, site build and entitled expanded provider capabilities | Privacy/cardinality/signature/compatibility evidence; no automatic deployment, public publication, paid service or cloud resource |

## 26. Feature Acceptance and Definition of Done

A feature entry is executable scope, not evidence it is finished. Definition of Done requires: implemented agreed public contract and ownership; complete numbered acceptance cases; meaningful deterministic positive/negative unit and independent protocol-contract tests; applicable security/PII/cancellation/resource/performance checks; API/package consumers and migration notes; current official schema/permissions for external operations; required permitted sandbox outcomes; exact revision-linked CI artifacts; reviewed docs/limitations; no unresolved required findings. Product-owner acceptance is recorded explicitly with date/revision/scope and does not arise automatically from a merged commit.

Maintain three orthogonal evidence fields in later updates: implementation state, synthetic execution revision/results, and live entitlement/verification state. Historical tested methods can sit inside a partially implemented master feature. If access is missing, mark that live gate Blocked with the exact prerequisite; do not mark the whole connector vendor-unsupported, fabricate a fixture from a guess or turn an unexecuted test into pass. If a planned write lacks a verified official contract, it is not releasable even when a generic JSON transport could send it. A schema/entitlement gap must not prevent independent neutral implementation work that does not depend on it.

First-release acceptance requires every first-release P0 feature and selected supported operation contract. P1/P2-priority preview APIs either satisfy their declared bounded scope with explicit limitations or remain clearly experimental; their existence does not waive P0 worker/auth/security gaps. Stable releases cannot silently advertise incomplete generic dispatch as complete lifecycle/payroll coverage. Public publication adds separate owner/license/environment/tag/checksum/security authorizations; P1 completion does not satisfy those gates.

## 27. Risks, Assumptions, and External Blockers

| Risk/blocker | Evidence and consequence | Closure / owner |
|---|---|---|
| Official ADP pages are JS-only or inaccessible | Current full termination/getting-started/catalog verification incomplete; public PDFs are older | Integration engineer obtains official current schemas/metadata through authorized access; owner supplies entitlement inventory |
| Unknown ADP canonical CAR/actor/population/version | Local allowlist is not a permission grant; WFN is not all ADP products | Consumer/application administrator verifies each supported operation |
| SAP current entity schemas/RBP missing | Generic client/upsert does not imply complete Employee Central schema or all writes | SAP administrator provides metadata, client/assertion and RBP-approved sandbox population |
| Qualtrics product/family/quota/consent unknown | Contact/participant/segment/batch/event contracts cannot be guessed | Qualtrics administrator/product owner identifies licensed families and current operation specs |
| No live credentials supplied | TLS/auth/permissions cannot be certified through synthetic tests | Authorized sandbox configuration supplied outside repository; no production access solicited or attempted |
| Package split breaks CLR identity | Case-insensitive NuGet names differ from namespace/assembly identity | P2 compatibility baseline/forwarder/wrapper decisions and old consumer tests |
| Unsafe host instrumentation | Caller HTTP telemetry may include URLs even when SDK hooks do not | Document safe factory defaults and host responsibility; security capture tests |
| Token/handler rotation | Current singleton/factory composition may retain stale handler/certificate beyond desired rotation | P2 lifecycle design and concurrency/disposal tests |
| Unbounded payloads / partial outputs | Current parser/stream operations do not establish a complete size policy | Add explicit limits/cancellation/backpressure and invalid-output handling |
| License/distribution and package name ownership unresolved | LICENSE-REVIEW is pending; actual repo public contradicts historical private sentence | Owner/legal review before publication; do not choose a license implicitly |
| Branch/environment protections unknown | Workflows alone do not enforce server-side rules | Read/verify actual settings before any future protection claim or promotion; no setting changed here |
| Conflicting old docs/IDs | Historical master overloaded SDK IDs and some acceptance text is stale | This document is canonical; crosswalk in section 3; preserve originals as historical evidence |

Assumptions: net10.0 is the initial supported runtime; owner chooses licensed products/sandbox access; SDK has no hosting requirements; no public NuGet release is established by repository evidence; first release is the five-package priority subset. Each assumption is revisited only when real evidence or owner direction changes it. No unsupported percentage of completion is used.

## 28. Architecture Decisions and Open Questions

| ID | Decision / default | Status and closure |
|---|---|---|
| ADR-M01 | Eight exact target package IDs, first five prioritized; current four are migration baseline | Specified; implement only in authorized P2+ |
| ADR-M02 | Neutral auth/credential/page/export/diagnostic contracts in Abstractions; implementations depend inward | Specified; freeze complete signatures with API tests in P2 |
| ADR-M03 | Authentication does not depend on Core; Core consumes IRequestAuthenticator from Abstractions | Specified; prevents auth/transport cycle |
| ADR-M04 | Providers do not depend on one another or Export/Observability; host composes projection/export | Specified; isolated-consumer/graph test required |
| ADR-M05 | Keep Core safe hooks; optional Observability owns OpenTelemetry integration without host exporters | Specified; exact optional dependency versions decided in P7 |
| ADR-M06 | Preserve preview consumers through forwards/wrappers where possible; explicit breaking migration otherwise | Proposed mechanism; final per-type forwarding inventory is P2 work |
| ADR-M07 | GET-only default retries, no hidden write retry or invented idempotency key | Specified; existing behavior retained and broadened safely |
| ADR-M08 | Host owns persistence, scheduling, job hosting, webhook endpoints and cross-system orchestration | Specified; no SDK hosted-service dependency |
| ADR-M09 | Vendor contract/entitlement unknowns are tracked separately from missing implementation | Specified; live access is not inferred from mocks |
| ADR-M10 | Keep runtime initially .NET-only where practical; justify external mapping/Excel/resilience dependencies | Default; exact packages reviewed when implementation needs them |

Open questions have safe defaults and named closure actions: owner/legal chooses distribution license before publication (default no publication); release engineer verifies NuGet ownership/protected environment before any push; integration engineer selects exact ADP product/version/CAR and SAP/Qualtrics entity/family schemas before expanding operations; P2 engineer chooses forwarding versus documented preview break per public type. Select observability library versions/site generator/signing key custody in P7 using current official docs. None prevents committing this P1 specification or authorizes guessing an external endpoint.

## 29. Development Readiness Checklist

- [x] Correct local repository and origin confirmed; develop active, clean starting worktree, baseline SHA recorded.
- [x] Default branch and open/merged PR state inspected; relevant legacy tips confirmed as ancestors without restoring source.
- [x] Solution, all projects/C# sources, tests, samples, package configuration, workflows, architecture/plans/history audited.
- [x] SDK-001-020 and ADP-001-022 names/priorities preserved; historical ID crosswalk explicit.
- [x] Sixteen SAP and sixteen Qualtrics features include twenty mandatory specification fields.
- [x] Eight-package graph, migration/casing/backward compatibility and independent provider use specified.
- [x] Vendor source retrieval limits, current schema/entitlement/live blockers explicitly identified.
- [x] Roadmap separates feature priority from phase and contains executable prerequisite order.
- [x] Acceptance/Done/test/security/release requirements distinguish source, synthetic runs and live verification.
- [x] P1 creates only the canonical Markdown artifact; existing source/config/docs are preserved.
- [ ] P2 implementation separately authorized - required before any runtime change.
- [ ] Required current vendor metadata/grants and live sandbox acceptance complete - release blocker, not P1 drafting blocker.
- [ ] License/publication/protected environment and signing identities resolved - not authorized by P1.

P1 document validation must check thirty ordered sections, unique detailed IDs/anchors, exact required priorities, every numbered field, complete index, internal links and repository evidence paths. Inspect intended commit paths and diff whitespace, then commit only this document on develop. Do not merge, push, publish or start P2 as part of finishing this assignment.

P1 validation record (November 12, 2026): thirty ordered sections; 74 unique detailed feature IDs/anchors and complete index; 1,480 numbered required fields; all 42 supplied SDK/ADP names and priorities compared directly with the attached request; internal feature links and 23 explicit existing file references checked; proposed eight-package graph checked acyclic; code fences, heading spacing and whitespace checked. Existing application tests were not rerun for this documentation-only change. Only this new document is the intended commit artifact; final Git scope verification is performed before committing.

## 30. Master Feature Index

The implementation counts and status columns below are the original audit snapshot. Current status lives only in the linked feature files.

| Whole-feature implementation state | Count | Feature IDs |
|---|---|---|
| Partially implemented | 57 | SDK-001, SDK-003, SDK-004, SDK-005, SDK-006, SDK-007, SDK-008, SDK-009, SDK-010, SDK-011, SDK-012, SDK-013, SDK-014, SDK-016, SDK-018, SDK-019, ADP-003, ADP-005, ADP-006, ADP-007, ADP-008, ADP-009, ADP-010, ADP-013, ADP-014, ADP-015, ADP-016, ADP-017, ADP-018, ADP-019, ADP-020, ADP-021, ADP-022, SAP-001, SAP-002, SAP-003, SAP-004, SAP-005, SAP-006, SAP-008, SAP-009, SAP-011, SAP-012, SAP-013, SAP-014, SAP-015, SAP-016, QLT-001, QLT-002, QLT-003, QLT-004, QLT-005, QLT-008, QLT-012, QLT-013, QLT-014, QLT-016 |
| Not started | 12 | SDK-002, SDK-015, SDK-017, SDK-020, ADP-012, SAP-010, QLT-006, QLT-007, QLT-009, QLT-010, QLT-011, QLT-015 |
| Implemented but unverified | 4 | ADP-001, ADP-002, ADP-004, ADP-011 |
| Planned | 1 | SAP-007 |
| Tested/Accepted as complete master requirements in P1 | 0 | Historical synthetic results remain separately recorded |

Live external verification is unverified/blocked for all provider features requiring access; these blockers overlap implementation states and must not be added as extra feature counts. Four implemented source slices (ADP-001/002/004/011) are unverified in this audit. The remaining 57 partial features, 12 not-started features and one planned feature have explicit gaps above.

| Feature ID | Name | Priority | Phase | Audit baseline state |
|---|---|---|---|---|
| [SDK-001](sdk/SDK-001.md) | .NET 10 solution and package structure | P0 | P2 | Partially implemented |
| [SDK-002](sdk/SDK-002.md) | Connector interface contracts | P0 | P2 | Not started |
| [SDK-003](sdk/SDK-003.md) | Dependency injection extensions | P0 | P2 | Partially implemented |
| [SDK-004](sdk/SDK-004.md) | HttpClientFactory integration | P0 | P2 | Partially implemented |
| [SDK-005](sdk/SDK-005.md) | Authentication abstraction | P0 | P2 | Partially implemented |
| [SDK-006](sdk/SDK-006.md) | Secure credential providers | P0 | P2 | Partially implemented |
| [SDK-007](sdk/SDK-007.md) | Pagination abstractions | P0 | P2 | Partially implemented |
| [SDK-008](sdk/SDK-008.md) | Retry and resilience policies | P0 | P2 | Partially implemented |
| [SDK-009](sdk/SDK-009.md) | Structured exception hierarchy | P0 | P2 | Partially implemented |
| [SDK-010](sdk/SDK-010.md) | Cancellation and timeout support | P0 | P2 | Partially implemented |
| [SDK-011](sdk/SDK-011.md) | Logging and correlation IDs | P0 | P2 | Partially implemented |
| [SDK-012](sdk/SDK-012.md) | OpenTelemetry instrumentation | P1 | P7 | Partially implemented |
| [SDK-013](sdk/SDK-013.md) | Export provider interfaces | P0 | P3 | Partially implemented |
| [SDK-014](sdk/SDK-014.md) | Configuration validation | P0 | P2 | Partially implemented |
| [SDK-015](sdk/SDK-015.md) | Package signing and provenance | P1 | P7 | Not started |
| [SDK-016](sdk/SDK-016.md) | NuGet publishing workflow | P0 | P4 | Partially implemented |
| [SDK-017](sdk/SDK-017.md) | API compatibility checks | P0 | P2-P4 | Not started |
| [SDK-018](sdk/SDK-018.md) | Security scanning and SBOM | P0 | P4 | Partially implemented |
| [SDK-019](sdk/SDK-019.md) | Versioning and release notes | P0 | P4 | Partially implemented |
| [SDK-020](sdk/SDK-020.md) | SDK documentation website | P1 | P7 | Not started |
| [ADP-001](adp/ADP-001.md) | Certificate and mTLS authentication | P0 | P2 | Implemented but unverified |
| [ADP-002](adp/ADP-002.md) | OAuth token acquisition and refresh | P0 | P2 | Implemented but unverified |
| [ADP-003](adp/ADP-003.md) | Secure client credentials configuration | P0 | P2 | Partially implemented |
| [ADP-004](adp/ADP-004.md) | Worker profile retrieval | P0 | P3 | Implemented but unverified |
| [ADP-005](adp/ADP-005.md) | Worker pagination and filtering | P0 | P3 | Partially implemented |
| [ADP-006](adp/ADP-006.md) | Worker changes and incremental sync | P0 | P3 | Partially implemented |
| [ADP-007](adp/ADP-007.md) | Worker schema mapping | P0 | P3 | Partially implemented |
| [ADP-008](adp/ADP-008.md) | CSV/JSON/XML export | P0 | P3 | Partially implemented |
| [ADP-009](adp/ADP-009.md) | Excel export | P1 | P3 | Partially implemented |
| [ADP-010](adp/ADP-010.md) | Structured errors and rate-limit handling | P0 | P3 | Partially implemented |
| [ADP-011](adp/ADP-011.md) | Retry, timeout, and cancellation | P0 | P3 | Implemented but unverified |
| [ADP-012](adp/ADP-012.md) | Connection diagnostics | P0 | P3 | Not started |
| [ADP-013](adp/ADP-013.md) | ADP organization and manager data | P1 | P3 extension | Partially implemented |
| [ADP-014](adp/ADP-014.md) | Employment status and lifecycle events | P1 | P3 extension | Partially implemented |
| [ADP-015](adp/ADP-015.md) | Payroll operations, where entitled | P1 | P3 extension | Partially implemented |
| [ADP-016](adp/ADP-016.md) | Time and attendance APIs | P2 | P7 | Partially implemented |
| [ADP-017](adp/ADP-017.md) | Benefits APIs | P2 | P7 | Partially implemented |
| [ADP-018](adp/ADP-018.md) | Webhook/event subscriptions | P2 | P3 primitives | Partially implemented |
| [ADP-019](adp/ADP-019.md) | Typed API response models | P0 | P3 | Partially implemented |
| [ADP-020](adp/ADP-020.md) | Sandbox and contract test suite | P0 | P3-P4 | Partially implemented |
| [ADP-021](adp/ADP-021.md) | API capability/entitlement matrix | P0 | P1 baseline | Partially implemented |
| [ADP-022](adp/ADP-022.md) | Documentation and runnable examples | P0 | P3-P4 | Partially implemented |
| [SAP-001](sap-successfactors/SAP-001.md) | OAuth and supported authentication mechanisms | P0 | P5 | Partially implemented |
| [SAP-002](sap-successfactors/SAP-002.md) | OData API foundation and employee/person data | P0 | P5 | Partially implemented |
| [SAP-003](sap-successfactors/SAP-003.md) | Upsert operations | P0 | P5 | Partially implemented |
| [SAP-004](sap-successfactors/SAP-004.md) | Pagination and filtering | P0 | P5 | Partially implemented |
| [SAP-005](sap-successfactors/SAP-005.md) | Error handling and diagnostics | P0 | P5 | Partially implemented |
| [SAP-006](sap-successfactors/SAP-006.md) | Contract and sandbox testing | P0 | P5 | Partially implemented |
| [SAP-007](sap-successfactors/SAP-007.md) | Additional APIs and complete business-schema validation | P1 | P5 | Planned |
| [SAP-008](sap-successfactors/SAP-008.md) | Employment records | P0 | P5 | Partially implemented |
| [SAP-009](sap-successfactors/SAP-009.md) | Job information | P0 | P5 | Partially implemented |
| [SAP-010](sap-successfactors/SAP-010.md) | Organizational data | P1 | P5 | Not started |
| [SAP-011](sap-successfactors/SAP-011.md) | Manager relationships | P1 | P5 | Partially implemented |
| [SAP-012](sap-successfactors/SAP-012.md) | Employee create/update operations where authorized | P1 | P5 | Partially implemented |
| [SAP-013](sap-successfactors/SAP-013.md) | Delta and incremental synchronization | P0 | P5 | Partially implemented |
| [SAP-014](sap-successfactors/SAP-014.md) | Metadata and schema discovery | P0 | P5 | Partially implemented |
| [SAP-015](sap-successfactors/SAP-015.md) | Data mapping | P0 | P5 | Partially implemented |
| [SAP-016](sap-successfactors/SAP-016.md) | Configuration, documentation and examples | P0 | P5 | Partially implemented |
| [QLT-001](qualtrics/QLT-001.md) | Authentication and API token configuration | P0 | P6 | Partially implemented |
| [QLT-002](qualtrics/QLT-002.md) | Survey APIs | P0 | P6 | Partially implemented |
| [QLT-003](qualtrics/QLT-003.md) | Response export APIs | P0 | P6 | Partially implemented |
| [QLT-004](qualtrics/QLT-004.md) | Pagination and transport resilience | P0 | P6 | Partially implemented |
| [QLT-005](qualtrics/QLT-005.md) | Typed responses, errors and diagnostics | P0 | P6 | Partially implemented |
| [QLT-006](qualtrics/QLT-006.md) | Directory and contact operations | P1 | P6 | Not started |
| [QLT-007](qualtrics/QLT-007.md) | Events and webhook capabilities | P2 | P7 | Not started |
| [QLT-008](qualtrics/QLT-008.md) | Contract and sandbox testing | P0 | P6 | Partially implemented |
| [QLT-009](qualtrics/QLT-009.md) | Mailing lists and segments | P1 | P6 | Not started |
| [QLT-010](qualtrics/QLT-010.md) | Participant management | P1 | P6 | Not started |
| [QLT-011](qualtrics/QLT-011.md) | Batch operations | P1 | P6 | Not started |
| [QLT-012](qualtrics/QLT-012.md) | Data mapping | P0 | P6 | Partially implemented |
| [QLT-013](qualtrics/QLT-013.md) | Rate-limit handling | P0 | P6 | Partially implemented |
| [QLT-014](qualtrics/QLT-014.md) | Documentation and examples | P0 | P6 | Partially implemented |
| [QLT-015](qualtrics/QLT-015.md) | Contact-specific typed schema and consent | P1 | P6 | Not started |
| [QLT-016](qualtrics/QLT-016.md) | API client foundation and configuration | P0 | P6 | Partially implemented |

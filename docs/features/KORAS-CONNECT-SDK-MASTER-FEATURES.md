# Koras Connect SDK - Master Feature Specification

## 1. Document Control and Version History

| Field | Value |
|---|---|
| Document | Canonical master feature specification, version 1.0 |
| Audit date | November 12, 2026, America/New_York |
| Assignment | P1, documentation only; stop before P2 |
| Repository | `C:\repos\koras-connect-sdk` |
| Origin | `https://github.com/kkora/koras-connect-sdk` |
| Baseline | `develop`, `15ce97f8b91c2321076a93e541238823ded0da9a` |
| Default branch | `develop`, confirmed through GitHub repository metadata and origin/HEAD |
| Initial working tree | Clean; no staged or unstaged work |
| Product owner | Repository owner; acceptance is not inferred from authorship or merging |
| Specification authority | Supplied master requirements, then this document; historical plans remain evidence |
| Approval | P1 creation/validation/local commit authorized by the assignment; feature/release acceptance not granted |

Version 1.0 consolidates the source audit, exact SDK/ADP requirements, independent connector catalogs, package migration and P1-P7 gates. The baseline SHA identifies implementation evidence, not this document's eventual commit. Git records the documentation commit separately so this file need not contain its own hash.

Status vocabulary applies to the **whole feature's defined scope**: **Not started** means no executable feature implementation found; **Planned** means a designed future scope with no implementation; **Partially implemented** means useful code exists but stated requirements remain; **Implemented but unverified** means source covers the bounded scope but execution evidence has not been independently established here; **Tested** requires recorded execution tied to a revision and test scope; **Accepted** requires explicit acceptance against all criteria; **Blocked** means a specific prerequisite prevents the next required action. Implementation status and external verification are separate: a partial feature can have blocked live validation. No feature is classified Accepted by P1.

Priority P0/P1/P2 is urgency; phase P1-P7 is a development grouping. They are separate axes. Proposed public symbols below are design commitments for later implementation, not claims that those symbols already exist.

## 2. Executive Summary

The current .NET 10 preview contains four packages and useful authentication, transport, ADP worker/operation, export, SAP OData and Qualtrics survey/export implementations. It does not yet implement the eight target package boundaries or a consistent connector, capability, credential, pagination, export-provider and structured-error contract family. Existing synthetic tests cover selected protocol behavior; neither tenant entitlement nor complete business-schema coverage follows from them.

The first release prioritizes Abstractions, Core, Authentication, Export and ADP. Preserve existing SAP/Qualtrics preview behavior during shared refactoring, then expand them in P5/P6. Observability integration and enterprise expansion follow in P7; safe baseline diagnostics remain P0 foundation work. Public publication, deployment, paid services and main promotion are separate actions, excluded from P1.

This catalog contains 20 SDK, 22 ADP, 16 SAP and 16 Qualtrics features, 74 total. Each detailed entry has the same twenty mandatory fields. Sections 16-26 supply binding common requirements; entries add capability-specific requirements rather than silently waiving common gates.

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

| ID | Feature | Priority | Phase | Current status |
|---|---|---|---|---|
| [SDK-001](#sdk-001) | .NET 10 solution and package structure | P0 | P2 | Partially implemented |
| [SDK-002](#sdk-002) | Connector interface contracts | P0 | P2 | Not started |
| [SDK-003](#sdk-003) | Dependency injection extensions | P0 | P2 | Partially implemented |
| [SDK-004](#sdk-004) | HttpClientFactory integration | P0 | P2 | Partially implemented |
| [SDK-005](#sdk-005) | Authentication abstraction | P0 | P2 | Partially implemented |
| [SDK-006](#sdk-006) | Secure credential providers | P0 | P2 | Partially implemented |
| [SDK-007](#sdk-007) | Pagination abstractions | P0 | P2 | Partially implemented |
| [SDK-008](#sdk-008) | Retry and resilience policies | P0 | P2 | Partially implemented |
| [SDK-009](#sdk-009) | Structured exception hierarchy | P0 | P2 | Partially implemented |
| [SDK-010](#sdk-010) | Cancellation and timeout support | P0 | P2 | Partially implemented |
| [SDK-011](#sdk-011) | Logging and correlation IDs | P0 | P2 | Partially implemented |
| [SDK-012](#sdk-012) | OpenTelemetry instrumentation | P1 | P7 | Partially implemented |
| [SDK-013](#sdk-013) | Export provider interfaces | P0 | P3 | Partially implemented |
| [SDK-014](#sdk-014) | Configuration validation | P0 | P2 | Partially implemented |
| [SDK-015](#sdk-015) | Package signing and provenance | P1 | P7 | Not started |
| [SDK-016](#sdk-016) | NuGet publishing workflow | P0 | P4 | Partially implemented |
| [SDK-017](#sdk-017) | API compatibility checks | P0 | P2-P4 | Not started |
| [SDK-018](#sdk-018) | Security scanning and SBOM | P0 | P4 | Partially implemented |
| [SDK-019](#sdk-019) | Versioning and release notes | P0 | P4 | Partially implemented |
| [SDK-020](#sdk-020) | SDK documentation website | P1 | P7 | Not started |

## 10. Shared SDK Detailed Feature Specifications

<a id="sdk-001"></a>
### SDK-001 - .NET 10 solution and package structure

1. **Feature ID:** SDK-001.
2. **Feature name:** .NET 10 solution and package structure.
3. **Description:** Deliver installable packages with predictable ownership and runtime support.
4. **Business value:** A clean Linux/Windows build and pack produces the first-release package set reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + Koras.Connect.Abstractions.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-BUILD. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Create the eight target boundaries in dependency order; preserve preview consumers through the migration in section 6; keep sample separate from runtime libraries.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing net10.0 projects; proposed eight package surfaces from sections 6-8.
13. **Dependencies:** SDK-017; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise graph cycle/forbidden-reference checks and metadata inspection, plus old/new package consumer compilation. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Graph cycle/forbidden-reference checks and metadata inspection, plus old/new package consumer compilation.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A clean Linux/Windows build and pack produces the first-release package set; each package installs independently and old preview breakages are documented.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor blocker; compatibility inventory required before moving types.

<a id="sdk-002"></a>
### SDK-002 - Connector interface contracts

1. **Feature ID:** SDK-002.
2. **Feature name:** Connector interface contracts.
3. **Description:** Let hosts discover connectors and capabilities without coupling to implementations.
4. **Business value:** A host can discover and substitute each connector using only Abstractions reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Not started; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT has only token/request-auth interfaces; no common connector found. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Define identity, product/version, supported operation and entitlement states; expose safe connection diagnostics; keep provider models out of neutral contracts.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed IConnector, ConnectorDescriptor and ConnectionDiagnostic in Abstractions.
13. **Dependencies:** SDK-001; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise unknown/denied capability states, immutable descriptors, fake connector substitution and API snapshot tests. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Unknown/denied capability states, immutable descriptors, fake connector substitution and API snapshot tests.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A host can discover and substitute each connector using only Abstractions; unknown entitlement is never reported as verified.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No external API needed for static contracts; runtime grants remain vendor-specific.

<a id="sdk-003"></a>
### SDK-003 - Dependency injection extensions

1. **Feature ID:** SDK-003.
2. **Feature name:** Dependency injection extensions.
3. **Description:** Reduce host configuration errors while retaining direct construction.
4. **Business value:** All provider clients can resolve independently reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-DI: AddKorasAdp only; E-TEST: CertificateAndDIContractsResolveWithoutExternalInfrastructure. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add shared/provider registration builders; validate eagerly; isolate named instances, token caches, certificate and handler lifetime; retain duplicate protection until named instances are implemented.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing AddKorasAdp; proposed AddKorasConnect, AddKorasSuccessFactors, AddKorasQualtrics.
13. **Dependencies:** SDK-002, SDK-004, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise provider resolution/disposal, duplicate registration, named instance isolation and options failure before network. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Provider resolution/disposal, duplicate registration, named instance isolation and options failure before network.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All provider clients can resolve independently; separate tenant registrations cannot exchange credentials; disposal ownership is documented and tested.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor blocker; lifecycle and named-registration API must be frozen in P2.

<a id="sdk-004"></a>
### SDK-004 - HttpClientFactory integration

1. **Feature ID:** SDK-004.
2. **Feature name:** HttpClientFactory integration.
3. **Description:** Provide safe reusable HTTP lifecycles across connectors.
4. **Business value:** Every factory registration preserves security defaults, supports safe custom handlers and documents refresh behavior without retaining stale certificates indefinitely reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-DI uses two named ADP clients; SAP/Qualtrics require caller clients. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Use separate token/API named clients; disable redirects and unsafe URL loggers; disable credential-bearing cookie reuse; document handler/DNS/certificate refresh and caller overrides.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing IHttpClientFactory use in AddKorasAdp; proposed provider builder HTTP customization.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise handler replacement, auth isolation, logger removal, redirect rejection and disposal/DNS lifecycle simulations. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Handler replacement, auth isolation, logger removal, redirect rejection and disposal/DNS lifecycle simulations.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Every factory registration preserves security defaults, supports safe custom handlers and documents refresh behavior without retaining stale certificates indefinitely.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Microsoft factory guidance [M1]; no live vendor required for lifecycle checks.

<a id="sdk-005"></a>
### SDK-005 - Authentication abstraction

1. **Feature ID:** SDK-005.
2. **Feature name:** Authentication abstraction.
3. **Description:** Allow vendor and host authentication adapters without changing transport.
4. **Business value:** Core accepts authentication without referencing Authentication implementation reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions + Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT: IAccessTokenSource; RequestAuthentication.cs: IRequestAuthenticator. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Move neutral interfaces to Abstractions with compatibility handling; support bearer/header adapters and cancellation; keep vendor grant semantics in provider adapters.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing GetTokenAsync and AuthenticateAsync; preserve signatures or document preview migration.
13. **Dependencies:** SDK-001, SDK-017; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise fake token source, invalid header token, auth cancellation, per-request token rotation and transport substitution. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Fake token source, invalid header token, auth cancellation, per-request token rotation and transport substitution.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Core accepts authentication without referencing Authentication implementation; cached and rotating token sources remain interoperable.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor blocker for abstraction; grant verification belongs to provider entries.

<a id="sdk-006"></a>
### SDK-006 - Secure credential providers

1. **Feature ID:** SDK-006.
2. **Feature name:** Secure credential providers.
3. **Description:** Enable secret rotation without embedding host-specific vault dependencies.
4. **Business value:** No secrets appear in errors/logs reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-AUTH redacted options and certificate loaders; E-TRANSPORT token injection; no credential-provider API. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add asynchronous credential/certificate sources; redact representations; define reload/cache key semantics; hosts configure secret stores and storage permissions.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed ICredentialProvider<TCredential> with redacted credential wrappers; retain IAccessTokenSource adapter.
13. **Dependencies:** SDK-005, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise concurrent rotation, canceled retrieval, source failure, stale cache invalidation and redacted diagnostics. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Concurrent rotation, canceled retrieval, source failure, stale cache invalidation and redacted diagnostics.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** No secrets appear in errors/logs; two credentials remain isolated; rotation is observable only through safe metadata and affects subsequent authentication.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Production secret store/certificate policy is host-owned; no paid store provisioned.

<a id="sdk-007"></a>
### SDK-007 - Pagination abstractions

1. **Feature ID:** SDK-007.
2. **Feature name:** Pagination abstractions.
3. **Description:** Allow resumable traversal without data loss or hidden persistence.
4. **Business value:** Resume produces the documented sequence with no client-induced omission reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions + Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-WORKERS iterators, E-SAP __next, E-QLT nextPage, E-SYNC; no common page/checkpoint. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add neutral pages/opaque continuation and query-bound checkpoint primitives; adapters retain vendor paging; bound cycles/pages and forbid cross-origin links.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed Page<T>, ContinuationToken and optional page-source contract; existing async enumerators remain supported.
13. **Dependencies:** SDK-002, SDK-010; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise empty/intermediate-empty pages, cycles, mismatched query tokens, hostile next links and cancellation after partial traversal. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Empty/intermediate-empty pages, cycles, mismatched query tokens, hostile next links and cancellation after partial traversal.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Resume produces the documented sequence with no client-induced omission; invalid tokens fail safely; host commits checkpoints only after delivery.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No universal vendor delta contract; provider cursor lifetime must be verified separately.

<a id="sdk-008"></a>
### SDK-008 - Retry and resilience policies

1. **Feature ID:** SDK-008.
2. **Feature name:** Retry and resilience policies.
3. **Description:** Recover safe transient reads while avoiding duplicate writes.
4. **Business value:** 429/selected 5xx retry only within budget reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT bounded GET backoff/jitter/Retry-After; E-TEST TransientReadsRetry and WritesAreNeverRetried. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Retain GET-only retry defaults, total deadlines and server minimum delay; expose deterministic clock/delay testing, optional concurrency/circuit policies without stacked retries.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing TransportOptions and IntegrationTransport; proposed immutable operation-safety policy.
13. **Dependencies:** SDK-010, SDK-009; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise 429/date and delta Retry-After, network failures, budget exhaustion, jitter bounds, circuit cancellation and no POST/DELETE replay. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** 429/date and delta Retry-After, network failures, budget exhaustion, jitter bounds, circuit cancellation and no POST/DELETE replay.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** 429/selected 5xx retry only within budget; excessive Retry-After fails without early retry; unsafe writes are sent at most once automatically.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor global rate number assumed; advanced policy library is optional and requires dependency review.

<a id="sdk-009"></a>
### SDK-009 - Structured exception hierarchy

1. **Feature ID:** SDK-009.
2. **Feature name:** Structured exception hierarchy.
3. **Description:** Let hosts classify failures safely and consistently.
4. **Business value:** Callers can distinguish auth, throttle and schema failures without parsing messages reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT IntegrationRequestException with status/attempts; auth also throws HttpRequestException. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Define configuration/authentication/authorization/throttle/transport/contract exceptions, safe provider codes and correlation; preserve inner exceptions only when sanitized.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed IntegrationException family and safe ErrorDetails; retain HTTP status/attempts compatibility.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise 401/403/404/429/5xx, malformed successful payload, unknown provider error, secret-bearing body and cancellation passthrough. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** 401/403/404/429/5xx, malformed successful payload, unknown provider error, secret-bearing body and cancellation passthrough.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Callers can distinguish auth, throttle and schema failures without parsing messages; error text and serialized fields omit payloads/secrets.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Vendor-specific error schemas require authoritative evidence; unknown responses use neutral classification.

<a id="sdk-010"></a>
### SDK-010 - Cancellation and timeout support

1. **Feature ID:** SDK-010.
2. **Feature name:** Cancellation and timeout support.
3. **Description:** Give hosts bounded work and prompt shutdown.
4. **Business value:** All async I/O and waits honor the caller token reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT linked total timeout; E-AUTH semaphore cancellation; E-EXPORT mixed sync/async APIs; E-TEST DeadlineCancelsInFlightResponse. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Propagate cancellation into auth/waits/body/paging/export; distinguish caller cancellation from deadline; use async stream export paths and explicit per-operation budgets.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing CancellationToken parameters/TransportOptions.RequestTimeout; proposed neutral deadline failure metadata.
13. **Dependencies:** SDK-009; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise cancel before send, during auth/retry/read/export, partial destination, deadline versus caller token and disposal. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Cancel before send, during auth/retry/read/export, partial destination, deadline versus caller token and disposal.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All async I/O and waits honor the caller token; no write retry after timeout; resource cleanup completes and partial outputs are documented invalid.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Host-provided HttpClient/token providers must honor their contracts; no external service prerequisite.

<a id="sdk-011"></a>
### SDK-011 - Logging and correlation IDs

1. **Feature ID:** SDK-011.
2. **Feature name:** Logging and correlation IDs.
3. **Description:** Support supportable failures without exposing employee data.
4. **Business value:** Logs contain stable event IDs and correlation, never tokens, raw URI/query or records reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT ActivitySource/Meter and allowlisted response IDs; E-DI RemoveAllLoggers; no shared logging contract. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add structured safe event IDs, operation/attempt/result fields, trace correlation and bounded sanitized vendor IDs; use host opt-in ILogger integration.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed safe DiagnosticContext and logging events; existing IntegrationJsonResponse header allowlist.
13. **Dependencies:** SDK-009, SDK-014; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise credential/PII canaries in URI/body/header/error, malicious/oversized request IDs, trace propagation and disabled logging. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Credential/PII canaries in URI/body/header/error, malicious/oversized request IDs, trace propagation and disabled logging.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Logs contain stable event IDs and correlation, never tokens, raw URI/query or records; unsafe vendor IDs are rejected or bounded.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Host framework instrumentation needs its own redaction configuration; SDK cannot guarantee arbitrary host logging.

<a id="sdk-012"></a>
### SDK-012 - OpenTelemetry instrumentation

1. **Feature ID:** SDK-012.
2. **Feature name:** OpenTelemetry instrumentation.
3. **Description:** Enable optional distributed tracing and useful bounded metrics.
4. **Business value:** A host can opt in to traces/metrics without changing clients reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Observability.
6. **Priority:** P1.
7. **Planned development phase:** P7; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-TRANSPORT ActivitySource/Meter Koras.Connect; E-TEST SdkActivityDoesNotEmitWorkerOrCredentialData. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add optional registration, durations/retries/errors and semantic tags with bounded cardinality; no global SDK/exporter setup or provider dependency.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing InstrumentationName/Activities; proposed AddKorasConnectInstrumentation in Observability.
13. **Dependencies:** SDK-011; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise listener/exporter capture, parent-child context, disabled overhead, cardinality and redaction tests. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Listener/exporter capture, parent-child context, disabled overhead, cardinality and redaction tests.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A host can opt in to traces/metrics without changing clients; removing Observability leaves Core usable; no PII reaches captured telemetry.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** OpenTelemetry versions and semantic conventions must be reviewed in P7; no exporter backend provisioned.

<a id="sdk-013"></a>
### SDK-013 - Export provider interfaces

1. **Feature ID:** SDK-013.
2. **Feature name:** Export provider interfaces.
3. **Description:** Provide replaceable exports with deterministic safe projections.
4. **Business value:** All four provider formats parse independently and preserve selected values/order reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions + Koras.Connect.Export.
6. **Priority:** P0.
7. **Planned development phase:** P3; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-EXPORT static writers; provider interface/async schema contract absent. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Implement providers for CSV/JSON/XML/XLSX and schema-driven async records; preserve stream ownership, bounded memory and formula defenses; cancellation/failure leaves incomplete output explicit.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed IExportProvider, ExportSchema<T>; existing TabularExport methods retained through migration.
13. **Dependencies:** SDK-002, SDK-010; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise empty records, null/date/culture/Unicode, delimiter/newline escaping, formula prefixes, width and Excel bounds, canceled stream. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Empty records, null/date/culture/Unicode, delimiter/newline escaping, formula prefixes, width and Excel bounds, canceled stream.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All four provider formats parse independently and preserve selected values/order; caller stream remains open; no implicit filesystem or provider dependency.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor entitlement; third-party Excel library requires license review if chosen.

<a id="sdk-014"></a>
### SDK-014 - Configuration validation

1. **Feature ID:** SDK-014.
2. **Feature name:** Configuration validation.
3. **Description:** Catch unsafe or contradictory settings before credentials leave the process.
4. **Business value:** Every invalid configuration fails before network with actionable redacted field identifiers reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + Koras.Connect.Authentication + provider packages (Koras.Connect.ADP, Koras.Connect.SAP.SuccessFactors, Koras.Connect.Qualtrics).
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-AUTH Validate, E-WORKERS options, E-SAP/E-QLT constructors, E-SAFETY; inconsistent validation surfaces. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Standardize eager validators for roots, token endpoints, limits, retries, credentials, effective dates and mutually exclusive settings; require exact configured origins and no redirects.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed options validators and safe validation results; existing constructor checks remain.
13. **Dependencies:** SDK-005; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise null/relative/HTTP/userinfo/query/fragment roots, invalid durations, unsafe IDs, date conflicts and duplicate names. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Null/relative/HTTP/userinfo/query/fragment roots, invalid durations, unsafe IDs, date conflicts and duplicate names.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Every invalid configuration fails before network with actionable redacted field identifiers; DI and direct construction enforce the same rules.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Exact vendor hosts/product versions are supplied by authorized consumers, never guessed.

<a id="sdk-015"></a>
### SDK-015 - Package signing and provenance

1. **Feature ID:** SDK-015.
2. **Feature name:** Package signing and provenance.
3. **Description:** Make package origin and artifact integrity verifiable.
4. **Business value:** Reviewed candidate verifies against approved trust policy and source revision reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P1.
7. **Planned development phase:** P7; P1 specifies only.
8. **Current implementation status:** Not started; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-BUILD deterministic metadata; E-RELEASE hashes/SBOM do not sign or attest builds. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Design timestamped author-signing/key rotation and artifact-to-source provenance; verify signatures on both OSes and bind attestation to exact package digests.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed release signature/provenance policy; no runtime API.
13. **Dependencies:** SDK-016, SDK-018; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise invalid/expired/untrusted signatures, tampered digest and mismatched source/build identity using disposable test certificates. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Invalid/expired/untrusted signatures, tampered digest and mismatched source/build identity using disposable test certificates.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Reviewed candidate verifies against approved trust policy and source revision; negative fixtures fail; no production key is stored in repository.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Approved signing identity, key custody and trust policy pending; procurement/public publication separately authorized [M3].

<a id="sdk-016"></a>
### SDK-016 - NuGet publishing workflow

1. **Feature ID:** SDK-016.
2. **Feature name:** NuGet publishing workflow.
3. **Description:** Produce reviewable packages and guard any future publication.
4. **Business value:** Candidate workflow works without secrets reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-RELEASE release.yml/manifest/checks; publicationApproved=false; release tests. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Update candidate inventory for target graph; preserve validate-only default, explicit version/license/approval/tag/hash checks; validate dependency order and partial remote publication recovery.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing manifest/approval schema and release_checks.validate; future workflow contract, no public push in P1.
13. **Dependencies:** SDK-001, SDK-017, SDK-018, SDK-019; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise wrong tag/version/license/hash, missing/extra archive, denied approval and synthetic eligible candidate. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Wrong tag/version/license/hash, missing/extra archive, denied approval and synthetic eligible candidate.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Candidate workflow works without secrets; forbidden publication scenarios fail closed; actual publication requires distinct owner authorization and configured environment protections.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Distribution license, package ownership, vendor terms, publishing authorization and server-side environment protection unverified.

<a id="sdk-017"></a>
### SDK-017 - API compatibility checks

1. **Feature ID:** SDK-017.
2. **Feature name:** API compatibility checks.
3. **Description:** Prevent accidental consumer breaks during package split and releases.
4. **Business value:** CI rejects an unapproved public break reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P2-P4; P1 specifies only.
8. **Current implementation status:** Not started; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-BUILD/E-RELEASE contain no ApiCompat/package-validation baseline. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Capture preview.2 surface before migration; add package validation against approved baseline and compile/run old and new consumers; require reviewed break records.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed reference API snapshots/EnablePackageValidation or ApiCompat workflow [M2].
13. **Dependencies:** none for initial API capture; split-package checks follow SDK-001; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise deliberately removed/changed API, type-forwarding identity and transitive dependency bounds. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Deliberately removed/changed API, type-forwarding identity and transitive dependency bounds.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** CI rejects an unapproved public break; approved preview migration is documented and consumed successfully on Linux/Windows.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Baseline nupkg identity must be generated/preserved from audited source; never assume unpublished artifacts are immutable public releases.

<a id="sdk-018"></a>
### SDK-018 - Security scanning and SBOM

1. **Feature ID:** SDK-018.
2. **Feature name:** Security scanning and SBOM.
3. **Description:** Expose vulnerable dependencies and shipping supply-chain contents.
4. **Business value:** Each shipping archive maps to a complete reviewed runtime SBOM/hash reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-RELEASE dependency audit, focused key/path scan, SPDX runtime inventory; no comprehensive code/history secret scan. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add reviewed code/dependency/secret scanning, explicit severity policy and SBOM completeness checks; include test/build tool inventories separately from runtime.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing eng/generate_sbom.py and check_sources.py; proposed security workflow and policy.
13. **Dependencies:** SDK-001; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise missing transitive dependency, changed archive, malicious test fixture, scanner threshold failures and license unknowns. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Missing transitive dependency, changed archive, malicious test fixture, scanner threshold failures and license unknowns.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** Each shipping archive maps to a complete reviewed runtime SBOM/hash; scanner findings block according to policy; scope/limitations are stated without certification claims.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Security finding triage/license review needed; external scanning service permissions require separate configuration.

<a id="sdk-019"></a>
### SDK-019 - Versioning and release notes

1. **Feature ID:** SDK-019.
2. **Feature name:** Versioning and release notes.
3. **Description:** Let consumers evaluate compatibility and upgrade safely.
4. **Business value:** One candidate version agrees everywhere reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** Partially implemented; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** E-BUILD preview.2; E-RELEASE versioned manifest; no changelog/release-note system found. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Coordinate versions across package/manifest/samples; adopt SemVer, immutable artifacts and migration notes; record known limitations/security changes per release.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed changelog/release-note format; existing version metadata.
13. **Dependencies:** SDK-017; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise version drift checks, invalid SemVer, mismatched consumer and upgrade smoke scenarios. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Version drift checks, invalid SemVer, mismatched consumer and upgrade smoke scenarios.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** One candidate version agrees everywhere; notes enumerate feature IDs, supported products, breaks and verification scope; no package overwrite is attempted.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Owner selects stable release number and distribution license; no release tag or publication created in P1.

<a id="sdk-020"></a>
### SDK-020 - SDK documentation website

1. **Feature ID:** SDK-020.
2. **Feature name:** SDK documentation website.
3. **Description:** Give users searchable trustworthy APIs, examples and limitations.
4. **Business value:** A reproducible local site build exposes supported APIs and migration/security guidance reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (documentation).
6. **Priority:** P1.
7. **Planned development phase:** P7; P1 specifies only.
8. **Current implementation status:** Not started; no full-feature acceptance or new runtime execution asserted.
9. **Existing implementation evidence:** README/planning Markdown only; no site source/build pipeline found. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Build site source from versioned docs/API references, samples and capability matrices; add link/accessibility checks and credential-safe examples; plan hosting separately.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed generated API/reference site; no runtime API.
13. **Dependencies:** SDK-019, ADP-022; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise broken links, absent API symbols, sample compilation, secret canaries and navigation checks. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Broken links, absent API symbols, sample compilation, secret canaries and navigation checks.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A reproducible local site build exposes supported APIs and migration/security guidance; all examples compile; deployment remains separately authorized.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Hosting/domain/deployment choices pending; site build does not authorize deployment.

## 11. ADP Master Feature Matrix

| ID | Feature | Priority | Phase | Current status |
|---|---|---|---|---|
| [ADP-001](#adp-001) | Certificate and mTLS authentication | P0 | P2 | Implemented but unverified |
| [ADP-002](#adp-002) | OAuth token acquisition and refresh | P0 | P2 | Implemented but unverified |
| [ADP-003](#adp-003) | Secure client credentials configuration | P0 | P2 | Partially implemented |
| [ADP-004](#adp-004) | Worker profile retrieval | P0 | P3 | Implemented but unverified |
| [ADP-005](#adp-005) | Worker pagination and filtering | P0 | P3 | Partially implemented |
| [ADP-006](#adp-006) | Worker changes and incremental sync | P0 | P3 | Partially implemented |
| [ADP-007](#adp-007) | Worker schema mapping | P0 | P3 | Partially implemented |
| [ADP-008](#adp-008) | CSV/JSON/XML export | P0 | P3 | Partially implemented |
| [ADP-009](#adp-009) | Excel export | P1 | P3 | Partially implemented |
| [ADP-010](#adp-010) | Structured errors and rate-limit handling | P0 | P3 | Partially implemented |
| [ADP-011](#adp-011) | Retry, timeout, and cancellation | P0 | P3 | Implemented but unverified |
| [ADP-012](#adp-012) | Connection diagnostics | P0 | P3 | Not started |
| [ADP-013](#adp-013) | ADP organization and manager data | P1 | P3 extension | Partially implemented |
| [ADP-014](#adp-014) | Employment status and lifecycle events | P1 | P3 extension | Partially implemented |
| [ADP-015](#adp-015) | Payroll operations, where entitled | P1 | P3 extension | Partially implemented |
| [ADP-016](#adp-016) | Time and attendance APIs | P2 | P7 | Partially implemented |
| [ADP-017](#adp-017) | Benefits APIs | P2 | P7 | Partially implemented |
| [ADP-018](#adp-018) | Webhook/event subscriptions | P2 | P3 primitives; P7 expansion | Partially implemented |
| [ADP-019](#adp-019) | Typed API response models | P0 | P3 | Partially implemented |
| [ADP-020](#adp-020) | Sandbox and contract test suite | P0 | P3-P4 | Partially implemented |
| [ADP-021](#adp-021) | API capability/entitlement matrix | P0 | P1 baseline; P3 completion | Partially implemented |
| [ADP-022](#adp-022) | Documentation and runnable examples | P0 | P3-P4 | Partially implemented |

## 12. ADP Detailed Feature Specifications

Baseline product scope is ADP U.S. Workforce Now. WFN Next Gen, RUN, Lyric, Vantage and international products are not assumed equivalent. The current worker/auth/retry code covers bounded operations, but acceptance also requires current schema and permitted sandbox verification. First-release P0 requirements are mandatory; existing P1/P2 dispatch code is preserved as an explicitly limited preview, not expanded during P1.

<a id="adp-001"></a>
### ADP-001 - Certificate and mTLS authentication

1. **Feature ID:** ADP-001.
2. **Feature name:** Certificate and mTLS authentication.
3. **Description:** Establish certificate-authenticated ADP connections without exposing private keys.
4. **Business value:** Establish certificate-authenticated ADP connections without exposing private keys.
5. **Owning package:** Koras.Connect.ADP with Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; no implementation in P1.
8. **Current implementation status:** Implemented but unverified; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-AUTH ClientCertificateTransport; E-DI; E-TEST certificate loader/DI tests; aliases resolve in section 3.
10. **Functional requirements:** Use registered certificate for both token and API handlers; reject missing private key; support injected/PEM/PFX certificates and document expiry/rotation and host disposal.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ClientCertificateTransport.Create/LoadPem/LoadPkcs12; proposed certificate-provider adapter.
13. **Dependencies:** SDK-005, SDK-006, SDK-004; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** PEM/PFX roundtrip, public-only rejection, invalid password, expiry policy, handler isolation and rotation. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** PEM/PFX roundtrip, public-only rejection, invalid password, expiry policy, handler isolation and rotation.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Both OAuth and API channels present the intended certificate in an authorized sandbox; missing/expired credentials fail safely and synthetic resource tests pass.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** ADP app/certificate registration and sandbox TLS verification unavailable; source implementation is not a successful handshake claim.

<a id="adp-002"></a>
### ADP-002 - OAuth token acquisition and refresh

1. **Feature ID:** ADP-002.
2. **Feature name:** OAuth token acquisition and refresh.
3. **Description:** Acquire and renew tokens predictably under concurrent load.
4. **Business value:** Acquire and renew tokens predictably under concurrent load.
5. **Owning package:** Koras.Connect.ADP with Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; no implementation in P1.
8. **Current implementation status:** Implemented but unverified; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-AUTH ClientCredentialsTokenSource; E-TEST ConcurrentTokenAcquisitionIsSingleFlightAndRedacted and TokenRenewsBeforeExpiryAndCanceledCallsCannotUseCache; aliases resolve in section 3.
10. **Functional requirements:** Use client_credentials on the exact configured ADP token endpoint; renew using expiry/skew; no refresh-token grant assumed; isolate caches and define credential-rotation invalidation.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing IAccessTokenSource/GetTokenAsync and ClientCredentialsOptions; proposed rotatable credential adapter.
13. **Dependencies:** ADP-001, SDK-005, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Concurrency single-flight, expiry boundary, bad/missing token/type/lifetime, failed renewal and canceled cache hit. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Concurrency single-flight, expiry boundary, bad/missing token/type/lifetime, failed renewal and canceled cache hit.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Concurrent requests share one valid acquisition; expired credentials cannot remain cached; request shape and live token grant pass with authorized test credentials.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Official getting-started page fetch failed in P1; current endpoint is source/README evidence and official collection candidate, not newly live-verified; app grant required.

<a id="adp-003"></a>
### ADP-003 - Secure client credentials configuration

1. **Feature ID:** ADP-003.
2. **Feature name:** Secure client credentials configuration.
3. **Description:** Prevent accidental credential leakage and enable replacement without code changes.
4. **Business value:** Prevent accidental credential leakage and enable replacement without code changes.
5. **Owning package:** Koras.Connect.ADP with Koras.Connect.Authentication.
6. **Priority:** P0.
7. **Planned development phase:** P2; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-AUTH ClientCredentialsOptions.Validate/ToString; E-DI certificate injection; aliases resolve in section 3.
10. **Functional requirements:** Validate ID/secret/endpoint before send; add provider-source configuration, redacted options and documented local development setup without committing secrets; expose no credential dump diagnostics.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ClientCredentialsOptions; proposed ICredentialProvider<AdpClientCredential> adapter.
13. **Dependencies:** SDK-006, SDK-014, ADP-002; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Whitespace/null values, unsafe roots, option formatting/serialization, provider failures, reload and cross-tenant isolation. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Whitespace/null values, unsafe roots, option formatting/serialization, provider failures, reload and cross-tenant isolation.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Invalid settings fail before network; all public credential representations redact secrets; documented external source rotation invalidates the relevant token cache.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Secret store and certificate custody supplied by host; no customer credentials available.

<a id="adp-004"></a>
### ADP-004 - Worker profile retrieval

1. **Feature ID:** ADP-004.
2. **Feature name:** Worker profile retrieval.
3. **Description:** Read an authorized worker using a stable public model.
4. **Business value:** Read an authorized worker using a stable public model.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Implemented but unverified; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-WORKERS GetWorkerAsync; E-TEST IndividualWorkerPreservesUnknownFieldsAndEscapesFilters; aliases resolve in section 3.
10. **Functional requirements:** Retrieve by associate OID, preserve extension fields and masked/absent values, define not-found/empty behavior and disposal; do not conflate workerID with associateOID.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Task<Worker?> GetWorkerAsync(string associateOid, CancellationToken); GET /hr/v2/workers/{associateOID}.
13. **Dependencies:** ADP-001, ADP-002, ADP-019; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Valid/empty/multiple worker envelopes, unsafe identifier encoding, unknown fields, 403/404 and malformed JSON. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Valid/empty/multiple worker envelopes, unsafe identifier encoding, unknown fields, 403/404 and malformed JSON.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Synthetic fixtures preserve typed and unknown data; empty authorized result returns documented null, non-success errors remain classified; live authorized worker matches sanitized schema.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** WFN product, worker read CAR grant, actor/population and field visibility must be verified against customer metadata [A1].

<a id="adp-005"></a>
### ADP-005 - Worker pagination and filtering

1. **Feature ID:** ADP-005.
2. **Feature name:** Worker pagination and filtering.
3. **Description:** Enumerate large authorized populations without unsafe query construction.
4. **Business value:** Enumerate large authorized populations without unsafe query construction.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-WORKERS page/search/iterators; E-TEST WorkersTests, SearchCannotInjectQueryParameters and RepeatedPagesStopInsteadOfLooping; aliases resolve in section 3.
10. **Functional requirements:** Preserve encoded metadata-approved filters; define typed filter whitelist and neutral page adapter; do not stop only because a page is smaller than requested; document current SDK size bound separately from server limits.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing GetWorkersPageAsync/SearchWorkersAsync/EnumerateTypedWorkersAsync; proposed Page<Worker> adapter.
13. **Dependencies:** ADP-004, SDK-007, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Empty/short/multiple/repeated pages, offset overflow, unsupported filters, special characters, partial cancellation and server-returned fewer records. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Empty/short/multiple/repeated pages, offset overflow, unsupported filters, special characters, partial cancellation and server-returned fewer records.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** All expected fixture records appear once per stable traversal; filters cannot inject query parameters; page resume validates query identity; 202 responses fail explicitly unless an authorized async protocol is implemented.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Current SDK accepts 1-100; that is not a universal ADP maximum. Async worker retrieval is outside current implementation and requires product-specific verification [A1].

<a id="adp-006"></a>
### ADP-006 - Worker changes and incremental sync

1. **Feature ID:** ADP-006.
2. **Feature name:** Worker changes and incremental sync.
3. **Description:** Reduce repeat delivery while retaining a reliable recovery path.
4. **Business value:** Reduce repeat delivery while retaining a reliable recovery path.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-SYNC fingerprints/batching; E-OPS notifications/ack; E-EVENT delivery identity; aliases resolve in section 3.
10. **Functional requirements:** Expose caller-owned query-bound checkpoints and event-triggered re-read plus periodic snapshot reconciliation; never invent modifiedSince, infer deletion from invisibility or acknowledge before downstream success.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Reconciliation.DetectChangesAsync, ReadNotificationAsync/AcknowledgeNotificationAsync; proposed synchronization result/checkpoint contract.
13. **Dependencies:** ADP-004, SDK-007, ADP-018; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Duplicate keys/delivery IDs, changed representation, interrupted scan, out-of-order events, permission shrink and failed downstream delivery. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Duplicate keys/delivery IDs, changed representation, interrupted scan, out-of-order events, permission shrink and failed downstream delivery.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** A failed scan cannot commit a completed checkpoint; duplicate events do not force duplicate host processing in sample harness; no universal last-modified filter or hidden persistence is emitted.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Exact subscribed event coverage/retention and permitted reads required; no universal WFN delta cursor verified [A4].

<a id="adp-007"></a>
### ADP-007 - Worker schema mapping

1. **Feature ID:** ADP-007.
2. **Feature name:** Worker schema mapping.
3. **Description:** Create explicit auditable export/domain projections without losing vendor fields silently.
4. **Business value:** Create explicit auditable export/domain projections without losing vendor fields silently.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-WORKERS typed subset/JSON fragments; E-EXPORT caller projections; no mapping-provider interface; aliases resolve in section 3.
10. **Functional requirements:** Provide opt-in mapper/projection profiles for identity, legal name and assignments; preserve absence/masking; version profiles and distinguish effective assignments; no built-in ADP-to-SAP workflow.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<Worker,T> and MappingResult<T>; current caller Func mappings.
13. **Dependencies:** ADP-019, SDK-002, SDK-013; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing names, multiple assignments, unknown codes, masked identifiers, future properties, effective dates and culture. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing names, multiple assignments, unknown codes, masked identifiers, future properties, effective dates and culture.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Golden synthetic worker-to-neutral projection tests pass; omitted/masked/invalid fields produce explicit documented results; source model remains unchanged.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Field meanings and authorized metadata vary per tenant; obtain exact schema/codes before claiming full worker mapping.

<a id="adp-008"></a>
### ADP-008 - CSV/JSON/XML export

1. **Feature ID:** ADP-008.
2. **Feature name:** CSV/JSON/XML export.
3. **Description:** Deliver selected worker data in portable file formats.
4. **Business value:** Deliver selected worker data in portable file formats.
5. **Owning package:** Koras.Connect.ADP (projection) and Koras.Connect.Export (serialization), composed by host.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-EXPORT WriteCsvAsync/WriteJsonAsync/WriteXml; E-TEST ExportsEscapeDataAndLeaveStreamOpen; E-SAMPLE; aliases resolve in section 3.
10. **Functional requirements:** Compose worker mappings with independent Export providers; stable column/schema and null/date encoding; stream without implicit file writes and reject partial success.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing TabularExport; proposed IExportProvider composed with worker projection, no ADP-Export dependency.
13. **Dependencies:** ADP-007, SDK-013, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** CSV quote/formula/control prefixes, XML invalid characters/names policy, JSON extension fields, Unicode/null values, cancellation and failing output stream. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** CSV quote/formula/control prefixes, XML invalid characters/names policy, JSON extension fields, Unicode/null values, cancellation and failing output stream.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Each format roundtrips the documented selected fixture fields; streams remain open and partial failure is surfaced; serialized output includes no implicit sensitive columns.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** No extra vendor grant beyond retrieving selected data; consumers must authorize data destinations.

<a id="adp-009"></a>
### ADP-009 - Excel export

1. **Feature ID:** ADP-009.
2. **Feature name:** Excel export.
3. **Description:** Allow spreadsheet consumption without formula execution.
4. **Business value:** Allow spreadsheet consumption without formula execution.
5. **Owning package:** Koras.Connect.ADP (projection) and Koras.Connect.Export (serialization), composed by host.
6. **Priority:** P1.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-EXPORT WriteExcel; E-TEST XLSX XML/no-formula checks; aliases resolve in section 3.
10. **Functional requirements:** Retain inline text/no formulas; support async provider composition, explicit row/column/text limits and large-output budgets; reject invalid input rather than silently truncate.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing WriteExcel; proposed XLSX IExportProvider and explicit schema.
13. **Dependencies:** SDK-013, ADP-007; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Formula prefixes, whitespace/Unicode, maximum and over-limit rows/columns/text, invalid XML characters and independent workbook-reader validation. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Formula prefixes, whitespace/Unicode, maximum and over-limit rows/columns/text, invalid XML characters and independent workbook-reader validation.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** An independent reader opens the workbook and recovers selected values; no formula nodes are generated; limits fail deterministically with documented partial-output handling.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** No Excel license/service required for generation; reader/library choice reviewed later, not installed in P1.

<a id="adp-010"></a>
### ADP-010 - Structured errors and rate-limit handling

1. **Feature ID:** ADP-010.
2. **Feature name:** Structured errors and rate-limit handling.
3. **Description:** Expose actionable ADP failure classes without leaking HR records.
4. **Business value:** Expose actionable ADP failure classes without leaking HR records.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-TRANSPORT IntegrationRequestException/retries; E-TEST ApiFailuresDoNotExposeResponseBody; aliases resolve in section 3.
10. **Functional requirements:** Map documented ADP error metadata/correlation to neutral hierarchy; preserve HTTP/attempt/retry hints safely; never emit raw ADP userMessage by default.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed AdpApiException/SafeProviderError layered on SDK-009; current IntegrationRequestException.
13. **Dependencies:** SDK-009, SDK-008, SDK-011; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** 401/403/404/429/5xx, blank benefits 400, malformed provider error, hostile IDs, excessive Retry-After and payload canaries. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** 401/403/404/429/5xx, blank benefits 400, malformed provider error, hostile IDs, excessive Retry-After and payload canaries.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Host can distinguish denied access, throttling, invalid input and malformed response with sanitized fields; no private message/body appears in diagnostics.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Per-operation official error schemas and tenant samples needed; no numeric ADP rate limit verified.

<a id="adp-011"></a>
### ADP-011 - Retry, timeout, and cancellation

1. **Feature ID:** ADP-011.
2. **Feature name:** Retry, timeout, and cancellation.
3. **Description:** Bound ADP work while protecting writes from replay.
4. **Business value:** Bound ADP work while protecting writes from replay.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Implemented but unverified; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-TRANSPORT bounded GET retry/deadline; E-TEST transient/deadline/no-write-retry tests; aliases resolve in section 3.
10. **Functional requirements:** Apply shared operation-safety defaults to workers, event reads, acknowledgement and writes; include auth/waits in documented operation budget or expose separate explicit auth budget.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing TransportOptions and operation CancellationToken parameters.
13. **Dependencies:** SDK-008, SDK-010, ADP-002; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Cancel during auth/page/delay/body, Retry-After date/delta, write timeout unknown outcome, no DELETE replay and disposed response. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Cancel during auth/page/delay/body, Retry-After date/delta, write timeout unknown outcome, no DELETE replay and disposed response.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Transient reads respect bounded delay/deadline; POST and acknowledgement DELETE are not automatically replayed; cancellation escapes promptly and writes advise reconciliation.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Sandbox rate behavior not verified; authorized live tests must avoid inducing damaging throttling or duplicate writes.

<a id="adp-012"></a>
### ADP-012 - Connection diagnostics

1. **Feature ID:** ADP-012.
2. **Feature name:** Connection diagnostics.
3. **Description:** Help operators separate TLS, auth, entitlement and schema failures.
4. **Business value:** Help operators separate TLS, auth, entitlement and schema failures.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Not started; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** No TestConnection/Diagnose API in E-WORKERS/E-OPS; E-TRANSPORT has only hooks; aliases resolve in section 3.
10. **Functional requirements:** Implement opt-in staged read-only diagnostics: local config/cert validity, token acquisition and a configured minimal authorized API probe; no writes or exhaustive tenant scan.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed AdpConnector.DiagnoseAsync returning ConnectionDiagnostic with stage/outcome/redacted correlation.
13. **Dependencies:** SDK-002, SDK-009, SDK-011, ADP-004; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Expired/missing cert, 401/403, timeout, DNS/TLS fault, malformed response, canceled probe and no write sends. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Expired/missing cert, 401/403, timeout, DNS/TLS fault, malformed response, canceled probe and no write sends.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Results identify the failed stage without exposing credential/worker data; default diagnostics never mutate ADP and do not claim full tenant permissions from one successful read.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Caller must select entitled probe and sandbox; no universal connectivity endpoint is invented.

<a id="adp-013"></a>
### ADP-013 - ADP organization and manager data

1. **Feature ID:** ADP-013.
2. **Feature name:** ADP organization and manager data.
3. **Description:** Expose reporting relationships and organizational structure accurately.
4. **Business value:** Expose reporting relationships and organizational structure accurately.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P1.
7. **Planned development phase:** P3 extension; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-OPS OrganizationDepartments/ChangeManager; E-WORKERS WorkAssignment JSON fragments; aliases resolve in section 3.
10. **Functional requirements:** Type supported department/manager reads and explicit relationship projections; separate documented manager writes and department-allocation events from arbitrary home-location/home-department replacement.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing department raw read and worker ReportsTo/OrganizationalUnits; proposed typed provider DTOs/mappers.
13. **Dependencies:** ADP-004, ADP-007, ADP-019; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Multiple managers/assignments, absent relationship, cycles in host traversal, unknown organization codes, inaccessible manager and write gating. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Multiple managers/assignments, absent relationship, cycles in host traversal, unknown organization codes, inaccessible manager and write gating.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Selected official read schemas roundtrip unknown fields; relationship identity is documented; no unsupported general replacement endpoint is exposed.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Department/read and relationship/write grants differ; exact current contracts/product versions and manager-write envelope need tenant metadata [A1/A5].

<a id="adp-014"></a>
### ADP-014 - Employment status and lifecycle events

1. **Feature ID:** ADP-014.
2. **Feature name:** Employment status and lifecycle events.
3. **Description:** Support entitled employment changes with deliberate safety gates.
4. **Business value:** Support entitled employment changes with deliberate safety gates.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P1.
7. **Planned development phase:** P3 extension; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-OPS Hire/Rehire/TerminateAssignment/ModifyWorkAssignment/ModifyDepartmentAllocations; E-WORKERS hire/termination fields; aliases resolve in section 3.
10. **Functional requirements:** Add typed, metadata-validated opt-in operations and effective-date/status modeling; reconcile unknown write outcomes; do not infer generic worker PATCH from event names.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing SubmitEventAsync low-level JSON dispatch; proposed operation-specific commands/results only after contract verification.
13. **Dependencies:** ADP-021, ADP-019, ADP-010, ADP-011; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing required fields, wrong code/effective date, unauthorized operation, mixed response errors, uncertain POST timeout and no retry. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing required fields, wrong code/effective date, unauthorized operation, mixed response errors, uncertain POST timeout and no retry.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Each enabled operation has current official request/response schema and synthetic positive/negative tests; live writes run only in a separately authorized sandbox with cleanup plan.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Full canonical grants/actor/product/metadata pending; termination portal is JS-only in P1. Existing dispatch does not prove complete lifecycle validation.

<a id="adp-015"></a>
### ADP-015 - Payroll operations, where entitled

1. **Feature ID:** ADP-015.
2. **Feature name:** Payroll operations, where entitled.
3. **Description:** Read or submit narrowly authorized payroll data without overstating coverage.
4. **Business value:** Read or submit narrowly authorized payroll data without overstating coverage.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P1.
7. **Planned development phase:** P3 extension; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-OPS ChangePayDataInput, PayrollInstructions, PayDistributions; E-TEST documented route/no-retry tests; aliases resolve in section 3.
10. **Functional requirements:** Document exact supported payroll subset; type approved contracts; gate writes by explicit opt-in/grants/open payroll context; preserve decimal/currency and per-item outcomes.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing raw read/SubmitEventAsync; proposed typed payroll result/command family after metadata review.
13. **Dependencies:** ADP-021, ADP-010, ADP-019; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Invalid batch/payroll context, precision/nulls, denied grants, failed items and unknown write outcomes. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Invalid batch/payroll context, precision/nulls, denied grants, failed items and unknown write outcomes.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Only approved operation contracts are callable; synthetic envelopes validate against reviewed schemas; no automatic payroll POST retry; broader payroll APIs remain unsupported.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Licensed payroll API subscription, allowed payroll groups and safe sandbox write authorization unavailable [A5]; no production payroll execution.

<a id="adp-016"></a>
### ADP-016 - Time and attendance APIs

1. **Feature ID:** ADP-016.
2. **Feature name:** Time and attendance APIs.
3. **Description:** Expose a scoped time capability rather than equating time-off reads with attendance coverage.
4. **Business value:** Expose a scoped time capability rather than equating time-off reads with attendance coverage.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P2.
7. **Planned development phase:** P7; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-OPS TimeOffBalances/TimeOffRequests; E-TEST documented worker read routes; aliases resolve in section 3.
10. **Functional requirements:** Retain time-off reads; research attendance/timecard contracts independently before adding them; model timezone/date/unit rules and entitlement-specific capabilities.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ReadAsync for two time-off operations; no attendance API proposed until verified.
13. **Dependencies:** ADP-021, ADP-019, SDK-007; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing worker, date/unit/timezone edge cases, paging where documented, forbidden product and unknown fields. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing worker, date/unit/timezone edge cases, paging where documented, forbidden product and unknown fields.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Time-off subset has typed reviewed schemas and tests; capability report explicitly says attendance/timecard operations unverified until their contracts and tests are added.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Time product/module and exact grants unavailable; no attendance endpoint or common quota inferred from time-off collection examples [A5].

<a id="adp-017"></a>
### ADP-017 - Benefits APIs

1. **Feature ID:** ADP-017.
2. **Feature name:** Benefits APIs.
3. **Description:** Retrieve authorized enrollment details within explicit product restrictions.
4. **Business value:** Retrieve authorized enrollment details within explicit product restrictions.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P2.
7. **Planned development phase:** P7; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-OPS ReadSpendingAccountEnrollmentsAsync; E-TEST BenefitsRequireEffectiveDateAndPreserveDocumentedFilter; aliases resolve in section 3.
10. **Functional requirements:** Retain date-required spending-account read; preserve documented filter exactly until a current contract supersedes it; type coverage/periods and handle sensitive unmasked data conservatively.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ReadSpendingAccountEnrollmentsAsync(string, DateOnly, CancellationToken); proposed typed enrollment response.
13. **Dependencies:** ADP-021, ADP-019, ADP-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing date/identifier, blank-body 400, overlapping periods, active/inactive dependents, masked/unmasked fields and denied grant. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing date/identifier, blank-body 400, overlapping periods, active/inactive dependents, masked/unmasked fields and denied grant.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Required effective-date query and permitted plan restrictions are documented; no broad benefits coverage is claimed; output mapping uses explicit sensitive-field selection.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** External-benefit-provider plan setup/ADP Benefits onboarding and current grant required; official guide contains route inconsistencies requiring tenant confirmation [A3].

<a id="adp-018"></a>
### ADP-018 - Webhook/event subscriptions

1. **Feature ID:** ADP-018.
2. **Feature name:** Webhook/event subscriptions.
3. **Description:** React to supported changes while maintaining host-owned durable delivery.
4. **Business value:** React to supported changes while maintaining host-owned durable delivery.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P2.
7. **Planned development phase:** P3 primitives; P7 expansion; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-EVENT publisher helper/delivery key; E-OPS notification read/ack; E-TEST polling/header tests; aliases resolve in section 3.
10. **Functional requirements:** Keep polling helpers and explicit ack; document subscription provisioning and covered events; authenticate receiver separately, deduplicate durably in host and re-read worker data; no SDK web server.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing VerifyPublisherHeader/GetDeliveryKey and notification methods; proposed parsed safe event envelope/capability descriptor.
13. **Dependencies:** SDK-007, ADP-004, ADP-021; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Invalid HMAC/header, duplicate/org-separated IDs, malformed envelope, empty 204 queue, ack after failure and no automatic ack. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Invalid HMAC/header, duplicate/org-separated IDs, malformed envelope, empty 204 queue, ack after failure and no automatic ack.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Synthetic workflow never acknowledges before simulated durable success; receiver docs explain static header cannot verify payload integrity/freshness; subscription is reported unknown until authorized evidence exists.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Subscription setup belongs to ADP project/partner administration, not a guessed API; live webhook endpoint and replay storage are host-owned [A4].

<a id="adp-019"></a>
### ADP-019 - Typed API response models

1. **Feature ID:** ADP-019.
2. **Feature name:** Typed API response models.
3. **Description:** Make supported responses discoverable and resilient to schema growth.
4. **Business value:** Make supported responses discoverable and resilient to schema growth.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-WORKERS Worker/Person/WorkAssignment; E-OPS other responses primarily JsonDocument; aliases resolve in section 3.
10. **Functional requirements:** Complete typed schemas for the first-release operation subset; preserve extension data, vendor codes, dates/precision, masked/null distinction and stable naming; raw escape hatch remains explicit.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Worker family; proposed response envelopes and safe error/metadata models tied to verified operations.
13. **Dependencies:** ADP-021, SDK-017, SDK-009; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Unknown/nested fields, null/missing collections, invalid code/date/number, oversized/deep JSON and forward/backward fixtures. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Unknown/nested fields, null/missing collections, invalid code/date/number, oversized/deep JSON and forward/backward fixtures.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Every declared first-release typed operation has official schema mapping and golden tests; unknown fields survive roundtrip; raw-only preview operations are labeled clearly.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Current complete official schemas and authorized metadata required; code fragments are not full vendor models.

<a id="adp-020"></a>
### ADP-020 - Sandbox and contract test suite

1. **Feature ID:** ADP-020.
2. **Feature name:** Sandbox and contract test suite.
3. **Description:** Verify protocol correctness and expose live-access gaps honestly.
4. **Business value:** Verify protocol correctness and expose live-access gaps honestly.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3-P4; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** E-TEST all five C# files; no live harness; historical execution in VERIFICATION.md; aliases resolve in section 3.
10. **Functional requirements:** Organize unit/contract/security/live suites; schema-version synthetic fixtures with provenance; opt-in read-only sandbox harness; approved writes isolated and never default.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed sandbox configuration/test category; no production runtime API.
13. **Dependencies:** ADP-001-019, SDK-018; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Contract fixture negative cases, missing credential skip reasons, no-network-by-default, redaction and destructive-operation exclusion. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Contract fixture negative cases, missing credential skip reasons, no-network-by-default, redaction and destructive-operation exclusion.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** CI runs deterministic synthetic tests on both OSes; separately authorized sandbox results identify product/schema/grants/revision; missing access is reported skipped/blocked, never passed.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Sandbox root, credentials/certificate, population and subscriptions absent; synthetic tests can proceed without them.

<a id="adp-021"></a>
### ADP-021 - API capability/entitlement matrix

1. **Feature ID:** ADP-021.
2. **Feature name:** API capability/entitlement matrix.
3. **Description:** Prevent customers from assuming availability from a method's existence.
4. **Business value:** Prevent customers from assuming availability from a method's existence.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P1 baseline; P3 completion; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
9. **Existing implementation evidence:** docs/features/ADP-FEATURE-MATRIX.md; E-OPS catalog; section 13; aliases resolve in section 3.
10. **Functional requirements:** Track product/version, family, method/path, schema, auth, canonical grant, actor/population, read/write, paging/quota, evidence date and live state per operation; unknown stays unknown.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed ConnectorDescriptor operation descriptors; section 13 is the P1 baseline.
13. **Dependencies:** SDK-002; current official schema/entitlement research precedes modeling, matrix acceptance consumes ADP-019/020 evidence; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Matrix-to-catalog consistency, unsupported/unknown states, grant evidence expiry and synthetic deny-before-network. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Matrix-to-catalog consistency, unsupported/unknown states, grant evidence expiry and synthetic deny-before-network.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Every exposed operation has a reviewed row; unknown grants/schemas block that operation's release acceptance without falsely blocking unrelated independently verified operations.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Exact customer CAR/exported current metadata and product access not supplied; public catalog availability does not establish entitlement.

<a id="adp-022"></a>
### ADP-022 - Documentation and runnable examples

1. **Feature ID:** ADP-022.
2. **Feature name:** Documentation and runnable examples.
3. **Description:** Let engineers adopt supported operations reproducibly and safely.
4. **Business value:** Let engineers adopt supported operations reproducibly and safely.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3-P4; no implementation in P1.
8. **Current implementation status:** Partially implemented; live tenant behavior unverified, full-feature acceptance not granted.
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

| ID | Feature | Priority | Phase | Current status |
|---|---|---|---|---|
| [SAP-001](#sap-001) | OAuth and supported authentication mechanisms | P0 | P5 | Partially implemented |
| [SAP-002](#sap-002) | OData API foundation and employee/person data | P0 | P5 | Partially implemented |
| [SAP-003](#sap-003) | Upsert operations | P0 | P5 | Partially implemented |
| [SAP-004](#sap-004) | Pagination and filtering | P0 | P5 | Partially implemented |
| [SAP-005](#sap-005) | Error handling and diagnostics | P0 | P5 | Partially implemented |
| [SAP-006](#sap-006) | Contract and sandbox testing | P0 | P5 | Partially implemented |
| [SAP-007](#sap-007) | Additional APIs and complete business-schema validation | P1 | P5 | Planned |
| [SAP-008](#sap-008) | Employment records | P0 | P5 | Partially implemented |
| [SAP-009](#sap-009) | Job information | P0 | P5 | Partially implemented |
| [SAP-010](#sap-010) | Organizational data | P1 | P5 | Not started |
| [SAP-011](#sap-011) | Manager relationships | P1 | P5 | Partially implemented |
| [SAP-012](#sap-012) | Employee create/update operations where authorized | P1 | P5 | Partially implemented |
| [SAP-013](#sap-013) | Delta and incremental synchronization | P0 | P5 | Partially implemented |
| [SAP-014](#sap-014) | Metadata and schema discovery | P0 | P5 | Partially implemented |
| [SAP-015](#sap-015) | Data mapping | P0 | P5 | Partially implemented |
| [SAP-016](#sap-016) | Configuration, documentation and examples | P0 | P5 | Partially implemented |

<a id="sap-001"></a>
### SAP-001 - OAuth and supported authentication mechanisms

1. **Feature ID:** SAP-001.
2. **Feature name:** OAuth and supported authentication mechanisms.
3. **Description:** Authenticate an independent tenant with authorized bearer or SAML exchange.
4. **Business value:** Authenticate an independent tenant with authorized bearer or SAML exchange.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP SamlBearerTokenSource; E-TEST SapSamlGrantUsesExactProtocolAndCachesWithoutLoggingAssertions; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Require exact company/client/API host and host-generated assertion; no implicit Basic fallback or assumption that unrelated IAS tokens work.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing SamlBearerTokenSource/IAccessTokenSource; POST /oauth/token with company_id/client_id/grant_type/assertion.
13. **Dependencies:** SDK-005, SDK-006; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Concurrent renewal, expiry/skew, invalid assertion/token, rotation and canceled exchange. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Concurrent renewal, expiry/skew, invalid assertion/token, rotation and canceled exchange.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Concurrent calls share valid acquisition; malformed tokens cannot poison cache; configured grant succeeds in permitted sandbox with no assertion disclosure.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Registered OAuth client/company, trusted IdP assertion and technical-user permissions absent [S1]; other auth modes need current verification.

<a id="sap-002"></a>
### SAP-002 - OData API foundation and employee/person data

1. **Feature ID:** SAP-002.
2. **Feature name:** OData API foundation and employee/person data.
3. **Description:** Read person records independently of other connectors.
4. **Business value:** Read person records independently of other connectors.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP SuccessFactorsClient/PerPerson; E-TEST SapPagesPreserveUnknownFieldsAndEffectiveDatesWithoutChangingNextLink; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Use explicit /odata/v2 root, metadata-named entity sets and d.results/d.__next parsing; preserve typed/unknown fields and masked/absent values.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EnumeratePersonsAsync/EnumerateAsync<T>/PerPerson; proposed Page<T> adapter.
13. **Dependencies:** SAP-001, SDK-007; shared paging contract before SAP-004 adapter completion; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Empty/malformed d.results, null fields, unknown extensions, unsafe entity name and access denial. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Empty/malformed d.results, null fields, unknown extensions, unsafe entity name and access denial.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Person fixtures preserve all mapped/unknown values; denied RBP is not reported as empty data; isolated consumer references no ADP package.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity/field visibility and target population require tenant RBP/current metadata; no universal employee schema.

<a id="sap-003"></a>
### SAP-003 - Upsert operations

1. **Feature ID:** SAP-003.
2. **Feature name:** Upsert operations.
3. **Description:** Submit authorized insert/update batches with per-row outcomes.
4. **Business value:** Submit authorized insert/update batches with per-row outcomes.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP UpsertAsync/UpsertResult; E-TEST HTTP200 row failures and no-retry tests; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Keep writesEnabled=false default and SDK 1-1000 bound; inject validated entity metadata, inspect every status/httpCode/index, never infer atomic success from HTTP200.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing UpsertAsync; POST /odata/v2/upsert?$format=json.
13. **Dependencies:** SAP-001, SAP-014; primitive precedes SAP-012 typed employee commands; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Mixed row outcomes, metadata injection, count boundaries, malformed results and unknown POST outcome. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Mixed row outcomes, metadata injection, count boundaries, malformed results and unknown POST outcome.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Every row has attributable result; default call sends nothing; explicit batch is sent once; row failures remain visible without logging their private messages.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity upsert support, required keys/fields, RBP and disposable sandbox objects required [S2].

<a id="sap-004"></a>
### SAP-004 - Pagination and filtering

1. **Feature ID:** SAP-004.
2. **Feature name:** Pagination and filtering.
3. **Description:** Traverse effective-dated data without unsafe continuations.
4. **Business value:** Traverse effective-dated data without unsafe continuations.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP ODataQuery/iterator; E-SAFETY; E-TEST continuation/cycle tests; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Preserve valid __next verbatim within configured origin/path; support filter/select/expand/as-of/ranges and cursor/snapshot only per entity; bind resumable tokens to query.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing ODataQuery; proposed Page<T>/ContinuationToken adapter.
13. **Dependencies:** SDK-007, SAP-002; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Off-origin/path/userinfo/HTTP links, cycles/max pages, escaped filters, date conflicts, expired cursor and empty page with continuation. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Off-origin/path/userinfo/HTTP links, cycles/max pages, escaped filters, date conflicts, expired cursor and empty page with continuation.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures use returned next URLs unchanged; hostile links fail before forwarding credentials; conflicting date options fail locally.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity paging modes/effective-date behavior depend on current schema; SDK bounds are not vendor capacity guarantees [S3].

<a id="sap-005"></a>
### SAP-005 - Error handling and diagnostics

1. **Feature ID:** SAP-005.
2. **Feature name:** Error handling and diagnostics.
3. **Description:** Separate auth/RBP/transport/business-row failures safely.
4. **Business value:** Separate auth/RBP/transport/business-row failures safely.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP redacted UpsertResult.ToString; E-TRANSPORT; no SAP diagnostic API; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Map safe codes/correlation and staged read-only diagnostics; apply read retry budget and no upsert replay; keep row messages out of automatic logs.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing UpsertResult/IntegrationRequestException; proposed SAP exception/DiagnoseAsync.
13. **Dependencies:** SDK-009, SDK-011, SAP-003; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** 401/403, HTTP200 row error, invalid field, malformed XML/error, rate hint and canceled probe. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** 401/403, HTTP200 row error, invalid field, malformed XML/error, rate hint and canceled probe.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Host identifies failed stage and row without parsing private messages; captured diagnostics contain no employee keys/assertions.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Current error schemas and minimal authorized probe required; returned row messages remain sensitive host data.

<a id="sap-006"></a>
### SAP-006 - Contract and sandbox testing

1. **Feature ID:** SAP-006.
2. **Feature name:** Contract and sandbox testing.
3. **Description:** Separate deterministic protocol tests from actual tenant compatibility.
4. **Business value:** Separate deterministic protocol tests from actual tenant compatibility.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-TEST ExpansionTests; E-SAMPLE SAP options smoke; historical execution only; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Version entity/metadata/upsert fixtures; isolated consumer; opt-in read-only sandbox; approved writes use explicit test objects and per-row verification.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed SAP test categories/config; no runtime infrastructure.
13. **Dependencies:** SAP-001-005, SDK-018; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Offline mode, missing credentials, fixture drift, RBP denial, mixed batches and secret canaries. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Offline mode, missing credentials, fixture drift, RBP denial, mixed batches and secret canaries.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Both OS jobs pass synthetic tests; live evidence identifies product/schema/RBP/revision or explicit access blocker; no production calls by default.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Sandbox/API host/company/client/assertion/RBP and population absent.

<a id="sap-007"></a>
### SAP-007 - Additional APIs and complete business-schema validation

1. **Feature ID:** SAP-007.
2. **Feature name:** Additional APIs and complete business-schema validation.
3. **Description:** Make expansion deliberate rather than implying all SAP coverage.
4. **Business value:** Make expansion deliberate rather than implying all SAP coverage.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Planned; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP generic queries; no complete business-schema validator; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Select approved entities from current references/metadata; publish per-entity operations/keys/fields/date support; generic query syntax is not authorization or full validation.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed entity capability/schema policies; no universal all-SAP client.
13. **Dependencies:** SAP-014, SAP-006, SDK-002; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Unsupported entity/field, stale schema, required keys, missing module and denied permission. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Unsupported entity/field, stale schema, required keys, missing module and denied permission.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Each newly advertised entity has official contract, fixtures and limitations; generic enumeration remains explicitly low-level.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Current official entity specs and tenant business rules needed; metadata presence alone is insufficient.

<a id="sap-008"></a>
### SAP-008 - Employment records

1. **Feature ID:** SAP-008.
2. **Feature name:** Employment records.
3. **Description:** Preserve employment identity and dates separately from person profiles.
4. **Business value:** Preserve employment identity and dates separately from person profiles.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP EmpEmployment/EnumerateAsync<T>; no dedicated employment method; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add typed employment queries/projection, documented composite keys, multiple employments and extension data; define effective-date and absence rules.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EmpEmployment; proposed EnumerateEmploymentAsync after contract review.
13. **Dependencies:** SAP-002, SAP-004, SAP-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Concurrent employments, absent endDate, SAP date values, composite keys and invisible records. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Concurrent employments, absent endDate, SAP date values, composite keys and invisible records.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures retain employment keys/dates and multiple employments; authorized query scope and field meanings are documented.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** EmpEmployment current metadata/RBP/target population unavailable.

<a id="sap-009"></a>
### SAP-009 - Job information

1. **Feature ID:** SAP-009.
2. **Feature name:** Job information.
3. **Description:** Represent effective-dated job history accurately.
4. **Business value:** Represent effective-dated job history accurately.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP EmpJob managerId/sequence; generic enumeration; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add typed job operations/model completeness; preserve startDate/sequence identity and codes; explicit as-of/range behavior and date conversion policy.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EmpJob; proposed EnumerateJobsAsync after metadata review.
13. **Dependencies:** SAP-008, SAP-004, SAP-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Same-day sequences, future/past jobs, missing manager, string numeric sequence and unknown fields. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Same-day sequences, future/past jobs, missing manager, string numeric sequence and unknown fields.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Golden fixtures preserve sequence/effective dates without collapsing job history; query semantics match reviewed entity contract.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** EmpJob entity schema/RBP and effective-date contract needed.

<a id="sap-010"></a>
### SAP-010 - Organizational data

1. **Feature ID:** SAP-010.
2. **Feature name:** Organizational data.
3. **Description:** Read entitled organization codes and relationships.
4. **Business value:** Read entitled organization codes and relationships.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live tenant acceptance unverified.
9. **Existing implementation evidence:** Generic traversal in E-SAP; no dedicated org models/contracts; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Select actual organization entity sets from metadata; model keys/effective dates/navigation; do not guess universal SAP organization routes.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed organizational DTO/query APIs; exact entity/method only after verification.
13. **Dependencies:** SAP-014, SAP-004, SAP-015; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Retired codes, duplicate effective versions, missing parent, cycle and denied entity. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Retired codes, duplicate effective versions, missing parent, cycle and denied entity.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Each advertised entity has verified read contract and typed fixtures; no unrelated module/provider dependency.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Exact organization entity sets/version and tenant RBP unverified.

<a id="sap-011"></a>
### SAP-011 - Manager relationships

1. **Feature ID:** SAP-011.
2. **Feature name:** Manager relationships.
3. **Description:** Expose reporting lines with temporal and employment context.
4. **Business value:** Expose reporting lines with temporal and employment context.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP EmpJob.ManagerId only; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Define job/user relationship projections and documented navigation; handle missing/external/inaccessible managers and multiple employments without whole-tenant recursive scans.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing EmpJob.ManagerId; proposed typed manager relationship projection.
13. **Dependencies:** SAP-009, SAP-015; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Missing manager, self-reference/cycle, multiple assignments, denied target and date boundary. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Missing manager, self-reference/cycle, multiple assignments, denied target and date boundary.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixtures preserve temporal/identity context; inaccessible manager is explicit; traversal cannot loop indefinitely.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Navigation names and manager visibility require current metadata/RBP.

<a id="sap-012"></a>
### SAP-012 - Employee create/update operations where authorized

1. **Feature ID:** SAP-012.
2. **Feature name:** Employee create/update operations where authorized.
3. **Description:** Support intentional employee mutations with real entity permissions.
4. **Business value:** Support intentional employee mutations with real entity permissions.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP generic opt-in UpsertAsync; no employee command validation; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add operation-specific commands using supported create/update/upsert semantics; keys/date validation, explicit write opt-in and target-population enforcement by SAP.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed typed commands atop UpsertAsync where supported; no invented universal PATCH/MERGE API.
13. **Dependencies:** SAP-003, SAP-007, SAP-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Read-only entity, missing key, invalid date, row rejection and uncertain write outcome. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Read-only entity, missing key, invalid date, row rejection and uncertain write outcome.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Only verified authorized operations are exposed; default mode sends no writes; fixtures and disposable sandbox cases demonstrate documented changes.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity-specific operations/RBP and separate sandbox write authorization required [S2].

<a id="sap-013"></a>
### SAP-013 - Delta and incremental synchronization

1. **Feature ID:** SAP-013.
2. **Feature name:** Delta and incremental synchronization.
3. **Description:** Reduce extraction while protecting against missed temporal changes.
4. **Business value:** Reduce extraction while protecting against missed temporal changes.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP filter support and E-SYNC; no delta/checkpoint contract; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Use entity-documented modification fields/overlap windows and host checkpoints/deduplication; distinguish modification time from effective date; keep reconciliation fallback.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed entity-specific DeltaQuery/checkpoint adapters; no universal delta endpoint.
13. **Dependencies:** SAP-004, SAP-009, SDK-007; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Late/same-timestamp changes, timezone, overlap, failed scan, visibility shrink and future-effective records. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Late/same-timestamp changes, timezone, overlap, failed scan, visibility shrink and future-effective records.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixture overlaps deduplicate with no checkpoint advance after failed traversal; effective-date policy is explicit.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Exact modified-field/filter/retention contract per entity unverified; generic lastModifiedOn is not assumed [S3].

<a id="sap-014"></a>
### SAP-014 - Metadata and schema discovery

1. **Feature ID:** SAP-014.
2. **Feature name:** Metadata and schema discovery.
3. **Description:** Discover actual entity definitions safely.
4. **Business value:** Discover actual entity definitions safely.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP GetMetadataAsync; E-TEST XXE rejection; no schema adapter; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Disable DTD/external resolution; cap document size/depth; expose entity/property/key/type descriptors and schema hash/version caching; never auto-generate writes from visibility.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing XDocument GetMetadataAsync; proposed typed metadata projection.
13. **Dependencies:** SAP-001, SDK-009, SDK-014; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** XXE/DTD, oversized/invalid XML, missing keys, changed namespace/schema and canceled retrieval. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** XXE/DTD, oversized/invalid XML, missing keys, changed namespace/schema and canceled retrieval.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Metadata fixtures yield exact descriptors; external entities cannot resolve; changed schemas invalidate stale validators visibly.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Tenant metadata access/schema required; metadata visibility does not establish write RBP.

<a id="sap-015"></a>
### SAP-015 - Data mapping

1. **Feature ID:** SAP-015.
2. **Feature name:** Data mapping.
3. **Description:** Keep date, key and code meanings explicit in neutral outputs.
4. **Business value:** Keep date, key and code meanings explicit in neutral outputs.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP extension-preserving DTOs; no mapper interface; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Add versioned person/employment/job projections, raw-date conversion policy and source-key provenance; no ADP-to-SAP workflow.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<SapEntity,T>/MappingResult<T>.
13. **Dependencies:** SDK-002, SAP-008, SAP-009; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** SAP date representations/timezones, composite keys, masked/custom fields, invariant culture and unknown codes. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** SAP date representations/timezones, composite keys, masked/custom fields, invariant culture and unknown codes.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Golden mappings are deterministic with explicit date policy; omitted/error fields are visible and original source remains intact.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Entity schema and business profiles require review; customer-specific rules remain host-configured.

<a id="sap-016"></a>
### SAP-016 - Configuration, documentation and examples

1. **Feature ID:** SAP-016.
2. **Feature name:** Configuration, documentation and examples.
3. **Description:** Make SAP adoption independently reproducible.
4. **Business value:** Make SAP adoption independently reproducible.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live tenant acceptance unverified.
9. **Existing implementation evidence:** E-SAP options/constructor checks; README and E-SAMPLE option smoke; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Document root/company/client/assertion/RBP, effective dates, row checks and checkpoints; offline examples plus optional sandbox; consistent DI/direct validation.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing SuccessFactorsOptions/ODataQuery; proposed AddKorasSuccessFactors and examples.
13. **Dependencies:** SDK-003, SDK-014, SAP-006; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Unsafe root/date conflicts/invalid limits, missing assertion, redacted config and sample compilation. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Unsafe root/date conflicts/invalid limits, missing assertion, redacted config and sample compilation.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Clean checkout runs SAP offline without ADP; invalid settings fail before send; docs list supported operations and live limits.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** API hostname/company/client/roles must come from authorized consumer; no host guessing.

## 15. Qualtrics Feature Catalog

This connector is usable without ADP or SAP. Historical QLT-001-008 subjects remain; QLT-003b/c alias progress/download acceptance scenarios inside QLT-003. Preserve existing behavior during P2-P4; expansion is P6, eventing P7.

Official references checked November 12, 2026: [Q1: setup/token permission](https://www.qualtrics.com/support/integrations/api-integration/overview/), [Q2: survey list](https://developer-docs.qualtrics.com/docs/public-api/surveys/list-surveys/), [Q3: export start](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/create-export/), [Q4: export progress](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-progress/), [Q5: file download](https://developer-docs.qualtrics.com/docs/public-api/responseImportsExports/get-export-file/), [Q6: contacts](https://developer-docs.qualtrics.com/docs/public-api/contacts/contacts/), [Q7: mailing lists](https://developer-docs.qualtrics.com/docs/public-api/mailingLists/mailing-lists/), [Q8: event subscriptions](https://developer-docs.qualtrics.com/docs/public-api/eventSubscriptions/event-subscriptions-api/). Survey/export routes were readable; family pages do not supply all operation schemas. Segment page fetch failed. Unverified directory/participant/batch/event routes remain unspecified until independently checked.

Verified sequence: GET /API/v3/surveys; POST /API/v3/surveys/{surveyId}/export-responses; GET its /{progressId}; then GET /{fileId}/file. Documented export formats/constraints apply; status complete and fileId, not percentComplete alone, determine readiness. Stream bytes as returned; host owns archive extraction. Exact account quotas and permissions remain unverified.

| ID | Feature | Priority | Phase | Current status |
|---|---|---|---|---|
| [QLT-001](#qlt-001) | Authentication and API token configuration | P0 | P6 | Partially implemented |
| [QLT-002](#qlt-002) | Survey APIs | P0 | P6 | Partially implemented |
| [QLT-003](#qlt-003) | Response export APIs | P0 | P6 | Partially implemented |
| [QLT-004](#qlt-004) | Pagination and transport resilience | P0 | P6 | Partially implemented |
| [QLT-005](#qlt-005) | Typed responses, errors and diagnostics | P0 | P6 | Partially implemented |
| [QLT-006](#qlt-006) | Directory and contact operations | P1 | P6 | Not started |
| [QLT-007](#qlt-007) | Events and webhook capabilities | P2 | P7 | Not started |
| [QLT-008](#qlt-008) | Contract and sandbox testing | P0 | P6 | Partially implemented |
| [QLT-009](#qlt-009) | Mailing lists and segments | P1 | P6 | Not started |
| [QLT-010](#qlt-010) | Participant management | P1 | P6 | Not started |
| [QLT-011](#qlt-011) | Batch operations | P1 | P6 | Not started |
| [QLT-012](#qlt-012) | Data mapping | P0 | P6 | Partially implemented |
| [QLT-013](#qlt-013) | Rate-limit handling | P0 | P6 | Partially implemented |
| [QLT-014](#qlt-014) | Documentation and examples | P0 | P6 | Partially implemented |
| [QLT-015](#qlt-015) | Contact-specific typed schema and consent | P1 | P6 | Not started |
| [QLT-016](#qlt-016) | API client foundation and configuration | P0 | P6 | Partially implemented |

<a id="qlt-001"></a>
### QLT-001 - Authentication and API token configuration

1. **Feature ID:** QLT-001.
2. **Feature name:** Authentication and API token configuration.
3. **Description:** Authenticate against the correct data center without leaking tokens.
4. **Business value:** Authenticate against the correct data center without leaking tokens.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT Create; E-TRANSPORT HeaderTokenAuthenticator; E-TEST QualtricsUsesOnlyApiTokenAndPagesSurveys; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Use rotating host X-API-TOKEN source, reject blank/newline tokens and disable redirects; OAuth remains a separately verified extension, not interchangeable silently.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing QualtricsClient.Create/IAccessTokenSource; proposed DI token registration.
13. **Dependencies:** SDK-005, SDK-006, SDK-004; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Header injection, empty source, rotation, off-origin links, redirects, cross-account isolation and redaction. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Header injection, empty source, rotation, off-origin links, redirects, cross-account isolation and redaction.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Create adds X-API-TOKEN without bearer; separate accounts cannot exchange credentials; Access API requirements are documented.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** API feature/Access API permission and authorized token/data center absent [Q1].

<a id="qlt-002"></a>
### QLT-002 - Survey APIs

1. **Feature ID:** QLT-002.
2. **Feature name:** Survey APIs.
3. **Description:** Read permitted survey summaries and definitions.
4. **Business value:** Read permitted survey summaries and definitions.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT EnumerateSurveysAsync/GetSurveyAsync/SurveySummary; E-TEST survey paging; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Preserve typed summaries/extensions, safely encode IDs and document raw design ownership; listing does not imply full survey access.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing GET /surveys and GET /surveys/{surveyId}; proposed typed design subset after schema verification.
13. **Dependencies:** QLT-001, QLT-004, QLT-005; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Empty/malformed list, unknown fields, invalid ID, private survey 403/404 and cancellation. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Empty/malformed list, unknown fields, invalid ID, private survey 403/404 and cancellation.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** All fixture summaries/unknown data survive; denied design access reports classified error; no survey mutation implied.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Survey ownership/sharing and full current design schema unverified; list route readable [Q2].

<a id="qlt-003"></a>
### QLT-003 - Response export APIs

1. **Feature ID:** QLT-003.
2. **Feature name:** Response export APIs.
3. **Description:** Retrieve response datasets with resumable export jobs.
4. **Business value:** Retrieve response datasets with resumable export jobs.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT start/progress/wait/download; E-TEST QualtricsExportUsesProgressThenFileIdAndStreamsWithoutExtraction; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Keep create -> progress -> file sequence, validated format/date/filter and bounded polling; stop failed/404/unknown states; never restart uncertain POST automatically.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing StartResponseExportAsync/GetExportProgressAsync/WaitForExportAsync/DownloadExportAsync and typed options/progress.
13. **Dependencies:** QLT-001, QLT-004, QLT-013, SDK-010; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** inProgress/complete/failed/unknown, missing IDs, bounds, cancellation, partial bytes and no POST replay. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** inProgress/complete/failed/unknown, missing IDs, bounds, cancellation, partial bytes and no POST replay.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Download only complete with fileId; IDs are not interchanged; bytes remain unchanged/stream stays open; resume existing progress after poll timeout.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Response permission, current export constraints/filters and account limits required [Q3-Q5]; direct response writes not claimed.

<a id="qlt-004"></a>
### QLT-004 - Pagination and transport resilience

1. **Feature ID:** QLT-004.
2. **Feature name:** Pagination and transport resilience.
3. **Description:** Traverse results without credential forwarding or infinite loops.
4. **Business value:** Traverse results without credential forwarding or infinite loops.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT nextPage/maxPages; E-SAFETY; E-TEST hostile continuation tests; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Preserve valid nextPage within configured origin/API path; cycle/page guards, neutral adapter and query-bound resume; no provider-wide offset guess.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing survey iterator/TransportOptions; proposed per-family Page<T> adapter.
13. **Dependencies:** SDK-007, SDK-008, SDK-010; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Host/path/protocol changes, repeated URL, empty page with next link, expired cursor and canceled delay. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Host/path/protocol changes, repeated URL, empty page with next link, expired cursor and canceled delay.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Next links are preserved and hostile links rejected before send; future directory/batch families use their own verified paging contracts.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Current per-family cursor/limit/expiration schemas required; no universal pagination assumption.

<a id="qlt-005"></a>
### QLT-005 - Typed responses, errors and diagnostics

1. **Feature ID:** QLT-005.
2. **Feature name:** Typed responses, errors and diagnostics.
3. **Description:** Expose stable results and safe actionable failures.
4. **Business value:** Expose stable results and safe actionable failures.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT SurveySummary/ExportProgress/options; E-TRANSPORT; no dedicated diagnostic API; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Add sanitized requestId/provider code metadata and staged read-only probe; preserve typed/unknown fields; raw documents remain caller-owned.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed QualtricsApiException/DiagnoseAsync; existing typed models/JsonDocument.
13. **Dependencies:** SDK-009, SDK-011, QLT-002; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** 401/403/404/429, malformed result/error meta, hostile request IDs, private messages and unknown export state. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** 401/403/404/429, malformed result/error meta, hostile request IDs, private messages and unknown export state.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Host distinguishes auth/throttle/schema failure by safe fields; captured logs contain no token/respondent/record data.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Current error schemas and authorized minimal probe needed; no invented diagnostics endpoint.

<a id="qlt-006"></a>
### QLT-006 - Directory and contact operations

1. **Feature ID:** QLT-006.
2. **Feature name:** Directory and contact operations.
3. **Description:** Manage an authorized directory independently.
4. **Business value:** Manage an authorized directory independently.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** No directory/contact methods in E-QLT; legacy QLT-006 directory/distribution writes planned; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Verify XM Directory operations then add typed list/get/create/update/contact methods; explicit opt-in mutations and capability descriptors; destructive calls need separate verified contracts.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed directory/contact clients; exact routes/verbs/schemas from [Q6], not survey APIs.
13. **Dependencies:** QLT-001, QLT-004, QLT-005; contract/schema design precedes QLT-012 mapping; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Invalid IDs, duplicate external IDs, missing fields, 403/404, opt-out/consent and uncertain write outcome. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Invalid IDs, duplicate external IDs, missing fields, 403/404, opt-out/consent and uncertain write outcome.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each exposed operation has official schema/permission evidence; defaults are read-only; write tests never blindly replay or disclose PII.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** XM Directory license/permissions and current operation schemas required; docs availability is not entitlement.

<a id="qlt-007"></a>
### QLT-007 - Events and webhook capabilities

1. **Feature ID:** QLT-007.
2. **Feature name:** Events and webhook capabilities.
3. **Description:** React to documented events with host-owned durable delivery.
4. **Business value:** React to documented events with host-owned durable delivery.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P2.
7. **Planned development phase:** P7; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** No event/subscription/receiver code; legacy QLT-007; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Choose verified event types, subscription operations, envelope/auth/verification and delivery identity; host owns endpoint/TLS/inbox and deduplication.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed event DTO/client/helpers only after [Q8] operation verification; no embedded server.
13. **Dependencies:** QLT-001, QLT-005, QLT-008; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Unsupported type, forged/stale/duplicate envelope, retries, missing identity, permission denial and explicit opt-in. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Unsupported type, forged/stale/duplicate envelope, retries, missing identity, permission denial and explicit opt-in.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Verified fixtures/auth behavior pass; docs state actual integrity/freshness guarantees and host obligations without borrowing ADP HMAC assumptions.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Current full subscription contract/auth/event schemas and tenant eligibility unverified; family index alone insufficient.

<a id="qlt-008"></a>
### QLT-008 - Contract and sandbox testing

1. **Feature ID:** QLT-008.
2. **Feature name:** Contract and sandbox testing.
3. **Description:** Separate mock protocol evidence from actual account compatibility.
4. **Business value:** Separate mock protocol evidence from actual account compatibility.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-TEST ExpansionTests; E-SAMPLE options smoke; no live harness; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Version survey/export/contact fixtures; independent consumer; opt-in read-only sandbox and separately authorized write fixtures.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed Qualtrics test categories/config; no required runtime host.
13. **Dependencies:** QLT-001-005, SDK-018; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Offline/missing-token behavior, schema drift, continuation cycles, leaked errors and interrupted download. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Offline/missing-token behavior, schema drift, continuation cycles, leaked errors and interrupted download.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Both OSes pass synthetic tests with provenance; live evidence identifies account/API/permissions/revision or blocker; CI never calls production by default.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Data center/token/permitted survey/directory and disposable records absent.

<a id="qlt-009"></a>
### QLT-009 - Mailing lists and segments

1. **Feature ID:** QLT-009.
2. **Feature name:** Mailing lists and segments.
3. **Description:** Organize contact groups without conflating static lists and dynamic segments.
4. **Business value:** Organize contact groups without conflating static lists and dynamic segments.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** No list/segment models or methods in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Verify list membership and segment queries separately; typed IDs/results, authorized reads and explicit writes; preserve consent/opt-out.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed list/segment adapters from current schemas [Q7]; segment retrieval incomplete.
13. **Dependencies:** QLT-006, QLT-004, QLT-012; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Missing list, dynamic changes, duplicate membership, consent changes, partial failure and denied permission. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Missing list, dynamic changes, duplicate membership, consent changes, partial failure and denied permission.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each advertised method has exact current contract/tests; unsupported segment operations remain unknown/absent, never simulated.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** List family documented; segments page fetch failed; actual operation schemas/licenses/permissions unverified.

<a id="qlt-010"></a>
### QLT-010 - Participant management

1. **Feature ID:** QLT-010.
2. **Feature name:** Participant management.
3. **Description:** Support product-specific participant workflows with consent.
4. **Business value:** Support product-specific participant workflows with consent.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** No participant/distribution methods in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Identify actual product (XM Directory/distributions/EX) before selecting APIs; distinguish contact/participant/response IDs; writes/message sends disabled by default.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed product-specific commands/models only after official verification; no endpoint guessed.
13. **Dependencies:** QLT-006, QLT-009, QLT-008; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Wrong identity type, opt-out, duplicates, denied access and unknown submission outcome. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Wrong identity type, opt-out, duplicates, denied access and unknown submission outcome.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Chosen product contract/fixtures show documented participant behavior; default tests/examples never send distributions/messages.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Product/family/license/current endpoints and scopes unspecified; missing research is not vendor-proven unavailability.

<a id="qlt-011"></a>
### QLT-011 - Batch operations

1. **Feature ID:** QLT-011.
2. **Feature name:** Batch operations.
3. **Description:** Support volume with bounded requests and visible item outcomes.
4. **Business value:** Support volume with bounded requests and visible item outcomes.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** E-SYNC BatchAsync is local chunking; no remote Qualtrics batch; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Choose verified bulk/contact-import contract; model async status/item results/idempotency/limits; local chunks are not a remote batch API.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed family-specific commands/results after schema verification; existing local BatchAsync remains neutral.
13. **Dependencies:** QLT-006, QLT-013, SDK-007; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Batch limits, duplicates, partial failure, expired job, poll interruption, ambiguous POST and cancellation. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Batch limits, duplicates, partial failure, expired job, poll interruption, ambiguous POST and cancellation.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each item has documented outcome; bounded resume does not silently start a new write or introduce scheduler/persistence.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Remote routes/formats/quotas/eligibility unverified; family names do not justify generic batch URLs.

<a id="qlt-012"></a>
### QLT-012 - Data mapping

1. **Feature ID:** QLT-012.
2. **Feature name:** Data mapping.
3. **Description:** Map selected data without losing identity or consent semantics.
4. **Business value:** Map selected data without losing identity or consent semantics.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT extension-preserving models; E-EXPORT projections; no mapper; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Add opt-in neutral profiles and null/timezone/code/embedded-data rules; preserve unknown fields and sensitive omissions; no cross-provider workflow.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<QualtricsEntity,T>/MappingResult<T>; raw downloads remain raw.
13. **Dependencies:** SDK-002, SDK-013, QLT-002, QLT-006; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Null/unknown fields, mixed response schemas, Unicode, embedded keys, identity distinctions and consent fields. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Null/unknown fields, mixed response schemas, Unicode, embedded keys, identity distinctions and consent fields.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Golden cases preserve selected identity/consent deterministically; raw file bytes change only when caller explicitly parses/maps them.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Product contact/response schema and consent review required; no universal normalized response model.

<a id="qlt-013"></a>
### QLT-013 - Rate-limit handling

1. **Feature ID:** QLT-013.
2. **Feature name:** Rate-limit handling.
3. **Description:** Avoid read storms and duplicate unsafe submissions.
4. **Business value:** Avoid read storms and duplicate unsafe submissions.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-TRANSPORT GET 429/Retry-After; no quota/concurrency model; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Provide bounded configured concurrency and safe throttle hints; respect server delay/total budget; never hardcode universal brand quota or retry POST automatically.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing TransportOptions; proposed quota/concurrency policy and safe metadata.
13. **Dependencies:** SDK-008, QLT-004, QLT-005; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** 429, long/date/malformed hints, concurrent callers, queue cancellation and POST throttle. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** 429, long/date/malformed hints, concurrent callers, queue cancellation and POST throttle.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Safe reads wait server minimum or fail budget; configured concurrency bounded; writes remain one automatic attempt with explicit reconciliation guidance.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Exact account/family/brand quotas not verified; obtain official limits rather than invent requests/minute.

<a id="qlt-014"></a>
### QLT-014 - Documentation and examples

1. **Feature ID:** QLT-014.
2. **Feature name:** Documentation and examples.
3. **Description:** Make survey/export and later directory adoption reproducible.
4. **Business value:** Make survey/export and later directory adoption reproducible.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** README example; E-SAMPLE options smoke; no dedicated end-to-end example; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Offline survey/export flow with resume/cancellation and sensitive-download guidance; data center/Access API setup, per-family rights and limits.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed samples/Qualtrics.SurveyExport and opt-in directory example; no orchestration service.
13. **Dependencies:** QLT-008, QLT-003, QLT-016; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Clean-checkout compile/offline execution, bad settings, interrupted resume and token/PII canaries. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Clean-checkout compile/offline execution, bad settings, interrupted resume and token/PII canaries.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Example runs without SAP/ADP and sends no messages by default; synthetic behavior is clearly distinguished from live account validation.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Live mode needs authorized token/data center/survey; offline example unblocked.

<a id="qlt-015"></a>
### QLT-015 - Contact-specific typed schema and consent

1. **Feature ID:** QLT-015.
2. **Feature name:** Contact-specific typed schema and consent.
3. **Description:** Avoid unsafe contact mutations and ambiguous field presence.
4. **Business value:** Avoid unsafe contact mutations and ambiguous field presence.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Not started; live account acceptance unverified.
9. **Existing implementation evidence:** No Contact/Directory DTO in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Review contact identity/custom fields/consent/opt-out/extension data; bind field requiredness/access to actual directory contract and preserve null versus omission.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed QualtricsContact/ContactChange validation after [Q6] schema review.
13. **Dependencies:** QLT-006, QLT-012; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Missing/duplicate external ID, custom fields, explicit null/omission, opted-out contact and sensitive attributes. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Missing/duplicate external ID, custom fields, explicit null/omission, opted-out contact and sensitive attributes.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Model/mapper preserve field presence and consent; unsupported changes are explicit; no implicit unsubscribe/resubscribe behavior.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Tenant schema/permissions/consent policy required; no customer contact data used in P1.

<a id="qlt-016"></a>
### QLT-016 - API client foundation and configuration

1. **Feature ID:** QLT-016.
2. **Feature name:** API client foundation and configuration.
3. **Description:** Keep Qualtrics independently installable and configured safely.
4. **Business value:** Keep Qualtrics independently installable and configured safely.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** Partially implemented; live account acceptance unverified.
9. **Existing implementation evidence:** E-QLT options/Create/root checks; E-SAFETY; no DI extension; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Consistent DI/direct construction, validated /API/v3 root, token/HTTP ownership, bounded options and capability descriptor; preserve preview methods.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing QualtricsClient/QualtricsOptions; proposed AddKorasQualtrics/descriptor.
13. **Dependencies:** SDK-001, SDK-002, SDK-003, SDK-014; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Unsafe roots, bad maxPages, duplicate names, wrong auth adapter, disposal and account isolation. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Unsafe roots, bad maxPages, duplicate names, wrong auth adapter, disposal and account isolation.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Independent consumer uses only neutral/shared packages; invalid config fails before send; no inferred data center or entitlement.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Exact data-center root and product access are supplied by consumer; no account provisioning.

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

| Whole-feature implementation state | Count | Feature IDs |
|---|---|---|
| Partially implemented | 57 | SDK-001, SDK-003, SDK-004, SDK-005, SDK-006, SDK-007, SDK-008, SDK-009, SDK-010, SDK-011, SDK-012, SDK-013, SDK-014, SDK-016, SDK-018, SDK-019, ADP-003, ADP-005, ADP-006, ADP-007, ADP-008, ADP-009, ADP-010, ADP-013, ADP-014, ADP-015, ADP-016, ADP-017, ADP-018, ADP-019, ADP-020, ADP-021, ADP-022, SAP-001, SAP-002, SAP-003, SAP-004, SAP-005, SAP-006, SAP-008, SAP-009, SAP-011, SAP-012, SAP-013, SAP-014, SAP-015, SAP-016, QLT-001, QLT-002, QLT-003, QLT-004, QLT-005, QLT-008, QLT-012, QLT-013, QLT-014, QLT-016 |
| Not started | 12 | SDK-002, SDK-015, SDK-017, SDK-020, ADP-012, SAP-010, QLT-006, QLT-007, QLT-009, QLT-010, QLT-011, QLT-015 |
| Implemented but unverified | 4 | ADP-001, ADP-002, ADP-004, ADP-011 |
| Planned | 1 | SAP-007 |
| Tested/Accepted as complete master requirements in P1 | 0 | Historical synthetic results remain separately recorded |

Live external verification is unverified/blocked for all provider features requiring access; these blockers overlap implementation states and must not be added as extra feature counts. Four implemented source slices (ADP-001/002/004/011) are unverified in this audit. The remaining 57 partial features, 12 not-started features and one planned feature have explicit gaps above.

| Feature ID | Name | Priority | Phase | Current implementation state |
|---|---|---|---|---|
| [SDK-001](#sdk-001) | .NET 10 solution and package structure | P0 | P2 | Partially implemented |
| [SDK-002](#sdk-002) | Connector interface contracts | P0 | P2 | Not started |
| [SDK-003](#sdk-003) | Dependency injection extensions | P0 | P2 | Partially implemented |
| [SDK-004](#sdk-004) | HttpClientFactory integration | P0 | P2 | Partially implemented |
| [SDK-005](#sdk-005) | Authentication abstraction | P0 | P2 | Partially implemented |
| [SDK-006](#sdk-006) | Secure credential providers | P0 | P2 | Partially implemented |
| [SDK-007](#sdk-007) | Pagination abstractions | P0 | P2 | Partially implemented |
| [SDK-008](#sdk-008) | Retry and resilience policies | P0 | P2 | Partially implemented |
| [SDK-009](#sdk-009) | Structured exception hierarchy | P0 | P2 | Partially implemented |
| [SDK-010](#sdk-010) | Cancellation and timeout support | P0 | P2 | Partially implemented |
| [SDK-011](#sdk-011) | Logging and correlation IDs | P0 | P2 | Partially implemented |
| [SDK-012](#sdk-012) | OpenTelemetry instrumentation | P1 | P7 | Partially implemented |
| [SDK-013](#sdk-013) | Export provider interfaces | P0 | P3 | Partially implemented |
| [SDK-014](#sdk-014) | Configuration validation | P0 | P2 | Partially implemented |
| [SDK-015](#sdk-015) | Package signing and provenance | P1 | P7 | Not started |
| [SDK-016](#sdk-016) | NuGet publishing workflow | P0 | P4 | Partially implemented |
| [SDK-017](#sdk-017) | API compatibility checks | P0 | P2-P4 | Not started |
| [SDK-018](#sdk-018) | Security scanning and SBOM | P0 | P4 | Partially implemented |
| [SDK-019](#sdk-019) | Versioning and release notes | P0 | P4 | Partially implemented |
| [SDK-020](#sdk-020) | SDK documentation website | P1 | P7 | Not started |
| [ADP-001](#adp-001) | Certificate and mTLS authentication | P0 | P2 | Implemented but unverified |
| [ADP-002](#adp-002) | OAuth token acquisition and refresh | P0 | P2 | Implemented but unverified |
| [ADP-003](#adp-003) | Secure client credentials configuration | P0 | P2 | Partially implemented |
| [ADP-004](#adp-004) | Worker profile retrieval | P0 | P3 | Implemented but unverified |
| [ADP-005](#adp-005) | Worker pagination and filtering | P0 | P3 | Partially implemented |
| [ADP-006](#adp-006) | Worker changes and incremental sync | P0 | P3 | Partially implemented |
| [ADP-007](#adp-007) | Worker schema mapping | P0 | P3 | Partially implemented |
| [ADP-008](#adp-008) | CSV/JSON/XML export | P0 | P3 | Partially implemented |
| [ADP-009](#adp-009) | Excel export | P1 | P3 | Partially implemented |
| [ADP-010](#adp-010) | Structured errors and rate-limit handling | P0 | P3 | Partially implemented |
| [ADP-011](#adp-011) | Retry, timeout, and cancellation | P0 | P3 | Implemented but unverified |
| [ADP-012](#adp-012) | Connection diagnostics | P0 | P3 | Not started |
| [ADP-013](#adp-013) | ADP organization and manager data | P1 | P3 extension | Partially implemented |
| [ADP-014](#adp-014) | Employment status and lifecycle events | P1 | P3 extension | Partially implemented |
| [ADP-015](#adp-015) | Payroll operations, where entitled | P1 | P3 extension | Partially implemented |
| [ADP-016](#adp-016) | Time and attendance APIs | P2 | P7 | Partially implemented |
| [ADP-017](#adp-017) | Benefits APIs | P2 | P7 | Partially implemented |
| [ADP-018](#adp-018) | Webhook/event subscriptions | P2 | P3 primitives | Partially implemented |
| [ADP-019](#adp-019) | Typed API response models | P0 | P3 | Partially implemented |
| [ADP-020](#adp-020) | Sandbox and contract test suite | P0 | P3-P4 | Partially implemented |
| [ADP-021](#adp-021) | API capability/entitlement matrix | P0 | P1 baseline | Partially implemented |
| [ADP-022](#adp-022) | Documentation and runnable examples | P0 | P3-P4 | Partially implemented |
| [SAP-001](#sap-001) | OAuth and supported authentication mechanisms | P0 | P5 | Partially implemented |
| [SAP-002](#sap-002) | OData API foundation and employee/person data | P0 | P5 | Partially implemented |
| [SAP-003](#sap-003) | Upsert operations | P0 | P5 | Partially implemented |
| [SAP-004](#sap-004) | Pagination and filtering | P0 | P5 | Partially implemented |
| [SAP-005](#sap-005) | Error handling and diagnostics | P0 | P5 | Partially implemented |
| [SAP-006](#sap-006) | Contract and sandbox testing | P0 | P5 | Partially implemented |
| [SAP-007](#sap-007) | Additional APIs and complete business-schema validation | P1 | P5 | Planned |
| [SAP-008](#sap-008) | Employment records | P0 | P5 | Partially implemented |
| [SAP-009](#sap-009) | Job information | P0 | P5 | Partially implemented |
| [SAP-010](#sap-010) | Organizational data | P1 | P5 | Not started |
| [SAP-011](#sap-011) | Manager relationships | P1 | P5 | Partially implemented |
| [SAP-012](#sap-012) | Employee create/update operations where authorized | P1 | P5 | Partially implemented |
| [SAP-013](#sap-013) | Delta and incremental synchronization | P0 | P5 | Partially implemented |
| [SAP-014](#sap-014) | Metadata and schema discovery | P0 | P5 | Partially implemented |
| [SAP-015](#sap-015) | Data mapping | P0 | P5 | Partially implemented |
| [SAP-016](#sap-016) | Configuration, documentation and examples | P0 | P5 | Partially implemented |
| [QLT-001](#qlt-001) | Authentication and API token configuration | P0 | P6 | Partially implemented |
| [QLT-002](#qlt-002) | Survey APIs | P0 | P6 | Partially implemented |
| [QLT-003](#qlt-003) | Response export APIs | P0 | P6 | Partially implemented |
| [QLT-004](#qlt-004) | Pagination and transport resilience | P0 | P6 | Partially implemented |
| [QLT-005](#qlt-005) | Typed responses, errors and diagnostics | P0 | P6 | Partially implemented |
| [QLT-006](#qlt-006) | Directory and contact operations | P1 | P6 | Not started |
| [QLT-007](#qlt-007) | Events and webhook capabilities | P2 | P7 | Not started |
| [QLT-008](#qlt-008) | Contract and sandbox testing | P0 | P6 | Partially implemented |
| [QLT-009](#qlt-009) | Mailing lists and segments | P1 | P6 | Not started |
| [QLT-010](#qlt-010) | Participant management | P1 | P6 | Not started |
| [QLT-011](#qlt-011) | Batch operations | P1 | P6 | Not started |
| [QLT-012](#qlt-012) | Data mapping | P0 | P6 | Partially implemented |
| [QLT-013](#qlt-013) | Rate-limit handling | P0 | P6 | Partially implemented |
| [QLT-014](#qlt-014) | Documentation and examples | P0 | P6 | Partially implemented |
| [QLT-015](#qlt-015) | Contact-specific typed schema and consent | P1 | P6 | Not started |
| [QLT-016](#qlt-016) | API client foundation and configuration | P0 | P6 | Partially implemented |

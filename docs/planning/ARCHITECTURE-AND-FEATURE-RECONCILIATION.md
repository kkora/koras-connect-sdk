# Architecture and feature-plan reconciliation

Status: documentation and implementation-plan PR; missing features are NOT implemented by this change.

Reviewed November 12, 2026 against develop commit 991eb8ccfeca8340624f1e6602082ef58085c082. Preserve the exact feature IDs, meanings and priorities below from the owner's supplied plan. Older documents reuse some IDs for different meanings; those identifiers must be reconciled explicitly rather than silently treated as equivalent.

## Eight-package target versus current state

| Target package | Responsibility | Current state |
|---|---|---|
| Koras.Connect.Abstractions | Public connector interfaces, models, capabilities | Separate package and common connector/capability contracts missing |
| Koras.Connect.Core | HTTP pipeline, pagination, retries, exceptions | Current transport/resilience consolidated into Koras.Connect |
| Koras.Connect.Authentication | OAuth, certificates, token management | Current authentication consolidated into Koras.Connect |
| Koras.Connect.Export | CSV, JSON, XML, Excel exporters | Static exporters inside Koras.Connect; provider interfaces missing |
| Koras.Connect.Observability | Logging, metrics, tracing | Hooks inside Koras.Connect; separate package missing |
| Koras.Connect.ADP | ADP-specific integration | Current provider package/project is Koras.Connect.Adp |
| Koras.Connect.SAP.SuccessFactors | SAP SuccessFactors integration | Current provider package/project is Koras.Connect.Sap.SuccessFactors |
| Koras.Connect.Qualtrics | Qualtrics integration | Separate provider package exists |

Four packages currently exist. Namespace separation is not independent package installation/dependency isolation. Treat the eight-package layout as the correction target, not a description of deployed code. Record naming/casing and API/assembly migration decisions before moving files; do not create case-only duplicate project directories on Windows. Do not resurrect superseded prototype projects just to obtain matching folder names.

Release ordering: prioritize Abstractions, Core, Authentication, Export and ADP. SAP/Qualtrics already exist as P0 subsets; preserve their tested behavior while stabilizing shared contracts. Define the Observability package boundary alongside the foundation. No CLI/Cloud runtime dependencies may enter any SDK package.

## ADP acceptance matrix

Implemented means source exists for the stated scope; it is not live vendor certification or proof of tenant entitlement. Partial means material requirements remain. Missing implementation, unverified vendor contracts, entitlement restrictions and verified vendor limitations must be recorded separately.

| Feature ID | Original requirement | Priority | Current finding | Evidence |
|---|---|---|---|---|
| ADP-001 | Certificate and mTLS authentication | P0 | Implemented; live mTLS unverified | ClientCertificateTransport.cs; certificate tests |
| ADP-002 | OAuth token acquisition and refresh | P0 | Implemented via token reacquisition/renewal | ClientCredentialsTokenSource.cs |
| ADP-003 | Secure client credentials configuration | P0 | Implemented validation/redaction; secret storage is host-owned | ClientCredentialsOptions; request authentication |
| ADP-004 | Worker profile retrieval | P0 | Implemented subset; live tenant unverified | AdpWorkersClient.GetWorkerAsync |
| ADP-005 | Worker pagination and filtering | P0 | Implemented basic paging and escaped filters | AdpWorkersClient |
| ADP-006 | Worker changes and incremental sync | P0 | Partial: snapshot fingerprints and event primitives | Reconciliation.cs; AdpOperationsClient |
| ADP-007 | Worker schema mapping | P0 | Partial: typed fields and caller mappings; no mapping-provider contract | Worker.cs; tabular exports |
| ADP-008 | CSV/JSON/XML export | P0 | Implemented | TabularExport.cs |
| ADP-009 | Excel export | P1 | Implemented basic XLSX | TabularExport.WriteExcel |
| ADP-010 | Structured errors and rate-limit handling | P0 | Partial: HTTP errors/throttling; limited provider error models | IntegrationTransport.cs |
| ADP-011 | Retry, timeout, and cancellation | P0 | Implemented | IntegrationTransport.cs |
| ADP-012 | Connection diagnostics | P0 | Missing dedicated connection diagnostic/test API | No current TestConnection/Diagnose API |
| ADP-013 | ADP organization and manager data | P1 | Partial: department reads and JSON assignment fragments | Worker.cs; AdpOperationCatalog |
| ADP-014 | Employment status and lifecycle events | P1 | Partial: low-level dispatch; incomplete typed business contracts | AdpOperationsClient |
| ADP-015 | Payroll operations, where entitled | P1 | Partial: selected operations; entitlements unverified | AdpOperationCatalog |
| ADP-016 | Time and attendance APIs | P2 | Partial: time-off only; broader attendance not implemented | AdpOperationCatalog |
| ADP-017 | Benefits APIs | P2 | Partial: spending-account enrollment subset | ReadSpendingAccountEnrollmentsAsync |
| ADP-018 | Webhook/event subscriptions | P2 | Partial: polling/ack/auth helpers; subscription management absent | AdpWebhookAuthentication; AdpOperationsClient |
| ADP-019 | Typed API response models | P0 | Partial: workers typed; other operations largely raw JSON | Worker.cs; AdpOperationsClient |
| ADP-020 | Sandbox and contract test suite | P0 | Synthetic tests implemented; live sandbox unverified | tests/Koras.Connect.Tests |
| ADP-021 | API capability/entitlement matrix | P0 | Partial: endpoint catalog; exact grants/tenant evidence incomplete | ADP-FEATURE-MATRIX.md |
| ADP-022 | Documentation and runnable examples | P0 | Partial: README/smoke consumer; dedicated ADP examples missing | samples/PackageConsumer |

Evidence paths are relative to src/Koras.Connect, src/Koras.Connect.Adp, docs/features or the repository test tree as appropriate. The current ADP endpoint matrix remains the detailed operation catalog; this matrix restores requirement traceability without changing endpoint claims.

## Shared SDK acceptance matrix

| Feature ID | Original requirement | Priority | Current finding |
|---|---|---|---|
| SDK-001 | .NET 10 solution and package structure | P0 | Partial: .NET 10 exists; eight-package target not implemented |
| SDK-002 | Connector interface contracts | P0 | Missing common connector/capability contracts |
| SDK-003 | Dependency injection extensions | P0 | Partial: ADP registration only |
| SDK-004 | HttpClientFactory integration | P0 | Implemented for ADP |
| SDK-005 | Authentication abstraction | P0 | Implemented: IAccessTokenSource and IRequestAuthenticator |
| SDK-006 | Secure credential providers | P0 | Partial: token injection; dedicated credential-provider API absent |
| SDK-007 | Pagination abstractions | P0 | Partial: provider iterators; no common page/checkpoint contracts |
| SDK-008 | Retry and resilience policies | P0 | Implemented basic bounded GET retries/backoff |
| SDK-009 | Structured exception hierarchy | P0 | Partial: limited HTTP exception types |
| SDK-010 | Cancellation and timeout support | P0 | Implemented |
| SDK-011 | Logging and correlation IDs | P0 | Partial: tracing/selected headers; logging/correlation contract incomplete |
| SDK-012 | OpenTelemetry instrumentation | P1 | Implemented compatible ActivitySource/Meter hooks |
| SDK-013 | Export provider interfaces | P0 | Missing; current exporters are static |
| SDK-014 | Configuration validation | P0 | Partial: principal options validated; consistency review needed |
| SDK-015 | Package signing and provenance | P1 | Missing signing/build-attestation automation |
| SDK-016 | NuGet publishing workflow | P0 | Workflow implemented; public publication disabled/unexecuted |
| SDK-017 | API compatibility checks | P0 | Missing baseline/API compatibility automation |
| SDK-018 | Security scanning and SBOM | P0 | Partial: dependency audit/SBOM/focused hygiene checks |
| SDK-019 | Versioning and release notes | P0 | Partial: preview versions; changelog/releases absent at audit |
| SDK-020 | SDK documentation website | P1 | No website source/build/publish workflow in repository |

Source evidence: IntegrationTransport.cs, RequestAuthentication.cs, AdpServiceCollectionExtensions.cs, TabularExport.cs, Directory.Build.props, .github/workflows/ci.yml, .github/workflows/release.yml and eng/*.py at the audited commit. The only current public interfaces in the source tree are IAccessTokenSource and IRequestAuthenticator. Passing build/test/package-consumer checks is not a baseline API compatibility check.

## Requested repository organization still to reconcile

- Add Directory.Packages.props with centrally managed versions.
- Establish .github/CODEOWNERS with the actual owner/review policy; do not invent reviewers.
- Establish security.yml with appropriate code/dependency/secret scanning; current focused PEM/path checks are not comprehensive security scanning.
- Publishing currently lives in release.yml. Renaming to publish-nuget.yml is organizational; retaining equivalent guarded functionality matters more than a filename.
- Organize Unit, Contract, Integration and Security tests with meaningful coverage and explicit live-test prerequisites. Folder names alone are not proof of coverage.
- Add minimal runnable samples ADP.WorkerExport, ADP.Authentication and ADP.ConnectionTest; PackageConsumer remains a packaging smoke test, not a complete ADP tutorial.
- Organize docs/architecture, docs/features, docs/providers, docs/security and docs/decisions, with links from the README.
- Replace stale live-status statements such as retaining already-merged PR #1 as a draft; retain historical verification evidence with dates instead of presenting it as current state.

## Staged correction work

1. **Traceability:** reconcile current architecture/master/ADP documents with these exact IDs. Every ID needs owner requirement, implementation location, validation evidence, status and remaining work. Do not overwrite the user's meaning of an ID.
2. **Contracts and package boundaries:** design the common connector/capability, credential, pagination/checkpoint, structured error, export, mapping and diagnostic contracts; establish the eight projects and an acyclic dependency graph. Abstractions must not depend on provider implementations. Document migration from Koras.Connect before moving public types or dropping compatibility.
3. **P0 implementation:** complete shared interfaces, consistent DI/factory/config validation, safe diagnostics/correlation, dedicated examples and meaningful contract/security tests. Preserve current working provider behavior.
4. **Provider verification:** strengthen typed ADP contracts and mappings using official vendor documentation and authorized metadata. Complete supported operation matrices. No speculative endpoints or fabricated entitlement assertions; do not classify an unresearched contract as vendor-proven unavailable.
5. **Release engineering:** add API compatibility baselines, release notes, documented security scans/SBOM coverage and signing/provenance design. Signing keys, live credentials and external approvals are explicit prerequisites, never embedded or fabricated. Public publishing remains disabled unless separately authorized.
6. **Documentation and acceptance:** build the documentation-site source/validation without deploying it; distinguish documentation completeness, mock verification, live verification and release acceptance. Report each remaining gap without a speculative completion percentage.

## Execution and scope constraints

All implementation occurs on develop or short-lived branches based on develop, with PRs targeting develop. Main accepts only same-repository develop promotions. This PR does not alter branch protection or claim administration settings are active. No runtime implementation, package restructuring, tests, workflow behavior, public publishing, deployment or paid service change is included here. The SDK boundary excludes persistent job scheduling, cross-system orchestration, subscription billing and tenant management; production CLI applications remain in the separate Phase 2 repository. The prohibited reference repository must not be consulted.

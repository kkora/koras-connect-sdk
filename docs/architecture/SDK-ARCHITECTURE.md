# Koras Connect SDK — Architecture Proposal v1.0

Status: PROPOSED. Planning only. No implementation authorization.

## Product boundaries
Phase 1: independently usable NuGet SDKs, owned by kkora.
Phase 2: separate koras-connect-cli repository, containing independent console integration applications.
Phase 3: separate commercial koras-integration-cloud repository, with hosted workers, tenancy, billing, and orchestration.
Dependency direction: CLI and Cloud consume SDK; SDK must never depend on CLI or Cloud.

## Proposed packages
- Koras.Connect.Abstractions: connector contracts and typed capabilities
- Koras.Connect.Core: HTTP client pipeline, pagination, errors, cancellation and resilience
- Koras.Connect.Authentication: certificate/mTLS, OAuth token acquisition, credential provider abstractions
- Koras.Connect.Export: CSV, JSON, XML; Excel as an optional extension
- Koras.Connect.Observability: diagnostics, structured logging and optional OpenTelemetry
- Koras.Connect.ADP: ADP product-specific APIs and typed models
- Later: Koras.Connect.SAP.SuccessFactors and Koras.Connect.Qualtrics

## Design constraints
.NET 10 LTS target proposed, with compatibility validation before finalizing target frameworks. Use IHttpClientFactory and DI-friendly optional registration. Support async/cancellation, typed errors, streaming and pagination, safe credential injection, mockable transports, and documented public APIs. Never embed certificates, client secrets, employee records, or tokens in logs or examples.

## Licensing
Initial source repository is private; public NuGet distribution is a separate approval gate. Apache-2.0 for eligible SDK source is proposed, not approved. Validate vendor API terms, dependencies, trademark, NuGet naming, and whether open-source source distribution is desired. Public binaries alone are not open-source.

## Source audit dependency
kkora/AdpIntegration is read-only reference. Verify source exists and classify code KEEP / REFACTOR / MOVE / REPLACE before implementation.

# G4–G6 implementation scope and acceptance

Owner request: implement G4–G6. The original roadmap defines G4 as release candidate, G5 as public NuGet release and G6 as SAP/Qualtrics expansion. This request authorizes implementation. Earlier restrictions against public publishing remain in effect; no package is published by this work. G0/G3 gaps in VERIFICATION.md remain open and are not waived by a green release-tooling build.

## G4: release candidate engineering

- Four .NET 10 packages at version 0.1.0-preview.2: Core, ADP, SAP SuccessFactors and Qualtrics.
- Versioned release manifest, package identity/assets/README validation, SHA256 checks, SPDX 2.3 runtime inventory and dependency/license declarations.
- Real package-reference consumer on Linux/Windows, automated connector tests, dependency audit and source hygiene checks.
- SBOM generation uses restored runtime dependency graphs and available NuGet archives. Test tooling, host-provided authentication infrastructure and external vendor services are outside its inventory. SPDX schema format is generated and checked internally; no external certification is claimed.
- Release eligibility checks fail for missing packages, version drift, incomplete SDK inventory or checksum mismatch. No credentials are needed for validation.

G4 tooling acceptance: all checks pass in CI and package artifacts/SBOM are retained. Product release acceptance remains blocked by G0/G3 and live tenant evidence.

## G5: publication implementation, no publication execution

`.github/workflows/release.yml` supports manual candidate validation and a separate optional publication job. Its default is validate only. Publication is disabled in `eng/release_manifest.json`. It requires all of:

1. Explicit owner publication authorization recorded in a reviewed `eng/publication_approval.json`, with version, approved=true and licenseExpression.
2. A reviewed manifest enabling publication, a matching vVERSION tag, passing package/SBOM checksum validation and a reviewed SPDX license expression in package metadata.
3. Owner-configured `nuget-release` environment protections and a scoped NUGET_API_KEY secret. These settings are not created or modified by this assignment.
4. A deliberate workflow dispatch requesting publication after all remaining product gates pass.

The source currently has no granted distribution license. No license is silently chosen. Approval, NuGet ownership/reserved names, vendor terms and account configuration remain external prerequisites. Workflow readiness is not a claim that a public release happened. Do not place secrets in approval records.

## G6: independent connector specifications

### SAP SuccessFactors package

Configured API root must end in `/odata/v2/`; no guessed host, company ID or UI-to-API mapping. Bearer tokens can come from an authorized host OAuth/OIDC implementation. `SamlBearerTokenSource` exchanges a host/IdP-generated Base64 assertion at the exact `/oauth/token` endpoint using company_id, client_id and the SAML2 bearer grant. It caches/renews tokens with cancellation and single-flight concurrency; signing keys remain outside the SDK. No Basic authentication or generic IAS-token fallback is invented.

Supported P0 contract: XML metadata; generic metadata-named entity enumeration; typed PerPerson, EmpEmployment and EmpJob models; escaped filter/select/expand; explicit effective-date query options; server `__next` continuation with same-origin/API-path enforcement and cycle/page bounds; explicitly enabled entity upsert with metadata injection, bounded batch size and per-row results. Reads use core resilience; writes are never replayed automatically. Entity support, effective dates, cursor availability, field schemas and RBP permissions remain tenant-specific. SAP enforces permissions; the client cannot inspect the entire tenant role configuration. No universal delta filter is synthesized: supply a documented entity-specific filter and retain caller-owned checkpoints/reconciliation.

### Qualtrics package

Configured API root ends in `/API/v3/`; host selects the actual data center. `QualtricsClient.Create` uses a rotating token source with X-API-TOKEN, without bearer auth. Supported P0 contract: typed survey listing with nextPage boundaries; survey design retrieval; response export creation, status polling, file-ID download and streamed raw ZIP/uncompressed bytes; supported formats csv/tsv/json/ndjson/spss/xml; bounded cancellable polling, safe errors and shared rate-limit handling. A timed-out start request must be reconciled rather than blindly restarted. The host owns persistence, archive extraction and scheduling.

Directory/distribution writes and webhook reception are P1 extensions, not implemented or represented as universally available without separate entitled contracts. No customer tenant or subscription evidence was supplied. All verification is synthetic and independent of the prohibited reference repository.

## Acceptance boundaries

- G6 implemented means the above P0 SDK contracts pass automated tests, not all vendor APIs or tenant business rules are covered.
- No CLI, Cloud implementation, external infrastructure, paid service or production API write is included.
- No automatic merge overrides the existing incomplete G0/G3 product acceptance gates.

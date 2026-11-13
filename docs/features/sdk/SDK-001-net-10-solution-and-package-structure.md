---
id: "SDK-001"
title: ".NET 10 solution and package structure"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-001 - .NET 10 solution and package structure

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-001.
2. **Feature name:** .NET 10 solution and package structure.
3. **Description:** Deliver installable packages with predictable ownership and runtime support.
4. **Business value:** A clean Linux/Windows build and pack produces the first-release package set reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Core + Koras.Connect.Abstractions.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `dotnet build Koras.Connect.slnx -c Release`: 0 errors/warnings, 9 projects (4 new: Abstractions/Core/Authentication/Export). `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 247, Skipped 3 (live-sandbox, unrelated), Failed 0, Total 250 — including the 16 new `PackageGraphTests` (no-cycle check, exact dependency-matrix allowlist check, folder/PackageId metadata check, and 13 reflection checks that the `Koras.Connect` facade's type forwarders resolve to the correct new assembly). `dotnet pack Koras.Connect.slnx -c Release -o artifacts`: 8 packages produced. `python eng/validate_packages.py`, `eng/generate_sbom.py`, `eng/release_checks.py`, `eng/check_sources.py`: all pass against the 8-package manifest. Both `samples/PackageConsumer` (unmodified, references only the old `Koras.Connect` facade package) and the new `samples/PackageConsumer.NextGen` (references `Koras.Connect.Abstractions`/`.Core`/`.Authentication`/`.Export` directly, no facade) restore and run successfully against the packed `artifacts/`, proving both old and new consumers compile and work. All of the above ran on Windows only (this session's environment); the Linux leg of `ci.yml`'s `ubuntu-latest` matrix job has not been separately verified here. |
| Remaining work / blockers | (1) **Observability deferred, not missing-by-oversight:** of the eight target package IDs in master spec section 6, only `Koras.Connect.Observability` was not scaffolded. It is phase P7 / release "Subsequent" and explicitly owned by SDK-012; creating an empty, unpacked package now would add CI/pack surface for zero content. (2) **Linux build/pack/test not verified in this session** — only Windows was exercised directly; `ci.yml`'s `ubuntu-latest` matrix leg (unchanged by this split, since it builds generically off `Koras.Connect.slnx`) needs to run once this change reaches a PR/push to close the "clean Linux/Windows build" half of the acceptance criteria. (3) **One documented preview break:** `Koras.Connect.Adp`'s `WorkerExportProjection.ExportJsonAsync`/`ProjectRows` were removed from the shipped package (they created a hard ADP→Export dependency that master spec section 7 forbids — "ADP does not require Export transitively"); the one call site was ported into `AdpWorkerExportTests.cs` as a local test helper composing `WorkerExportProjection.CreateSchema` + `TabularExport.WriteJsonAsync` at the call-site layer instead. No other public API removals. (4) **Dependency-matrix deviation, resolved and enforced by test:** `Koras.Connect.Authentication` references `Koras.Connect.Core` (for the `IntegrationException` hierarchy only — section 8 states "Exceptions belong in Core"), which reads narrower than section 7's "Direct internal dependencies: Abstractions" cell for Authentication; this is read as consistent with section 7's own "must not depend on: **Core transport implementation**" wording (a named carve-out, not "Core" wholesale) and is enforced going forward by `PackageGraphTests.EveryProjectReferenceIsOnTheReviewedAllowlist`. Similarly, `BearerTokenAuthenticator`/`HeaderTokenAuthenticator` stay in Core rather than moving to Authentication as section 7's prose ("Authentication implements it [`IRequestAuthenticator`] without needing Core") suggested — `Koras.Connect.Core`'s own `IntegrationTransport` constructor directly instantiates `BearerTokenAuthenticator`, so moving it to Authentication would have created a Core↔Authentication cycle. (5) Owner acceptance not yet recorded (see below). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Split the monolithic `Koras.Connect` project into `Koras.Connect.Abstractions`/`.Core`/`.Authentication`/`.Export` per master spec section 6-8, rewired `Koras.Connect.Adp`/`.Qualtrics`/`.Sap.SuccessFactors` and the test project onto the new graph with zero `using`-statement changes (every type kept its original namespace even where it physically moved), turned `Koras.Connect` into a type-forwarding compatibility facade (`Facade/TypeForwards.cs`, ~45 forwarded types), fixed the one real ADP→Export architecture violation this split exposed, added `PackageGraphTests` (cycle/allowlist/metadata/facade-forwarding checks), added the `samples/PackageConsumer.NextGen` direct-reference consumer alongside the unmodified old-facade `samples/PackageConsumer`, and updated `eng/release_manifest.json` and both CI/release workflows' sample-consumer steps accordingly. `Koras.Connect.Observability` intentionally not created (see remaining work). Checked off 4 of 6 completion gates; left "acceptance criteria/DoD" and "docs/acceptance recorded" unchecked pending a Linux CI run and owner acceptance. |

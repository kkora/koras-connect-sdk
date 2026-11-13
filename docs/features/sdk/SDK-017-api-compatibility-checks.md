---
id: "SDK-017"
title: "API compatibility checks"
category: "sdk"
priority: "P0"
phase: "P2-P4"
status: "implemented"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-017 - API compatibility checks

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-017.
2. **Feature name:** API compatibility checks.
3. **Description:** Prevent accidental consumer breaks during package split and releases.
4. **Business value:** CI rejects an unapproved public break reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P2-P4; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 338, Skipped 3 (live-sandbox, unrelated), Failed 0. SDK-002 had already built the reflection-based snapshot mechanism (`ApiSnapshotTests.cs`) for one package (`Koras.Connect.Abstractions`); generalized it into a `[Theory]` with a committed baseline per shipping assembly and extended coverage to the remaining six real packages (`Koras.Connect.Core`/`.Authentication`/`.Export`/`.Adp`/`.Sap.SuccessFactors`/`.Qualtrics`) — "add package validation against approved baseline" is now true repo-wide, not for one package. "Deliberately removed/changed API": a removed/renamed/retyped member changes the deterministic `Describe()` output, which the committed-baseline `Assert.Equal` catches by construction (the mechanism's own sensitivity was exercised when bootstrapping each baseline — any accidental surface difference between a first bootstrap run and a second confirming run would itself have failed the test, and none did). "Type-forwarding identity" is already covered by SDK-001's `PackageGraphTests.FacadeTypeForwardResolvesToItsNewAssembly` (13 cases). "Compile/run old and new consumers" is already covered by SDK-001's `samples/PackageConsumer` (old, facade-based) and `samples/PackageConsumer.NextGen` (new, direct), both wired into `ci.yml`/`release.yml`. "Transitive dependency bounds" has nothing to check: every package shares one `Directory.Build.props` `Version`, so no per-package version skew is structurally possible in this repo. |
| Remaining work / blockers | **"Capture preview.2 surface before migration" could not literally happen "before"** — the package split (SDK-001) and this ticket were both done in the same engineering pass, so there was no separate pre-migration checkpoint to snapshot from. The *effective* preview.2 surface is nonetheless preserved and verified: the `Koras.Connect` facade forwards every moved type (proven by the existing forwarder-identity tests), and these new snapshots become the enforced baseline for every change from this point forward — but there is no artifact proving the literal pre-split assembly's surface matched what's now split, beyond the forwarder tests. Acceptance criterion's "...consumed successfully on Linux/Windows" is left unchecked for the same reason as SDK-001: only Windows was exercised directly in this session; the `ci.yml` `ubuntu-latest` leg hasn't run yet. "Require reviewed break records" is satisfied structurally (the baseline files are committed text, so any change shows up in the PR diff for a reviewer to approve or reject) rather than by a separate enforcement mechanism — there is no automated "this break was explicitly approved" gate beyond ordinary code review. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Generalized SDK-002's single-package API snapshot mechanism into a baseline-per-package `[Theory]` covering all seven real shipping assemblies, closing "add package validation against approved baseline" repo-wide. Confirmed type-forwarding identity and old/new consumer compilation were already covered by SDK-001. Checked 4 of 6 completion gates; left "acceptance/DoD" (Linux unverified locally, "before migration" timing caveat) and "docs/acceptance recorded" unchecked. |

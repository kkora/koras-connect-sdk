---
id: "SDK-019"
title: "Versioning and release notes"
category: "sdk"
priority: "P0"
phase: "P4"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-019 - Versioning and release notes

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-019.
2. **Feature name:** Versioning and release notes.
3. **Description:** Let consumers evaluate compatibility and upgrade safely.
4. **Business value:** One candidate version agrees everywhere reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [ ] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | Not newly executed during documentation split; baseline evidence is in the specification |
| Remaining work / blockers | See functional requirements, acceptance criteria and external blockers above; replace with precise remaining items as work progresses |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `CHANGELOG.md` at the repository root with an `[Unreleased] — targeting 0.1.0-preview.2` section matching `Directory.Build.props`'s `<Version>`, referencing feature IDs rather than restating specifications, and an explicit "Known gaps" section so nothing implies live-tenant verification beyond what each feature's own tracker records. Still open: no CI check yet enforces that a `<Version>` change requires a corresponding changelog entry. `dotnet build`/`dotnet test` pass (197 passed, 3 intentionally skipped, 0 failed). |

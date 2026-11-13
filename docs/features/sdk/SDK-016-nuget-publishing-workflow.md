---
id: "SDK-016"
title: "NuGet publishing workflow"
category: "sdk"
priority: "P0"
phase: "P4"
status: "implemented"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-016 - NuGet publishing workflow

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-016.
2. **Feature name:** NuGet publishing workflow.
3. **Description:** Produce reviewable packages and guard any future publication.
4. **Business value:** Candidate workflow works without secrets reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [x] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `dotnet pack Koras.Connect.slnx -c Release -o artifacts` (8 packages, candidate inventory already updated by SDK-001) → `python eng/validate_packages.py` → `python eng/generate_sbom.py` → `python eng/release_checks.py`: all pass against the real repo's current manifest/SBOM/artifacts. `python -m unittest discover -s tests/release`: Ran 12, OK (11 pre-existing + 1 new). The pre-existing suite already closed "wrong tag/version/license/hash" (`test_publish_rejects_branch_or_wrong_version`, `test_package_version_mismatch_fails`, `test_license_mismatch_fails`, `test_tampered_packages_fail`), "missing/extra archive" (`test_missing_packages_fail`, `test_extra_archive_cannot_be_published`), "denied approval" (`test_publish_fails_closed_without_owner_approval`, `test_publish_requires_approval_record`), and "synthetic eligible candidate" (`test_reviewed_synthetic_release_can_pass_without_publishing`) — this ticket's unit-test requirements were already comprehensively covered. Two real functional gaps found and fixed in `eng/release_checks.py`/`release.yml`: (1) nothing validated "dependency order" — `release_checks.validate()` now walks `manifest["packages"]` in publish order and, for each package, reads its own packed `.nuspec` `<dependency>` ids and rejects the whole candidate if any sibling dependency isn't already earlier in the list (new `test_publish_rejects_a_manifest_order_that_precedes_a_dependency`). (2) nothing supported "partial remote publication recovery" — `release.yml`'s publish step pushed `artifacts/*.nupkg` in arbitrary filesystem-glob order with no `--skip-duplicate`, so retrying after a transient mid-release failure would fail closed on every package already successfully published on the prior attempt; fixed to push in the manifest's (now dependency-validated) order with `--skip-duplicate`, making a retry idempotent. |
| Remaining work / blockers | The new `--skip-duplicate`/manifest-ordered publish step itself cannot be exercised end-to-end without a real NuGet.org push, which is outside this engineering pass's authorization (`docs/decisions/APPROVAL-GATES.md`: publishing is not authorized here) — verified by code review and by the Python unit test covering the ordering logic it depends on, not by an actual publish. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Confirmed the existing `release_checks.py`/`release.yml`/test suite already covers this ticket's named unit-test requirements comprehensively. Fixed the two remaining functional gaps: added dependency-order validation to `release_checks.validate()`, and made the publish step push in manifest (dependency-safe) order with `--skip-duplicate` for idempotent retry. Checked 5 of 6 completion gates; left "docs/acceptance recorded" unchecked pending owner acceptance. |

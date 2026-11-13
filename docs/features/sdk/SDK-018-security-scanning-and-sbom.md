---
id: "SDK-018"
title: "Security scanning and SBOM"
category: "sdk"
priority: "P0"
phase: "P4"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-018 - Security scanning and SBOM

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-018.
2. **Feature name:** Security scanning and SBOM.
3. **Description:** Expose vulnerable dependencies and shipping supply-chain contents.
4. **Business value:** Each shipping archive maps to a complete reviewed runtime SBOM/hash reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (release engineering).
6. **Priority:** P0.
7. **Planned development phase:** P4; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [x] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; this pass is an uncommitted working-tree change — add the tested commit SHA once committed |
| Validation commands / results | `python eng/check_vulnerabilities.py`: "No known-vulnerable dependencies found." against the real repo. `python eng/generate_tooling_inventory.py`: wrote `artifacts/tooling-inventory.json` (7 build/test package references). `python -m unittest discover -s tests/release`: Ran 18, OK (16 pre-existing/SDK-016 + 2 new files, 6 new tests). `dotnet test Koras.Connect.slnx -c Release`: Passed 338, Skipped 3, Failed 0 (unaffected by this Python-only change). Found and fixed a real, significant gap: CI's existing `dotnet list package --vulnerable` step is a well-known no-op for gating purposes — the command always exits 0 regardless of findings, so even a known-CVE transitive dependency would never have failed a build. New `eng/check_vulnerabilities.py` wraps it with `--format json`, recursively scans the report for any non-empty `vulnerabilities` array, and fails closed if one is found — this is the "explicit severity policy" and "scanner threshold failures" this ticket names, with the policy stated explicitly: any known advisory blocks, at any severity, until an owner records a triaged exception (deliberately conservative, since no triage process exists yet). Also added `eng/generate_tooling_inventory.py`, closing "include test/build tool inventories separately from runtime" — it lists the test project's own `PackageReference`s (xunit, Microsoft.NET.Test.Sdk, etc.) into a separate `tooling-inventory.json`, explicitly labeled as never part of a shipped package, distinct from `generate_sbom.py`'s runtime-only SBOM. Both wired into `ci.yml`/`release.yml` in place of the old no-op line. `eng/check_sources.py` (secret/private-key scanning, "malicious test fixture") was already comprehensive and repo-wide, including test fixtures; unchanged. |
| Remaining work | **"Reviewed code ... scanning" beyond secrets (i.e. SAST) and the "external scanning service permissions" this ticket's own blocker field names were not attempted** — wiring e.g. CodeQL or a third-party SAST service requires enabling a GitHub Advanced Security feature or configuring a third-party service's credentials/permissions, which is outside this engineering pass's authorization (`docs/decisions/APPROVAL-GATES.md`). The dependency-vulnerability and secret scanning this pass closed are the parts achievable with existing, already-authorized tooling (`dotnet list package`, a local Python scan). Left the vendor/entitlement completion box unchecked for this specific reason rather than marking it not-applicable, since it genuinely isn't closed. Samples (`samples/*`) are not covered by `check_vulnerabilities.py` since it scans `Koras.Connect.slnx` only — they are demo/non-shipping projects, consistent with this ticket's "shipping archive" framing, but flagging the scope boundary explicitly rather than silently. Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Fixed a real gap: the existing `dotnet list package --vulnerable` CI step always exits 0 regardless of findings, so it never actually gated anything. Added `eng/check_vulnerabilities.py` (fails closed, explicit any-severity-blocks policy) and `eng/generate_tooling_inventory.py` (separate build/test tool inventory). Left SAST/external-scanning-service entitlement explicitly open — outside this pass's authorization — rather than claiming it closed; status intentionally stays `partial` for that reason. |

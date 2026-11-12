# G0–G3 verification and merge gate

Status: implementation expanded; **NOT approved as complete G0–G3 and NOT ready to merge**. Owner authorization is present; this is an acceptance-evidence gate, not a request for repeated approval.

## Local evidence

- .NET SDK 10.0.404; Release solution build: 0 warnings, 0 errors.
- Automated tests: 37 passed, 0 failed, 0 skipped. Synthetic unit/endpoint-contract tests, not live ADP certification.
- Token renewal/concurrency/redaction, deadline/cancellation, retry/rate-limit behavior, no write retry, certificate PEM/PFX loading, DI construction, worker paging/unknown fields, event routes, acknowledgement header retention, publisher HMAC, exports, reconciliation and batching covered.
- Both `Koras.Connect.0.1.0-preview.1.nupkg` and `Koras.Connect.Adp.0.1.0-preview.1.nupkg` built. Archive checks confirm net10.0 assemblies, XML documentation, README and absence of source/private keys.
- Independent package-reference consumer restored the built packages, compiled and ran without ADP access.
- NuGet direct/transitive audit: no vulnerable packages reported. Dependency/license declarations recorded in DEPENDENCIES.md. Test dependency xunit.abstractions uses legacy license metadata and remains a distribution-review item, not a shipped runtime dependency.
- Source hygiene scan: no private key material/files or forbidden project-boundary references found. This is a focused scan, not a claim of an external penetration test or comprehensive historical secret scan.
- Existing baseline CI run 43150939618 passed only the earlier foundation. Updated Linux/Windows CI must be evaluated at the new branch head.

## Gate assessment

| Gate | Status | Remaining acceptance evidence |
|---|---|---|
| G0 | Partial | Supported endpoint matrix and independent architecture exist. Exact canonical grants, payload schemas and actor/product/version constraints are not fully verified for every operation. Generic grant descriptions are not a comprehensive entitlement matrix. |
| G1 | Implemented locally | New Linux/Windows CI, package checks and independent consumer must pass on the committed head. |
| G2 | Implemented locally | Core tests/build/package checks pass; CI must confirm platform behavior. |
| G3 | Partial | Workers and a documented low-level operation subset implemented. Low-level JSON dispatch is not equivalent to fully validated lifecycle/payroll schemas. General home department/location replacement contracts are not verified. Do not label these missing contracts as vendor-proven unavailability. |
| Merge | Blocked | Full requested G0–G3 acceptance has not been demonstrated; retain PR #1 as a draft. |

## Precise external evidence needed

1. Authorized current WFN metadata/OpenAPI/schema exports for the intended lifecycle, manager, work assignment, home department/location, payroll, benefits and time operation set; exact CAR grant and product/version inventory. Official catalog access alone does not establish customer permissions or complete payload contracts.
2. For live verification only: sandbox API root, client credentials, registered mTLS certificate, subscribed operations/events and synthetic authorized worker population. No live access was supplied or attempted; lack of credentials by itself does not prevent mock validation or justify a speculative endpoint.
3. No authenticated general home department/location replacement contract was available. Allocation changes and notification names must not be substituted for that contract.

Webhook helpers follow the official static publisher-header contract. Hosts must provide separate receiver authentication, TLS, durable inbox/deduplication, acknowledgement and replay handling. Notifications have documented coverage gaps; use periodic full-snapshot reconciliation. No universal delta timestamp filter is invented.

## Phase 2 readiness

The package-reference consumer demonstrates an independent CLI can compile against this preview. Read-only worker/export prototyping is ready. Full Phase 2 integration and production use remain gated by the missing G0/G3 contracts and tenant verification. No CLI/Cloud repository, infrastructure or public package publication was created.

## G4–G6 expansion (November 12, 2026)

Version advances to 0.1.0-preview.2 to avoid mutable/cached preview package identities. Four packages are now built. SAP/Qualtrics P0 contracts, safe token-header authentication, streaming downloads, continuation boundary checks, release manifest/checksums/SPDX inventory and fail-closed publication tooling are implemented. Publication remains disabled; no owner approval record, license grant, NuGet key or live tenant credentials were supplied. Current CI evidence is recorded on PR #1 at the exact expansion head; earlier 37-test evidence above describes the prior ADP/core revision. G0/G3 remain incomplete and are not waived by these changes.

Expansion local verification: .NET 10 Release build has 0 warnings/errors; all 55 .NET tests and 11 Python release-gate tests pass. Four preview.2 packages validate, a 17-package SPDX runtime inventory is generated, checksum checks pass, the independent package consumer restores/compiles/runs and the NuGet audit reports no known direct/transitive vulnerabilities. Publication-negative and synthetic positive tests exercise approval/tag/license/hash checks without any remote push. G5 publication execution remains blocked; no account secret or licensing approval has been fabricated.

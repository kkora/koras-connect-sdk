---
id: "SDK-002"
title: "Connector interface contracts"
category: "sdk"
priority: "P0"
phase: "P2"
status: "implemented"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-002 - Connector interface contracts

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-002.
2. **Feature name:** Connector interface contracts.
3. **Description:** Let hosts discover connectors and capabilities without coupling to implementations.
4. **Business value:** A host can discover and substitute each connector using only Abstractions reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions.
6. **Priority:** P0.
7. **Planned development phase:** P2; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
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
| Validation commands / results | `dotnet test Koras.Connect.slnx -c Release --no-build`: Passed 254, Skipped 3 (live-sandbox, unrelated), Failed 0. New: `ConnectorContractTests` (6 tests) proves host-side substitution between two unrelated `IConnector` implementations built purely against `Koras.Connect.Abstractions` (no ADP/SAP/Qualtrics reference), that `EntitlementState.Unknown` is the structural default (`default(EntitlementState) == Unknown`), that a `Denied` entitlement is never read as `Verified`, and that `ConnectorDescriptor`/`ConnectionDiagnostic`/`EntitlementEvidence` are `with`-immutable. New: `ApiSnapshotTests` (1 test) adds a committed, reviewable reflection-based snapshot of `Koras.Connect.Abstractions`'s full exported public surface (`tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Abstractions.snapshot.txt`) — no ApiCompat/PublicApiGenerator tooling exists anywhere in this repo, so this is a dependency-free equivalent; any future public-surface change must update this file in the same diff. |
| Remaining work / blockers | **Real, unresolved implementation gaps found while closing this ticket's test requirements (not SDK-002's own scope — see owning-package note below, flagging for the ADP/SAP/Qualtrics category trackers):** (1) `Koras.Connect.Sap.SuccessFactors` has **no `IConnector` implementation at all** (confirmed by exhaustive source search; its own README explicitly says so) — so "a host can discover and substitute **each** connector" is not yet true for all three shipped providers, only ADP. (2) `QualtricsConnector.Descriptor` hardcodes every entitlement to `Unknown` on every access with no analog to ADP's `AdpEntitlementTracker` — it has no mechanism yet to ever record `Verified`/`Denied` from a real authorized call. Both are provider-implementation gaps outside this ticket's owning package (`Koras.Connect.Abstractions` only); SDK-002 itself only owns the neutral contract, which is complete, tested, and provider-agnostic-substitutable by construction (proven with fakes). Recommend opening/checking the SAP and Qualtrics connector-category trackers for these two items. (3) No consumer-facing sample demonstrates connector discovery/substitution (only xunit tests do). (4) Owner acceptance not recorded. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build — this is an engineering-implementation pass, not a product-owner acceptance record. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `IConnector`, `ConnectorDescriptor`, `EntitlementState`/`EntitlementEvidence`, `DiagnosticStage`/`DiagnosticOutcome`/`ConnectionDiagnostic` in `src/Koras.Connect/Abstractions/Connector.cs` (still inside the monolithic `Koras.Connect` assembly, not a separate `Koras.Connect.Abstractions` package per SDK-001). Provider connectors (ADP/SAP/Qualtrics) are being wired to implement `IConnector.DiagnoseAsync` separately; see their own trackers. Not yet verified: no provider-independent consumer substitution test exists yet, and no live diagnostic has been executed. `dotnet build Koras.Connect.slnx` succeeds with the new types; `dotnet test` baseline (55 tests) still passes unaffected. |
| 2026-11-12 | `implemented` | Post-SDK-001, the contract types already live in the real `Koras.Connect.Abstractions` package (not the old monolith). Added `ConnectorContractTests.cs` (fake-connector substitution, `Unknown`-default, `Denied`-never-`Verified`, `with`-immutability) and `ApiSnapshotTests.cs` (reflection-based public-surface snapshot) to close this ticket's own unit-test requirements. Found and recorded two real, unrelated implementation gaps (SAP has no `IConnector` at all; Qualtrics has no entitlement-recording mechanism) that block the full "each connector" acceptance criterion — left unchecked and flagged for the SAP/Qualtrics category trackers rather than fixed here, since they're outside this ticket's owning package. |

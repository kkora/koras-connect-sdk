---
id: "SAP-013"
title: "Delta and incremental synchronization"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SAP-013 - Delta and incremental synchronization

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-013.
2. **Feature name:** Delta and incremental synchronization.
3. **Description:** Reduce extraction while protecting against missed temporal changes.
4. **Business value:** Reduce extraction while protecting against missed temporal changes.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP filter support and E-SYNC; no delta/checkpoint contract; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Use entity-documented modification fields/overlap windows and host checkpoints/deduplication; distinguish modification time from effective date; keep reconciliation fallback.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed entity-specific DeltaQuery/checkpoint adapters; no universal delta endpoint.
13. **Dependencies:** SAP-004, SAP-009, SDK-007; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Late/same-timestamp changes, timezone, overlap, failed scan, visibility shrink and future-effective records. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Late/same-timestamp changes, timezone, overlap, failed scan, visibility shrink and future-effective records.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Fixture overlaps deduplicate with no checkpoint advance after failed traversal; effective-date policy is explicit.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Exact modified-field/filter/retention contract per entity unverified; generic lastModifiedOn is not assumed [S3].

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
| Implementation revision | Uncommitted working tree as of 2026-11-12; see `src/Koras.Connect.Sap.SuccessFactors/SapDeltaSync.cs` |
| Validation commands / results | `dotnet build Koras.Connect.slnx` - 0 errors; `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj` - 145/145 passed, including `tests/Koras.Connect.Tests/SapDeltaSyncTests.cs` (3 tests) |
| Remaining work / blockers | Timezone-specific handling, "visibility shrink" (a previously-visible record disappearing from a later scan) and future-effective records are not separately tested; the exact per-entity modification-time field name is still host-supplied, not derived from metadata |
| Acceptance / completion | Unit-level only: `OverlapWindowDedupsUnchangedRecordsButStillDetectsALateArrivingSameTimestampChange` composes `Reconciliation.DetectChangesAsync` end-to-end and proves overlap-window dedup plus a same-timestamp content change is still caught; `FailedScanNeverProducesAnAdvancedCheckpoint` proves a mid-scan failure never yields an advanced checkpoint (the checkpoint is only returned after the full enumeration completes). Live/sandbox acceptance not recorded. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `SapDeltaCheckpoint { EntityName, ModificationField, LastValue }`, `SapDeltaFilters.BuildFilter` (modification-time lower bound + overlap window) and `SapDeltaSync.RunAsync` composing `SuccessFactorsClient.EnumerateAsync` with `Koras.Connect.Synchronization.Reconciliation.DetectChangesAsync`, in `src/Koras.Connect.Sap.SuccessFactors/SapDeltaSync.cs`. Tests in `tests/Koras.Connect.Tests/SapDeltaSyncTests.cs` cover overlap-window dedup, a late-arriving same-timestamp change, and failed-scan-does-not-advance-checkpoint. Kept `partial`: timezone/visibility-shrink/future-effective-record cases remain untested. |

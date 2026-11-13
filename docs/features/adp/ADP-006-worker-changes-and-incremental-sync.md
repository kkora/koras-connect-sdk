---
id: "ADP-006"
title: "Worker changes and incremental sync"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-006 - Worker changes and incremental sync

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-006.
2. **Feature name:** Worker changes and incremental sync.
3. **Description:** Reduce repeat delivery while retaining a reliable recovery path.
4. **Business value:** Reduce repeat delivery while retaining a reliable recovery path.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SYNC fingerprints/batching; E-OPS notifications/ack; E-EVENT delivery identity; aliases resolve in section 3.
10. **Functional requirements:** Expose caller-owned query-bound checkpoints and event-triggered re-read plus periodic snapshot reconciliation; never invent modifiedSince, infer deletion from invisibility or acknowledge before downstream success.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Reconciliation.DetectChangesAsync, ReadNotificationAsync/AcknowledgeNotificationAsync; proposed synchronization result/checkpoint contract.
13. **Dependencies:** ADP-004, SDK-007, ADP-018; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Duplicate keys/delivery IDs, changed representation, interrupted scan, out-of-order events, permission shrink and failed downstream delivery. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Duplicate keys/delivery IDs, changed representation, interrupted scan, out-of-order events, permission shrink and failed downstream delivery.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** A failed scan cannot commit a completed checkpoint; duplicate events do not force duplicate host processing in sample harness; no universal last-modified filter or hidden persistence is emitted.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Exact subscribed event coverage/retention and permitted reads required; no universal WFN delta cursor verified [A4].

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "no caller-owned persistent `IWorkerFingerprintStore` implementation exists yet" gap by adding `FileWorkerFingerprintStore` (`src/Koras.Connect.Adp/FileWorkerFingerprintStore.cs`) — writes the new snapshot to a temporary file beside the target then atomically replaces it via `File.Move(..., overwrite: true)`, satisfying the interface's own documented "never partially written" contract without a database dependency. Tests in `tests/Koras.Connect.Tests/FileWorkerFingerprintStoreTests.cs`: empty-before-first-commit, round trip, a second commit fully replacing (not merging into) the first, missing parent directories created on first commit, no leftover temp file after a successful commit, and an already-canceled commit leaving the prior committed snapshot completely untouched. Full suite: 424 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified for the fingerprint-store half. A durable notification-id inbox (for `HandleNotificationAsync`'s `seenMessageIds` set) remains caller-owned and unimplemented here by design — that set's persistence is host infrastructure, not a documented SDK deliverable — and no live-tenant exercise of either path has been performed. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `Koras.Connect.Adp.AdpChangeDetector` and `IWorkerFingerprintStore` (`src/Koras.Connect.Adp/AdpChangeDetector.cs`), composing `AdpWorkersClient.EnumerateTypedWorkersAsync` with `Koras.Connect.Synchronization.Reconciliation.DetectChangesAsync`. `HandleNotificationAsync` treats a notification only as a trigger to re-read one worker from source (never trusting the notification payload), suppressing duplicate message ids via a caller-owned `ISet<string>`; `ScanAsync`/`CommitAsync` are deliberately separate so a scan that throws or is canceled mid-drain never reaches the commit step. Tests added in `tests/Koras.Connect.Tests/AdpChangeDetectorTests.cs`: `DuplicateMessageIdSuppressesSecondReReadWithoutIssuingARequest`, `OutOfOrderNotificationsEachTriggerAnIndependentReReadFromTheSourceOfTruth`, `FullyDrainedScanCanBeCommittedAndReflectsOnlyChangedWorkers`, and `InterruptedScanNeverPartiallyCommitsAndThePriorSnapshotSurvives` (a scan that fails mid-page leaves a seeded prior snapshot completely untouched). Still `partial`: no real durable store or production notification pipeline has been wired or verified against a live tenant. |
| 2026-11-13 | `implemented` | Added `FileWorkerFingerprintStore`, closing the "no caller-owned persistent store exists yet" gap with an atomic-rename-based implementation. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: the notification-id durable inbox remains caller-owned/unimplemented by design, and no live-tenant exercise has occurred. |

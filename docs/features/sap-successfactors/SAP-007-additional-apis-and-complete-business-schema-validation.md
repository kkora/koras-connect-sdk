---
id: "SAP-007"
title: "Additional APIs and complete business-schema validation"
category: "sap-successfactors"
priority: "P1"
phase: "P5"
status: "blocked"
baseline_status: "Planned"
verification: "blocked"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-007 - Additional APIs and complete business-schema validation

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-007.
2. **Feature name:** Additional APIs and complete business-schema validation.
3. **Description:** Make expansion deliberate rather than implying all SAP coverage.
4. **Business value:** Make expansion deliberate rather than implying all SAP coverage.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP generic queries; no complete business-schema validator; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Select approved entities from current references/metadata; publish per-entity operations/keys/fields/date support; generic query syntax is not authorization or full validation.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Proposed entity capability/schema policies; no universal all-SAP client.
13. **Dependencies:** SAP-014, SAP-006, SDK-002; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Unsupported entity/field, stale schema, required keys, missing module and denied permission. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Unsupported entity/field, stale schema, required keys, missing module and denied permission.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Each newly advertised entity has official contract, fixtures and limitations; generic enumeration remains explicitly low-level.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Current official entity specs and tenant business rules needed; metadata presence alone is insufficient.

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
| Remaining work / blockers | Explicitly re-confirmed as blocked during the same session that closed the other 15 SAP features. Offered a choice between a best-effort capability-descriptor scaffold and staying blocked; staying blocked (the recommended option) was accepted. Rationale: unlike the other 15 features, this one requires both current official entity specs AND tenant business rules before any capability/schema-validation policy can be published - the official "SAP SuccessFactors API Reference Guide" (2H 2026 edition) resolves the documentation half for individual entities as they're addressed (see SAP-010's FO entity-set citations), but the tenant-business-rules half is inherently unverifiable without a live, authorized tenant and was not and cannot be supplied by public documentation alone. No capability policy, entity allowlist, or schema validator was invented. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `planned` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `blocked` | Explicitly re-confirmed via a direct implement-now-or-stay-blocked choice, now that the official API Reference Guide was available as a resource; staying blocked (the recommended option) was accepted because the tenant-business-rules half of the blocker remains genuinely unverifiable without a live tenant regardless of documentation access. `status` moves from `planned` to `blocked` and `verification` to `blocked` to reflect that this is now a confirmed, not merely deferred, external blocker - no capability policy was invented. |

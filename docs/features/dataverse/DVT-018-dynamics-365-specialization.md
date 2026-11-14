---
id: "DVT-018"
title: "Dynamics 365 specialization"
category: "dataverse"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-018 - Dynamics 365 specialization

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-018 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-018.
2. **Feature name:** Dynamics 365 specialization.
3. **Description:** Document that Dynamics 365 first-party app tables (`account`, `contact`, `opportunity`, `lead`, `task`, etc.) are reached through exactly the same generic Dataverse mechanism as any custom table - there is no separate "Dynamics 365 API."
4. **Business value:** Document that Dynamics 365 first-party app tables (`account`, `contact`, `opportunity`, `lead`, `task`, etc.) are reached through exactly the same generic Dataverse mechanism as any custom table - there is no separate "Dynamics 365 API."
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific; this feature is primarily a documentation/typed-convenience-wrapper feature over DVT-004 through DVT-013, not a new protocol.
10. **Functional requirements:** Provide optional, pre-built `DataverseQuery`/typed-model convenience wrappers for the handful of first-party Dynamics 365 Sales tables most hosts actually touch (`account`, `contact`, `opportunity`) as thin, documented pass-throughs over the already-specified generic CRUD/query surface - mirroring exactly how ADP-012/SAP-010's "thin typed wrapper over an already-verified primitive" pattern works, not a new API family. **No official page draws a crisp architectural line between "core Dataverse tables" and "Dynamics-365-app-specific tables" beyond the shared `EntitySet`/entity-type-reference mechanism** - this is confirmed by consistent documentation usage across every fetched Dataverse page (DVT-004's `CreateRowAsync`/DVT-010's `AssociateDisassociateTableRows` examples all use `account`/`contact` as illustrative generic-mechanism examples), not an explicit policy statement, and is recorded here as such rather than invented.
11. **Nonfunctional requirements:** None beyond the underlying CRUD/query features'.
12. **Public API contracts or proposed contracts:** Official API: Uses the generic `EntitySet` mechanism (DVT-004) unchanged; `account`/`contact`/`opportunity` are the documented illustrative examples across multiple fetched pages. Proposed C# contracts:

```csharp
public sealed class DataverseAccount { public string? Name { get; set; } /* ... */ public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<DataverseAccount> EnumerateAccountsAsync(DataverseQuery? query = null, CancellationToken ct = default);
// Equivalent thin wrappers proposed for contact/opportunity; exact field lists require metadata verification (DVT-009)
// against a live tenant before any field beyond the documented examples is hardcoded into a typed model.
```
13. **Dependencies:** DVT-004, DVT-005, DVT-009, DVT-021.
14. **Security considerations:** Authentication and authorization: Identical to any other table (DVT-004). Required permissions/scopes/licenses: Standard security-role privilege on the specific table; Dynamics 365 Sales app licensing is a separate, tenant-side concern the SDK does not manage or infer. Data privacy and security: Account/contact/opportunity data is frequently customer PII/commercially sensitive - no special handling beyond the SDK's general row-data policy, which already treats all row data as sensitive by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Unchanged from DVT-006/014. Error handling and concurrency: Unchanged from DVT-004.
16. **Unit-test requirements:** Typed wrapper composes the identical generic query/CRUD calls a hand-written caller would; unknown fields preserved via `AdditionalData`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures using the documented `account`/`contact` example shapes from DVT-004/DVT-010's own fetched pages. Integration-test requirements: Authorized sandbox with a real Dynamics 365 Sales app provisioned - not available this pass.
18. **Acceptance criteria:** The typed convenience wrappers are proven to be pure pass-throughs with no invented field beyond what DVT-009's metadata discovery confirms.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact field lists for `account`/`contact`/`opportunity` must come from DVT-009's live metadata discovery, not from this document's necessarily-illustrative examples; no field beyond `Name`-style documented examples should be hardcoded until independently confirmed.

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
| Implementation revision | Not recorded; no Phase 2 implementation exists yet |
| Validation commands / results | Not recorded; no Phase 2 implementation exists yet |
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Exact field lists for `account`/`contact`/`opportunity` must come from DVT-009's live metadata discovery, not from this document's necessarily-illustrative examples; no field beyond `Name`-style documented examples should be hardcoded until independently confirmed. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

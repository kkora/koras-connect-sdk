---
id: "SFD-008"
title: "External IDs and upsert"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-008 - External IDs and upsert

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-008 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-008.
2. **Feature name:** External IDs and upsert.
3. **Description:** Create-or-update a record keyed by a business-meaningful external ID field instead of the Salesforce-internal 18-character record ID.
4. **Business value:** Create-or-update a record keyed by a business-meaningful external ID field instead of the Salesforce-internal 18-character record ID.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Parallels DVT-008/SAP SuccessFactors's upsert-gating pattern.
10. **Functional requirements:** `PATCH /services/data/vXX.X/sobjects/{sObject}/{fieldName}/{fieldValue}` where `{fieldName}` must be a custom field flagged as an External ID; optional `updateOnly` parameter restricts to update-only semantics [PDF, directly confirmed]. Permissions: External ID + Unique together need no special permission; External ID without Unique requires "View All Data" [PDF, directly confirmed]. Response status: **201** = record inserted, **204** = record updated, **300** = duplicate external-ID value (response body contains the list of matching records) [PDF, directly confirmed].
11. **Nonfunctional requirements:** Explicit write opt-in gate, matching every other write operation in this specification.
12. **Public API contracts or proposed contracts:** Official API: Upsert URL syntax, permission rules, and all three status codes - directly confirmed [PDF]. Proposed C# contracts:

```csharp
public sealed record SalesforceUpsertResult(string? RecordId, bool Created, IReadOnlyList<string>? AmbiguousMatchIds);
public async Task<SalesforceUpsertResult> UpsertByExternalIdAsync(string sObject, string externalIdField, string externalIdValue,
    JsonElement fields, bool updateOnly = false, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** SFD-004, SFD-007.
14. **Security considerations:** Authentication and authorization: Create + Update object permission; field-level security on the External ID field. Required permissions/scopes/licenses: "View All Data" specifically required only for an External-ID-without-Unique field, per the documented rule above. Data privacy and security: External ID values and record fields are caller-owned business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-record operation; never auto-retried (it is a write). Error handling and concurrency: A 300 ambiguous-match response is surfaced distinctly (with the matching-record list), not treated as a generic failure.
16. **Unit-test requirements:** Create-vs-update outcome distinguished by status code; 300 ambiguous-match parses the matching-record list; upsert without `writesEnabled` throws before any HTTP call; `updateOnly` sends the documented query parameter.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for all three documented response shapes (201/204/300). Integration-test requirements: Authorized sandbox org with a real External ID field configured; not available this pass.
18. **Acceptance criteria:** All three documented outcomes are distinguished correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Which fields are flagged as External ID on any given org is entirely org-specific.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. Which fields are flagged as External ID on any given org is entirely org-specific. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

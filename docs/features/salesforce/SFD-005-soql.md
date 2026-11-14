---
id: "SFD-005"
title: "SOQL"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-005 - SOQL

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-005 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-005.
2. **Feature name:** SOQL.
3. **Description:** Query Salesforce records with a SQL-like language supporting filtering, relationships, and aggregation.
4. **Business value:** Query Salesforce records with a SQL-like language supporting filtering, relationships, and aggregation.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Follows the same safe-query-composition discipline as DVT-005/every existing connector's filter builder.
10. **Functional requirements:** `GET /services/data/vXX.X/query?q={SOQL}` (URL-encoded `q` parameter); sibling `GET /services/data/vXX.X/queryAll?q=...` additionally includes soft-deleted/archived records [PDF/SOQL-SOSL PDF, directly confirmed]. Syntax: `SELECT fieldList[,subquery] FROM object [WHERE ...] [GROUP BY ...] [ORDER BY ...] [LIMIT n] [OFFSET n]` [directly confirmed]. Child-to-parent traversal via dot notation (`SELECT Id, Account.Name FROM Contact`), up to 5 levels, max 55 relationships per query; parent-to-child via a subquery naming the plural relationship (`SELECT Name, (SELECT LastName FROM Contacts) FROM Account`), up to 5 levels (v58.0+), max 20 parent-to-child relationships per query [directly confirmed in the SOQL/SOSL PDF].
11. **Nonfunctional requirements:** Query strings built via a safe-composition helper, never raw string interpolation of caller-supplied filter text without escaping, matching every existing connector's discipline.
12. **Public API contracts or proposed contracts:** Official API: `/query`, `/queryAll`, SOQL syntax and relationship-traversal limits - all directly confirmed in the fetched PDFs. Proposed C# contracts:

```csharp
public async Task<SalesforceQueryResult<T>> QueryAsync<T>(string soql, bool includeDeleted = false, CancellationToken ct = default);
```
13. **Dependencies:** SFD-003.
14. **Security considerations:** Authentication and authorization: Standard object/field-level security applies per queried field. Required permissions/scopes/licenses: Read access on every queried object/field. Data privacy and security: SOQL text itself may reveal sensitive filter/field semantics; redacted from default logging exactly as every other connector's query-string composition.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Delegated to SFD-006. Error handling and concurrency: Malformed SOQL returns a documented 400 with an `errorCode`, classified via SFD-019's shared error shape.
16. **Unit-test requirements:** Query-string composition escapes special characters correctly; child-to-parent/parent-to-child syntax composes as documented; `queryAll` sets the correct resource path.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures matching the documented query-result shape, including a relationship subquery. Integration-test requirements: Authorized sandbox org with related records; not available this pass.
18. **Acceptance criteria:** SOQL composition and execution round-trip correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-org caveat.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. None beyond the standing no-live-org caveat. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

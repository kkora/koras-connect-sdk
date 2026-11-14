---
id: "SFD-015"
title: "Relationships and references"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: SalesforceTypeOfClause composes the documented TYPEOF...WHEN...THEN...ELSE END syntax with identifier validation; child-to-parent/parent-to-child traversal itself is caller-authored SOQL text with no separate builder, matching SFD-005's precedent. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-015 - Relationships and references

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-015 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-015.
2. **Feature name:** Relationships and references.
3. **Description:** Traverse and query parent/child and polymorphic relationships between sObjects.
4. **Business value:** Traverse and query parent/child and polymorphic relationships between sObjects.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Salesforce-specific; extends SFD-005's query surface.
10. **Functional requirements:** Child-to-parent: dot notation, max 5 levels, max 55 relationships per query [snippet, consistent with the directly-confirmed SOQL basics in SFD-005]. Parent-to-child: subquery using the plural relationship name (discoverable via `describe`'s `relationshipName` field, SFD-007), max 5 levels (v58.0+) [snippet]. Polymorphic relationships (e.g., Task/Event's `Who`/`What` fields): `TYPEOF field WHEN Type1 THEN f1,f2 WHEN Type2 THEN f3 ELSE f4 END` syntax, available v46.0+ [snippet].
11. **Nonfunctional requirements:** None beyond SFD-005's query-composition safety.
12. **Public API contracts or proposed contracts:** Official API: Relationship-traversal limits and `TYPEOF` syntax are snippet-sourced from `developer.salesforce.com/docs/platform/salesforce-soql-sosl/guide/{...-relationships-query-using,...-relationships-query-limits}.html`; relationship-traversal basics corroborate SFD-005's directly-confirmed SOQL/SOSL PDF content. Proposed C# contracts:

```csharp
public sealed record SalesforceTypeOfClause(string Field, IReadOnlyDictionary<string, IReadOnlyList<string>> WhenFields, IReadOnlyList<string>? ElseFields);
```
13. **Dependencies:** SFD-005, SFD-007.
14. **Security considerations:** Authentication and authorization: Read access on every traversed object/field. Required permissions/scopes/licenses: None beyond SFD-005's. Data privacy and security: No additional concern beyond SFD-005's.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Unchanged from SFD-006. Error handling and concurrency: A relationship-traversal depth/count violation returns a documented 400, classified via SFD-019.
16. **Unit-test requirements:** `TYPEOF` clause composition matches the documented syntax; relationship-depth/count limits enforced locally before send.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a polymorphic `Who`/`What` query. Integration-test requirements: Authorized sandbox org with Task/Event records; not available this pass.
18. **Acceptance criteria:** Relationship traversal and polymorphic queries compose correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Exact numeric limits are snippet-only; re-verify before release.

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
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceRelationships.cs` (`SalesforceTypeOfClause`) and `tests/Koras.Connect.Tests/Salesforce/SalesforceRelationshipsTests.cs`. Covers: the composed clause matches the documented `TYPEOF ... WHEN ... THEN ... ELSE ... END` syntax exactly, including the no-`ELSE` form; an empty `WHEN` map and unsafe field/type names are rejected. Relationship-depth/count limits are not locally enforced - they apply to caller-authored SOQL text (dot notation, parent-to-child subqueries), which this SDK does not parse or validate, matching SFD-005's "no query-builder DSL" precedent exactly. Full suite: 781 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | Exact numeric relationship-traversal limits remain snippet-only; no live org with Task/Event records available this pass to confirm the `TYPEOF` syntax end-to-end. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceTypeOfClause` plus `SalesforceRelationshipsTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: numeric limits are snippet-only and no live org is available this pass. |

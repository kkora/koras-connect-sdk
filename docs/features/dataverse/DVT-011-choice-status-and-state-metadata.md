---
id: "DVT-011"
title: "Choice, status and state metadata"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-011 - Choice, status and state metadata

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-011 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-011.
2. **Feature name:** Choice, status and state metadata.
3. **Description:** Resolve the human-readable label behind a Dataverse choice/status/state numeric value without the caller hardcoding a label-to-value mapping that can change per environment/language.
4. **Business value:** Resolve the human-readable label behind a Dataverse choice/status/state numeric value without the caller hardcoding a label-to-value mapping that can change per environment/language.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific.
10. **Functional requirements:** Request `Prefer: odata.include-annotations="OData.Community.Display.V1.FormattedValue"` to receive a `{property}@OData.Community.Display.V1.FormattedValue` sibling property carrying the current label for any choice/status/state column, including `statecode`/`statuscode` [D6, confirmed via the query-overview annotation mechanism]. Enumerate global option sets via `GET {environment}/api/data/v9.2/GlobalOptionSetDefinitions`, filterable by name, e.g. `GlobalOptionSetDefinitions(Name='incident_caseorigincode')` - **this entity-set name is corroborated by search of the official create-update-optionsets documentation page, not independently re-verified by a full-page fetch this session; treat the entity-set name as high-confidence but flag it for re-verification before release** (explicit vendor-research gap, Section 3 carried forward honestly rather than silently upgraded to "verified").
11. **Nonfunctional requirements:** None beyond standard read behavior.
12. **Public API contracts or proposed contracts:** Official API: `Prefer: odata.include-annotations` formatted-value mechanism confirmed [D6]; `GlobalOptionSetDefinitions` entity-set name is a documented-but-not-independently-refetched fact (see above). Proposed C# contracts:

```csharp
public sealed record DataverseOptionSetValue(int Value, string? FormattedLabel);
public IAsyncEnumerable<JsonDocument> EnumerateGlobalOptionSetsAsync(string? filterByName = null, CancellationToken ct = default);
```
13. **Dependencies:** DVT-004, DVT-009.
14. **Security considerations:** Authentication and authorization: Unchanged. Required permissions/scopes/licenses: None specific. Data privacy and security: Choice labels are schema metadata, not row data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses DVT-006 for `GlobalOptionSetDefinitions` enumeration. Error handling and concurrency: A row read without the annotation `Prefer` header simply omits the formatted-value sibling property - not an error, just absent data; the connector must not treat its absence as a parse failure.
16. **Unit-test requirements:** Formatted-value sibling property parsed correctly when present; absent gracefully when the `Prefer` header was not sent; `statecode`/`statuscode` specifically round-trip through the same mechanism as any other choice column.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture with and without the annotation header. Integration-test requirements: Authorized sandbox table with a real global option set, to confirm the `GlobalOptionSetDefinitions` entity-set name before release.
18. **Acceptance criteria:** Formatted labels resolve correctly when requested; the global-option-set entity-set name is re-verified against a live tenant or a freshly re-fetched official page before this feature ships as `implemented`.
19. **Definition of Done:** Standard template; the open vendor-research gap above is a release blocker for the `GlobalOptionSetDefinitions` path specifically, not for the formatted-value mechanism.
20. **External API entitlement or documentation blockers:** `GlobalOptionSetDefinitions` entity-set name needs one more independent confirmation before release (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. `GlobalOptionSetDefinitions` entity-set name needs one more independent confirmation before release (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

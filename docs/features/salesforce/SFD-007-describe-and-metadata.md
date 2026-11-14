---
id: "SFD-007"
title: "Describe and metadata"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-007 - Describe and metadata

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-007 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-007.
2. **Feature name:** Describe and metadata.
3. **Description:** Discover an sObject's actual current schema (fields, types, permissions) rather than assuming a hardcoded shape.
4. **Business value:** Discover an sObject's actual current schema (fields, types, permissions) rather than assuming a hardcoded shape.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Parallels DVT-009/SAP SuccessFactors's metadata-driven discipline, adapted to Salesforce's JSON (not CSDL/XML) describe format.
10. **Functional requirements:** Object describe: `GET /services/data/vXX.X/sobjects/{sObject}/describe/`, supporting `If-Modified-Since`/`If-Unmodified-Since` with a `304` response when unchanged [PDF, directly confirmed]. Global describe: `GET /services/data/vXX.X/sobjects/` → `{encoding, maxBatchSize, sobjects[]}` [PDF, directly confirmed]. `sobjects[]` entry fields (directly confirmed, verbatim): `name, label, custom, keyPrefix, updateable, searchable, labelPlural, layoutable, activateable, urls, createable, customSetting, deletable, deprecatedAndHidden, feedEnabled, mergeable, queryable, replicateable, retrieveable, undeletable, triggerable`. Per-field metadata (directly confirmed, verbatim): `name, label, type, length, nillable, createable, updateable, picklistValues, referenceTo, relationshipName, custom, unique, externalId, defaultValue, calculated, precision, scale, soapType, filterable, sortable, autoNumber, idLookup`.
11. **Nonfunctional requirements:** Content-hash-keyed reparse cache so an unchanged schema is not reparsed on every call, matching the Dataverse/SAP metadata pattern.
12. **Public API contracts or proposed contracts:** Official API: Both describe endpoints and every listed field name are directly confirmed in the fetched PDF. Proposed C# contracts:

```csharp
public sealed record SalesforceFieldDescriptor(string Name, string Type, bool Createable, bool Updateable, bool Nillable,
    bool Unique, bool ExternalId, IReadOnlyList<string>? ReferenceTo, string? RelationshipName);
public sealed record SalesforceObjectDescriptor(string Name, string Label, bool Createable, bool Updateable, bool Deletable,
    bool Queryable, string? KeyPrefix, IReadOnlyList<SalesforceFieldDescriptor> Fields);
public Task<SalesforceObjectDescriptor> DescribeObjectAsync(string sObject, CancellationToken ct = default);
public Task<IReadOnlyList<SalesforceObjectDescriptor>> DescribeGlobalAsync(CancellationToken ct = default);
```
13. **Dependencies:** SFD-003.
14. **Security considerations:** Authentication and authorization: Describe generally requires no object-specific data permission beyond read access to metadata, but field-level security can still hide specific field metadata on some orgs. Required permissions/scopes/licenses: None beyond a valid session. Data privacy and security: Metadata is schema information, not row-level business data, but still excluded from aggressive default logging.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-document fetches, not paginated; reuses the shared GET-retry policy and the `304`-on-unchanged caching behavior. Error handling and concurrency: Oversized/malformed describe payload rejected before parsing, matching the Dataverse/SAP metadata-size-cap pattern.
16. **Unit-test requirements:** Object/global describe parse correctly against the documented field list; a `304 Not Modified` response is handled as "unchanged," not a parse failure; an unknown/removed field in a future describe response is preserved, not dropped.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic describe fixtures for a representative standard object and a representative custom object (`__c` suffix). Integration-test requirements: Authorized sandbox org's real describe response; not available this pass.
18. **Acceptance criteria:** Field/permission discovery works against both a minimal synthetic fixture and (once available) a real org's describe response.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live org describe response available this pass.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live org describe response available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

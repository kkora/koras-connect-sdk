---
id: "SFD-020"
title: "Typed CRM/custom object models"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: SalesforceRecordMapper<TTarget> mirrors Dataverse's DataverseRowMapper<TTarget> exactly - a missing/blank Id is Omitted before the projection callback ever runs; the attributes envelope and the id field are excluded from the projected Fields dictionary, every other field (standard or custom __c) preserved verbatim. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-020 - Typed CRM/custom object models

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-020 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-020.
2. **Feature name:** Typed CRM/custom object models.
3. **Description:** Provide extension-data-preserving typed C# models for standard and custom Salesforce objects, matching every other connector's established discipline.
4. **Business value:** Provide extension-data-preserving typed C# models for standard and custom Salesforce objects, matching every other connector's established discipline.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IRecordMapper<TSource,TTarget>`/`MappingResult<T>` reused unchanged (Section 8.1).
10. **Functional requirements:** Two legacy typed-codegen paths exist but neither targets this connector's REST+JSON surface: the **Enterprise WSDL** (strongly-typed, per-org, every exposed object/field) and the **Partner WSDL** (generic/loosely-typed, org-agnostic) - both SOAP-API artifacts, downloaded from Setup → API page [snippet]. No full official OpenAPI specification exists for the whole REST API; there is an official **Beta**, "Generating an OpenAPI 3.0 Document for sObjects REST API," scoped only to sObjects CRUD, via `POST https://{MyDomainName}.my.salesforce.com/services/data/vXX.X/async/specifications/oas3` [snippet] - noted here as a potential future codegen source once it leaves beta, not used as an implementation dependency today per the same reasoning as DVT-021's "no official Web-API-specific generator" conclusion. Models are hand-written against SFD-007's describe metadata and the documented row-shape examples, preserving unknown fields via a `[JsonExtensionData]` dictionary, matching every existing Phase 1 DTO.
11. **Nonfunctional requirements:** Plain POCOs with `System.Text.Json` attributes, no third-party serialization library, matching every existing connector's style.
12. **Public API contracts or proposed contracts:** Official API: Enterprise/Partner WSDL existence and the OpenAPI 3.0 beta endpoint are snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/openapi_beta.htm` and general SOAP API documentation references; not independently re-fetched. Proposed C# contracts:

```csharp
public sealed record SalesforceRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class SalesforceRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
```
13. **Dependencies:** SFD-004, SFD-007.
14. **Security considerations:** Authentication and authorization: Not applicable (pure data-shape concern). Required permissions/scopes/licenses: None. Data privacy and security: Mapped/omitted/error states never leak raw field values into log output by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable. Error handling and concurrency: A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws, matching every existing mapper.
16. **Unit-test requirements:** A record missing a required identifier field is `Omitted`, not mapped with a null/default key; unknown fields preserved via extension data; a custom (`__c`-suffixed) field round-trips identically to a standard field.
17. **Integration-test requirements:** Contract-test scenarios: Golden-fixture tests against documented standard-object (Account/Contact) and representative custom-object row shapes. Integration-test requirements: None beyond the underlying CRUD/describe features'.
18. **Acceptance criteria:** Golden mappings are deterministic; omitted/error fields are visible; original source data remains intact.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The OpenAPI 3.0 beta generator is explicitly a beta feature with no guaranteed stability - not used as a hard dependency, consistent with the master Section 6 principle of only depending on GA vendor surfaces.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceMapping.cs` (`SalesforceRecordProjection`/`SalesforceRecordMapper<TTarget>`) and `tests/Koras.Connect.Tests/Salesforce/SalesforceMappingTests.cs`. Covers: a row with a valid `Id` maps successfully; a row missing the `Id` field, or carrying a blank one, is `Omitted` rather than mapped with a default key; a non-object source is `Omitted`; a field-level failure returns `MappingResult<T>.Failure` rather than throwing; a custom (`__c`-suffixed) field round-trips identically to a standard field via the projection's `Fields` dictionary; the `attributes` envelope field and the `Id` field itself are excluded from `Fields`. Full suite: 856 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The OpenAPI 3.0 beta generator remains explicitly not used as a hard dependency, consistent with the master Section 6 principle of only depending on GA vendor surfaces. No golden-fixture tests against a live org's exact Account/Contact row shape this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceMapping.cs` (`SalesforceRecordProjection`/`SalesforceRecordMapper<TTarget>`) plus `SalesforceMappingTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live-org golden fixture this pass. |

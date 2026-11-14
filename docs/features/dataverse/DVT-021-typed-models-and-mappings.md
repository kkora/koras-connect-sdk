---
id: "DVT-021"
title: "Typed models and mappings"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-021 - Typed models and mappings

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-021 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-021.
2. **Feature name:** Typed models and mappings.
3. **Description:** Provide extension-data-preserving typed C# models for the Dataverse rows/metadata this specification covers, and neutral `IRecordMapper` projections, matching every Phase 1 connector's established discipline.
4. **Business value:** Provide extension-data-preserving typed C# models for the Dataverse rows/metadata this specification covers, and neutral `IRecordMapper` projections, matching every Phase 1 connector's established discipline.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Directly parallels `Koras.Connect.Abstractions.IRecordMapper<TSource,TTarget>`/`MappingResult<TTarget>` (reused unchanged) and the `[JsonExtensionData]`-bag convention every existing typed DTO (`Worker`, `PerPerson`, `QualtricsContact`) already follows.
10. **Functional requirements:** No official first-party OpenAPI/Swagger document exists for the Dataverse Web API [vendor research, confirmed via search]; the only official codegen path is `pac modelbuilder build` (Power Platform CLI) for the *organization-service* (SDK-for-.NET) side, the documented successor to the legacy `CrmSvcUtil.exe` - **neither targets the Web API/JSON contract this connector uses**, so the connector's typed models are hand-written against the CSDL `$metadata` document (DVT-009) and the documented row-shape examples, not generated from an official Web-API-specific tool. Every typed model preserves unknown fields via a `[JsonExtensionData]` dictionary, matching every existing Phase 1 DTO.
11. **Nonfunctional requirements:** Models are plain POCOs with `System.Text.Json` attributes, matching the existing connectors' style exactly (no third-party serialization library).
12. **Public API contracts or proposed contracts:** Official API: No official Web-API-specific codegen tool exists [vendor research]; `pac modelbuilder build`/`CrmSvcUtil.exe` target the organization-service SDK, a distinct surface. Proposed C# contracts:

```csharp
public sealed record DataverseRecordProjection(Guid Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class DataverseRowMapper<TTarget> : IRecordMapper<JsonElement, TTarget> { /* caller-supplied projection delegate, mirroring PerPersonMapper's explicit field-by-field approach rather than reflection-based binding */ }
```
13. **Dependencies:** DVT-004, DVT-009.
14. **Security considerations:** Authentication and authorization: Not applicable (pure data-shape concern). Required permissions/scopes/licenses: None. Data privacy and security: Mapped/omitted/error states never leak raw field values into log output by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable. Error handling and concurrency: A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws, matching every existing mapper.
16. **Unit-test requirements:** A record missing a required identifier field is `Omitted`, not mapped with a null/default key (matching `PerPersonMapper`'s precedent exactly); unknown fields preserved via extension data; an unparseable date-like field surfaces as a field-level error, not a silent null (matching `SapDates`'s policy).
17. **Integration-test requirements:** Contract-test scenarios: Golden-fixture tests against the documented `account`/`contact` example row shapes. Integration-test requirements: None beyond the underlying CRUD/metadata features'.
18. **Acceptance criteria:** Golden mappings are deterministic; omitted/error fields are visible; original source data remains intact and inspectable.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No official Web-API-specific typed-client generator exists to validate against; hand-written models carry the same inherent "must be re-verified against live metadata before release" caveat every field-name claim in this catalog carries.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No official Web-API-specific typed-client generator exists to validate against; hand-written models carry the same inherent "must be re-verified against live metadata before release" caveat every field-name claim in this catalog carries. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

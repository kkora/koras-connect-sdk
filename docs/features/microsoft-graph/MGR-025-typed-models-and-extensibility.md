---
id: "MGR-025"
title: "Typed models and extensibility"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "implemented"
baseline_status: "Implemented: GraphRecordMapper<TTarget> projects a raw resource row (JsonElement) to an id plus a field dictionary (GraphRecordProjection), excluding the id field and any @odata.* annotation property, then delegates to a caller-supplied projection function returning MappingResult<TTarget>. A missing/unparsable identifier is Omitted, never mapped with a default key; unknown fields (including a v1.0-vs-beta shape difference) are preserved verbatim. No dependency on the official Microsoft Graph SDK. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-025 - Typed models and extensibility

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-025 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-025.
2. **Feature name:** Typed models and extensibility.
3. **Description:** Provide extension-data-preserving typed C# models for Graph resources, consistent with every other connector's established discipline, without taking a hard dependency on the official Microsoft Graph SDK.
4. **Business value:** Provide extension-data-preserving typed C# models for Graph resources, consistent with every other connector's established discipline, without taking a hard dependency on the official Microsoft Graph SDK.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IRecordMapper<TSource,TTarget>`/`MappingResult<T>` reused unchanged (Section 8.1).
10. **Functional requirements:** `$metadata` CSDL documents exist at `https://graph.microsoft.com/v1.0/$metadata` and `https://graph.microsoft.com/beta/$metadata` [confirmed]. Official `Microsoft.Graph`/`Microsoft.Graph.Core`/`Microsoft.Graph.Beta` NuGet packages exist (github.com/microsoftgraph/msgraph-sdk-dotnet) [confirmed existence] - referenced here **only as a model-shape precedent for field naming conventions**, explicitly **not** a dependency of `Koras.Connect.Microsoft.Graph` per the Section 6 architecture decision to avoid the official SDK's own dependency weight and its divergent DI/auth conventions. Every typed model preserves unknown fields via `[JsonExtensionData]`, matching every existing Phase 1/Phase 2 DTO.
11. **Nonfunctional requirements:** Plain POCOs with `System.Text.Json` attributes, no third-party serialization or the official Graph SDK's object model.
12. **Public API contracts or proposed contracts:** Official API: `$metadata` URLs and official SDK package existence, confirmed; not used as an implementation dependency. Proposed C# contracts:

```csharp
public sealed record GraphRecordProjection(string? Id, IReadOnlyDictionary<string, JsonElement> Fields);
public sealed class GraphRecordMapper<TTarget> : IRecordMapper<JsonElement, TTarget>
```
13. **Dependencies:** MGR-003, MGR-004 (illustrative model).
14. **Security considerations:** Authentication and authorization: Not applicable (pure data-shape concern). Required permissions/scopes/licenses: None. Data privacy and security: Mapped/omitted/error states never leak raw field values into log output by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable. Error handling and concurrency: A mapping failure returns `MappingResult<TTarget>.Failure([FieldError...])`, never throws.
16. **Unit-test requirements:** A record missing a required identifier field is `Omitted`; unknown fields preserved via extension data; v1.0 and beta responses for the same logical resource (where field sets differ) both map without data loss.
17. **Integration-test requirements:** Contract-test scenarios: Golden-fixture tests against documented user/group row shapes. Integration-test requirements: None beyond the underlying CRUD features'.
18. **Acceptance criteria:** Golden mappings are deterministic; omitted/error fields are visible; the connector never takes a dependency on the official Microsoft Graph SDK.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphMapping.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphMappingTests.cs`, mirroring `SalesforceRecordMapper<TTarget>` exactly (dictionary-of-`JsonElement` projection rather than the `[JsonExtensionData]` sketch in the functional requirements, which is the same pragmatic deviation already applied to Dataverse/Salesforce). Covers: a row with a valid id and documented field maps successfully; a row missing the id field or carrying a blank id is `Omitted`, never mapped with a default key; a non-object source is `Omitted`; a field-level projection failure returns `MappingResult.Failure` without throwing; an unknown field (standing in for a beta-only field absent from v1.0) round-trips into `GraphRecordProjection.Fields` unchanged; `@odata.*` annotation properties and the id field itself are excluded from the projected fields; a custom id field name is respected. Full suite: 1016 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | None beyond the standing no-live-tenant caveat (not applicable here - this feature is a pure, offline data-shape concern). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphMapping.cs` (`GraphRecordProjection`/`GraphRecordMapper<TTarget>`) plus `GraphMappingTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no golden fixture validated against a live tenant response this pass. |

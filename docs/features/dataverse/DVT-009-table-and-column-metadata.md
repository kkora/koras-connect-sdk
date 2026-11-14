---
id: "DVT-009"
title: "Table and column metadata"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: GetMetadataAsync/RefreshMetadataAsync/ParseEntityDescriptors mirror SuccessFactorsClient's $metadata pattern (size cap, hash-cached reparse, XXE-safe reader); EnumerateEntityDefinitionsAsync enumerates the EntityDefinitions metadata entity set. Unit-tested; no live tenant metadata document available this pass."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-009 - Table and column metadata

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-009 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-009.
2. **Feature name:** Table and column metadata.
3. **Description:** Discover a table's actual current schema (columns, types, keys) rather than assuming a hardcoded shape, mirroring the metadata-driven discipline SAP SuccessFactors's `GetMetadataAsync`/`ParseEntityDescriptors` already establish for a different OData dialect.
4. **Business value:** Discover a table's actual current schema (columns, types, keys) rather than assuming a hardcoded shape, mirroring the metadata-driven discipline SAP SuccessFactors's `GetMetadataAsync`/`ParseEntityDescriptors` already establish for a different OData dialect.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Directly parallels `SuccessFactorsClient.Metadata.cs`'s existing CSDL-parsing pattern (`EntityDescriptor`, size cap, content-hash parse cache, XXE-safe `XmlReaderSettings`) - the same approach is proposed here against Dataverse's own `$metadata`, not a shared implementation (the two CSDL dialects differ enough - EntityDefinitions/Attributes metadata-query entities vs. SAP's plain CSDL - that copying the SAP class verbatim would be wrong, but the *pattern* - size cap, hash-cached reparse, XXE defenses - transfers directly).
10. **Functional requirements:** Fetch the CSDL metadata document via `GET {environment}/api/data/v9.2/$metadata` (optionally `?annotations=true`, or `Prefer: odata.include-annotations="*"`) [D11]. Separately, query the richer metadata-entity surface: `EntityDefinitions` (e.g. filter by `ChangeTrackingEnabled eq true`) and nested `Attributes` (e.g. `EntityDefinitions(LogicalName='account')/Attributes(LogicalName='...')/Microsoft.Dynamics.CRM.FileAttributeMetadata`) [D11]. Namespace is `Microsoft.Dynamics.CRM` (alias `mscrm`) [D11]. The service document, `GET {environment}/api/data/v9.2/` (no further path), lists every `EntitySet` [D11].
11. **Nonfunctional requirements:** Same defense-in-depth as SAP SuccessFactors's existing metadata handling: explicit document-size cap checked before parsing, XXE/DTD-disabled `XmlReaderSettings`, content-hash-keyed reparse cache so an unchanged schema is not reparsed on every call.
12. **Public API contracts or proposed contracts:** Official API: `$metadata`, service document, `EntityDefinitions`/`Attributes` - all per [D11]. Proposed C# contracts:

```csharp
public sealed record DataverseEntityDescriptor(string LogicalName, IReadOnlyList<string> Keys, IReadOnlyList<string> Properties);
public Task<XDocument> GetMetadataAsync(CancellationToken ct = default);
public Task<XDocument> RefreshMetadataAsync(CancellationToken ct = default);
public static IReadOnlyList<DataverseEntityDescriptor> ParseEntityDescriptors(XDocument metadata);
public IAsyncEnumerable<JsonDocument> EnumerateEntityDefinitionsAsync(DataverseQuery? query = null, CancellationToken ct = default);
```
13. **Dependencies:** DVT-003.
14. **Security considerations:** Authentication and authorization: Metadata read generally requires no table-specific privilege beyond a valid session, but the richer `EntityDefinitions` surface can still be restricted on some tenants. Required permissions/scopes/licenses: None specific beyond a valid token, in the general case. Data privacy and security: Metadata/schema is not row-level business data but can reveal customization details some tenants consider sensitive; still excluded from aggressive default logging as a precaution.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: `EntityDefinitions`/`Attributes` enumeration reuses DVT-006's pagination; `$metadata` itself is a single document fetch. Error handling and concurrency: Oversized metadata document rejected before parsing (`InvalidDataException`), matching SAP's existing behavior exactly.
16. **Unit-test requirements:** CSDL parses correctly independent of exact EDM namespace version; oversized document rejected; XXE/DTD payload rejected; changed-content triggers reparse on `GetMetadataAsync`, `RefreshMetadataAsync` always reparses; `EntityDefinitions` filter-by-`ChangeTrackingEnabled` round-trips.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic CSDL fixture plus a synthetic `EntityDefinitions` JSON fixture. Integration-test requirements: Authorized sandbox environment's real `$metadata` document, to confirm the parser handles actual current schema size/shape - not available this pass.
18. **Acceptance criteria:** Entity/key/property discovery works against both a minimal synthetic fixture and (once available) a real tenant's metadata document.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live tenant metadata document available this pass; real-world metadata document size (potentially large for a heavily customized tenant) is unverified against the proposed size cap.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseMetadata.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseMetadataTests.cs`. Covers: CSDL parses entity keys/properties by local element name (namespace-version independent); an unchanged document is not reparsed (`GetMetadataAsync` returns the same cached instance) while the network call itself still always happens; `RefreshMetadataAsync` always reparses; an oversized document is rejected before parsing; an XXE/DTD payload is rejected; `EnumerateEntityDefinitionsAsync` composes a `$filter` against the PascalCase `EntityDefinitions` metadata entity set without going through the lowercase business-table-name validation. Full suite: 675 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live tenant metadata document available this pass; real-world metadata document size (potentially large for a heavily customized tenant) is unverified against the size cap. `Attributes` nested metadata (e.g. `EntityDefinitions(...)/Attributes(...)/Microsoft.Dynamics.CRM.FileAttributeMetadata`) is not separately implemented - callers can reach it today via `GetRowAsync`-style raw enumeration against the parsed `EntityDefinitions` navigation, but no dedicated typed helper exists. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `GetMetadataAsync`/`RefreshMetadataAsync`/`ParseEntityDescriptors`/`EnumerateEntityDefinitionsAsync` plus `DataverseMetadataTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live tenant metadata available this pass. |

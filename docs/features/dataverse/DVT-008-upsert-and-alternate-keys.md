---
id: "DVT-008"
title: "Upsert and alternate keys"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: UpsertByAlternateKeyAsync always requests return=representation to disambiguate 201/created from 200/updated, and supports UpdateOnly/CreateOnly via If-Match/If-None-Match: *. Unit-tested; no live tenant with a configured alternate key available this pass."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# DVT-008 - Upsert and alternate keys

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-008 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-008.
2. **Feature name:** Upsert and alternate keys.
3. **Description:** Create-or-update a row keyed by a business-meaningful alternate key instead of Dataverse's internal GUID, avoiding a separate existence-check round trip.
4. **Business value:** Create-or-update a row keyed by a business-meaningful alternate key instead of Dataverse's internal GUID, avoiding a separate existence-check round trip.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Follows the exact upsert-gating pattern `SuccessFactorsClient.UpsertAsync`/Qualtrics' `CreateContactAsync` opt-in-write convention already establish.
10. **Functional requirements:** `PATCH {entityset}(key1=val1,key2=val2)` - creates the row if the alternate-key combination does not exist, updates it if it does [D10]. `If-Match: *` forces update-only (errors if absent, rather than creating); `If-None-Match: *` forces create-only (errors if the key already exists) [D10]. Because the plain success response (204 + `OData-EntityId`) is identical for both outcomes, `Prefer: return=representation` is required to distinguish "a new row was created" (201) from "an existing row was updated" (200) [D10]. Alternate keys are discoverable via the CSDL `$metadata` document's annotations (DVT-009) - the connector must not assume every table has an alternate key, and must surface a clear local error if the caller requests upsert-by-key on a table/key combination it has not verified exists.
11. **Nonfunctional requirements:** Explicit write opt-in gate, matching every other write operation in this specification.
12. **Public API contracts or proposed contracts:** Official API: Alternate-key PATCH URL syntax, `If-Match`/`If-None-Match` semantics, `return=representation` disambiguation - all per [D10]. Proposed C# contracts:

```csharp
public sealed record DataverseUpsertResult(Guid RowId, bool Created);
public async Task<DataverseUpsertResult> UpsertByAlternateKeyAsync(string entitySet,
    IReadOnlyDictionary<string, string> alternateKey, JsonElement fields,
    DataverseUpsertMode mode = DataverseUpsertMode.CreateOrUpdate, bool writesEnabled = false, CancellationToken ct = default);
public enum DataverseUpsertMode { CreateOrUpdate, UpdateOnly, CreateOnly }
```
13. **Dependencies:** DVT-004, DVT-009.
14. **Security considerations:** Authentication and authorization: Unchanged; the caller's security role must cover both Create and Write privilege on the table, since either outcome is possible. Required permissions/scopes/licenses: Create + Write privilege on the target table. Data privacy and security: Alternate-key values and row fields are caller-owned business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-row operation; never auto-retried (it is a write). Error handling and concurrency: `If-Match: *` failing when the key does not exist, or `If-None-Match: *` failing when it does, both surface as documented 4xx failures the connector classifies, not generic contract errors.
16. **Unit-test requirements:** Create-vs-update outcome correctly distinguished via `return=representation` status code; `UpdateOnly`/`CreateOnly` modes send the correct `If-Match`/`If-None-Match` header and surface the correct error when violated; upsert without `writesEnabled` throws before any HTTP call.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both the 201 (created) and 200 (updated) response shapes. Integration-test requirements: Authorized sandbox table with a real alternate key configured.
18. **Acceptance criteria:** A caller can distinguish create-from-update outcomes reliably and cannot accidentally upsert against an unverified alternate key.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** Which tables have alternate keys configured is entirely tenant-specific.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseUpsert.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseUpsertTests.cs`. Covers: upsert without `writesEnabled` throws before any HTTP call; at least one alternate-key field required; the key segment escapes an internal quote by doubling it (OData string-literal convention) rather than percent-encoding it; `201 Created` reports `Created=true`, `200 OK` reports `Created=false`; `UpdateOnly` sends `If-Match: *` and `CreateOnly` sends `If-None-Match: *`; a mode violation (412) surfaces as `DataverseConcurrencyException` (DVT-013) for **both** `UpdateOnly` and `CreateOnly` (review pass 1 found only the `UpdateOnly` direction was actually tested despite the doc claiming both; added `CreateOnlyViolationSurfacesAsDataverseConcurrencyException`). The SDK does not pre-verify the alternate key exists on the target table - an unconfigured key surfaces as the server's own error. |
| Remaining work / blockers | No authorized sandbox table with a real alternate key configured available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found the `CreateOnly` precondition-violation direction was untested (only `UpdateOnly` had a test, despite the doc describing both as covered); added the missing test. No code behavior changed. |
| 2026-11-13 | `implemented` | Added `UpsertByAlternateKeyAsync`/`DataverseUpsertResult`/`DataverseUpsertMode` plus `DataverseUpsertTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live tenant with a configured alternate key available this pass. |

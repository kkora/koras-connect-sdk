---
id: "DVT-017"
title: "Attachments and notes"
category: "dataverse"
priority: "P1"
phase: "P2-D5"
status: "partial"
baseline_status: "Implemented: DataverseAnnotation typed model round-trips documentbody/filename/mimetype/isdocument via DVT-004's generic CRUD against annotations. Unit-tested, but the field names remain an unverified vendor-research gap per the spec's own release-blocker language - held at partial, not implemented."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-017 - Attachments and notes

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-017 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-017.
2. **Feature name:** Attachments and notes.
3. **Description:** Read and write the legacy `annotation` (Note) entity's attached-file content, distinct from DVT-016's native file-column mechanism.
4. **Business value:** Read and write the legacy `annotation` (Note) entity's attached-file content, distinct from DVT-016's native file-column mechanism.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific; reuses DVT-004's generic CRUD for the `annotation` entity set itself.
10. **Functional requirements:** The `annotation` entity carries `documentbody` (a Base64-encoded string of the attached file content), `filename`, `mimetype`, `isdocument` - corroborated via search of the official attachment-annotation-files documentation and the `annotation` entity reference page, but **not independently verified via a full page fetch this session** (explicit vendor-research gap, carried forward honestly) [vendor research]. The legacy `activitymimeattachment.body` field serves the equivalent role specifically for email/activity attachments [vendor research].
11. **Nonfunctional requirements:** Base64 encode/decode handled transparently by the typed model; explicit write opt-in on create.
12. **Public API contracts or proposed contracts:** Official API: Uses DVT-004's generic CRUD against the `annotations` entity set; the `documentbody`/`filename`/`mimetype` field names are documented but not independently re-fetched this session. Proposed C# contracts:

```csharp
public sealed class DataverseAnnotation
{
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public string? DocumentBodyBase64 { get; set; } // caller decodes/encodes explicitly; the SDK never auto-decodes into a Stream
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
```
13. **Dependencies:** DVT-004.
14. **Security considerations:** Authentication and authorization: Standard table-level Create/Read/Write/Delete privilege on `annotation`. Required permissions/scopes/licenses: Standard security-role privilege; no distinct license requirement documented. Data privacy and security: `documentbody` can carry arbitrarily sensitive attached-file content; never logged or included in diagnostics by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses DVT-004/DVT-006 unchanged. Error handling and concurrency: Standard CRUD error classification; no annotation-specific behavior documented beyond the generic entity CRUD contract.
16. **Unit-test requirements:** `DataverseAnnotation` round-trips `documentbody`/`filename`/`mimetype`/`isdocument` without data loss; unknown fields preserved via `AdditionalData`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic `annotation` row fixture. Integration-test requirements: Authorized sandbox with a real annotation row - not available this pass; field-name facts should be re-verified against a live tenant or a full documentation fetch before this feature ships as `implemented`.
18. **Acceptance criteria:** The documented field names round-trip correctly in a synthetic fixture, with the open gap explicitly recorded, not silently resolved.
19. **Definition of Done:** Standard template; the field-name verification gap above is a release blocker for calling this feature `implemented` with `verification: verified`.
20. **External API entitlement or documentation blockers:** `documentbody`/`filename`/`mimetype`/`isdocument` field names need one more independent confirmation (full-page fetch or live metadata query) before release (Section 28).

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied (field-name verification gap below is an explicit release blocker).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseAnnotation.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseAnnotationTests.cs`. Covers: `documentbody`/`filename`/`mimetype`/`isdocument` round-trip through serialize/deserialize without data loss; unknown fields preserved via `AdditionalData`; `DocumentBodyBase64` is never auto-decoded by the SDK (caller explicitly calls `Convert.FromBase64String` when it wants bytes). No dedicated client method was added - the model is used with DVT-004's existing `CreateRowAsync`/`GetRowAsync`/etc. against the `annotations` entity set. Full suite: 705 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | **Release blocker, per the spec's own language**: `documentbody`/`filename`/`mimetype`/`isdocument` field names are corroborated by vendor-research search only, not independently re-verified via a full page fetch or live metadata query - this keeps the feature at `partial` rather than `implemented`/`verified`. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `DataverseAnnotation` plus `DataverseAnnotationTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; held at `partial` per the spec's own explicit release-blocker language around the unverified field names. |

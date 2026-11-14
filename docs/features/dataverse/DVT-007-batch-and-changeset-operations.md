---
id: "DVT-007"
title: "Batch and changeset operations"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "implemented"
baseline_status: "Implemented: ExecuteBatchAsync builds the documented multipart/mixed $batch wire format (individual requests, nested changesets, Content-ID, CRLF) and parses the response back into BatchItemResult<JsonElement>, reporting a changeset rollback for every member rather than a partial success. Unit/contract-tested; no live tenant available this pass."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-007 - Batch and changeset operations

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-007 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-007.
2. **Feature name:** Batch and changeset operations.
3. **Description:** Submit multiple create/update/delete operations in one HTTP round trip, with Dataverse's specific atomicity semantics.
4. **Business value:** Submit multiple create/update/delete operations in one HTTP round trip, with Dataverse's specific atomicity semantics.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `BatchItemResult<T>` (Section 8.2, item 2) reused for the mixed-outcome response shape; no other reuse - the multipart/mixed wire format itself is new, connector-owned code.
10. **Functional requirements:** `POST {environment}/api/data/v9.2/$batch` with `Content-Type: multipart/mixed; boundary="batch_<id>"`; each top-level part is `Content-Type: application/http` + `Content-Transfer-Encoding: binary` wrapping one HTTP request [D8]. Up to **1,000** individual requests per batch; batches cannot nest [D8]. A **changeset** (`Content-Type: multipart/mixed; boundary="changeset_<id>"`, nested inside the batch body) groups write operations with **all-or-nothing atomicity** - any single failure rolls back the entire changeset; `GET` is not permitted inside a changeset [D8]. Cross-request references within a changeset use `Content-ID` + `$<id>` substitution to let request N reference the not-yet-committed result of request M [D8]. `Prefer: odata.continue-on-error` changes a plain (non-changeset) batch's behavior from "stop at first failure" to "return 200 with per-item success/failure in the response body" [D8]. All line endings inside the multipart body must be CRLF [D8] - a documented, easy-to-get-wrong wire-format detail the SDK must handle internally so callers never construct the multipart body by hand.
11. **Nonfunctional requirements:** Bounded batch size (reject >1,000 locally before send); cancellation-aware; explicit write opt-in gate on any batch containing a write.
12. **Public API contracts or proposed contracts:** Official API: `$batch` multipart/mixed shape, 1,000-request limit, changeset atomicity, CRLF requirement - all per [D8]. Proposed C# contracts:

```csharp
public sealed record DataverseBatchRequest(HttpMethod Method, string RelativeUrl, JsonElement? Body = null, string? ContentId = null);
public sealed record DataverseChangeset(IReadOnlyList<DataverseBatchRequest> Requests); // all-or-nothing
public async Task<IReadOnlyList<BatchItemResult<JsonElement>>> ExecuteBatchAsync(
    IReadOnlyList<DataverseBatchRequest> requests, IReadOnlyList<DataverseChangeset>? changesets = null,
    bool continueOnError = false, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** DVT-003, DVT-004, DVT-008 (upsert can appear inside a batch).
14. **Security considerations:** Authentication and authorization: One bearer token covers the whole batch; individual sub-requests are still evaluated against the same security-role entitlements as if sent standalone. Required permissions/scopes/licenses: Union of whatever privileges the contained sub-requests individually require. Data privacy and security: Batch bodies may contain business data across many rows at once; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: A `GET` sub-request inside a non-changeset batch can itself return a paginated result; the batch as a whole is never auto-retried (it may contain writes). Error handling and concurrency: A changeset failure rolls back atomically per the documented semantics - the SDK must surface which sub-request failed and that the whole changeset was rolled back, not report partial success for a changeset.
16. **Unit-test requirements:** >1,000 requests rejected locally; a changeset failure reports rollback for every member, not just the failing one; `Content-ID` cross-reference substitution round-trips; CRLF line endings are produced regardless of host OS line-ending defaults; `odata.continue-on-error` surfaces per-item results instead of throwing at first failure.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multipart/mixed fixture matching the documented request/response wire format exactly, including a nested changeset. Integration-test requirements: Authorized sandbox with disposable rows for a real changeset-rollback observation.
18. **Acceptance criteria:** A caller never hand-constructs multipart/mixed text; changeset atomicity and non-changeset partial-failure behavior are both observably correct against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live tenant to observe real rollback timing/behavior this pass.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseBatch.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseBatchTests.cs`. Added `IntegrationTransport.SendRawAsync`/`IntegrationRawResponse` to `Koras.Connect.Core` (a generic raw-body/raw-text capability, not Dataverse-specific, since no existing transport method can POST a non-JSON body or read back non-JSON text) and `BatchItemResult<T>` to `Koras.Connect.Abstractions` per the master specification's Section 8.2 proposal - adapted to carry a sanitized `ErrorCode`/`ErrorCorrelationId` pair instead of `Koras.Connect.Exceptions.ErrorDetails`, since Abstractions has no dependency on Core (the reviewed package graph) and the master spec's literal proposal would have created a cycle; also adapted `Value` to plain `T` rather than `T?`, since an unconstrained `T?` does not become `Nullable<T>` for a value-typed `T` (confirmed by compiler error against `BatchItemResult<JsonElement>`) without a `struct` constraint this type intentionally avoids to stay usable with reference-typed results too. Tests cover: >1,000 requests rejected locally; `GET` inside a changeset rejected locally; a write without `writesEnabled` throws before any HTTP call; CRLF line endings are produced; `Content-ID` is both auto-assigned for changeset members and preserved when explicitly supplied; `odata.continue-on-error` surfaces per-item results instead of throwing; a changeset failure (single or missing nested response) reports rollback for every member with the correct index, never a partial success. Full suite: 656 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No authorized sandbox tenant available this pass to observe real rollback timing/behavior or a genuine `Content-ID` cross-reference substitution against a live server. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `ExecuteBatchAsync` plus the shared `IntegrationTransport.SendRawAsync`/`BatchItemResult<T>` extensions (adapted from the master spec's Section 8.2 proposal to resolve a package-graph cycle and a generic-nullability compile error) and `DataverseBatchTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live tenant available this pass. |

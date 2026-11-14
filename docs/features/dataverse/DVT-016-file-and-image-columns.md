---
id: "DVT-016"
title: "File and image columns"
category: "dataverse"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-016 - File and image columns

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-016 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-016.
2. **Feature name:** File and image columns.
3. **Description:** Upload and download binary content stored directly in a Dataverse file/image column (not a related Note/attachment row).
4. **Business value:** Upload and download binary content stored directly in a Dataverse file/image column (not a related Note/attachment row).
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationTransport.CopyToAsync` reused for the download-streaming primitive; the chunked-upload session concept has no Phase 1 precedent (closest analog is ADP's event-notification polling, a different domain).
10. **Functional requirements:** Single-request upload/download for files **under 128 MB** [D15]: `PATCH {entityset}(guid)/{filecolumn}` with `Content-Type: application/octet-stream` + `x-ms-file-name: <name>` header and raw bytes as the body; download via `GET .../{filecolumn}/$value`, returning `x-ms-file-size`/`x-ms-file-name`/`mimetype` response headers [D15]. Chunked upload: initiate with `PATCH .../{filecolumn}?x-ms-file-name=<name>` + `x-ms-transfer-mode: chunked`, receiving a `Location` header (with a `sessiontoken`) and a recommended `x-ms-chunk-size` (4 MiB) [D15]; subsequent `PATCH` requests to that `Location` carry `Content-Range: bytes <start>-<end>/<total>`, returning `206 Partial Content` until the final chunk returns `204` [D15]. Chunked download uses the `Range` header against `.../{filecolumn}/$value`, with the server responding `206` and `x-ms-chunk-size` [D15]. An action-based alternative exists for both SDK and Web API: `InitializeFileBlocksUpload` → repeated `UploadBlock` (Base64 `BlockData`, ≤4 MB/block) → `CommitFileBlocksUpload`; mirrored by `InitializeFileBlocksDownload`/`DownloadBlock` for download; `DeleteFile` action or a plain `DELETE` on the column URL removes content [D15]. Column max size is exposed via `FileAttributeMetadata.MaxSizeInKB` (DVT-009's metadata surface) [D15]. On-premises/BYOK environments may disable chunking entirely (`IsChunkingSupported=false`) [D15] - the connector must check this rather than assume chunking is always available.
11. **Nonfunctional requirements:** Streamed upload/download (no whole-file buffering for large files); cancellation-aware mid-transfer; explicit write opt-in on upload/delete.
12. **Public API contracts or proposed contracts:** Official API: Single-request and chunked upload/download shapes, action-based alternative, `IsChunkingSupported` caveat - all per [D15]. Proposed C# contracts:

```csharp
public async Task UploadFileColumnAsync(string entitySet, Guid id, string column, Stream content, string fileName, bool writesEnabled = false, CancellationToken ct = default);
public async Task DownloadFileColumnAsync(string entitySet, Guid id, string column, Stream destination, CancellationToken ct = default);
public async Task DeleteFileColumnAsync(string entitySet, Guid id, string column, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** DVT-004, DVT-009.
14. **Security considerations:** Authentication and authorization: Write privilege on the table for upload/delete; Read for download. Required permissions/scopes/licenses: Standard table-level security privileges; `FileAttributeMetadata.MaxSizeInKB` is a tenant-configured column limit, not a license gate. Data privacy and security: File content is caller-owned, potentially highly sensitive binary data - never buffered into a log or diagnostic payload.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Chunked transfer is its own sequencing mechanism, not reusing `Page<T>`; chunk-level retry is a connector-local concern (a failed chunk PATCH is not automatically retried by the shared GET-only transport policy, since PATCH is a write). Error handling and concurrency: A file exceeding `MaxSizeInKB` should be rejected locally once metadata is known, rather than only discovered via a server error.
16. **Unit-test requirements:** Small-file single-request upload/download round-trips; chunked upload sequencing (session initiation, sequential chunk PATCH, final-chunk 204) matches the documented protocol; chunked download `Range`/`206` handling matches; interrupted mid-transfer preserves partial bytes without corrupting the destination stream (matching the existing Qualtrics/SAP "partial bytes" test pattern).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both single-request and chunked transfer shapes. Integration-test requirements: Authorized sandbox table with a real file column, including a file large enough to require chunking - not available this pass.
18. **Acceptance criteria:** Both transfer modes round-trip correctly against synthetic fixtures; a file exceeding the known column limit is rejected before any upload is attempted.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No live tenant with a file column available this pass; exact behavior when `IsChunkingSupported=false` is encountered mid-transfer (vs. known in advance) was not independently tested.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. No live tenant with a file column available this pass; exact behavior when `IsChunkingSupported=false` is encountered mid-transfer (vs. known in advance) was not independently tested. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

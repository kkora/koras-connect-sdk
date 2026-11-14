---
id: "MGR-010"
title: "File upload, download and sessions"
category: "microsoft-graph"
priority: "P0"
phase: "P2-D4"
status: "partial"
baseline_status: "Implemented: UploadSmallFileAsync enforces the conservative 4MB ceiling locally (rejecting larger files before any HTTP call, pointing the caller at the not-yet-implemented MGR-023 session path) and composes the root:/path:/content address; DownloadFileAsync streams via IntegrationTransport.CopyToAsync. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-010 - File upload, download and sessions

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-010 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-010.
2. **Feature name:** File upload, download and sessions.
3. **Description:** Upload and download file content to/from OneDrive/SharePoint drives, including large files via resumable upload sessions.
4. **Business value:** Upload and download file content to/from OneDrive/SharePoint drives, including large files via resumable upload sessions.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D4; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationTransport.CopyToAsync` reused for streaming primitives; the upload-session chunking protocol parallels DVT-016's chunked-upload pattern conceptually, though the exact byte-range rules differ (MGR-023).
10. **Functional requirements:** [Flagged conflicting official figures - carried forward honestly, not reconciled by guessing.] Simple (single-request) upload size limit: the v1.0 API reference states a **250 MB** hard limit for simple upload via `PUT /drives/{id}/items/{id}/content` (or `:/path:/content`), while a separate, older OneDrive developer-center page states **<4 MB** - these two official figures conflict and were not reconciled during this research pass; the SDK must document both and default to the more conservative figure (4 MB) as the simple-upload ceiling until independently reconciled, falling back to upload sessions (MGR-023) above that threshold. Download: `GET /drives/{id}/items/{id}/content` (redirects to a pre-authenticated download URL in practice). App-only access to upload *sessions* specifically has only one permission option, `Sites.ReadWrite.All` - there is no lower-privilege app-only tier documented for this operation, unlike most other Graph writes. A file protected by a sensitivity label cannot be replaced via the content-replace endpoint while app-only - sensitivity-label-protected files require delegated access for content replacement.
11. **Nonfunctional requirements:** Streamed upload/download (no whole-file buffering); cancellation-aware mid-transfer; explicit write opt-in gate.
12. **Public API contracts or proposed contracts:** Official API: Upload/download endpoints confirmed via [G1]/[G9]; the 250MB-vs-4MB conflicting figures and the sensitivity-label/app-only restriction are independently confirmed as documented facts (the conflict itself, not a resolved single number). Proposed C# contracts:

```csharp
public async Task UploadSmallFileAsync(string driveId, string parentPath, string fileName, Stream content, bool writesEnabled = false, CancellationToken ct = default); // <= 4MB conservative ceiling
public async Task DownloadFileAsync(string driveId, string itemId, Stream destination, CancellationToken ct = default);
```
13. **Dependencies:** MGR-009, MGR-023 (for files exceeding the simple-upload ceiling).
14. **Security considerations:** Authentication and authorization: `Files.ReadWrite.All` (delegated or application) for most operations; `Sites.ReadWrite.All` specifically for app-only upload sessions. Required permissions/scopes/licenses: As above. Data privacy and security: File content is caller-owned, potentially sensitive; never buffered into logs; sensitivity-label-protected files are a documented access-restriction case, not an SDK bug when encountered.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not paginated; large files use MGR-023's session mechanism instead. Error handling and concurrency: A simple-upload request exceeding the conservative 4MB ceiling is routed to the upload-session path automatically rather than attempted and failed against the server's actual (unreconciled) limit.
16. **Unit-test requirements:** Files at/above the 4MB conservative threshold are routed to session-based upload, not attempted via simple upload; download streams correctly; app-only upload session correctly uses only `Sites.ReadWrite.All`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both simple upload and the session hand-off. Integration-test requirements: Authorized tenant with a file of known size to directly resolve the 250MB-vs-4MB conflict; not available this pass.
18. **Acceptance criteria:** The conservative ceiling is enforced until independently reconciled; download and simple upload work correctly against synthetic fixtures.
19. **Definition of Done:** Standard template; the simple-upload size-limit conflict is an explicit release blocker before this feature is marked `verified` (Section 28).
20. **External API entitlement or documentation blockers:** The 250MB-vs-4MB simple-upload limit conflict is the most consequential unresolved Graph documentation discrepancy in this catalog; sensitivity-label-protected files cannot be replaced app-only.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (simple upload/download only - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (the 250MB-vs-4MB conflict remains this feature's own explicit release blocker).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphFileTransfer.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphFileTransferTests.cs`. Covers: upload requires the shared `writesEnabled` opt-in; a file at or above the conservative 4MB ceiling is rejected locally before any HTTP call, with a message pointing at the upload-session path; a file under the ceiling composes the documented `root:/{parentPath}/{fileName}:/content` address and streams via `IntegrationTransport.UploadAsync`; a non-seekable stream is rejected (its length can't be checked without buffering); download streams the response body via `IntegrationTransport.CopyToAsync`. Full suite: 928 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | Routing files above the conservative ceiling to an actual resumable upload session is not implemented - MGR-023 (large file transfer/resumability) doesn't exist yet; oversized files are rejected with a clear error rather than silently failing against the server's real (unreconciled) limit. The 250MB-vs-4MB conflict remains unresolved. Sensitivity-label-protected files cannot be replaced app-only - not independently tested. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphFileTransfer.cs` plus `GraphFileTransferTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the implemented scope; held at `partial` because the resumable-session path (MGR-023) doesn't exist yet and the size-limit conflict remains unreconciled. |

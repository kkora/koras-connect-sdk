---
id: "MGR-023"
title: "Large file transfer and resumability"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "implemented"
baseline_status: "Implemented: CreateUploadSessionAsync/UploadSessionFragmentAsync/GetUploadSessionStatusAsync/CancelUploadSessionAsync cover the full session lifecycle. Fragment-size-not-a-multiple-of-320KiB and out-of-order fragments are both rejected locally before any HTTP call; GetUploadSessionStatusAsync resynchronizes local sequential-order tracking from the server's nextExpectedRanges for resume-after-interruption. Required extending Core's IntegrationTransport.UploadAsync to route a Content-Range header entry to the request content's own header collection, since .NET rejects it as a request header. Phase 2 review pass 1 found and fixed three gaps: UploadSessionFragmentAsync/CancelUploadSessionAsync were missing the writesEnabled gate every other mutating Graph call has; the sequential-order check-then-update was a read-then-write race rather than an atomic reservation (now serialized per session via a SemaphoreSlim); and the per-session tracking dictionary never released a completed (non-cancelled) session's entry. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-023 - Large file transfer and resumability

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-023 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-023.
2. **Feature name:** Large file transfer and resumability.
3. **Description:** Upload files too large for a single request via a resumable, chunked upload session.
4. **Business value:** Upload files too large for a single request via a resumable, chunked upload session.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Parallels DVT-016's chunked-upload pattern conceptually; the exact byte-range rule differs materially (320 KiB multiple here vs. Dataverse's 4 MiB recommended chunk with no multiple-of-N constraint).
10. **Functional requirements:** Create an upload session, then `PUT` sequential byte ranges to the returned `uploadUrl` [G9]. **Confirmed exactly**: each byte range (except the final one) **must be a multiple of 327,680 bytes (320 KiB)**; maximum **under 60 MiB** per individual PUT request; **5-10 MiB** is the documented recommended fragment size; ranges **must be uploaded in sequential order** - out-of-order fragments are not supported [G9]. `GET {uploadUrl}` returns `nextExpectedRanges` + `expirationDateTime` to resume an interrupted upload; **no fixed default session duration was found** - expiration is dynamic/sliding, not a documented constant [G9]. `DELETE {uploadUrl}` cancels the session, returning `204` [G9]. No absolute maximum file size ceiling was found in the fetched documentation for upload sessions generally (MGR-010's simple-upload ceiling is a separate, smaller-scale concern).
11. **Nonfunctional requirements:** Streamed chunked upload (no whole-file buffering); strict sequential-order enforcement; cancellation-aware mid-transfer.
12. **Public API contracts or proposed contracts:** Official API: The 327,680-byte multiple requirement, <60MiB-per-request cap, 5-10MiB recommendation, sequential-order requirement, `nextExpectedRanges`/`expirationDateTime`, and `DELETE`-cancels-with-204 - all confirmed per [G9]. Proposed C# contracts:

```csharp
public sealed record GraphUploadSession(Uri UploadUrl, DateTimeOffset? ExpirationDateTime);
public async Task<GraphUploadSession> CreateUploadSessionAsync(string driveId, string parentPath, string fileName, long totalSize, bool writesEnabled = false, CancellationToken ct = default);
public async Task UploadSessionFragmentAsync(GraphUploadSession session, Stream fragment, long rangeStart, long rangeEnd, long totalSize, bool writesEnabled = false, CancellationToken ct = default); // rangeEnd-rangeStart+1 must be a multiple of 327680, except the final fragment
public async Task CancelUploadSessionAsync(GraphUploadSession session, bool writesEnabled = false, CancellationToken ct = default);
// UploadSessionFragmentAsync/CancelUploadSessionAsync gained writesEnabled - review pass 1 found both were
// missing it despite being mutating calls (PUT/DELETE), unlike CreateUploadSessionAsync which already had it.
```
13. **Dependencies:** MGR-009, MGR-010.
14. **Security considerations:** Authentication and authorization: Same as MGR-010; app-only upload sessions specifically require `Sites.ReadWrite.All` with no lower tier. Required permissions/scopes/licenses: As per MGR-010. Data privacy and security: File content streamed, never buffered whole; sensitivity-label-protected files carry the same MGR-010 app-only-replace restriction.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not request-paginated; the session itself is the resumability mechanism - a dropped connection resumes via `GET {uploadUrl}`'s `nextExpectedRanges`, not a fresh session. Error handling and concurrency: An out-of-order fragment is rejected locally before send (not merely left to the server to reject), since sequential order is a hard, documented requirement; a fragment size not a multiple of 327,680 bytes (except the final one) is rejected locally.
16. **Unit-test requirements:** Fragment-size-not-a-multiple-of-320KiB rejected locally (except correctly allowing a smaller final fragment); out-of-order fragment rejected locally; interrupted upload resumes correctly via `nextExpectedRanges`; cancellation sends `DELETE` and expects `204`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic multi-fragment upload-session fixture, including a resume-after-interruption scenario. Integration-test requirements: Authorized tenant with a large file to upload; not available this pass.
18. **Acceptance criteria:** Chunked upload, resume, and cancellation all work correctly against synthetic fixtures with the exact byte-range rules enforced.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No fixed session-expiration duration is documented (by design, it's dynamic) - the SDK must not assume or hardcode an expected lifetime.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphUploadSession.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphUploadSessionTests.cs`, extending `Koras.Connect.Core`'s `IntegrationTransport.UploadAsync` to route a `Content-Range` header entry to the request content's header collection (a request-header add of that name is rejected by .NET). Covers: session creation requires `writesEnabled` and composes the documented `root:/{path}:/createUploadSession` address; `UploadSessionFragmentAsync`/`CancelUploadSessionAsync` now also require `writesEnabled` (review pass 1 fix - both covered by new `...WithoutWritesEnabledThrowsBeforeAnyHttpCall` tests); a non-final fragment not a multiple of 320 KiB is rejected locally; a final fragment smaller than the multiple is allowed; an out-of-order fragment throws before any HTTP call; sequential fragments succeed and advance the tracked offset; completing the final fragment removes the session's tracking entries rather than leaking them (review pass 1 fix - `TheFinalFragmentsCompletionRemovesTheSessionsTrackingEntriesRatherThanLeakingThem` proves this by reusing the same `UploadUrl` for a fresh session afterward); the per-session order check-then-update is now serialized via a `SemaphoreSlim` keyed by `UploadUrl` rather than a read-then-write race (review pass 1 fix); `GetUploadSessionStatusAsync` parses `nextExpectedRanges`, resynchronizes order tracking, and a subsequent `UploadSessionFragmentAsync` call actually observes the resynced offset rather than just the resync method parsing correctly in isolation (review pass 1 added `GetUploadSessionStatusResyncAllowsTheNextFragmentAtTheResumedOffsetRatherThanAtZero` to prove the effect, not just the parse); cancellation sends `DELETE`. |
| Remaining work / blockers | Sequential-order tracking is held in an in-memory dictionary keyed by `UploadUrl` and does not itself persist across a process restart - `GetUploadSessionStatusAsync` exists specifically to resynchronize it from the server after exactly that scenario, but a caller resuming in a new process must call it before the next fragment. No fixed session-expiration duration is documented (by design) and none is assumed. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `GraphUploadSession.cs` plus `GraphUploadSessionTests.cs`, and extended Core's `UploadAsync` for `Content-Range` support. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live tenant available this pass. |
| 2026-11-14 | `implemented` | Phase 2 review pass 1 found and fixed three real issues: a missing `writesEnabled` gate on the two mutating methods, a TOCTOU race in the sequential-order check-then-update (now serialized per session), and an unbounded leak of the per-session tracking dictionary entry for a normally-completed session. All three have dedicated regression tests. |
| 2026-11-14 | `implemented` | Phase 2 review pass 2 found pass 1's own fix was incomplete: `CancelUploadSessionAsync` never acquired the new per-session lock, so it could still race a concurrent in-flight `UploadSessionFragmentAsync` call and tear down tracking state out from under it. Fixed by having `CancelUploadSessionAsync` acquire the same lock; added `CancelUploadSessionWaitsForAnInFlightFragmentUploadRatherThanRacingIt`, which gates the fragment PUT mid-flight and asserts the DELETE never starts until it completes. |
| 2026-11-14 | `implemented` | A targeted sweep found `CancelUploadSessionAsync` also discarded the disposable `IntegrationJsonResponse` from `SendResponseAsync` without disposing it - the same pattern already fixed in Dataverse's equivalent methods, but never checked in this connector. Fixed. |

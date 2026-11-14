---
id: "SFD-016"
title: "Files and ContentVersion"
category: "salesforce"
priority: "P1"
phase: "P2-D5"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# SFD-016 - Files and ContentVersion

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-016 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-016.
2. **Feature name:** Files and ContentVersion.
3. **Description:** Upload, link, and retrieve file attachments stored as Salesforce Content (the modern file-storage model, superseding legacy Attachments).
4. **Business value:** Upload, link, and retrieve file attachments stored as Salesforce Content (the modern file-storage model, superseding legacy Attachments).
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Follows SFD-004's generic sObject-create pattern for `ContentVersion`.
10. **Functional requirements:** sObjects involved: `ContentVersion`, `ContentDocument`, `ContentDocumentLink` [snippet]. Upload: create a `ContentVersion` with `{Title (string, max 255), PathOnClient (string, max 500), VersionData (Base64-encoded file content)}` via the standard sObject-create mechanism (SFD-004) [snippet]. Linking: the created `ContentVersion.ContentDocumentId` is used to create a `ContentDocumentLink{ContentDocumentId, LinkedEntityId, ShareType, Visibility}` record associating the file with any other record [snippet].
11. **Nonfunctional requirements:** Streamed Base64 encoding for large files to avoid full in-memory buffering; explicit write opt-in gate, matching SFD-004.
12. **Public API contracts or proposed contracts:** Official API: `ContentVersion`/`ContentDocument`/`ContentDocumentLink` field shapes are snippet-sourced from the object-reference documentation pages for each; the upload mechanism (reusing sObject-create) is a direct, low-risk inference from the directly-confirmed SFD-004 mechanism, not independently re-confirmed for this specific object. Proposed C# contracts:

```csharp
public async Task<string> UploadContentVersionAsync(string title, string pathOnClient, Stream content, bool writesEnabled = false, CancellationToken ct = default);
public async Task LinkContentDocumentAsync(string contentDocumentId, string linkedEntityId, string shareType, string visibility, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** SFD-004.
14. **Security considerations:** Authentication and authorization: Create permission on `ContentVersion`/`ContentDocumentLink`; sharing-model-dependent visibility on the linked record. Required permissions/scopes/licenses: Standard Salesforce Files feature entitlement (included in all editions that support the modern Content model). Data privacy and security: File content is caller-owned, potentially highly sensitive binary data; Base64-encoded in transit, never logged.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Single-record writes; never auto-retried. Error handling and concurrency: Standard sObject-create error shape (SFD-019).
16. **Unit-test requirements:** `VersionData` Base64 encoding round-trips without corruption; `ContentDocumentLink` composition matches the documented field shape.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for both the upload and link steps. Integration-test requirements: Authorized sandbox org; not available this pass.
18. **Acceptance criteria:** Upload-then-link round-trips correctly against synthetic fixtures.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** A REST-specific file-size cap was not found; only a SOAP API figure (50MB) was located, and it is explicitly **not** assumed to apply to the REST path without independent confirmation (Section 28).

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. A REST-specific file-size cap was not found; only a SOAP API figure (50MB) was located, and it is explicitly **not** assumed to apply to the REST path without independent confirmation (Section 28). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

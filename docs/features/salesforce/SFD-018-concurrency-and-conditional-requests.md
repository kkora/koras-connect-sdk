---
id: "SFD-018"
title: "Concurrency and conditional requests"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "partial"
baseline_status: "Implemented: UpdateRecordAsync gained an ifUnmodifiedSince parameter sending the documented If-Unmodified-Since header; a 412 response classifies as the new SalesforceConcurrencyException (mirroring DVT-013). Strong validation (ETag/If-Match) is not exposed - its scope was flagged as possibly Account-only and unconfirmed. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-018 - Concurrency and conditional requests

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-018 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-018.
2. **Feature name:** Concurrency and conditional requests.
3. **Description:** Prevent a caller from silently overwriting a record that changed since it was last read, and avoid unnecessary re-fetches of unchanged data.
4. **Business value:** Prevent a caller from silently overwriting a record that changed since it was last read, and avoid unnecessary re-fetches of unchanged data.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Salesforce-specific; conceptually parallel to DVT-013's ETag handling, with materially different (and narrower) official scope.
10. **Functional requirements:** Salesforce documents a dedicated "Conditional Request Headers" mechanism [snippet], correcting an initial assumption that Salesforce lacks this entirely. Strong validation: `ETag` response header, `If-Match`, `If-None-Match` - **[snippet, flagged for independent re-verification]**: one source described this as scoped to **"Account records only"**, an unusually narrow scope that is inconsistent with how most REST APIs apply ETags uniformly, and must not be treated as settled fact until re-checked against a live page. Weak validation (broader scope, per snippet): `If-Modified-Since`/`If-Unmodified-Since` on sObject Rows, sObject Describe, Describe Global, and Invocable Actions. Documented behavior: a malformed conditional header → 400; `If-Match` mismatch → 412; `If-None-Match` match on GET → 304, on PATCH → 412; `If-Unmodified-Since` violated → 412; `If-Range` and DELETE are explicitly unsupported for conditional semantics.
11. **Nonfunctional requirements:** None beyond explicit write opt-in, already covered by SFD-004.
12. **Public API contracts or proposed contracts:** Official API: All of the above is snippet-sourced from `developer.salesforce.com/docs/atlas.en-us.api_rest.meta/api_rest/intro_rest_conditional_requests.htm`; the "Account-only" ETag scoping claim in particular is explicitly flagged as unconfirmed and higher-risk than the rest of this feature's facts. Proposed C# contracts:

```csharp
public async Task UpdateRecordAsync(string sObject, string id, JsonElement fields, string? ifUnmodifiedSince = null, bool writesEnabled = false, CancellationToken ct = default);
// A 412 response classifies to a dedicated SalesforceConcurrencyException : Koras.Connect.Exceptions.ContractException,
// mirroring DVT-013's decision not to extend the shared classifier for a single-vendor 412 case.
```
13. **Dependencies:** SFD-004.
14. **Security considerations:** Authentication and authorization: Unchanged from SFD-004. Required permissions/scopes/licenses: None beyond SFD-004's. Data privacy and security: ETags/timestamps are opaque version/time tokens, not business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable; concurrency conflicts are never auto-retried. Error handling and concurrency: This feature's entire subject is distinguishing 412/304/400 from other 4xx outcomes reliably.
16. **Unit-test requirements:** `If-Unmodified-Since` sent correctly when supplied and omitted when not; a `412` response classifies distinctly as `SalesforceConcurrencyException`, not the generic classifier. **Narrowed by Phase 2 review pass 1**: this doc previously also listed `304`/`400` as "each classify distinctly," but neither is actually reachable by the shipped implementation - only `If-Unmodified-Since` (weak validation) is exposed, and a `304`/`400` response to a `PATCH` with that single header is not a code path this SDK produces or has a dedicated classification for; it falls through to the standard classifier like any other non-412 failure, which is already covered by `EveryOtherFailureStillGoesThroughTheStandardClassifier`. The `304`/`400` distinct-classification requirement would only become real work once strong validation (`If-Match`/`ETag`) is implemented. The SDK does not assume `If-Match`/ETag support beyond `Account` until independently confirmed - meaning the SDK's own typed concurrency helper should be documented as broadly available only for the weak-validation (`If-Modified-Since`/`If-Unmodified-Since`) path unless/until the strong-validation scope is confirmed wider than Account.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for 412/304/400. Integration-test requirements: Authorized sandbox org with both an Account record and a non-Account record, to directly test the flagged ETag-scope claim; not available this pass.
18. **Acceptance criteria:** 412/304/400 are classified correctly; the Account-only ETag scope claim is either confirmed or corrected before this feature ships as `verified`.
19. **Definition of Done:** Standard template; the ETag-scope re-verification is an explicit release blocker (Section 28).
20. **External API entitlement or documentation blockers:** The "Account records only" ETag-scoping claim is the highest-priority re-verification item in this feature.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (weak-validation path only - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (ETag-scope re-verification remains this feature's own explicit release blocker).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: extended `UpdateRecordAsync` (`src/Koras.Connect.Salesforce/SalesforceRecords.cs`) with an `ifUnmodifiedSince` parameter and added `SalesforceConcurrencyException` plus `tests/Koras.Connect.Tests/Salesforce/SalesforceConcurrencyTests.cs`. Covers: the `If-Unmodified-Since` header is sent when supplied and omitted when not; a `412` response classifies as `SalesforceConcurrencyException` distinct from every other failure, which still goes through the standard classifier. |
| Remaining work / blockers | Strong validation (`ETag`/`If-Match`) is not exposed - one source scoped it to Account records only, an unusually narrow and unconfirmed claim that remains this feature's highest-priority re-verification item. `If-None-Match` (304) and malformed-header (400) classification are not separately exercised since this SDK's current read paths do not accept conditional headers; `If-Range`/DELETE conditional semantics are correctly not implemented, matching the documented "explicitly unsupported" guidance. No live org available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `ifUnmodifiedSince` to `UpdateRecordAsync` plus `SalesforceConcurrencyException` and `SalesforceConcurrencyTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the weak-validation scope; held at `partial` because strong validation's scope remains unconfirmed. |
| 2026-11-14 | `partial` | Phase 2 review pass 1 found the unit-test-requirements bullet overclaimed distinct `304`/`400` classification that the shipped implementation cannot actually reach (only weak validation is exposed) - narrowed the requirement to match reality instead of adding tests for an unreachable path. No code changed. |

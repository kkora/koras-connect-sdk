---
id: "SFD-019"
title: "Error normalization and partial successes"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "partial"
baseline_status: "Implemented: SalesforceApiException.ClassifyErrorCode maps documented errorCode values (INVALID_SESSION_ID/INSUFFICIENT_ACCESS/REQUEST_LIMIT_EXCEEDED plus a ContractException default) into the typed hierarchy; ExecuteCompositeAsync (SFD-009) now extracts the real per-subrequest errorCode via the existing TryExtractErrorCode helper instead of a generic http_{status} code, falling back to it when extraction fails. The proposed contract's signature was adapted to accept an explicit HttpStatusCode, since the type structurally requires one. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-019 - Error normalization and partial successes

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-019 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-019.
2. **Feature name:** Error normalization and partial successes.
3. **Description:** Present every Salesforce failure - single-record or composite/batch partial failure - through the SDK's own typed exception hierarchy rather than forcing every caller to parse raw Salesforce JSON error arrays themselves.
4. **Business value:** Present every Salesforce failure - single-record or composite/batch partial failure - through the SDK's own typed exception hierarchy rather than forcing every caller to parse raw Salesforce JSON error arrays themselves.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationExceptionClassifier`/`BatchItemResult<T>` reused (Section 8.1/8.2).
10. **Functional requirements:** Standard error body: a JSON array of `{message, errorCode, fields[]}` [snippet]. Observed `errorCode` values (snippet): `REQUIRED_FIELD_MISSING, INVALID_FIELD, MALFORMED_ID, DUPLICATE_VALUE, FIELD_INTEGRITY_EXCEPTION, INSUFFICIENT_ACCESS, INVALID_SESSION_ID, RECORD_NOT_FOUND, ENTITY_IS_DELETED, ENTITY_IS_LOCKED, STORAGE_LIMIT_EXCEEDED, REQUEST_LIMIT_EXCEEDED, INVALID_CROSS_REFERENCE_KEY`. Composite-batch partial-success behavior (snippet, corroborating SFD-009): the outer HTTP status remains 200 even when some subrequests fail; the body carries `hasErrors`+`results[]`, each with its own `statusCode`/`result` - callers must inspect per-subrequest status, never just the outer code.
11. **Nonfunctional requirements:** None beyond standard exception-mapping discipline.
12. **Public API contracts or proposed contracts:** Official API: Error body shape and errorCode list are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/errorcodes.html`; composite-batch partial-success corroborates SFD-009's own snippet-sourced shape. No verbatim raw JSON error sample was independently retrieved this pass. Proposed C# contracts:

```csharp
public sealed class SalesforceApiException : IntegrationException
{
    public static SalesforceApiException Classify(string errorCode, string message, IReadOnlyList<string>? fields);
    public static SalesforceApiException FromTransportFailure(IntegrationRequestException inner);
}
```
13. **Dependencies:** SFD-004, SFD-009.
14. **Security considerations:** Authentication and authorization: `INVALID_SESSION_ID`/`INSUFFICIENT_ACCESS` map to `AuthenticationException`/`AuthorizationException` respectively. Required permissions/scopes/licenses: Not applicable (this is error-handling infrastructure). Data privacy and security: Error messages may echo back submitted field values in some cases; treated as potentially sensitive and not logged verbatim by default, matching SAP's `message.value`-is-tenant-specific redaction precedent exactly.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: `REQUEST_LIMIT_EXCEEDED` maps to `ThrottleException` (SFD-017's no-`Retry-After` caveat applies here too). Error handling and concurrency: Every documented `errorCode` maps to a specific exception type or a reasoned default (`ContractException`) for unrecognized codes - matching every existing connector's classifier-default discipline.
16. **Unit-test requirements:** Every listed `errorCode` maps to the correct exception type; an unrecognized `errorCode` falls back to `ContractException` rather than throwing an unrelated error; composite-batch partial failure surfaces per-subrequest status via `BatchItemResult<T>`, not a single aggregate exception.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixtures for each documented `errorCode`, explicitly labeled snippet-confidence pending a verbatim raw-JSON sample. Integration-test requirements: Authorized sandbox org to capture at least one real raw error body; not available this pass.
18. **Acceptance criteria:** Every documented `errorCode` classifies correctly; composite-batch partial success is never misreported as either "fully succeeded" or "fully failed."
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** No verbatim raw JSON error sample was independently retrieved; re-verify exact body shape (array vs. object wrapper, field casing) before release.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (adapted signature - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (raw body shape not independently re-verified).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `SalesforceApiException.ClassifyErrorCode` (`src/Koras.Connect.Salesforce/SalesforceOptions.cs`) and wired it into `ExecuteCompositeAsync`'s per-subrequest result (`src/Koras.Connect.Salesforce/SalesforceComposite.cs`), plus `tests/Koras.Connect.Tests/Salesforce/SalesforceErrorNormalizationTests.cs`. Covers: `INVALID_SESSION_ID`→`AuthenticationException`, `INSUFFICIENT_ACCESS`→`AuthorizationException`, `REQUEST_LIMIT_EXCEEDED`→`ThrottleException` with no assumed `RetryAfter`; every other documented code (`REQUIRED_FIELD_MISSING`/`INVALID_FIELD`/`MALFORMED_ID`/`DUPLICATE_VALUE`/`FIELD_INTEGRITY_EXCEPTION`/`RECORD_NOT_FOUND`/`ENTITY_IS_DELETED`/`ENTITY_IS_LOCKED`/`STORAGE_LIMIT_EXCEEDED`/`INVALID_CROSS_REFERENCE_KEY`) and an unrecognized code both classify as `ContractException`; the raw `message` is never attached to the thrown exception. `SalesforceCompositeTests.cs` updated: a failed subrequest with an extractable body now reports the real `errorCode` (e.g. `NOT_FOUND`) via `BatchItemResult<T>.ErrorCode` instead of a generic `http_{status}`, with a new test confirming the `http_{status}` fallback when the body shape doesn't match. Full suite: 845 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The proposed `Classify(string errorCode, string message, IReadOnlyList<string>? fields)` contract was adapted to `ClassifyErrorCode(HttpStatusCode, string errorCode, ...)` - the type structurally requires a status code, and this SDK's standard single-record write/read paths never expose a failed response's body in the first place (by design, to avoid logging business data), so this classifier is reachable today only from `/composite` subrequests, which do carry both. No verbatim raw JSON error sample was independently retrieved; body shape (array vs. object wrapper, field casing) remains unverified. No live org available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `SalesforceApiException.ClassifyErrorCode` and wired it into `ExecuteCompositeAsync`, plus `SalesforceErrorNormalizationTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates for the implemented scope; held at `partial` because the raw body shape remains unverified and the classifier is reachable only from the composite path today. |

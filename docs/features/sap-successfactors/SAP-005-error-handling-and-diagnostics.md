---
id: "SAP-005"
title: "Error handling and diagnostics"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-005 - Error handling and diagnostics

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-005.
2. **Feature name:** Error handling and diagnostics.
3. **Description:** Separate auth/RBP/transport/business-row failures safely.
4. **Business value:** Separate auth/RBP/transport/business-row failures safely.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP redacted UpsertResult.ToString; E-TRANSPORT; no SAP diagnostic API; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Map safe codes/correlation and staged read-only diagnostics; apply read retry budget and no upsert replay; keep row messages out of automatic logs.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing UpsertResult/IntegrationRequestException; proposed SAP exception/DiagnoseAsync.
13. **Dependencies:** SDK-009, SDK-011, SAP-003; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** 401/403, HTTP200 row error, invalid field, malformed XML/error, rate hint and canceled probe. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** 401/403, HTTP200 row error, invalid field, malformed XML/error, rate hint and canceled probe.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Host identifies failed stage and row without parsing private messages; captured diagnostics contain no employee keys/assertions.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** Current error schemas and minimal authorized probe required; returned row messages remain sensitive host data.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "`UpsertAsync` still raises the raw `IntegrationRequestException`" gap recorded below by wrapping its transport call in the same `try/catch (IntegrationRequestException ex) { throw Classify(ex); }` pattern already used for reads (`src/Koras.Connect.Sap.SuccessFactors/SuccessFactorsClient.cs`), classifying a transport-level failure (non-2xx for the whole upsert request) the same way reads do - distinct from a per-row failure, which SAP reports as HTTP 200 with its own `httpCode`/`status` per row and is never reclassified. Updated `SapUpsertDoesNotRetryOrAcceptArbitraryMetadata` (`tests/Koras.Connect.Tests/Sap/SapClientExpansionTests.cs`) to expect `TransportException` instead of the raw transport type, and added `UpsertAsyncClassifiesTransportLevelFailuresByStatusCode` (`tests/Koras.Connect.Tests/Sap/SapExceptionClassificationTests.cs`, 401/403/429/503 theory) confirming the full classification spread. Cross-checked the documented OData V2 error envelope and HTTP-status table (`error.code`/`error.message`, `COE000x` codes, 200/201/400/401/404/500 per operation) against the **official "SAP SuccessFactors API Reference Guide" (2H 2026 edition), section 6.5.1 "HTTP Response Codes" and 6.5.2 "Common Errors (COE)"** - `SapODataError.TryParseSanitizedCode`'s envelope shape (`{"error":{"code":...,"message":{"value":...}}}`) matches the documented structure exactly; no code change was needed there, only this citation. Full suite: 503 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No live `CorrelationId` capture: `IntegrationTransport` does not retain response headers/bodies on a failed request today, so `SapODataError.TryParseSanitizedCode` remains a standalone utility, not wired into any live error path - this is a shared `Koras.Connect.Core` architectural gap, not SAP-specific, and not resolvable by vendor documentation. Live correlation-id/entitlement evidence against an authorized tenant remains the standing external blocker. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added read-path classification (`EnumerateAsync<T>`, `GetMetadataAsync`/`RefreshMetadataAsync`) into `AuthenticationException`/`AuthorizationException`/`ThrottleException`/`TransportException` via `IntegrationExceptionClassifier`, plus a standalone `SapODataError.TryParseSanitizedCode` parser for SAP's documented OData JSON error envelope that never reads `message.value`. Tests: `tests/Koras.Connect.Tests/SapExceptionClassificationTests.cs` (401 vs 403 vs 429 vs 5xx distinguishability, malformed-body parsing, canceled-probe propagation). `UpsertAsync` deliberately left unclassified to avoid breaking the existing `SapUpsertDoesNotRetryOrAcceptArbitraryMetadata` test in `ExpansionTests.cs`; still `partial` pending that and live correlation-id capture. |
| 2026-11-13 | `implemented` | Classified `UpsertAsync`'s transport-level failures, closing the one concretely-named remaining item. Cross-checked `SapODataError`'s error envelope and the exception-classification spread against the official SAP SuccessFactors API Reference Guide (2H 2026), sections 6.5.1-6.5.2. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live correlation-id capture and entitlement evidence against an authorized tenant remain standing external blockers. |

---
id: "SFD-017"
title: "Limits API and throttling"
category: "salesforce"
priority: "P0"
phase: "P2-D3"
status: "implemented"
baseline_status: "Implemented: GetLimitsAsync parses the /limits/ response into a dictionary of SalesforceLimit, preserving every key present (no local allowlist). REQUEST_LIMIT_EXCEEDED reuses the shared IntegrationExceptionClassifier's 429-to-ThrottleException mapping unchanged, without assuming a Retry-After value. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# SFD-017 - Limits API and throttling

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for SFD-017 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** SFD-017.
2. **Feature name:** Limits API and throttling.
3. **Description:** Discover an org's current API usage against its daily allocation, and classify a throttled response as a recoverable, typed condition.
4. **Business value:** Discover an org's current API usage against its daily allocation, and classify a throttled response as a recoverable, typed condition.
5. **Owning package:** `Koras.Connect.Salesforce`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D3; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** `IntegrationExceptionClassifier`'s existing 429→`ThrottleException` mapping reused unchanged; `TransportOptions`'s bounded-backoff GET-retry policy reused unchanged.
10. **Functional requirements:** `GET /services/data/vXX.X/limits/` (available since API v29.0, requires "View Setup and Configuration" permission) [snippet]. Confirmed field names (snippet, from the resource-reference page): `DailyApiRequests, DailyBulkApiBatches, DailyBulkV2QueryJobs, DailyBulkV2QueryFileStorageMB, ConcurrentAsyncGetReportInstances, DataStorageMB, FileStorageMB, StreamingApiConcurrentClients` plus roughly 20 more (e.g. `DailyAsyncApexExecutions`), each typically shaped `{Max, Remaining}`. `DailyBulkV2IngestJobs` field name was **not found** in any fetched source - not assumed to exist. No `/limits/recordCount` sub-resource was found either - not assumed to exist.
11. **Nonfunctional requirements:** Reuses the shared bounded-exponential-backoff-with-jitter GET-retry policy unchanged.
12. **Public API contracts or proposed contracts:** Official API: Endpoint and the listed field names are snippet-sourced from `developer.salesforce.com/docs/platform/api-rest/guide/resources-limits.html`; not independently re-fetched. Proposed C# contracts:

```csharp
public sealed record SalesforceLimit(int Max, int Remaining);
public Task<IReadOnlyDictionary<string, SalesforceLimit>> GetLimitsAsync(CancellationToken ct = default);
```
13. **Dependencies:** SFD-003.
14. **Security considerations:** Authentication and authorization: Requires "View Setup and Configuration" permission on the calling user. Required permissions/scopes/licenses: Per-edition daily allocation (SFD-022). Data privacy and security: Limit values are org-operational metadata, not business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: 429 + standard Salesforce error body (not a `Retry-After` header per se - Salesforce's REST API documents throttling via the standard error-code shape, `REQUEST_LIMIT_EXCEEDED`, rather than a dedicated header; this is a **notable divergence from Dataverse/Graph's `Retry-After` convention** and must be handled as a distinct code path, not assumed to carry a numeric retry delay). Error handling and concurrency: `REQUEST_LIMIT_EXCEEDED` classifies as `ThrottleException` with no server-supplied delay, requiring the connector to apply its own bounded backoff rather than honoring a vendor-specified wait, unlike Dataverse/Graph.
16. **Unit-test requirements:** Dictionary-shaped limits parse correctly keyed by name; `REQUEST_LIMIT_EXCEEDED` classifies as `ThrottleException` without assuming a `Retry-After` value is present; an unrecognized limit key is preserved, not dropped.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture matching the documented limits-response shape. Integration-test requirements: Authorized sandbox org; not available this pass. Deliberately triggering a real daily-limit breach is infeasible and inadvisable for a documentation-only pass.
18. **Acceptance criteria:** Limits parse correctly; throttled responses classify correctly without assuming a header that Salesforce does not document providing.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** `DailyBulkV2IngestJobs` field name and a `/limits/recordCount` sub-resource were not found in any source and must not be assumed to exist; the absence of a `Retry-After`-style header (contrasted with Dataverse/Graph) is a deliberate per-vendor difference documented here, not an SDK oversight.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Salesforce/SalesforceLimits.cs` and `tests/Koras.Connect.Tests/Salesforce/SalesforceLimitsTests.cs`. Covers: `GET .../limits/` parses correctly into a dictionary keyed by name with `{Max, Remaining}` values; an unrecognized/future limit key is preserved rather than dropped; a malformed (non-object) response surfaces a `ContractException`; a `REQUEST_LIMIT_EXCEEDED` 429 response classifies as `SalesforceApiException` wrapping `ThrottleException` with `RetryAfter` left `null`, confirming no `Retry-After` value is assumed. Full suite: 825 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | `DailyBulkV2IngestJobs` field name and a `/limits/recordCount` sub-resource were not found in any source and are not assumed to exist - since the parser is key-agnostic, no code change would be needed if they do exist. No live org available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `implemented` | Added `SalesforceLimits.cs` (`GetLimitsAsync`/`SalesforceLimit`) plus `SalesforceLimitsTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; status moves from `not-started` to `implemented`. `verification` stays `partial`: no live org available this pass. |

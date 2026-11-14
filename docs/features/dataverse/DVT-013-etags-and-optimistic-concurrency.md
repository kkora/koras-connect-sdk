---
id: "DVT-013"
title: "ETags and optimistic concurrency"
category: "dataverse"
priority: "P0"
phase: "P2-D2"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-013 - ETags and optimistic concurrency

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-013 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-013.
2. **Feature name:** ETags and optimistic concurrency.
3. **Description:** Prevent a caller from silently overwriting a row that changed since it was last read.
4. **Business value:** Prevent a caller from silently overwriting a row that changed since it was last read.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P0.
7. **Planned development phase:** P2-D2; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None Dataverse-specific; conceptually parallel to how every existing connector treats a write as never-auto-retried, extended here with an explicit vendor-native concurrency token.
10. **Functional requirements:** Every retrieved row carries `@odata.etag` [D3, D10]. `If-Match: <etag>` (or `If-Match: *`) on `PATCH`/`DELETE` enforces optimistic concurrency - a mismatch returns `412 Precondition Failed` with documented codes `ConcurrencyVersionMismatch`/`DuplicateRecord` [D10]. `If-None-Match: null` should be sent on every request to defeat caching of expanded collection-valued navigation properties - a documented Dataverse-specific caching quirk, not a generic OData convention [D10]. `If-None-Match: *` on upsert prevents an update, forcing create-only (already covered in DVT-008).
11. **Nonfunctional requirements:** None beyond explicit opt-in on writes, already covered by DVT-004/008.
12. **Public API contracts or proposed contracts:** Official API: `@odata.etag`, `If-Match`/`If-None-Match` semantics, `412`/`ConcurrencyVersionMismatch` - all per [D3]/[D10]. Proposed C# contracts:

```csharp
public async Task UpdateRowAsync(string entitySet, Guid id, JsonElement fields, string? ifMatchETag = null, bool writesEnabled = false, CancellationToken ct = default);
// A 412 response classifies to a dedicated DataverseConcurrencyException : Koras.Connect.Exceptions.ContractException
// rather than the generic classifier default, since 412 is not currently a mapped status in IntegrationExceptionClassifier.
```
13. **Dependencies:** DVT-004, DVT-008.
14. **Security considerations:** Authentication and authorization: Unchanged. Required permissions/scopes/licenses: None beyond Write privilege, already covered by DVT-004. Data privacy and security: ETags are opaque version tokens, not business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Not applicable; concurrency conflicts are never auto-retried (the caller must re-read and decide). Error handling and concurrency: This is the feature's entire subject: a 412 must be distinguishable from every other 4xx so a caller can implement its own re-read-and-retry policy, rather than treating it as a generic failure.
16. **Unit-test requirements:** `If-Match` header sent correctly when supplied; `If-None-Match: null` sent on reads involving expanded navigation properties; a 412 response classifies as the dedicated concurrency exception, not a generic `ContractException`.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic 412 fixture with the documented `ConcurrencyVersionMismatch` code. Integration-test requirements: Authorized sandbox with two concurrent writers to the same row, to observe a real 412 - not available this pass.
18. **Acceptance criteria:** A 412 is classified distinctly and the caller is never left unable to tell a concurrency conflict apart from a generic contract failure.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** The shared `IntegrationExceptionClassifier` does not currently map 412 to anything specific (its default arm returns `ContractException`) - whether to extend the shared classifier or wrap purely in the Dataverse connector is an open architecture decision (Section 28), leaning toward connector-local wrapping per Section 8's "no abstraction creep for one vendor" principle, since 412-as-concurrency-conflict is not yet proven needed by Salesforce or Graph.

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
| Remaining work / blockers | Full feature implementation (client code, typed models, tests, samples) remains. The shared `IntegrationExceptionClassifier` does not currently map 412 to anything specific (its default arm returns `ContractException`) - whether to extend the shared classifier or wrap purely in the Dataverse connector is an open architecture decision (Section 28), leaning toward connector-local wrapping per Section 8's "no abstraction creep for one vendor" principle, since 412-as-concurrency-conflict is not yet proven needed by Salesforce or Graph. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |

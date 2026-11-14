---
id: "DVT-015"
title: "Actions and functions"
category: "dataverse"
priority: "P1"
phase: "P2-D5"
status: "implemented"
baseline_status: "Implemented: InvokeUnboundActionAsync/InvokeBoundActionAsync/InvokeFunctionAsync compose all three documented invocation shapes; no named action/function beyond the generic invocation API is implemented as a typed wrapper, matching ADP-021's no-invented-operation discipline. Unit-tested; no live custom action available this pass."
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# DVT-015 - Actions and functions

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for DVT-015 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** DVT-015.
2. **Feature name:** Actions and functions.
3. **Description:** Invoke Dataverse's custom and built-in server-side business logic (actions) and computed queries (functions) beyond plain CRUD.
4. **Business value:** Invoke Dataverse's custom and built-in server-side business logic (actions) and computed queries (functions) beyond plain CRUD.
5. **Owning package:** `Koras.Connect.Dataverse`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** Parallels ADP's explicit-allowlist `AdpOperationCatalog` discipline (never invent an operation name) and QLT-007's "verify shape before implementing" precedent.
10. **Functional requirements:** Unbound action: `POST {environment}/api/data/v9.2/{ActionName}` with a JSON body of named parameters (documented example: `Merge`) [D14]. Bound-to-entity action: `POST .../{entityset}(guid)/Microsoft.Dynamics.CRM.{ActionName}` - must use the fully-qualified name including namespace [D14]. Bound-to-collection action: `POST .../{entityset}/Microsoft.Dynamics.CRM.{ActionName}` [D14]. Entity-typed parameters with an ambiguous base type require an explicit `"@odata.type": "Microsoft.Dynamics.CRM.{logicalname}"` annotation in the body [D14]. Functions use `GET` with parameter-alias syntax `Fn(Param=@p1)?@p1=...` to stay under the 260-character URL-segment limit (DVT-003) - the companion official page for functions specifically was referenced consistently across fetched pages but not independently re-fetched this session (vendor-research gap, carried forward honestly).
11. **Nonfunctional requirements:** Explicit allowlist of action/function names the connector exposes typed wrappers for - never dynamically invoke an arbitrary caller-supplied name without the same `EndpointSafety.Segment`-style validation every other path segment gets.
12. **Public API contracts or proposed contracts:** Official API: Action/function invocation syntax per [D14]; the functions-specific companion page was not independently re-fetched this session. Proposed C# contracts:

```csharp
public async Task<JsonDocument> InvokeUnboundActionAsync(string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> InvokeBoundActionAsync(string entitySet, Guid id, string actionName, JsonElement parameters, bool writesEnabled = false, CancellationToken ct = default);
public async Task<JsonDocument> InvokeFunctionAsync(string functionName, IReadOnlyDictionary<string,string> parameters, CancellationToken ct = default);
```
13. **Dependencies:** DVT-003, DVT-004.
14. **Security considerations:** Authentication and authorization: Each action/function has its own, potentially distinct, security-privilege requirement - entirely action-specific and not enumerable generically. Required permissions/scopes/licenses: Action/function-specific; unknown until a specific action is targeted and its tenant-side privilege requirement confirmed. Data privacy and security: Action/function parameters may be sensitive business data.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Actions (writes) are never auto-retried; functions (reads via GET) reuse the shared GET-retry policy. Error handling and concurrency: An action name not recognized by the tenant returns a documented 4xx the connector classifies, not a crash.
16. **Unit-test requirements:** Unbound/bound action URL composition matches the documented fully-qualified-name requirement; `@odata.type` annotation included when the parameter type is ambiguous; function parameter-alias URL composition stays under the 260-character segment limit.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for the documented `Merge` action example. Integration-test requirements: Authorized sandbox with a real custom action configured - not available this pass, and inherently tenant-specific even when available.
18. **Acceptance criteria:** The three documented invocation shapes (unbound action, bound action, function) compose correctly.
19. **Definition of Done:** Standard template; per Section 28, no action/function beyond the documented examples is implemented as a named typed wrapper until its tenant-specific contract is independently verified - mirroring ADP-021's "no invented operation name" discipline exactly.
20. **External API entitlement or documentation blockers:** The functions-specific official page was not independently re-fetched this session; which custom actions exist on any given tenant is entirely tenant-specific and cannot be enumerated from generic documentation.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Dataverse/DataverseActions.cs` and `tests/Koras.Connect.Tests/Dataverse/DataverseActionsTests.cs`. Covers: unbound action posts directly to the action name; bound action uses the fully-qualified `Microsoft.Dynamics.CRM.{ActionName}` form; function composes the documented `Fn(Param=@p1)?@p1=...` alias syntax (including the zero-parameter `Fn()` form); unsafe action/function names rejected locally; invocation without `writesEnabled` throws before any HTTP call. Function parameter values are forwarded exactly as supplied (the caller provides the already-quoted OData literal) rather than the SDK guessing type-specific quoting - documented, not silently wrong. Full suite: 705 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The functions-specific official page was not independently re-fetched this session. No named action/function beyond the generic three-shape invocation API is exposed as a typed wrapper - which custom actions exist on any tenant is entirely tenant-specific and cannot be enumerated from generic documentation, matching ADP-021's precedent exactly. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Added `InvokeUnboundActionAsync`/`InvokeBoundActionAsync`/`InvokeFunctionAsync` plus `DataverseActionsTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership completion gates; status moves from `not-started` to `implemented`. `verification` moves to `partial`: no live custom action/function available this pass. |

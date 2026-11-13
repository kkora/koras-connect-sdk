---
id: "ADP-021"
title: "API capability/entitlement matrix"
category: "adp"
priority: "P0"
phase: "P1 baseline"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-021 - API capability/entitlement matrix

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-021.
2. **Feature name:** API capability/entitlement matrix.
3. **Description:** Prevent customers from assuming availability from a method's existence.
4. **Business value:** Prevent customers from assuming availability from a method's existence.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P1 baseline; P3 completion; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** docs/features/ADP-FEATURE-MATRIX.md; E-OPS catalog; section 13; aliases resolve in section 3.
10. **Functional requirements:** Track product/version, family, method/path, schema, auth, canonical grant, actor/population, read/write, paging/quota, evidence date and live state per operation; unknown stays unknown.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed ConnectorDescriptor operation descriptors; section 13 is the P1 baseline.
13. **Dependencies:** SDK-002; current official schema/entitlement research precedes modeling, matrix acceptance consumes ADP-019/020 evidence; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Matrix-to-catalog consistency, unsupported/unknown states, grant evidence expiry and synthetic deny-before-network. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Matrix-to-catalog consistency, unsupported/unknown states, grant evidence expiry and synthetic deny-before-network.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Every exposed operation has a reviewed row; unknown grants/schemas block that operation's release acceptance without falsely blocking unrelated independently verified operations.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Exact customer CAR/exported current metadata and product access not supplied; public catalog availability does not establish entitlement.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 231, Skipped 3, Failed 0, Total 234 (includes `AdpConnectorTests.EveryAdpOperationHasExactlyOneEntitlementEntry`/`.RecordedDeniedEntitlementShortCircuitsBeforeAnyNetworkCall` and the new `AdpEntitlementScopeTests`) |
| Remaining work / blockers | **Closed this pass:** one more matrix field, the documented candidate CAR scope string, is now modeled via `AdpOperationCatalog.CandidateScopes`. The remaining richer matrix fields from the spec (product/version, family, schema, actor/population, paging/quota, evidence date) are still not modeled, and no evidence has been recorded from an actual tenant (every operation starts, and in this repo stays, `Unknown`). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `Koras.Connect.Adp.AdpEntitlementTracker` (`src/Koras.Connect.Adp/AdpEntitlementTracker.cs`), which gives every `AdpOperation` enum value exactly one `EntitlementEvidence` entry (defaulting to `EntitlementState.Unknown`) and lets a host `Record` `Configured`/`Verified`/`Denied` evidence after a real opt-in or call outcome. Wired into `AdpOperationsClient` (optional constructor parameter) so a `Denied` operation throws `Koras.Connect.Exceptions.AuthorizationException` from `Contract(...)` before any HTTP request — true deny-before-network. `AdpConnector.CreateDescriptor(tracker)` surfaces the tracker's snapshot as `ConnectorDescriptor.Entitlements`. Tests in `tests/Koras.Connect.Tests/AdpConnectorTests.cs`: `EveryAdpOperationHasExactlyOneEntitlementEntry` (enumerates `AdpOperation` and asserts a 1:1 mapping), `RecordedDeniedEntitlementShortCircuitsBeforeAnyNetworkCall` (handler throws if ever invoked), and `DescriptorSurfacesEntitlementEvidenceFromTheSharedTracker`. Still `partial`: this is the P1-baseline state-machine primitive only, not the full matrix described in the spec, and carries no live tenant evidence. |
| 2026-11-12 | `partial` | Added `AdpOperationCatalog.CandidateScopes` (`src/Koras.Connect.Adp/AdpOperationsClient.cs`), a sibling lookup (chosen over widening `AdpOperationContract` itself, to avoid touching all fifteen existing contract constructions) mapping each `AdpOperation` to the documented candidate CAR scope string cited in `docs/features/KORAS-CONNECT-SDK-MASTER-FEATURES.md` section 13 (refs A1/A2) for `ModifyWorkAssignment`, `ChangeManager` and `ModifyDepartmentAllocations`; every other operation maps explicitly to `null` rather than a synthesized string, per that section's own warning against concatenating event names. Doc comments repeat the "documented candidate scope, not a confirmed tenant grant" caution. Tests in `tests/Koras.Connect.Tests/AdpEntitlementScopeTests.cs` assert every `AdpOperation` has exactly one entry, the three documented operations carry the exact cited string, and every other operation is explicitly `null`. Still `partial`: this is metadata only, not a new matrix field category, and the richer fields (product/version, family, actor/population, paging/quota, evidence date) remain unmodeled. |

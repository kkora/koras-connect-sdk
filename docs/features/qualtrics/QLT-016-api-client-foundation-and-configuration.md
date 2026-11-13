---
id: "QLT-016"
title: "API client foundation and configuration"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-016 - API client foundation and configuration

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-016.
2. **Feature name:** API client foundation and configuration.
3. **Description:** Keep Qualtrics independently installable and configured safely.
4. **Business value:** Keep Qualtrics independently installable and configured safely.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-QLT options/Create/root checks; E-SAFETY; no DI extension; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Consistent DI/direct construction, validated /API/v3 root, token/HTTP ownership, bounded options and capability descriptor; preserve preview methods.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Existing QualtricsClient/QualtricsOptions; proposed AddKorasQualtrics/descriptor.
13. **Dependencies:** SDK-001, SDK-002, SDK-003, SDK-014; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Unsafe roots, bad maxPages, duplicate names, wrong auth adapter, disposal and account isolation. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Unsafe roots, bad maxPages, duplicate names, wrong auth adapter, disposal and account isolation.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Independent consumer uses only neutral/shared packages; invalid config fails before send; no inferred data center or entitlement.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Exact data-center root and product access are supplied by consumer; no account provisioning.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: a re-audit found all six named unit-test requirements already covered by existing tests, with no engineering gap remaining; this pass only consolidates the evidence under this feature's own doc. Unsafe roots -> `AddKorasQualtricsRejectsUnsafeOrWrongRootBeforeAnyHttpAttempt`; bad `maxPages` -> `AddKorasQualtricsRejectsBadMaxPages`; duplicate names -> `AddKorasQualtricsRejectsDuplicateRegistration`; wrong auth adapter (no accidental bearer token) -> `QualtricsUsesOnlyApiTokenAndPagesSurveys`'s `Assert.Null(r.Headers.Authorization)`; disposal -> `DependencyInjectionDisposalTests.QualtricsTokenSourceLifetimeIsHostOwnedNotContainerManaged` (documents the real, intentional asymmetry: unlike ADP/SAP, Qualtrics never registers the resolved token source as its own DI service, so a disposable host-supplied token source is not disposed by the container - the host must dispose it independently); account isolation -> the duplicate-registration rejection above (forcing separate `IServiceCollection`s per tenant) plus QLT-001's `TwoClientInstancesWithDifferentTokenSourcesNeverLeakEachOthersToken`. Full suite: 475 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Exact data-center root and product access remain consumer-supplied, with no account provisioning claimed by this SDK (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `AddKorasQualtrics` DI registration and `QualtricsOptionsBuilder` (`src/Koras.Connect.Qualtrics/QualtricsServiceCollectionExtensions.cs`), and a capability descriptor via `QualtricsConnector.Descriptor` (`ConnectorDescriptor` listing `SupportedOperations` and per-operation `EntitlementEvidence`, all starting `Unknown` rather than inferred). `QualtricsOptions` now implements `Koras.Connect.Abstractions.IValidatableOptions` so both direct construction and DI validate the same way. Preview direct-construction methods (`QualtricsClient.Create`, constructor) are unchanged. Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (the `AddKorasQualtrics*` tests). Status stays `partial`: no live sandbox validation, data-center inference, or entitlement provisioning was added or claimed. `dotnet build`/`dotnet test` passing as of this revision. |
| 2026-11-13 | `implemented` | Consolidated evidence showing all six unit-test requirements (unsafe roots, bad maxPages, duplicate names, wrong auth adapter, disposal, account isolation) already have dedicated test coverage; no new engineering gap remained. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: live sandbox validation, data-center inference and entitlement provisioning remain explicitly out of scope / standing external blockers. |

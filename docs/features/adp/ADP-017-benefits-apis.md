---
id: "ADP-017"
title: "Benefits APIs"
category: "adp"
priority: "P2"
phase: "P7"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-017 - Benefits APIs

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-017.
2. **Feature name:** Benefits APIs.
3. **Description:** Retrieve authorized enrollment details within explicit product restrictions.
4. **Business value:** Retrieve authorized enrollment details within explicit product restrictions.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P2.
7. **Planned development phase:** P7; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-OPS ReadSpendingAccountEnrollmentsAsync; E-TEST BenefitsRequireEffectiveDateAndPreserveDocumentedFilter; aliases resolve in section 3.
10. **Functional requirements:** Retain date-required spending-account read; preserve documented filter exactly until a current contract supersedes it; type coverage/periods and handle sensitive unmasked data conservatively.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing ReadSpendingAccountEnrollmentsAsync(string, DateOnly, CancellationToken); proposed typed enrollment response.
13. **Dependencies:** ADP-021, ADP-019, ADP-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Missing date/identifier, blank-body 400, overlapping periods, active/inactive dependents, masked/unmasked fields and denied grant. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Missing date/identifier, blank-body 400, overlapping periods, active/inactive dependents, masked/unmasked fields and denied grant.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Required effective-date query and permitted plan restrictions are documented; no broad benefits coverage is claimed; output mapping uses explicit sensitive-field selection.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** External-benefit-provider plan setup/ADP Benefits onboarding and current grant required; official guide contains route inconsistencies requiring tenant confirmation [A3].

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: this doc had not been touched since the November 12 import despite the typed enrollment response (`SpendingAccountEnrollment`/`SpendingAccountEnrollmentsResponse`) already existing from ADP-019's work (`src/Koras.Connect.Adp/Workers/SpendingAccountEnrollment.cs`, round-trip-tested in `AdpTypedModelsTests.SpendingAccountEnrollmentsResponseRoundTripsUnknownFieldsAndPreservesVariableCoverageShape`) — recording that evidence here, plus adding `tests/Koras.Connect.Tests/AdpBenefitsEnrollmentTests.cs` to close every remaining unit-test-requirement gap: missing/unsafe identifier rejected before any request (3 cases), blank-body HTTP 400 classifies as `ContractException` without leaking the body, a `Denied` entitlement specifically on `AdpOperation.SpendingAccountEnrollments` short-circuits before any network call, masked and unmasked `accountNumber` values both survive round-trip unmodified, and overlapping coverage periods plus active/inactive dependents are all preserved raw rather than collapsed or lost. Full suite: 418 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. `ReadSpendingAccountEnrollmentsAsync` still returns a raw `JsonDocument` rather than the typed `SpendingAccountEnrollmentsResponse` — intentional, matching this package's existing "raw escape hatch remains explicit" posture elsewhere, since every typed model in this package is a caller-side `Deserialize<T>()` convenience over the same JSON, not a replacement for the raw read. External-benefit-provider plan setup, current grant and the official guide's documented route inconsistencies remain unresolved blockers (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `implemented` | Recorded the typed-model evidence ADP-019 had already produced for this feature and closed every remaining unit-test-requirement gap (missing identifier, blank-body 400, operation-specific denied grant, masked/unmasked fields, overlapping periods/dependents). Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: the external benefit-provider/grant/route-inconsistency blockers remain unresolved. |

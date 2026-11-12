---
id: "QLT-015"
title: "Contact-specific typed schema and consent"
category: "qualtrics"
priority: "P1"
phase: "P6"
status: "partial"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-015 - Contact-specific typed schema and consent

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-015.
2. **Feature name:** Contact-specific typed schema and consent.
3. **Description:** Avoid unsafe contact mutations and ambiguous field presence.
4. **Business value:** Avoid unsafe contact mutations and ambiguous field presence.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No Contact/Directory DTO in E-QLT; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Review contact identity/custom fields/consent/opt-out/extension data; bind field requiredness/access to actual directory contract and preserve null versus omission.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed QualtricsContact/ContactChange validation after [Q6] schema review.
13. **Dependencies:** QLT-006, QLT-012; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Missing/duplicate external ID, custom fields, explicit null/omission, opted-out contact and sensitive attributes. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Missing/duplicate external ID, custom fields, explicit null/omission, opted-out contact and sensitive attributes.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Model/mapper preserve field presence and consent; unsupported changes are explicit; no implicit unsubscribe/resubscribe behavior.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Tenant schema/permissions/consent policy required; no customer contact data used in P1.

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
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | Not newly executed during documentation split; baseline evidence is in the specification |
| Remaining work / blockers | See functional requirements, acceptance criteria and external blockers above; replace with precise remaining items as work progresses |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `QualtricsContact` (`src/Koras.Connect.Qualtrics/QualtricsDirectoryClient.cs`): identity fields (`Id`, `FirstName`, `LastName`, `Email`, `ExternalReference`, `Language`), an `Unsubscribed` field modeled as a non-nullable `JsonElement` so field-absent (`JsonValueKind.Undefined`), field-explicit-null (`JsonValueKind.Null`) and field-present-with-value are all distinguishable (a `bool?` or `JsonElement?` cannot make the first two distinctions — confirmed empirically: System.Text.Json's `Nullable<T>` wrapper collapses an explicit JSON `null` to CLR `null` before the inner converter runs), with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]` so an unset field is omitted from outgoing payloads rather than throwing on the unrepresentable `Undefined` sentinel, and `[JsonExtensionData] AdditionalData` preserving every other field Qualtrics returns. **Endpoint shape based on the publicly documented Qualtrics API v3 contract; NOT verified against a live tenant**, so status moves to `partial`, not `implemented`, pending the tenant schema/permission/consent-policy review noted below. Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`ListContactsPagesAndPreservesUnknownFieldsAndNullVersusOmission`, `QualtricsContactMapperGoldenFixtureDistinguishesNullFromOmissionAtTheSourceDtoLevel`). `dotnet build`/`dotnet test` passing as of this revision. |

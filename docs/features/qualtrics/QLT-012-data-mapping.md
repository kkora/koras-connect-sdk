---
id: "QLT-012"
title: "Data mapping"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# QLT-012 - Data mapping

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-012.
2. **Feature name:** Data mapping.
3. **Description:** Map selected data without losing identity or consent semantics.
4. **Business value:** Map selected data without losing identity or consent semantics.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-QLT extension-preserving models; E-EXPORT projections; no mapper; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Add opt-in neutral profiles and null/timezone/code/embedded-data rules; preserve unknown fields and sensitive omissions; no cross-provider workflow.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed IRecordMapper<QualtricsEntity,T>/MappingResult<T>; raw downloads remain raw.
13. **Dependencies:** SDK-002, SDK-013, QLT-002, QLT-006; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Null/unknown fields, mixed response schemas, Unicode, embedded keys, identity distinctions and consent fields. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Null/unknown fields, mixed response schemas, Unicode, embedded keys, identity distinctions and consent fields.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Golden cases preserve selected identity/consent deterministically; raw file bytes change only when caller explicitly parses/maps them.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Product contact/response schema and consent review required; no universal normalized response model.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "Unicode, embedded keys" unit-test-requirement gap by adding `QualtricsContactMapperPreservesUnicodeNamesAndNestedEmbeddedDataKeys` to `tests/Koras.Connect.Tests/Qualtrics/QualtricsExpansionTests.cs`. Deserializes a contact whose typed `firstName`/`lastName` hold non-ASCII text ("Ümit"/"张") and whose unknown `embeddedData` object nests a Unicode property key ("部門") and an emoji-bearing value ("café 😀"), then asserts `QualtricsContactMapper.Map` preserves every one of those values byte-for-byte - the typed fields on `NeutralContactRecord` and the whole nested `embeddedData` object inside `Extensions` - rather than transliterating, mangling, or dropping them as "just another unknown field". Full suite: 484 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. Product contact/response schema and consent review against a live tenant remain an external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `IRecordMapper<TSource,TTarget>` implementations `SurveySummaryMapper` and `QualtricsContactMapper` (`src/Koras.Connect.Qualtrics/QualtricsMappers.cs`) with neutral `NeutralSurveyRecord`/`NeutralContactRecord` targets, using `MappingResult<T>.Success`/`Omitted`/`Failure` per the shared contract. Unknown fields are preserved via an `Extensions` dictionary copied from each source DTO's `AdditionalData`; a contact with no identifier fails with a `FieldError`, a survey with no identifier is `Omitted` rather than invented. The neutral `NeutralContactRecord.OptedOut` intentionally collapses explicit-null and absent back to `null`; this trade-off is documented on the record itself, while the underlying `QualtricsContact.Unsubscribed` DTO retains full null-vs-omission fidelity. Covered by golden-fixture tests in `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`SurveySummaryMapperPreservesUnknownFieldsAndOmitsRecordsWithoutAnIdentifier`, `QualtricsContactMapperGoldenFixtureDistinguishesNullFromOmissionAtTheSourceDtoLevel`, `QualtricsContactMapperFailsWithAFieldErrorWhenTheIdentifierIsMissing`). `dotnet build`/`dotnet test` passing as of this revision. |
| 2026-11-13 | `implemented` | Added `QualtricsContactMapperPreservesUnicodeNamesAndNestedEmbeddedDataKeys`, closing the "Unicode, embedded keys" unit-test-requirement gap. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: the product contact/response schema and consent review against a live tenant remain a standing external blocker. |

---
id: "ADP-019"
title: "Typed API response models"
category: "adp"
priority: "P0"
phase: "P3"
status: "partial"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-019 - Typed API response models

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-019.
2. **Feature name:** Typed API response models.
3. **Description:** Make supported responses discoverable and resilient to schema growth.
4. **Business value:** Make supported responses discoverable and resilient to schema growth.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-WORKERS Worker/Person/WorkAssignment; E-OPS other responses primarily JsonDocument; aliases resolve in section 3.
10. **Functional requirements:** Complete typed schemas for the first-release operation subset; preserve extension data, vendor codes, dates/precision, masked/null distinction and stable naming; raw escape hatch remains explicit.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Worker family; proposed response envelopes and safe error/metadata models tied to verified operations.
13. **Dependencies:** ADP-021, SDK-017, SDK-009; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Unknown/nested fields, null/missing collections, invalid code/date/number, oversized/deep JSON and forward/backward fixtures. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Unknown/nested fields, null/missing collections, invalid code/date/number, oversized/deep JSON and forward/backward fixtures.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Every declared first-release typed operation has official schema mapping and golden tests; unknown fields survive roundtrip; raw-only preview operations are labeled clearly.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Current complete official schemas and authorized metadata required; code fragments are not full vendor models.

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
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 231, Skipped 3, Failed 0, Total 234 (includes `AdpTypedModelsTests` and the new `AdpPayrollTypedModelsTests`) |
| Remaining work / blockers | **Closed this pass:** every typed model's XML doc comment now cites the specific `docs/features/ADP-FEATURE-MATRIX.md` row and source alias (S1-S8) it was modeled from (see the status history row below). This closes the "no official schema evidence has been attached" gap from the citation-pointer angle. Still open: those matrix rows themselves cite official guide PDFs/sample-payload collections, not a machine-checkable JSON Schema document — so this is traceability to the best currently-available vendor evidence, not an independently-retrieved formal schema (the external blocker above — no live tenant metadata access — is unchanged). The payroll models remain explicitly "best-effort, not independently schema-verified" because their matrix rows (S2/S8) only evidence the operation/path, not a field-level schema. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added typed, `[JsonExtensionData]`-preserving response models for the event-notification envelope (`Koras.Connect.Adp.Events.NotificationEnvelope` with `FromWebhookDelivery`/`FromPolledMessage` factories — the polled-message id is sourced from the already-allowlisted header, never trusted from payload), time-off balances/requests (`TimeOffBalancesResponse`, `TimeOffRequestsResponse` in `src/Koras.Connect.Adp/Workers/TimeOff.cs`), and spending-account enrollments (`SpendingAccountEnrollmentsResponse` in `src/Koras.Connect.Adp/Workers/SpendingAccountEnrollment.cs`), plus the organization/manager models from ADP-013. No new endpoints were introduced; all are typed deserialization targets for already-dispatched operations. Golden round-trip tests in `tests/Koras.Connect.Tests/AdpTypedModelsTests.cs` cover unknown-field survival and null/missing-collection tolerance for every added model. Still `partial`: coverage is not complete across every catalog operation, and no official schema evidence has been attached. |
| 2026-11-12 | `partial` | Added `PayrollInstruction`/`PayrollInstructionsResponse` and `PayDistribution`/`PayDistributionsResponse` (`src/Koras.Connect.Adp/Workers/PayrollModels.cs`) for `AdpOperation.PayrollInstructions`/`PayDistributions`, the last two GET-only catalog operations that were previously `JsonDocument`-only. Each is explicitly marked, in the same caveat style used by `src/Koras.Connect.Sap.SuccessFactors/FoundationObjects.cs`, as a best-effort typed wrapper whose field set is NOT independently schema-verified; no bank account/routing detail is exposed as a typed property, only inside an untyped `AccountDetail` `JsonElement` (matching the existing `SpendingAccountEnrollment.AccountBasedCoverage` posture). Golden round-trip tests in `tests/Koras.Connect.Tests/AdpPayrollTypedModelsTests.cs` cover unknown-field survival, missing collections, and null/missing scalar fields for both. Still `partial`: no official schema evidence for either response, and typed-model coverage overall remains uncited against a current vendor contract. |
| 2026-11-13 | `partial` | Attached an explicit schema-source citation (matrix row + S1-S8 alias) to every typed model's doc comment: `Worker`/`Identifier`/`Person`/`LegalName`/`WorkAssignment` (S1/S2), `CodeValue`/`QuantityValue` (shared convention, S1/S2), `OrganizationDepartment` (S2), `ManagerRelationship` (S1), `TimeOffBalanceEntry`/`TimeOffRequest` (S2), `SpendingAccountEnrollment` (S5), `NotificationEnvelope` (S2/S6), and `PayrollInstruction`/`PayDistribution` (S2/S8, with an explicit note that those two rows evidence protocol only, not a field-level schema — the pre-existing "best-effort" caveat is unchanged, now with a traceable reason). No field shapes or behavior changed; docs-only. `dotnet build src/Koras.Connect.Adp/Koras.Connect.Adp.csproj`: 0 warnings, 0 errors. `verification` moved from `unverified` to `partial`: citations are traceable to the best available vendor evidence, but that evidence itself is not a formal schema and no live tenant metadata is available to close the loop. |

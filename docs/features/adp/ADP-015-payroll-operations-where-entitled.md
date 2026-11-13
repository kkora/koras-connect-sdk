---
id: "ADP-015"
title: "Payroll operations, where entitled"
category: "adp"
priority: "P1"
phase: "P3 extension"
status: "partial"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-015 - Payroll operations, where entitled

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-015.
2. **Feature name:** Payroll operations, where entitled.
3. **Description:** Read or submit narrowly authorized payroll data without overstating coverage.
4. **Business value:** Read or submit narrowly authorized payroll data without overstating coverage.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P1.
7. **Planned development phase:** P3 extension; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-OPS ChangePayDataInput, PayrollInstructions, PayDistributions; E-TEST documented route/no-retry tests; aliases resolve in section 3.
10. **Functional requirements:** Document exact supported payroll subset; type approved contracts; gate writes by explicit opt-in/grants/open payroll context; preserve decimal/currency and per-item outcomes.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing raw read/SubmitEventAsync; proposed typed payroll result/command family after metadata review.
13. **Dependencies:** ADP-021, ADP-010, ADP-019; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Invalid batch/payroll context, precision/nulls, denied grants, failed items and unknown write outcomes. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Invalid batch/payroll context, precision/nulls, denied grants, failed items and unknown write outcomes.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Only approved operation contracts are callable; synthetic envelopes validate against reviewed schemas; no automatic payroll POST retry; broader payroll APIs remain unsupported.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Licensed payroll API subscription, allowed payroll groups and safe sandbox write authorization unavailable [A5]; no production payroll execution.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: closed the "explicit opt-in gating + typed command for ChangePayDataInput" gap by adding `ChangePayDataInputCommand` (`src/Koras.Connect.Adp/Workers/AdpWriteCommands.cs`). Unlike ADP-013/014's typed commands, this one deliberately does NOT model `payDataInput`'s field-level shape (the matrix's "Payroll input" row documents only the surrounding envelope, not earnings/amount field names) — the caller supplies `payDataInput` as an opaque, pre-built `JsonElement` that is passed through byte-for-byte, and the command additionally requires an explicit `explicitPayrollWriteOptIn: true` on every call (distinct from, and in addition to, `AdpEntitlementTracker`'s tenant-grant gating), since a payroll write moves real money. Wired as `AdpOperationsClient.SubmitChangePayDataInputAsync`. Tests added to `tests/Koras.Connect.Tests/AdpWriteCommandTests.cs`: exact envelope shape, exact decimal-precision preservation (`1234.500` survives rather than being renormalized through a `double`), explicit `null` fields inside `payDataInput` preserved, the opt-in requirement enforced (`InvalidOperationException` when `false`), missing associate OID/pay group id rejected locally, non-object `payDataInput` rejected locally, and the convenience method wiring the built envelope through unmodified. Full suite: 459 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No typed command exists for Hire/Rehire/ChangePersonalEmail (see ADP-014). The "payGroupID" field name itself remains an unverified best-effort placement (documented in the type's own doc comment) since the matrix does not cite an exact field name for payroll group/job identification; broader payroll APIs (instructions/distributions writes) remain unsupported by design. Licensed payroll subscription and safe sandbox write authorization remain unavailable (see external blockers above). |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-13 | `partial` | Added `ChangePayDataInputCommand`/`AdpOperationsClient.SubmitChangePayDataInputAsync` with an explicit payroll-write opt-in requirement and exact-precision pass-through of caller-supplied `payDataInput` content. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates. Remains `partial`: the payroll-group field name is an unverified placement, and no live tenant/sandbox evidence exists. `verification` moved from `unverified` to `partial`. |

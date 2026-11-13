---
id: "ADP-009"
title: "Excel export"
category: "adp"
priority: "P1"
phase: "P3"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-009 - Excel export

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-009.
2. **Feature name:** Excel export.
3. **Description:** Allow spreadsheet consumption without formula execution.
4. **Business value:** Allow spreadsheet consumption without formula execution.
5. **Owning package:** Koras.Connect.ADP (projection) and Koras.Connect.Export (serialization), composed by host.
6. **Priority:** P1.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-EXPORT WriteExcel; E-TEST XLSX XML/no-formula checks; aliases resolve in section 3.
10. **Functional requirements:** Retain inline text/no formulas; support async provider composition, explicit row/column/text limits and large-output budgets; reject invalid input rather than silently truncate.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing WriteExcel; proposed XLSX IExportProvider and explicit schema.
13. **Dependencies:** SDK-013, ADP-007; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Formula prefixes, whitespace/Unicode, maximum and over-limit rows/columns/text, invalid XML characters and independent workbook-reader validation. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Formula prefixes, whitespace/Unicode, maximum and over-limit rows/columns/text, invalid XML characters and independent workbook-reader validation.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** An independent reader opens the workbook and recovers selected values; no formula nodes are generated; limits fail deterministically with documented partial-output handling.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** No Excel license/service required for generation; reader/library choice reviewed later, not installed in P1.

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
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `AdpWorkerExportTests.XlsxExportIsIndependentlyReadableAndContainsNoFormulaNodes`, closing the "independent workbook-reader validation" unit-test requirement this feature previously left as only a non-empty-output check. It reuses the exact independent-reader technique already established for the shared `Koras.Connect.Export.XlsxExportProvider` itself (`CoreTests.ExportsEscapeDataAndLeaveStreamOpen`, `ExportProviderTests` — raw `ZipArchive`/`XDocument` parsing of `xl/worksheets/sheet1.xml`, not the writer's own code), applied here to `WorkerExportProjection`'s output: asserts zero `<f>` formula nodes, recovers the documented header row and every expected cell value (`w1`, `Ada`, `Lovelace`, `Engineer`) from `<t>` inline-string nodes, and confirms the vendor extension-data field/value never appear. Full suite: 362 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified. No official schema/live-tenant evidence backs the exported fields, and the "maximum and over-limit rows/columns/text" bounds are exercised by the shared `Koras.Connect.Export` test suite (`CoreTests.ExcelColumnCountOutOfBoundsIsRejectedBeforeAnyRowIsWritten` and neighbors), not duplicated per-connector here — that shared coverage is this feature's dependency (SDK-013), not a gap in ADP-009 itself. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | `Koras.Connect.Adp.Export.WorkerExportProjection.ExportAsync` (`src/Koras.Connect.Adp/Export/WorkerExportProjection.cs`) now also streams to `Koras.Connect.Export.XlsxExportProvider`, so worker XLSX export shares the exact same documented, fixed column set and extension-data exclusion as CSV/XML. Covered by `tests/Koras.Connect.Tests/AdpWorkerExportTests.cs` (`XmlAndColumnRestrictedJsonAndXlsxExportsAllCompleteWithoutBufferingTheWholeSequence`). Kept at `implemented` rather than fully accepted: no independent workbook-reader assertion (e.g. opening the produced file with a third-party XLSX reader) was added, and no official schema/live-tenant evidence backs the exported fields. |
| 2026-11-13 | `implemented` | Added `XlsxExportIsIndependentlyReadableAndContainsNoFormulaNodes`, closing the independent-workbook-reader gap recorded above. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates. `verification` moved from `unverified` to `partial`: no official schema/live-tenant evidence is available to close the remaining gate. |
| 2026-11-13 | `implemented` | Extended `samples/ADP.WorkerExport` with an XLSX export scenario, independently verified via raw `ZipArchive`/`XDocument` parsing (no `<f>` formula node, safe-column-only, matching the test's technique) rather than trusting the writer's own code — closing this feature's dedicated-sample gap. Also discovered and fixed a real, pre-existing bug surfaced while verifying this: `samples/ADP.WorkerExport.csproj` was missing a `ProjectReference` to `Koras.Connect.Export`, so the sample could not have compiled despite ADP-022's prior "executed successfully" evidence record — fixed by adding the missing reference. Docs-only status change; no feature behavior changed. |

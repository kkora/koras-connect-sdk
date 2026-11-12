---
id: "SDK-013"
title: "Export provider interfaces"
category: "sdk"
priority: "P0"
phase: "P3"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-013 - Export provider interfaces

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-013.
2. **Feature name:** Export provider interfaces.
3. **Description:** Provide replaceable exports with deterministic safe projections.
4. **Business value:** All four provider formats parse independently and preserve selected values/order reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Abstractions + Koras.Connect.Export.
6. **Priority:** P0.
7. **Planned development phase:** P3; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-EXPORT static writers; provider interface/async schema contract absent. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Implement providers for CSV/JSON/XML/XLSX and schema-driven async records; preserve stream ownership, bounded memory and formula defenses; cancellation/failure leaves incomplete output explicit.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed IExportProvider, ExportSchema<T>; existing TabularExport methods retained through migration.
13. **Dependencies:** SDK-002, SDK-010; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise empty records, null/date/culture/Unicode, delimiter/newline escaping, formula prefixes, width and Excel bounds, canceled stream. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Empty records, null/date/culture/Unicode, delimiter/newline escaping, formula prefixes, width and Excel bounds, canceled stream.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** All four provider formats parse independently and preserve selected values/order; caller stream remains open; no implicit filesystem or provider dependency.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** No vendor entitlement; third-party Excel library requires license review if chosen.

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
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added the proposed `IExportProvider`/`ExportSchema<T>`/`ExportColumn<T>` contracts and `CsvExportProvider`/`JsonExportProvider`/`XmlExportProvider`/`XlsxExportProvider` implementations in `src/Koras.Connect/Export/ExportProvider.cs`, wrapping the existing `TabularExport` static methods (kept, not replaced, per the migration note in master section 6/8). `TabularExport` itself gained `IAsyncEnumerable<T>`/`IAsyncEnumerable<IReadOnlyList<string?>>` overloads for `WriteJsonAsync`/`WriteCsvAsync`/`WriteXmlAsync`/`WriteExcelAsync` so records can stream from provider `IAsyncEnumerable<T>` sources without buffering the whole population first — this closes the cross-cutting gap flagged against every provider's export feature (ADP-008/009, and the SAP/Qualtrics equivalents). Provider-specific projections (e.g. mapping `Worker`/`PerPerson`/`SurveySummary` to an `ExportSchema<T>`) are being added per-provider; see their own trackers. `dotnet build`/`dotnet test` (55 baseline tests) pass unaffected; no new test in this shared location yet exercises `ExportSchema<T>.Project` directly (covered indirectly once provider projections land). |

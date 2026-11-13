---
id: "ADP-008"
title: "CSV/JSON/XML export"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-008 - CSV/JSON/XML export

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-008.
2. **Feature name:** CSV/JSON/XML export.
3. **Description:** Deliver selected worker data in portable file formats.
4. **Business value:** Deliver selected worker data in portable file formats.
5. **Owning package:** Koras.Connect.ADP (projection) and Koras.Connect.Export (serialization), composed by host.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-EXPORT WriteCsvAsync/WriteJsonAsync/WriteXml; E-TEST ExportsEscapeDataAndLeaveStreamOpen; E-SAMPLE; aliases resolve in section 3.
10. **Functional requirements:** Compose worker mappings with independent Export providers; stable column/schema and null/date encoding; stream without implicit file writes and reject partial success.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing TabularExport; proposed IExportProvider composed with worker projection, no ADP-Export dependency.
13. **Dependencies:** ADP-007, SDK-013, SDK-010; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** CSV quote/formula/control prefixes, XML invalid characters/names policy, JSON extension fields, Unicode/null values, cancellation and failing output stream. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** CSV quote/formula/control prefixes, XML invalid characters/names policy, JSON extension fields, Unicode/null values, cancellation and failing output stream.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Each format roundtrips the documented selected fixture fields; streams remain open and partial failure is surfaced; serialized output includes no implicit sensitive columns.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** No extra vendor grant beyond retrieving selected data; consumers must authorize data destinations.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 183, Skipped 1, Failed 0, Total 184 (includes `AdpWorkerExportTests`) |
| Remaining work / blockers | No further engineering gap identified. No official schema evidence attached for the exported fields. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `implemented` | Added `Koras.Connect.Adp.Export.WorkerExportProjection` (`src/Koras.Connect.Adp/Export/WorkerExportProjection.cs`) composing `AdpWorkersClient.EnumerateTypedWorkersAsync` with the new SDK-wide `Koras.Connect.Export.ExportSchema<Worker>`/`IExportProvider`, streaming CSV/JSON/XML directly without buffering the whole population. The documented, fixed column set (`AssociateOid`, `GivenName`, `FamilyName`, `JobTitle`, `HireDate`, `TerminationDate`) excludes SSN/compensation fields and never surfaces `Worker.AdditionalData`; added `ExportJsonAsync` specifically because the generic `JsonExportProvider` serializes the raw record type (and would otherwise leak extension data), while CSV/XML/XLSX already honor the schema. Tests in `tests/Koras.Connect.Tests/AdpWorkerExportTests.cs`: `CsvExportIncludesOnlyTheDocumentedColumnsAndExcludesExtensionData`, a combined XML/column-restricted-JSON/XLSX completion test, and `StreamingExportStopsPromptlyOnCancellationMidSequence`. Kept at `implemented` rather than fully accepted because the column set has no official schema citation and has not been validated against a live tenant. |
| 2026-11-13 | `implemented` | Closed the "column set is fixed/unversioned" gap: added an opt-in field-selection overload of `CreateSchema`/`ExportAsync` taking an `includeColumns` subset of `ColumnNames` (for a tenant authorized for fewer fields than the full documented set), always rendered in the fixed canonical column order regardless of request order, and rejecting any name outside `ColumnNames` with an `ArgumentException` rather than silently dropping it. Added `OptInColumnSelectionExportsOnlyTheRequestedFieldsInCanonicalOrder`, `SelectingAnUnapprovedColumnNameIsRejectedRatherThanSilentlyIgnored` and `EmptyColumnSelectionIsRejected`; updated two ambiguous `<see cref="WorkerExportProjection.CreateSchema"/>` doc-comment references (now resolved to the `DateOnly?` overload) and `tests/Koras.Connect.Tests/ApiSnapshots/Koras.Connect.Adp.snapshot.txt` for the two new public overloads. Full suite: 366 passed, 3 skipped, 0 failed. Checked the unit/contract/integration and security/cancellation/resource-ownership completion gates. `verification` moved from `unverified` to `partial`: no official schema evidence is available to close the remaining gate. |

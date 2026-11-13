---
id: "SDK-012"
title: "OpenTelemetry instrumentation"
category: "sdk"
priority: "P1"
phase: "P7"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-012 - OpenTelemetry instrumentation

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-012.
2. **Feature name:** OpenTelemetry instrumentation.
3. **Description:** Enable optional distributed tracing and useful bounded metrics.
4. **Business value:** A host can opt in to traces/metrics without changing clients reduces integration risk and makes this capability independently consumable.
5. **Owning package:** Koras.Connect.Observability.
6. **Priority:** P1.
7. **Planned development phase:** P7; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TRANSPORT ActivitySource/Meter Koras.Connect; E-TEST SdkActivityDoesNotEmitWorkerOrCredentialData. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Add optional registration, durations/retries/errors and semantic tags with bounded cardinality; no global SDK/exporter setup or provider dependency.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Existing InstrumentationName/Activities; proposed AddKorasConnectInstrumentation in Observability.
13. **Dependencies:** SDK-011; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise listener/exporter capture, parent-child context, disabled overhead, cardinality and redaction tests. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Listener/exporter capture, parent-child context, disabled overhead, cardinality and redaction tests.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A host can opt in to traces/metrics without changing clients; removing Observability leaves Core usable; no PII reaches captured telemetry.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** OpenTelemetry versions and semantic conventions must be reviewed in P7; no exporter backend provisioned.

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
| Remaining work / blockers | **Genuinely blocked, not skipped.** This ticket's owning package is `Koras.Connect.Observability`, which SDK-001 deliberately did not create (see SDK-001's own remaining-work note): this ticket's phase is P7 ("Planned development phase: P7; P1 specifies only") and its own external-blocker field states "OpenTelemetry versions and semantic conventions must be reviewed in P7" — i.e. the exact dependency versions and tag/attribute naming this ticket would need to pin are explicitly gated on review that has not happened yet. Building `AddKorasConnectInstrumentation` now would mean guessing at a semantic-convention surface the spec itself says isn't reviewed, and shipping a 5th/8th package with no real review behind its dependency choice. The one thing this ticket's test requirements don't need the new package for — proving Core's existing `IntegrationTransport.Activities`/`Meter` never emit PII — is already covered by the pre-existing `SecurityAndLifecycleTests.SdkActivityDoesNotEmitWorkerOrCredentialData` test, confirmed still passing in this pass's full-suite run (315 passed). Following up: create the `Koras.Connect.Observability` package and `AddKorasConnectInstrumentation` once the P7 OpenTelemetry package-version/semantic-convention review this ticket names has actually happened — not before. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Audited and confirmed this ticket is genuinely blocked, not merely unstarted: its owning package (`Koras.Connect.Observability`) is P7-phase and explicitly requires an OpenTelemetry version/semantic-convention review this ticket itself says hasn't happened. No implementation attempted; status intentionally left `partial` rather than advanced. Logged as a followup for whoever runs the P7 pass. Moving to the next SDK ticket per the agreed sequencing. |

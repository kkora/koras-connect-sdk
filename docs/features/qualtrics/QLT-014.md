---
id: "QLT-014"
title: "Documentation and examples"
category: "qualtrics"
priority: "P0"
phase: "P6"
status: "partial"
baseline_status: "Partially implemented"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-014 - Documentation and examples

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-014.
2. **Feature name:** Documentation and examples.
3. **Description:** Make survey/export and later directory adoption reproducible.
4. **Business value:** Make survey/export and later directory adoption reproducible.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** README example; E-SAMPLE options smoke; no dedicated end-to-end example; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Offline survey/export flow with resume/cancellation and sensitive-download guidance; data center/Access API setup, per-family rights and limits.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed samples/Qualtrics.SurveyExport and opt-in directory example; no orchestration service.
13. **Dependencies:** QLT-008, QLT-003, QLT-016; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Clean-checkout compile/offline execution, bad settings, interrupted resume and token/PII canaries. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Clean-checkout compile/offline execution, bad settings, interrupted resume and token/PII canaries.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Example runs without SAP/ADP and sends no messages by default; synthetic behavior is clearly distinguished from live account validation.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** Live mode needs authorized token/data center/survey; offline example unblocked.

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
| 2026-11-12 | `partial` | Added `samples/Qualtrics.SurveyExport` (standalone console project, `ProjectReference` to `Koras.Connect.Qualtrics` only, not added to `Koras.Connect.slnx`, following the same non-slnx-member convention as `samples/PackageConsumer`). Demonstrates survey enumeration -> `StartResponseExportAsync` -> an explicit `GetExportProgressAsync` poll followed by a simulated process-restart -> `WaitForExportAsync` resume using the same progress ID -> `DownloadExportAsync`, entirely against a synthetic in-process `HttpMessageHandler` with no network access, no token/data-center/survey setup required, and no message/distribution ever sent. Verified with `dotnet build samples/Qualtrics.SurveyExport/Qualtrics.SurveyExport.csproj` and `dotnet run --project samples/Qualtrics.SurveyExport` on a clean checkout; both succeeded and the console output clearly narrates the synthetic nature of every step. The directory/contact/mailing-list surface added under QLT-006/009/015 is not yet demonstrated by a dedicated example; only the survey/export flow is covered here. |

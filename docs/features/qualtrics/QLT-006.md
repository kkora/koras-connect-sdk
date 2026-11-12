---
id: "QLT-006"
title: "Directory and contact operations"
category: "qualtrics"
priority: "P1"
phase: "P6"
status: "partial"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# QLT-006 - Directory and contact operations

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** QLT-006.
2. **Feature name:** Directory and contact operations.
3. **Description:** Manage an authorized directory independently.
4. **Business value:** Manage an authorized directory independently.
5. **Owning package:** Koras.Connect.Qualtrics; shared contracts follow section 7.
6. **Priority:** P1.
7. **Planned development phase:** P6; preview preserved during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** No directory/contact methods in E-QLT; legacy QLT-006 directory/distribution writes planned; aliases resolve in section 3; generic infrastructure is not a complete business contract.
10. **Functional requirements:** Verify XM Directory operations then add typed list/get/create/update/contact methods; explicit opt-in mutations and capability descriptors; destructive calls need separate verified contracts.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers and cancellation-aware I/O; no SAP/ADP, scheduler, cloud or database dependency. Measure performance before release budgets.
12. **Public API contracts or proposed contracts:** Proposed directory/contact clients; exact routes/verbs/schemas from [Q6], not survey APIs.
13. **Dependencies:** QLT-001, QLT-004, QLT-005; contract/schema design precedes QLT-012 mapping; acyclic graph in section 7.
14. **Security considerations:** Account/data-center token isolation, no redirects, field/operation permissions, consent preservation; no token/contact/response data in telemetry. Writes require explicit opt-in, verified contract and authorization for testing.
15. **Error and edge-case handling:** Invalid IDs, duplicate external IDs, missing fields, 403/404, opt-out/consent and uncertain write outcome. Preserve cancellation, sanitize diagnostics and reconcile uncertain writes instead of replaying blindly.
16. **Unit-test requirements:** Invalid IDs, duplicate external IDs, missing fields, 403/404, opt-out/consent and uncertain write outcome.
17. **Integration-test requirements:** Versioned synthetic HTTP/schema/stream fixtures and independent consumer on both OSes; separately opt-in permitted account reads and authorized disposable writes where applicable. Record product/permissions/schema/revision and result or blocker.
18. **Acceptance criteria:** Each exposed operation has official schema/permission evidence; defaults are read-only; write tests never blindly replay or disclose PII.
19. **Definition of Done:** Criteria plus section 26 unit/contract/security/API/consumer/documentation gates; current official operation evidence and required sandbox outcomes attached. Missing required contract/access prevents acceptance, never becomes a pass.
20. **External API entitlement or documentation blockers:** XM Directory license/permissions and current operation schemas required; docs availability is not entitlement.

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
| 2026-11-12 | `partial` | Added `QualtricsContact` DTO and `ListContactsAsync`/`ListContactsPageAsync`/`GetContactAsync` (read, default-enabled) plus `CreateContactAsync`/`UpdateContactAsync` (write, explicit `writesEnabled` opt-in gate, following the SAP `SuccessFactorsClient.UpsertAsync` pattern) in `src/Koras.Connect.Qualtrics/QualtricsDirectoryClient.cs`. **Endpoint shape (`GET/POST /directories/{directoryId}/contacts`, `GET/PUT /directories/{directoryId}/contacts/{contactId}`) is based on the publicly documented Qualtrics API v3 contract; it is NOT verified against a live tenant** — every new public member's XML doc states this explicitly. Covered by `tests/Koras.Connect.Tests/QualtricsExpansionTests.cs` (`ListContactsPagesAndPreservesUnknownFieldsAndNullVersusOmission`, `CreateContactRequiresExplicitOptIn`, `CreateContactRejectsMissingRequiredFieldLocallyBeforeAnyHttpCall`, `CreateContactRejectsInvalidDirectoryIdLocally`, `GetContactClassifiesForbiddenAndNotFoundWithoutLeakingBody`, `UpdateContactRequiresExplicitOptInAndSendsPut`). Status moves to `partial` (not `implemented`) specifically because the [Q6] XM Directory license/permission/schema verification blocker is still open; `dotnet build`/`dotnet test` passing as of this revision. |

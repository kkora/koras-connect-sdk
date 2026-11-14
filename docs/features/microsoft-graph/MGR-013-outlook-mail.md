---
id: "MGR-013"
title: "Outlook mail"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "partial"
baseline_status: "Implemented: EnumerateMyMessagesAsync (delegated-only, applying MGR-004's general /me rule) and EnumerateUserMessagesAsync (app-only path) both clamp $top to the documented 1000 max rather than reject; SendMailAsync composes the documented message/toRecipients envelope and treats 202-no-body as success via the shared transport. GraphMessage.BodyHtml is a computed property over a nested body object, not a flat field, since Graph's wire shape nests content/contentType. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-013 - Outlook mail

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-013 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-013.
2. **Feature name:** Outlook mail.
3. **Description:** Read and send mail via Outlook/Exchange Online.
4. **Business value:** Read and send mail via Outlook/Exchange Online.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /me/messages` / `GET /users/{id}/messages` - default page size **10**, `$top` max **1000** [confirmed]; message bodies are **HTML-only** in the documented response shape (no separate plain-text content type returned alongside) [confirmed]. `POST /me/sendMail` - returns **202 Accepted with no response body** [confirmed].
11. **Nonfunctional requirements:** Explicit write opt-in gate on send.
12. **Public API contracts or proposed contracts:** Official API: `/messages` paging defaults/max, HTML-only body, `/sendMail`'s 202-no-body response - all confirmed. Proposed C# contracts:

```csharp
public sealed class GraphMessage { public string? Id { get; set; } public string? Subject { get; set; } public string? BodyHtml { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task SendMailAsync(string subject, string bodyHtml, IReadOnlyList<string> toRecipients, bool writesEnabled = false, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003, MGR-004.
14. **Security considerations:** Authentication and authorization: `Mail.Read`/`Mail.ReadWrite`/`Mail.Send` (application or delegated) [G11]. Required permissions/scopes/licenses: As above; application-permission mail access is a notably sensitive grant and should be flagged prominently in documentation. Data privacy and security: Mail content/metadata is highly sensitive PII; never logged by default under any circumstance.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019; default page size of 10 is small relative to other Graph resources and should be documented so callers don't assume a larger implicit batch. Error handling and concurrency: `/sendMail`'s 202-no-body response must not be misinterpreted as "no confirmation the mail was queued" - it is the documented success shape, not an error.
16. **Unit-test requirements:** Default page size of 10 handled correctly without assuming a larger batch; `$top` clamped to the documented max of 1000; `/sendMail` 202-no-body treated as success.
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a message list page and a sendMail call. Integration-test requirements: Authorized tenant with Exchange Online mailboxes; not available this pass.
18. **Acceptance criteria:** Mail read/send work correctly against synthetic fixtures with the documented defaults respected.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-tenant caveat.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (adapted `BodyHtml` shape - see below).
- [ ] Acceptance criteria and Definition of Done satisfied (the `/me`-only restriction for mail was inferred from MGR-004's general rule, not independently confirmed for mail specifically).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphMail.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphMailTests.cs`. Covers: `EnumerateMyMessagesAsync` throws `InvalidOperationException` before any HTTP call without a delegated context; `EnumerateUserMessagesAsync` works without one; a `$top` above 1000 is clamped down to 1000, not rejected; `SendMailAsync` requires both `writesEnabled` and a delegated context, composes the documented `message`/`toRecipients` envelope, and treats a `202`-no-body response as success (the shared transport already does this). Full suite: 951 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | The `/me`-only restriction applied to `EnumerateMyMessagesAsync`/`SendMailAsync` is inferred from MGR-004's general `/me` rule - this feature's own spec does not explicitly state mail shares that restriction, so it is a reasoned extension, not an independently confirmed fact, and should be reconfirmed before `verified`. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphMail.cs` plus `GraphMailTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; held at `partial` because the `/me`-only restriction's applicability to mail specifically is inferred, not independently confirmed. |
| 2026-11-14 | `partial` | A targeted sweep found `SendMailAsync` discarded the disposable `IntegrationJsonResponse` from `SendResponseAsync` without disposing it - the same pattern already fixed in Dataverse's equivalent methods, but never checked in this connector. Fixed. |

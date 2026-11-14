---
id: "MGR-014"
title: "Outlook calendar"
category: "microsoft-graph"
priority: "P1"
phase: "P2-D5"
status: "partial"
baseline_status: "Implemented: EnumerateEventsAsync/CreateEventAsync/UpdateEventAsync/DeleteEventAsync all address /users/{userId}/events uniformly with no /me-only restriction, matching the documented full app-only/delegated symmetry. EnumerateMyEventsAsync (added in review pass 1, closing an undisclosed gap against this feature's own documented GET /me/events requirement) addresses /me/events, guarded by GraphOptions.IsDelegatedContext like every other /me-shortcut method in this connector. GraphEvent.Start/End were adapted from the proposed DateTimeOffset? to a nested GraphEventDateTime (local time string + time zone name), since Graph's wire shape doesn't carry a UTC offset and this SDK takes no time zone database dependency. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-014 - Outlook calendar

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-014 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-014.
2. **Feature name:** Outlook calendar.
3. **Description:** Read and manage Outlook calendar events.
4. **Business value:** Read and manage Outlook calendar events.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P1.
7. **Planned development phase:** P2-D5; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /me/events`, `GET /users/{id}/events`, `POST .../events`, `PATCH`/`DELETE .../events/{id}` [confirmed]. **Notable, independently confirmed contrast with Teams/Planner (MGR-015 through MGR-017)**: calendar application permissions are **fully supported and symmetric** with delegated permissions - `Calendars.ReadBasic`/`Calendars.Read`/`Calendars.ReadWrite` are identical in capability across both delegated and application permission types, unlike Teams (RSC-gated) or Planner (app-only unsupported entirely).
11. **Nonfunctional requirements:** Explicit write opt-in gate on create/update/delete.
12. **Public API contracts or proposed contracts:** Official API: `/events` CRUD and the full app-only/delegated symmetry claim, confirmed. Proposed C# contracts:

```csharp
public sealed class GraphEvent { public string? Id { get; set; } public string? Subject { get; set; } public DateTimeOffset? Start { get; set; } public DateTimeOffset? End { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public async Task<string> CreateEventAsync(string userId, GraphEvent @event, bool writesEnabled = false, CancellationToken ct = default);
// EnumerateMyEventsAsync(GraphQuery? query = null, CancellationToken ct = default) - added in review pass 1 to
// close the gap against the documented GET /me/events requirement, which was otherwise entirely unimplemented.
```
13. **Dependencies:** MGR-001, MGR-003, MGR-004.
14. **Security considerations:** Authentication and authorization: `Calendars.ReadBasic`/`Calendars.Read`/`Calendars.ReadWrite`, application or delegated, with identical capability. Required permissions/scopes/licenses: As above. Data privacy and security: Calendar data can reveal sensitive scheduling/business information; not specially redacted beyond the SDK's general policy.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: Standard classification.
16. **Unit-test requirements:** Event CRUD composition; app-only vs. delegated symmetry is reflected correctly (no artificial restriction imposed where the vendor imposes none).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for an event list and a created event. Integration-test requirements: Authorized tenant with Exchange Online calendars; not available this pass.
18. **Acceptance criteria:** Calendar CRUD works identically under app-only and delegated tokens, matching the documented symmetry.
19. **Definition of Done:** Standard template.
20. **External API entitlement or documentation blockers:** None beyond the standing no-live-tenant caveat.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied (adapted `Start`/`End` shape - see below).
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphCalendar.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphCalendarTests.cs`. Covers: enumeration works without a delegated context, confirming no artificial `/me`-only restriction is imposed; create/update/delete all require the shared `writesEnabled` opt-in and address `/users/{userId}/events` uniformly; create posts the typed `GraphEvent` (via its `AdditionalData` extension-data merge) and parses the returned `id`; `GraphEventDateTime` round-trips the documented `dateTime`/`timeZone` pair; `EnumerateMyEventsAsync` (review pass 1) throws locally without a delegated context and hits `/me/events` with one. |
| Remaining work / blockers | `GraphEvent.Start`/`End` are `GraphEventDateTime` (local time string + time zone name) rather than the proposed contract's literal `DateTimeOffset?` - converting that pair to a precise UTC instant needs a time zone database this SDK does not depend on; documented as a deliberate adaptation. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphCalendar.cs` plus `GraphCalendarTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; held at `partial` because `Start`/`End` deviate from the proposed `DateTimeOffset?` contract. |
| 2026-11-14 | `partial` | Phase 2 review pass 1 found `GET /me/events` was a documented functional requirement with no corresponding method at all - added `EnumerateMyEventsAsync`, guarded by `IsDelegatedContext` per the established `/me`-shortcut pattern, plus tests. `verification` stays `partial` for the pre-existing `Start`/`End` deviation. |
| 2026-11-14 | `partial` | A targeted sweep found `UpdateEventAsync`/`DeleteEventAsync` discarded the disposable `IntegrationJsonResponse` from `SendResponseAsync` without disposing it - the same pattern already fixed in Dataverse's equivalent methods, but never checked in this connector. Fixed both. |

---
id: "MGR-018"
title: "Microsoft To Do"
category: "microsoft-graph"
priority: "P2"
phase: "P2-D7"
status: "partial"
baseline_status: "Implemented: EnumerateTodoListsAsync/EnumerateTodoTasksAsync/CreateTodoTaskAsync/UpdateTodoTaskAsync all address /me/todo/... with no delegated-only guard, correcting the common misconception that To Do shares Planner's delegated-only restriction. Unit-tested."
verification: "partial"
last_updated: "2026-11-14"
implementation_commit: null
---

# MGR-018 - Microsoft To Do

[Feature tracker](../README.md) | [Category](README.md) | [Phase 2 master architecture and scope](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md)

## Specification

Evidence aliases and official source citations [D1], [S*], [G1] etc. resolve in the [Phase 2 master specification](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md), which remains the authoritative source for shared architecture, cross-connector sections 15-30, and official API research. This file is the single, authoritative per-feature tracker for MGR-018 going forward; no Phase 2 implementation exists yet.

1. **Feature ID:** MGR-018.
2. **Feature name:** Microsoft To Do.
3. **Description:** Read and manage personal To Do task lists.
4. **Business value:** Read and manage personal To Do task lists.
5. **Owning package:** `Koras.Connect.Microsoft.Graph`.
6. **Priority:** P2.
7. **Planned development phase:** P2-D7; see the [implementation roadmap](../KORAS-CONNECT-SDK-PHASE-2-MASTER-FEATURES.md#24-implementation-roadmap).
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. No Phase 2 connector code exists in the repository as of the Phase 2 master specification commit `eb063ba`; this feature is `not-started`.
9. **Existing implementation evidence:** None.
10. **Functional requirements:** `GET /me/todo/lists`, `GET /me/todo/lists/{id}/tasks` [confirmed]. **Corrects a common misconception that To Do is delegated-only like Planner**: the official overview explicitly states "The API supports both delegated and application permissions" - independently confirmed, directly quoting the official overview. The application-permission read path uses `Tasks.Read.All`, with **no higher-tier application permission found** for read access beyond that single scope. **Not independently verified this pass**: whether a corresponding application-permission write scope exists with equivalent symmetry - only the read-side application permission was independently checked.
11. **Nonfunctional requirements:** Explicit write opt-in gate on task create/update.
12. **Public API contracts or proposed contracts:** Official API: `/me/todo/lists`/`tasks` endpoints and the both-delegated-and-application-permission support, confirmed via direct quote of the official overview. Proposed C# contracts:

```csharp
public sealed class GraphTodoTask { public string? Id { get; set; } public string? Title { get; set; } public Dictionary<string, JsonElement>? AdditionalData { get; set; } }
public IAsyncEnumerable<GraphTodoTask> EnumerateTodoTasksAsync(string listId, CancellationToken ct = default);
```
13. **Dependencies:** MGR-001, MGR-003.
14. **Security considerations:** Authentication and authorization: `Tasks.Read.All` (application, read confirmed) or `Tasks.ReadWrite`/`Tasks.Read` (delegated). Required permissions/scopes/licenses: As above; write-side application-permission symmetry is unconfirmed (see below). Data privacy and security: Personal task content is PII; never logged by default.
15. **Error and edge-case handling:** Pagination, throttling and retry behavior: Reuses MGR-019. Error handling and concurrency: Standard classification.
16. **Unit-test requirements:** Application-permission read path (`Tasks.Read.All`) works correctly; a write attempted under an application-only token source is explicitly flagged as unverified in test documentation rather than silently assumed to work (since write-side app-permission symmetry was not independently confirmed).
17. **Integration-test requirements:** Contract-test scenarios: Synthetic fixture for a task-list read under an application token. Integration-test requirements: Authorized tenant with To Do provisioned, specifically to confirm write-side application-permission support; not available this pass.
18. **Acceptance criteria:** Read access works under both delegated and application tokens; write-side application-permission support is explicitly marked unconfirmed until independently verified.
19. **Definition of Done:** Standard template; write-side app-permission confirmation is an explicit release blocker for calling the write path `verified` (Section 28).
20. **External API entitlement or documentation blockers:** Write-side application-permission symmetry with the read path is unconfirmed.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied (write-side application-permission confirmation is this feature's own explicit release blocker for `verified`).
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: added `src/Koras.Connect.Microsoft.Graph/GraphTodo.cs` and `tests/Koras.Connect.Tests/MicrosoftGraph/GraphTodoTests.cs`. Covers: list/task enumeration both work without a delegated context, confirming no artificial restriction is imposed; create/update both require the shared `writesEnabled` opt-in and compose the documented `/me/todo/lists/{listId}/tasks[/{taskId}]` paths. Full suite: 971 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | Write-side application-permission symmetry with the read path remains unconfirmed per this feature's own spec - not locally enforced or distinguished, since the code path is identical regardless; an app-only caller should independently verify write access works. No live tenant available this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-13 | `not-started` | Imported from the Phase 2 master specification (commit `eb063ba`), split into a per-feature tracker file matching the SDK/ADP/SAP SuccessFactors/Qualtrics convention. No implementation status upgraded by this split. |
| 2026-11-14 | `partial` | Added `GraphTodo.cs` plus `GraphTodoTests.cs`. Checked the unit/contract and security/cancellation/resource-ownership gates; held at `partial` because write-side application-permission symmetry remains unconfirmed. |
| 2026-11-14 | `partial` | A targeted sweep found `UpdateTodoTaskAsync` discarded the disposable `IntegrationJsonResponse` from `SendResponseAsync` without disposing it - the same pattern already fixed in Dataverse's equivalent methods, but never checked in this connector. Fixed. |

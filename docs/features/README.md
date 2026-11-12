# Feature tracker

Each feature has one Markdown file. Open the file to review its requirements, current status, remaining work and completion evidence. The [master specification](KORAS-CONNECT-SDK-MASTER-FEATURES.md) owns shared architecture, scope, audit evidence and release gates. Feature files own individual requirements and current tracking state; master status tables remain a dated baseline snapshot.

## Update a feature

1. Edit the YAML `status` at the top of the feature file; keep its ID and filename stable.
2. Update `last_updated` and `implementation_commit` when a tested implementation revision exists. Preserve `baseline_status` as audit history.
3. Record the actual changes, remaining work, blockers, validation commands/results and applicable vendor evidence under Completion evidence.
4. Check only evidenced completion gates and append a dated Status history entry. Update `verification` separately; partial implementation and blocked live access can coexist.
5. Set `completed` only when all feature acceptance/Done requirements and applicable approvals are satisfied. A commit or green build alone does not establish completion.

| Status | Meaning |
|---|---|
| `not-started` | No feature implementation identified |
| `planned` | Future scope designed; implementation not started |
| `partial` | Useful implementation exists; requirements or validation remain |
| `implemented` | Bounded implementation exists; full verification remains |
| `tested` | Required execution evidence recorded; acceptance/completion still pending |
| `completed` | All acceptance/Done gates met and required acceptance recorded |
| `blocked` | A specific prerequisite prevents the next required action; record it explicitly |

`verification` may be `unverified`, `partial`, `verified`, `blocked` or `not-applicable`, supported by evidence in the file. Imported statuses preserve the master audit; no feature is marked completed and no new runtime tests were run during this split. Phase and priority are separate fields. Evidence paths in specifications are relative to the repository root, not the feature folder.

## Categories

| Category | Features | Index |
|---|---|---|
| Shared SDK | 20 | [sdk/](sdk/README.md) |
| ADP | 22 | [adp/](adp/README.md) |
| SAP SuccessFactors | 16 | [sap-successfactors/](sap-successfactors/README.md) |
| Qualtrics | 16 | [qualtrics/](qualtrics/README.md) |

Total: **74 features** (20 SDK, 22 ADP, 16 SAP, 16 Qualtrics). No application source, package or workflow changes are part of this reorganization.

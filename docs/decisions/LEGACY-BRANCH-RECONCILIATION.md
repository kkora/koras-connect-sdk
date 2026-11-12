# Legacy branch reconciliation — November 12, 2026

This reconciliation incorporates the histories of the remaining SDK development branches while retaining the reviewed four-package SDK implementation on main.

| Branch | Reviewed tip | Resolution |
|---|---|---|
| feature/g4-g6-sdk-release-expansion | 987965cee2948c05de1593220b1169e5117771e6 | Already merged by PR #2; no further change required. |
| feature/independent-sdk-g0-g3 | c968628e32abe4905e2cbdd7bbc1ce14482a0ffa | G4–G6 implementation is already on main. Its two different documentation files are older versions superseded by the PR #2 reconciliation and architecture update. Retain main's versions. |
| feature/sdk-g0-g3-foundation | cd2f66f547fe485abc4f00d8f3adb9c311cce457 | Superseded prototype; preserve commits as merge ancestry, without adding its obsolete project graph/source to the active solution. |

## Review findings

The foundation branch has four preliminary projects (Abstractions, Core, Authentication, ADP), a raw worker reader, a one-shot token acquisition helper and generic connector contracts. Their transport/authentication responsibilities are superseded by main's tested core and provider clients. The old connector contracts have no consumers in the current solution and are not introduced as a second public API contract. The prototype has no test project or CI verification; its own status document labels it partial/not accepted. Its credential-options record has no custom redacted string representation, its HTTP/token clients do not require HTTPS, and its build props disable warnings-as-errors. These behaviors must not replace the current credential protection and engineering settings.

The old `src/Koras.Connect.ADP` directory conflicts by case with current `src/Koras.Connect.Adp`, making a literal combination unsafe for Windows checkouts and duplicating the provider/package surface. Keep the current case, packages, solution, tests, version and release manifest. The old status document also proposes reference-repository inspection; that proposal is superseded by the owner's prohibition and is not adopted.

## Merge semantics and verification

The reconciliation commit has main as its first parent and the two outstanding branch tips as additional parents. The resolved tree is main plus this decision record; no runtime, build, package, workflow, credential, infrastructure or publication behavior changes. This explicitly resolves overlapping/obsolete implementations in favor of the current tested SDK while retaining their history. A merge commit (not squash/rebase) is required so all reviewed branch tips remain ancestors of main and are safe to delete afterwards.

Run the existing Linux/Windows build, 55 .NET tests, 11 release-gate tests, dependency audit, package/SBOM checks and independent consumer on the reconciliation PR. Confirm each reviewed branch has zero commits ahead after the merge. Branch deletion is left to the owner. No previously recorded live-tenant or G0/G3 acceptance gap is reclassified as complete by this history reconciliation.

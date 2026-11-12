# Development and main promotion policy

Owner direction: all development belongs on develop; main receives promotions only from develop. No direct development commits to main.

## Branch flow

- develop is created from main commit 55987f4a715b31af4ca7b18f2f4b4716a802f87f.
- Use develop for integration and as the default PR target. Optional short-lived feature/fix branches must target develop.
- Promote with a PR from this repository's develop branch into main, using a merge commit to preserve ancestry.
- Do not squash develop promotions: preserving ancestry keeps subsequent promotions and comparisons predictable.
- Do not push development commits or force updates directly to main.

## CI and promotion guard

SDK CI runs on develop as well as main and short-lived development branches. The Main promotion policy workflow uses pull_request_target, does not check out PR code, and only reads PR metadata. It reports main-source/develop on the PR head and rejects a main PR whose source is not this repository's develop branch. This guard must be made REQUIRED in server-side main protection; merely adding the workflow does not protect main.

## Required owner/admin settings

The connector used for implementation cannot modify repository administration settings. Until independently verified in GitHub, these are required settings, not claims of active enforcement:

1. Set repository default branch to develop.
2. Create an ACTIVE rule targeting exactly refs/heads/main, with no bypass actors (including administrators).
3. Require a pull request before merging; block branch deletion and force pushes. Do not enable direct-push exceptions.
4. Require build-and-test (ubuntu-latest), build-and-test (windows-latest), and main-source/develop from GitHub Actions. Add the source status after its first promotion-policy run.
5. Require branches to be up to date; require conversations to be resolved. Use merge commits for develop promotions.
6. An independent approving review can be required when another reviewer is available; it must not be fabricated by the SDK automation.

Branch protection does not natively select an allowed PR source branch. The required workflow/status is part of the intended enforcement. Repository owners/admins must also protect workflow changes and permissions against bypass; GitHub's normal administrator/plan limitations apply.

The first develop-to-main PR installs the policy on main. Because pull_request_target runs trusted default-branch workflow definitions, the new source-policy workflow may not execute until it exists on the default branch. During bootstrap verify source=develop in the same repository explicitly; enable the required source check after it has reported. Public package publishing and deployments remain excluded.

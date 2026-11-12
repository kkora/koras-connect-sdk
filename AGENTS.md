# Koras Connect SDK development rules

- All development and integration work belongs on `develop`. Never commit or push development changes directly to `main`.
- Before editing, verify the current branch. If starting from `main`, switch to `develop` or a short-lived feature/fix branch based on `develop`.
- Feature/fix pull requests target `develop`.
- `main` accepts only promotion pull requests from `develop` in `kkora/koras-connect-sdk`; use merge commits to preserve promotion ancestry.
- Require successful SDK CI before promoting. Never bypass configured branch protections, required checks, or review requirements.
- Public NuGet publication, production deployments and paid services require their separate existing authorizations.
- Do not read, copy, port or follow code/design from `kkora/AdpIntegration`.
- See `docs/decisions/DEVELOPMENT-BRANCH-POLICY.md` for CI and administrator protection settings. The workflow alone does not establish server-side branch protection; verify those settings before claiming main is protected.

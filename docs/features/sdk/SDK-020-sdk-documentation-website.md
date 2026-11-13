---
id: "SDK-020"
title: "SDK documentation website"
category: "sdk"
priority: "P1"
phase: "P7"
status: "not-started"
baseline_status: "Not started"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# SDK-020 - SDK documentation website

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SDK-020.
2. **Feature name:** SDK documentation website.
3. **Description:** Give users searchable trustworthy APIs, examples and limitations.
4. **Business value:** A reproducible local site build exposes supported APIs and migration/security guidance reduces integration risk and makes this capability independently consumable.
5. **Owning package:** all shipping packages (documentation).
6. **Priority:** P1.
7. **Planned development phase:** P7; P1 specifies only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** README/planning Markdown only; no site source/build pipeline found. Evidence aliases resolve in section 3; missing symbols are proposed only.
10. **Functional requirements:** Build site source from versioned docs/API references, samples and capability matrices; add link/accessibility checks and credential-safe examples; plan hosting separately.
11. **Nonfunctional requirements:** No hidden host/infrastructure dependency; bounded buffers and work; deterministic output where applicable; net10.0/Linux/Windows support. Capture a P2 benchmark baseline before setting a release performance budget.
12. **Public API contracts or proposed contracts:** Proposed generated API/reference site; no runtime API.
13. **Dependencies:** SDK-019, ADP-022; package graph in section 7.
14. **Security considerations:** No secret/PII in defaults, logs, exceptions or package assets; host-owned credential stores and streams; least dependency privilege. Apply section 16.
15. **Error and edge-case handling:** Exercise broken links, absent API symbols, sample compilation, secret canaries and navigation checks. Apply the sanitized error and operation-safety rules in section 17; preserve cancellation and document incomplete work.
16. **Unit-test requirements:** Broken links, absent API symbols, sample compilation, secret canaries and navigation checks.
17. **Integration-test requirements:** Run isolated package-reference consumers on Linux/Windows and deterministic synthetic handlers/streams; test cross-package registration and compatibility. No external service is needed for the neutral behavior; provider live scenarios are governed by their feature entries.
18. **Acceptance criteria:** A reproducible local site build exposes supported APIs and migration/security guidance; all examples compile; deployment remains separately authorized.
19. **Definition of Done:** All numbered acceptance scenarios above pass with revision-linked unit/contract/consumer evidence; public API, docs, security review and compatibility gate in section 26 pass. Required live validation is recorded separately and never reported passed without access.
20. **External API entitlement or documentation blockers:** Hosting/domain/deployment choices pending; site build does not authorize deployment.

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
| Remaining work / blockers | **Deliberately deferred, not attempted this pass.** This is the last ticket in the SDK-001..SDK-020 sweep and, unlike the other P7 tickets (SDK-012/SDK-015), it isn't blocked on external procurement — it genuinely requires standing up a new documentation-site toolchain from nothing (`eng/*` has no site generator, templates, or link/accessibility-checking tooling of any kind today, confirmed by this pass's own repo-wide audits across SDK-016/017/018/019). Building that properly means choosing and configuring a static-site generator that can pull API references from the XML doc comments every package already generates (`GenerateDocumentationFile=true` in `Directory.Build.props`, confirmed present), wiring link-checking and accessibility checks, and deciding how samples/capability matrices surface in navigation — a real toolchain/technology choice (DocFX vs. Docusaurus vs. a hand-rolled static generator) that deserves its own planning pass rather than a rushed scaffold at the tail of a 20-ticket sweep. Doing it hastily would risk shipping a half-built site that creates more review/maintenance burden than the markdown docs it would replace. Following up: plan and implement as its own piece of work, picking a toolchain deliberately; `docs/features/**/*.md`'s existing per-feature trackers and `CHANGELOG.md` (SDK-019) are the versioned-docs source this site would need to render from. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `not-started` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `not-started` | Audited and confirmed no documentation-site toolchain exists anywhere in the repo. Deliberately deferred rather than rushed: this needs a real technology choice (static-site generator) and its own planning pass, not a scaffold bolted on at the end of an unrelated sweep. No implementation attempted; status intentionally left `not-started`. Logged as the final followup of this pass. |

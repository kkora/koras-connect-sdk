---
id: "ADP-020"
title: "Sandbox and contract test suite"
category: "adp"
priority: "P0"
phase: "P3-P4"
status: "partial"
baseline_status: "Partially implemented"
verification: "blocked"
last_updated: "2026-11-13"
implementation_commit: null
---

# ADP-020 - Sandbox and contract test suite

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-020.
2. **Feature name:** Sandbox and contract test suite.
3. **Description:** Verify protocol correctness and expose live-access gaps honestly.
4. **Business value:** Verify protocol correctness and expose live-access gaps honestly.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3-P4; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-TEST all five C# files; no live harness; historical execution in VERIFICATION.md; aliases resolve in section 3.
10. **Functional requirements:** Organize unit/contract/security/live suites; schema-version synthetic fixtures with provenance; opt-in read-only sandbox harness; approved writes isolated and never default.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Proposed sandbox configuration/test category; no production runtime API.
13. **Dependencies:** ADP-001-019, SDK-018; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Contract fixture negative cases, missing credential skip reasons, no-network-by-default, redaction and destructive-operation exclusion. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Contract fixture negative cases, missing credential skip reasons, no-network-by-default, redaction and destructive-operation exclusion.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** CI runs deterministic synthetic tests on both OSes; separately authorized sandbox results identify product/schema/grants/revision; missing access is reported skipped/blocked, never passed.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** Sandbox root, credentials/certificate, population and subscriptions absent; synthetic tests can proceed without them.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [ ] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [ ] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [ ] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available (uncommitted at time of writing) |
| Validation commands / results | `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: Passed 183, Skipped 1, Failed 0, Total 184 (the sandbox test itself reports as the 1 Skipped, never Passed or silently absent) |
| Remaining work / blockers | **Important limitation found during this work:** xunit's documented runtime/dynamic skip convention (`Xunit.Sdk.SkipException.ForSkip`, the `$XunitDynamicSkip$` message marker) was tried first and empirically reports as a hard **Failure**, not Skipped, under this repository's exact `xunit 2.9.3` + `xunit.runner.visualstudio 3.1.4` combination (verified with a disposable throwaway repro project outside this repo). The shipped test therefore uses the static, compile-time `[Fact(Skip = "...")]` gate instead, which is verified to report as `[SKIP]`/"Skipped: 1" and never fails the run. This means the gate does not yet dynamically re-enable itself purely by setting the KORAS_ADP_SANDBOX_* environment variables; a maintainer must still remove the `Skip` attribute (after wiring an authorized read and recording tenant/grant evidence here) to actually run it. No live sandbox credentials exist in this environment regardless, so this has no practical effect today. **Genuinely blocked on real-world access, not an engineering gap:** this ticket's own external-blocker field states "Sandbox root, credentials/certificate, population and subscriptions absent; synthetic tests can proceed without them" — and they have (the full synthetic/contract suite is green). Actually running the live check requires an authorized sandbox tenant this environment does not have; documented and left as a followup (see `docs/features/adp/ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md` for the exact prerequisites a future maintainer would need). No further implementation attempted this pass. |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `[Trait("Category","Sandbox")] Koras.Connect.Tests.AdpSandboxTests` (`tests/Koras.Connect.Tests/AdpSandboxTests.cs`), documenting the KORAS_ADP_SANDBOX_TENANT/CLIENT_ID/CLIENT_SECRET/CERT_PATH opt-in variables and gating the (not-yet-implemented) live check behind `[Fact(Skip = "...")]` with an explicit reason string, verified to render as a distinct `Skipped` xunit result rather than Passed/absent/Failed. See the remaining-work note above for why the originally-intended dynamic per-run skip was not used. Remains `partial`: no live-tenant check is implemented, and the skip is static rather than truly dynamic. |
| 2026-11-13 | `partial` | Audited this pass's options and confirmed the remaining live-check gap is genuinely blocked on unavailable authorized sandbox access, not an engineering gap; the full synthetic/contract half this ticket also requires is green. Added the prerequisites a future maintainer would need in `docs/features/adp/ADP-SAMPLES-AND-LIVE-SANDBOX-PREREQUISITES.md` (ADP-012/ADP-022). No further implementation attempted this pass. `verification` set to `blocked` (from `unverified`) to reflect this accurately. |

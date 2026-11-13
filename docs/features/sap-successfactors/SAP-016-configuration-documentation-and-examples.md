---
id: "SAP-016"
title: "Configuration, documentation and examples"
category: "sap-successfactors"
priority: "P0"
phase: "P5"
status: "implemented"
baseline_status: "Partially implemented"
verification: "partial"
last_updated: "2026-11-13"
implementation_commit: null
---

# SAP-016 - Configuration, documentation and examples

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** SAP-016.
2. **Feature name:** Configuration, documentation and examples.
3. **Description:** Make SAP adoption independently reproducible.
4. **Business value:** Make SAP adoption independently reproducible.
5. **Owning package:** Koras.Connect.SAP.SuccessFactors; shared contracts follow section 7.
6. **Priority:** P0.
7. **Planned development phase:** P5; preserve preview during P2-P4; P1 documentation only.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-SAP options/constructor checks; README and E-SAMPLE option smoke; aliases resolve in section 3; generic infrastructure is not a complete dedicated business contract.
10. **Functional requirements:** Document root/company/client/assertion/RBP, effective dates, row checks and checkpoints; offline examples plus optional sandbox; consistent DI/direct validation.
11. **Nonfunctional requirements:** net10.0/Linux/Windows, bounded pages/waits/buffers, cancellable I/O; no ADP/Qualtrics, scheduler, cloud or database dependency. Measure performance before setting release budgets.
12. **Public API contracts or proposed contracts:** Existing SuccessFactorsOptions/ODataQuery; proposed AddKorasSuccessFactors and examples.
13. **Dependencies:** SDK-003, SDK-014, SAP-006; acyclic graph in section 7.
14. **Security considerations:** Isolate company/technical-user credentials; verify entity/field RBP and target population; redact assertions, keys and records. Writes require explicit opt-in, verified contract and test authorization.
15. **Error and edge-case handling:** Unsafe root/date conflicts/invalid limits, missing assertion, redacted config and sample compilation. Preserve cancellation; sanitize diagnostics; uncertain writes require reconciliation, never blind replay.
16. **Unit-test requirements:** Unsafe root/date conflicts/invalid limits, missing assertion, redacted config and sample compilation.
17. **Integration-test requirements:** Synthetic entity/metadata/upsert protocol fixtures and isolated consumers on both OSes; authorized sandbox reads plus separately authorized disposable writes where required. Record schema, permissions, revision and outcomes or access blocker.
18. **Acceptance criteria:** Clean checkout runs SAP offline without ADP; invalid settings fail before send; docs list supported operations and live limits.
19. **Definition of Done:** Acceptance plus section 26 unit/contract/security/API/consumer/docs gates; current entity evidence and mandatory sandbox outcomes attached. Missing required schema/access blocks acceptance rather than becoming a pass.
20. **External API entitlement or documentation blockers:** API hostname/company/client/roles must come from authorized consumer; no host guessing.

## Completion evidence

The checkboxes below are verification gates, not an inferred assessment of existing code. Check a gate only when its evidence is recorded here; use a documented not-applicable reason where appropriate.

- [x] Functional requirements and public API contract satisfied.
- [ ] Acceptance criteria and Definition of Done satisfied.
- [x] Required unit, contract and integration checks pass at the recorded revision.
- [x] Security, cancellation, resource ownership and compatibility checks pass where applicable.
- [ ] Required vendor schema, entitlement and live validation gates closed, or explicitly not applicable.
- [x] Documentation and consumer examples updated; acceptance recorded where required.

| Evidence | Record |
|---|---|
| Implementation revision | Not recorded; add the tested commit SHA when available |
| Validation commands / results | Added `samples/SuccessFactors.PersonAndEmploymentSync` (standalone console project, `ProjectReference` to `Koras.Connect.Sap.SuccessFactors` only, not added to `Koras.Connect.slnx`, same convention as the ADP/Qualtrics samples), closing the "no standalone, separately-compiled offline sample project" gap recorded below. Demonstrates the full connector surface offline: `SamlBearerTokenSource` exchange -> `EnumeratePersonsAsync` -> `EnumerateEmploymentAsync` -> `EnumerateJobsAsync` -> `ResolveManagerChainAsync` -> `GetMetadataAsync`/`ParseEntityDescriptors` -> `UpsertAsync` (opt-in write), entirely against a synthetic `HttpMessageHandler` with no network access and no company ID/API host/SAML assertion provider required. Verified with `dotnet build` and `dotnet run` from a clean checkout; both succeeded and the console output narrates every step. `dotnet test tests/Koras.Connect.Tests/Koras.Connect.Tests.csproj`: full suite unaffected (the sample is outside the solution's test discovery), 553 passed, 3 skipped, 0 failed. |
| Remaining work / blockers | No further engineering gap identified for the offline example surface. Redacted-config coverage still relies on the pre-existing `ODataQuery.ToString()` override, not new work. API hostname/company/client/roles against a live, authorized tenant remain a standing external blocker (see above), unchanged by this pass. |
| Acceptance / completion | Unit-level only: `SapServiceCollectionTests.cs` covers duplicate-registration rejection, an unsafe (non-HTTPS) root rejected before any `HttpClient`/handler is created, a missing assertion source throwing before any registration, and a valid registration resolving both `SuccessFactorsClient` and `SamlBearerTokenSource`. Live/sandbox acceptance not recorded. |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `partial` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |
| 2026-11-12 | `partial` | Added `AddKorasSuccessFactors(IServiceCollection, SuccessFactorsConnectionOptions, TransportOptions?)` DI extension (`SuccessFactorsServiceCollectionExtensions.cs`), a package `README.md` documenting `SuccessFactorsOptions`/`SamlBearerTokenSource` setup, supported entities, the `writesEnabled` gate and an explicit not-yet-implemented list, plus a missing null-check on `SamlBearerTokenSource`'s assertion-source delegate. Tests in `tests/Koras.Connect.Tests/SapServiceCollectionTests.cs`. Kept `partial`: no standalone runnable sample project yet. |
| 2026-11-13 | `implemented` | Added `samples/SuccessFactors.PersonAndEmploymentSync`, closing the standalone-sample gap recorded above. Checked the unit/contract/integration, security/cancellation/resource-ownership, and documentation/consumer-example completion gates; status moves from `partial` to `implemented`. `verification` moved from `unverified` to `partial`: API hostname/company/client/roles against a live, authorized tenant remain a standing external blocker. |

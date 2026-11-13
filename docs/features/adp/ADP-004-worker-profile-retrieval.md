---
id: "ADP-004"
title: "Worker profile retrieval"
category: "adp"
priority: "P0"
phase: "P3"
status: "implemented"
baseline_status: "Implemented but unverified"
verification: "unverified"
last_updated: "2026-11-12"
implementation_commit: null
---

# ADP-004 - Worker profile retrieval

[Feature tracker](../README.md) | [Category](README.md) | [Master architecture and scope](../KORAS-CONNECT-SDK-MASTER-FEATURES.md)

## Specification

Evidence aliases (for example E-AUTH) resolve in the [repository audit](../KORAS-CONNECT-SDK-MASTER-FEATURES.md#3-repository-audit-and-baseline). Shared requirements in master sections 16-26 remain binding. Proposed symbols are not existing implementation evidence.

1. **Feature ID:** ADP-004.
2. **Feature name:** Worker profile retrieval.
3. **Description:** Read an authorized worker using a stable public model.
4. **Business value:** Read an authorized worker using a stable public model.
5. **Owning package:** Koras.Connect.ADP.
6. **Priority:** P0.
7. **Planned development phase:** P3; no implementation in P1.
8. **Current implementation status:** The `status` metadata at the top of this file is authoritative. The `baseline_status` preserves the November 12, 2026 master audit finding.
9. **Existing implementation evidence:** E-WORKERS GetWorkerAsync; E-TEST IndividualWorkerPreservesUnknownFieldsAndEscapesFilters; aliases resolve in section 3.
10. **Functional requirements:** Retrieve by associate OID, preserve extension fields and masked/absent values, define not-found/empty behavior and disposal; do not conflate workerID with associateOID.
11. **Nonfunctional requirements:** Bound time/memory/request count; support net10.0 on Linux/Windows, stream records without whole-population buffering and retain host-owned resources. No host scheduling or storage dependency.
12. **Public API contracts or proposed contracts:** Existing Task<Worker?> GetWorkerAsync(string associateOid, CancellationToken); GET /hr/v2/workers/{associateOID}.
13. **Dependencies:** ADP-001, ADP-002, ADP-019; graph in section 7.
14. **Security considerations:** Require actual tenant/product/operation permissions; no credentials or HR payloads in errors/logs; default vendor masking, explicit field selection, TLS and no redirects. Writes need explicit enablement and authorization for the test environment.
15. **Error and edge-case handling:** Valid/empty/multiple worker envelopes, unsafe identifier encoding, unknown fields, 403/404 and malformed JSON. Use section 17 classification; uncertain writes require reconciliation rather than retry; absence is not deletion or loss of employment.
16. **Unit-test requirements:** Valid/empty/multiple worker envelopes, unsafe identifier encoding, unknown fields, 403/404 and malformed JSON.
17. **Integration-test requirements:** Synthetic HttpMessageHandler/schema/stream tests on both OSes plus isolated package consumers; authorized read-only sandbox checks for applicable vendor behavior, and separately authorized disposable write cases only where required. Record exact product/grants/schema and revision; no live access is claimed here.
18. **Acceptance criteria:** Synthetic fixtures preserve typed and unknown data; empty authorized result returns documented null, non-success errors remain classified; live authorized worker matches sanitized schema.
19. **Definition of Done:** Meet the feature acceptance criteria, unit/contract/security/consumer gates and section 26; attach current official schema/grant evidence for claimed operations and explicit live result or blocker. Do not mark Accepted while a mandatory external gate is unresolved.
20. **External API entitlement or documentation blockers:** WFN product, worker read CAR grant, actor/population and field visibility must be verified against customer metadata [A1].

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
| Remaining work / blockers | See functional requirements, acceptance criteria and external blockers above; replace with precise remaining items as work progresses |
| Acceptance / completion | Not recorded; do not infer completion from a commit or passing build |

## Status history

| Date | Status | Evidence / reason |
|---|---|---|
| 2026-11-12 | `implemented` | Imported from master specification commit `4f9c080a07e9c6a8ef076658f25e0df12049d023`; no implementation status upgraded by this split. |

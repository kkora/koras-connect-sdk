# ADP WFN capability and entitlement matrix

Scope: U.S. Workforce Now, not a promise of compatibility with WFN Next Gen, Lyric, RUN, Vantage or international products. All implemented endpoints have mock/contract coverage; no live credentials or tenant subscriptions are available. “Implemented” describes SDK behavior, not an ADP grant or live certification.

Sources reviewed November 11, 2026 (owner local date):

- [S1: ADP WFN worker guide](https://developers.adp.com/articles/preview/guide-worker-management-api-guide-for-adp-workforce-now-0) and [official PDF](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Worker_Management_API_Guide_for_ADP_Workforce_Now).
- [S2: ADP official WFN request collection](https://github.com/adpllc/marketplace-sample-payloads/blob/master/wfn/ADP%20WFN%20APIs.postman_collection.json). Used as endpoint/protocol documentation, never as implementation source.
- [S3: termination](https://developers.adp.com/articles/preview/guide-worker-termination-api-guide-for-adp-workforce-now-3?chapter=2).
- [S4: work-assignment guide](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Work_Assignment_API_Guide_for_ADP_Workforce_Now).
- [S5: spending accounts](https://marketplace-cdn.adp.com/dev-portal/pdf/protected/Benefit_Spending_Account_Enrollment_API_Guide_%28HROpen%29_for_ADP_Workforce_Now).
- [S6: webhook guide](https://marketplace-cdn.adp.com/dev-portal/pdf/Webhooks).
- [S7: OAuth/mTLS](https://developers.adp.com/guides/api-guides/adp-marketplace---getting-started).
- [S8: current product catalog](https://developers.adp.com/articles/guides/adp-workforce-now-api-catalog).

## Implemented operation contracts

All paths are relative to a caller-selected HTTPS ADP API root. All requests require bearer token plus registered client mTLS certificate. CAR scope and actor/population access must be granted by ADP; the SDK allowlist does not grant them. Default worker data remains vendor-masked. No unmasking header is automatically added.

| Operation | Method and path | Request / response | Source / access |
|---|---|---|---|
| Worker list / search | GET `/hr/v2/workers` | `$top` 1–100, `$skip`, escaped `$filter`; workers array | S1/S2; authorized worker population, metadata determines filters |
| Individual worker | GET `/hr/v2/workers/{associateOID}` | encoded identifier; workers array → typed Worker | S1/S2; authorized worker |
| Worker metadata | GET `/hr/v2/workers/meta` | JSON capability metadata | S1/S2; workers metadata grant |
| Hire | POST `/events/hr/v1/worker.hire` | events[].data.transform.worker, customer-required personal/employment data; raw result | S1/S2; lifecycleManagement/worker.hire; product/version restrictions |
| Rehire | POST `/events/hr/v1/worker.rehire` | events[].data.transform.worker incl. identity, dates/reason; raw result | S1/S2; lifecycleManagement/worker.rehire |
| Assignment termination | POST `/events/hr/v1/worker.work-assignment.terminate` | events[].data.eventContext.worker.workAssignment.itemID; transform worker assignment dates/status; raw result | S3; workAssignmentManagement/worker.workAssignment.terminate; Practitioner |
| Manager change | POST `/events/hr/v1/worker.reports-to.modify` | vendor events envelope; customer reports-to context/transform; raw result | S1; workRelationshipManagement/worker.reportsTo.modify; not universally supported in Next Gen |
| Job title / assignment details | POST `/events/hr/v1/worker.work-assignment.modify` | associateOID, workAssignmentID, effectiveDateTime, eventReasonCode, workAssignment plus metadata-required fields | S4; workAssignmentManagement/worker.workAssignment.modify; Practitioner |
| Department allocations | POST `/events/hr/v1/worker.work-assignment.assigned-organizational-units.modify` | vendor events envelope and allocation codes; raw result | S1; workAssignmentManagement/worker.workAssignment.assignedOrganizationalUnits.modify; not equivalent to arbitrary home department replacement |
| Personal email change | POST `/events/hr/v1/worker.personal-communication.email.change` | vendor events envelope, contact context/transform | S2; communication change grant, tenant metadata |
| Payroll input | POST `/events/payroll/v1/pay-data-input.modify` | events data context payroll group/job ID and transform payDataInput | S2/S8; licensed payroll input grant; valid open payroll batch |
| Payroll instructions | GET `/payroll/v1/workers/{associateOID}/payroll-instructions` | JSON; no payload | S2; payroll instruction read grant |
| Pay distributions | GET `/payroll/v2/workers/{associateOID}/pay-distributions` | JSON; no payload | S2; pay distribution read grant |
| Time-off balances | GET `/time/v2/workers/{associateOID}/time-off-details/time-off-balances` | JSON; no payload | S2; time-off read grant |
| Time-off requests | GET `/time/v2/workers/{associateOID}/time-off-details/time-off-requests` | JSON; no payload | S2; time-off request read grant |
| Spending-account enrollments | GET `/benefits/v1/associates/{associateOID}/spending-account-enrollments` | mandatory effective date filter; JSON | S5; external benefit provider and spending-account enrollment grant, limited plan types |
| Departments | GET `/hr/v1/organization-departments` | JSON; no payload | S2; department read grant |
| Event polling | GET `/core/v1/event-notification-messages` | JSON or 204; retain adp-msg-msgid response header | S2; separate notification subscriptions per event |
| Event acknowledgement | DELETE `/core/v1/event-notification-messages/{messageId}` | explicit caller action, no retry; JSON or 204 | S2; queue subscription and processing rights |

Metadata GET routes are exposed only where verified: hire, rehire, personal email change, payroll input and work-assignment modify, using the event path plus `/meta`. Raw write payloads are intentionally not presented as fully typed or fully schema-validated; tenant metadata and ADP enforce full business rules. Worker/assignment/person/name/identifier models are typed with unknown fields preserved; complex organizational/manager/location fragments retain vendor JSON without dropping fields.

Exact canonical grants are available from the customer's CAR configuration and product metadata. Scope strings above use the documented suffix under `/hr/workerInformationManagement/workerManagement/` where applicable; do not infer any unlisted canonical scope or reuse grants from a different ADP product. OAuth `scope` configuration is optional; sending a scope does not provision CAR permissions.

## Explicit limits and unsupported capabilities

| Capability | Classification and evidence |
|---|---|
| WFN Next Gen / other ADP products | Not validated. S1 identifies per-operation product differences; do not reuse WFN paths as a universal contract. |
| Arbitrary home department or location writes | Unsupported in this preview. S1 lists department allocation writes separately and home organization/location notifications; no general replacement operation is claimed from those notification names. Use tenant-specific official contracts before adding another write. Reads preserve these fields. |
| All payroll, benefits or time endpoints | Unsupported beyond the explicit subset above. S8 contains many independently licensed/product-specific APIs; catalog presence is not an entitlement. |
| Universal last-modified incremental query | Unsupported. No universal WFN timestamp/delta-token contract was verified. Use subscribed notifications plus full-snapshot reconciliation, never a fabricated modifiedSince filter. |
| Complete event coverage | Vendor-limited. S1/S4 document WFNINFRAST-6728: assignment notifications cover job title/FLSA changes, not every position change. Retain periodic reconciliation. |
| Webhook receiver / durable inbox | Host-owned. SDK verifies S6 publisher HMAC and extracts delivery identity; no server, durable store, scheduling, or infrastructure is required by the SDK. |
| HMAC payload integrity / replay prevention | Not provided by S6's static client-ID HMAC. Require separate receiver authentication, TLS and durable deduplication. |
| ADP server-side generic batch write | Unsupported. Local BatchAsync partitions consumer work only; payroll input follows its documented vendor batch schema. |
| Server-side deletions inferred from missing workers | Unsupported: authorized population can change. Do not treat absence as termination. |
| Live sandbox certification | Unverified: no client ID/secret, certificate, authorized population or subscription inventory supplied. All tests are synthetic; no customer data or API writes were used. |

## G0–G3 deliverables

Architecture/contracts, two independent packages, Linux/Windows CI, token renewal and mTLS, configurable transport/retries/timeouts, safe instrumentation, workers, explicit operation dispatch, event helpers, reconciliation, batching and all four exports are implemented. Public publishing, CLI implementation and Cloud implementation remain outside this assignment. Verification evidence is recorded separately and never inferred from a green foundation build.

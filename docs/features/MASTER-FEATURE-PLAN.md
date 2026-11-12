# Koras Connect SDK — Master Feature Plan v1.0

Status: PLANNING ONLY; features are targets, not implemented capabilities.

| ID | Feature | Priority | Acceptance evidence |
|---|---|---|---|
| SDK-001 | Solution/package boundaries | P0 | Independent SDK build and package graph |
| SDK-002 | Public connector interfaces | P0 | Contract tests and documented API surface |
| SDK-003 | HTTP transport and DI | P0 | Mockable typed clients; no host dependency |
| SDK-004 | Certificate and OAuth abstractions | P0 | Secure credential lifecycle tests |
| SDK-005 | Pagination, cancellation, timeouts | P0 | Large/multipage mocked response tests |
| SDK-006 | Resilience and typed errors | P0 | Throttle, timeout and failure tests |
| SDK-007 | CSV/JSON/XML exports | P0 | Deterministic schema and encoding tests |
| SDK-008 | Excel export | P1 | Large-file and injection-safe export tests |
| SDK-009 | Logging and diagnostics | P0 | No secrets/PII leakage tests |
| SDK-010 | NuGet packaging and versioning | P0 | Clean external consumer installation |
| SDK-011 | Security/SBOM and license review | P0 | Reviewed dependency/license inventory |
| SDK-012 | API documentation and examples | P0 | Reproducible sanitized sample runs |
| SDK-013 | ADP connector | P0 | ADP capability matrix and sandbox evidence |
| SDK-014 | SAP SuccessFactors connector | P1 | OData V2/auth/entity contract verification |
| SDK-015 | Qualtrics connector | P1 | Survey/response/auth contract verification |

## Release acceptance
No claims of complete ADP coverage without documented product/API/entitlement matrix. Every P0 capability requires unit and contract tests. Vendor-backed integration tests require authorized sandboxes. CLI and Cloud compatibility are validated through independent consumer examples, not runtime dependencies.

## Not in SDK
Scheduling, cross-provider orchestration, tenant administration, cloud billing, job persistence, and managed worker operations.

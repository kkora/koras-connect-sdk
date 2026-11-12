# ADP Capability Matrix — Target and Audit Template

Status: UNVERIFIED until source audit and ADP documentation/entitlements review.

| ID | Capability | Priority | Existing implementation | Verification |
|---|---|---|---|---|
| ADP-001 | Certificate/mTLS authentication | P0 | User-reported | Inspect source, test authorized flow |
| ADP-002 | OAuth client credentials/token lifecycle | P0 | User-reported | Verify token handling and refresh semantics |
| ADP-003 | Worker profile retrieval | P0 | User-reported | Verify endpoints, typed responses |
| ADP-004 | Pagination/filtering | P0 | Unknown | Contract tests |
| ADP-005 | Worker change detection/incremental reads | P0 | Unknown | API capability and checkpoint tests |
| ADP-006 | Worker models and field mapping | P0 | Unknown | Schema fixtures |
| ADP-007 | CSV/JSON/XML export | P0 | Formats unverified | Inspect and test exporters |
| ADP-008 | Excel export | P1 | Unknown | Format/escaping tests |
| ADP-009 | Retry/throttle/errors | P0 | Unknown | Failure-injection tests |
| ADP-010 | Connection diagnostics | P0 | Unknown | Safe diagnostic output |
| ADP-011 | Employee lifecycle/organization/manager | P1 | Unknown | Entitlement/endpoint verification |
| ADP-012 | Payroll | P1 | Unknown | Licensed API verification |
| ADP-013 | Time/attendance | P2 | Unknown | Licensed API verification |
| ADP-014 | Benefits | P2 | Unknown | Licensed API verification |
| ADP-015 | Webhooks/events | P2 | Unknown | Provider-supported event review |
| ADP-016 | Sandbox, contract, security tests | P0 | Unknown | Passing test evidence |
| ADP-017 | ADP product/API/entitlement catalog | P0 | Unknown | Versioned supported-operation matrix |
| ADP-018 | CLI and hosted-worker consumer compatibility | P0 | Unknown | Independent host smoke tests |

## Coverage definition
'Complete' means verified coverage of documented, commercially accessible endpoints for explicitly supported ADP products and entitlements; not all ADP endpoints for all customers. Mark each operation Supported, Partial, Not implemented, Not available, or Entitlement required, with evidence.

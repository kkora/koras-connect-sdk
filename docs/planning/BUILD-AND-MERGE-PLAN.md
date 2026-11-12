# Koras Connect SDK — Independent implementation and merge plan

Status: execution plan; implementation incomplete. This document does not assert all ADP APIs are available or tested.

## Non-negotiable architecture
- Clean independent implementation based on official ADP specifications and authorized sandbox behavior.
- Do not consult, copy, translate or model code/design after kkora/AdpIntegration.
- SDK is reusable without CLI, Azure Functions, SaaS control plane or cloud account.
- Provider-specific contracts isolated from generic authentication, transport, resilience, serialization and diagnostics.
- No production customer credentials, employee PII or private keys in repository or logs.

## Acceptance criteria
1. Solution compiles on .NET 10, tests pass on Linux and Windows, packages build without warnings or accidental secrets.
2. Authentication: OAuth 2 client credentials, token expiry/refresh, mTLS with PEM/PFX or injected certificates, cancellation, concurrency, credential redaction.
3. HTTP: configurable timeouts, bounded retries for transient failures with Retry-After, rate limiting, typed errors, cancellation, streaming, dependency injection.
4. ADP: independent API capability/entitlement matrix with exact documented endpoint, method, scope, payload and permissions per supported operation.
5. Workers: list, page, query, retrieve individual worker, typed models preserving unknown fields, pagination safety and predictable error contracts.
6. Lifecycle: support documented read/write operations only when verified against official API contracts; no speculative endpoints or claims of universal write access.
7. Change synchronization: checkpoint-based incremental retrieval when supported, reconciliation and idempotent downstream handoff, without hidden persistence.
8. Exports: SDK-level serializers for JSON, CSV, XML and XLSX where justified; avoid tying SDK to filesystem, CLI, SFTP or cloud jobs.
9. Eventing: validate supported ADP webhook/event authenticity, delivery and deduplication against official specs before shipping.
10. Tests: auth, transport, pagination, error handling, edge cases, redaction, exports, and contract fixtures; live ADP sandbox tests when credentials/entitlements are available.
11. Docs: public API usage, NuGet packaging, configuration, security, supported API matrix, CLI/Cloud consumption examples.
12. PR ready only after green CI, review of diff, no secret/PII leakage and all declared G0–G3 acceptance criteria evidenced.

## Work order
A. Stabilize G0 independent API matrix and capability decisions.
B. Complete G1 solution/build/test/package automation.
C. Complete G2 auth/transport/resilience/diagnostics with unit tests.
D. Complete G3 documented ADP Workers APIs, typed models, query/paging, event and lifecycle capability slices, contract tests.
E. Build/test/pack, resolve CI failures, record test evidence and unsupported entitlements.
F. Mark PR ready, merge to main only after evidence demonstrates acceptance.

## Scope limitations
ADP access varies by product subscription and tenant permissions. Features without authoritative endpoint contracts or test entitlements are documented as unsupported/unverified rather than simulated. Public NuGet publishing and deployment remain out of scope.

## Evidence at plan creation
GitHub Actions run 43150810500 succeeded for the initial PR head 534f21ecd8815323b1e6bf554d84b7ccf3d32d3e. This only validates the initial foundation, not the complete feature matrix.

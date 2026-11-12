# SDK-G0 through SDK-G3 — Evidence and Outstanding Work

Branch: feature/sdk-g0-g3-foundation
Status: PARTIAL IMPLEMENTATION, NOT ACCEPTED.

## G0
kkora/AdpIntegration returned repository size 0 at inspection. Existing code cannot be audited. KEEP/REFACTOR/MOVE/REPLACE classification pending sanitized source upload. No claim of complete ADP API coverage.

## G1
Private repository and planning documentation exist. Branch contains initial .NET project files. Branch protections, workflows and package naming have not been verified.

## G2
Initial source for Abstractions, Core HTTP JSON client, and OAuth client credentials is added. Not yet compiled or tested in CI. Certificate loading/mTLS, token caching, export, resilience, and observability remain outstanding.

## G3
ADP worker raw-response client prototype added. Customer-specific endpoint is injected. Does not claim paging, typed models, all worker operations, payroll, benefits, time or attendance. ADP vendor sandbox tests and original-code audit are outstanding.

## Next validation
Run dotnet build and tests in a trusted development environment; add safe mocks, certificate/mTLS and full token lifecycle; inspect reference source; validate actual ADP endpoints, schema and entitlements. Do not publish NuGet or deploy cloud resources based on this prototype.

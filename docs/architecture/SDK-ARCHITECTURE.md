# Independent SDK architecture

Owner authorization: G0–G3 implementation, tests, commits and merge; public publishing remains excluded.

Two packages are intentional for this preview:

- `Koras.Connect`: provider-neutral OAuth, certificate loading, JSON transport, diagnostics, tabular export and reconciliation. Uses only the .NET runtime.
- `Koras.Connect.Adp`: WFN workers, operation allowlist, polling acknowledgements, webhook publisher authentication and optional DI registration. Depends on the core and Microsoft.Extensions.Http.

The earlier proposal for seven very small packages is consolidated to avoid unnecessary package/version coupling. Namespaces preserve separation. Neither package references CLI, Cloud, an application host, a database or external infrastructure. SAP and Qualtrics are future scope.

All code is independently engineered. No reference-repository material was consulted. Historical source-audit tasks are superseded by the owner's explicit prohibition.

## Ownership and security

Hosts retain certificate lifetime, supply secrets, configure TLS, own HttpClient lifetimes and persist checkpoints/inbox state. DI registration disables redirects and default HTTP URL loggers, uses separate named token/API clients, and rejects duplicate ADP tenant registrations in a provider. Direct consumers must disable redirects and redact URI-bearing framework telemetry at their host.

Core instrumentation uses `ActivitySource` and `Meter` named `Koras.Connect`, method/status tags only, with no worker identifiers, URIs, request bodies or tokens. Add these sources to the host's OpenTelemetry configuration; there is no exporter dependency.

Transport streams response parsing, enforces a total request deadline including retry waits, retries bounded GET failures with jitter, honors Retry-After, and never automatically replays POST or DELETE. A Retry-After beyond the configured budget fails rather than retrying early. Client-credential tokens are renewed before expiry under a semaphore; no refresh token is assumed. SDK errors omit response content. Arbitrary HTTP clients and host instrumentation remain the host's responsibility.

## Public contracts

`IAccessTokenSource` supports external secret/token stores. `IntegrationTransport` returns caller-owned JsonDocument or IntegrationJsonResponse with allowlisted protocol headers. Workers offer raw pages, escaped metadata-driven filters, individual lookup and typed async enumeration. Unknown worker fields survive deserialization. Paging continues until an empty page; consecutive repeated pages and an overall offset bound prevent runaway requests.

`AdpOperationsClient` is a low-level endpoint client, not a complete vendor schema validator. Supported write requests use customer metadata and original JSON payloads supplied by the host; enable operations only after checking CAR scope, population permissions, actor and product/version. There is no invented generic worker PATCH. The library does not infer permissions from enum values.

`Reconciliation` compares caller-supplied previous fingerprints with an asynchronous full snapshot. Record hashes are representation-sensitive (property ordering may cause safe extra updates). Commit state only after successful delivery and complete traversal. Deletions are deliberately not inferred because visibility/permissions can change. Batching is local enumeration, not an ADP bulk endpoint.

Webhook helpers verify the exact publisher header specified by ADP and extract organization/message delivery keys. The static credential-derived HMAC does not cover the payload or prevent replay: TLS, separate receiver authentication, durable deduplication and downstream transactions belong to the host. No HTTP webhook server is embedded in this SDK.

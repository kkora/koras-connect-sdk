# Independent Koras Connect SDK — G0–G3

Decision: implement independently from official vendor API contracts. kkora/AdpIntegration is excluded from design and implementation.

G0: provider-neutral architecture direction defined; independent vendor API coverage review still pending.
G1: .NET 10 project foundation and CI workflow authored.
G2: independent OAuth client-credentials, token cache, certificate transport, JSON transport and HTTP errors.
G3: ADP worker-read API prototype, async pagination and mocked HTTP tests.

Unfinished: CI results, full ADP entitlement/API matrix, typed models, lifecycle operations, events, export libraries, retries, integration testing, release validation. G0–G3 are NOT accepted or complete. No NuGet publication or deployment.

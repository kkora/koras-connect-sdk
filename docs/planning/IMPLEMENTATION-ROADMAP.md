# Phase 1 — Approval-Gated Roadmap

Status: PROPOSED. Repository planning-document authorization is not SDK implementation authorization.

| Gate | Scope | Exit evidence |
|---|---|---|
| SDK-G0 | Read-only audit of kkora/AdpIntegration and vendor coverage | Inventory, security findings, KEEP/REFACTOR/MOVE/REPLACE matrix |
| SDK-G1 | Repository planning foundation | Private repository, architecture/features/decisions documents reviewed |
| SDK-G2 | Core SDK implementation | Tests, contracts, licensing/security gates |
| SDK-G3 | ADP connector | Authorized sandbox evidence, capability coverage matrix |
| SDK-G4 | Release candidate | NuGet package validation in external consumer, SBOM, docs |
| SDK-G5 | Public NuGet release | Explicit publication approval and verified package |
| SDK-G6 | SAP/Qualtrics expansion | Separate approved connector specifications and tests |

## Next review
1. Verify reference repo contains sanitized source.
2. Conduct read-only source audit.
3. Revise feature priorities from evidence.
4. Request explicit implementation approval per gate.

No code, deployment, cloud resources, production integrations or NuGet publishing authorized.

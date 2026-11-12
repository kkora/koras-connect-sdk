# Independent Phase 1 execution roadmap

| Gate | Deliverable | Evidence |
|---|---|---|
| G0 | Original architecture and official ADP capability contracts | SDK-ARCHITECTURE.md and ADP-FEATURE-MATRIX.md |
| G1 | .NET 10 solution, packages, Linux/Windows build/test/pack pipeline | Koras.Connect.slnx, ci.yml, package validators |
| G2 | Authentication, certificates, safe transport, retries, diagnostics, exports, reconciliation | CoreTests and source |
| G3 | WFN workers, documented operations, event helpers, contract verification | AdpContractTests and feature matrix |

The prior reference-repository audit proposal is withdrawn under the owner's explicit instruction. No source from that repository is read or used. Missing tenant access and limitations must remain visible in VERIFICATION.md. CLI and Cloud are separate future products.

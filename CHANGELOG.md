# Changelog

All notable changes to the Koras Connect SDK are recorded here. Versions follow
`Directory.Build.props`'s `<Version>`; this file's top entry must always match it.
Entries reference feature IDs from `docs/features/` rather than restating full
specifications; see each feature's tracker file for current status, evidence
and remaining work. No entry here implies live-tenant, sandbox, or acceptance
verification beyond what its referenced tracker records.

## [Unreleased] — targeting 0.1.0-preview.2

### Added
- Shared connector/credential/pagination/mapping/validation abstractions and a structured exception hierarchy
  (`Koras.Connect.Abstractions`, `Koras.Connect.Exceptions`) — SDK-002, SDK-006, SDK-007, SDK-009, SDK-014.
- Schema-driven, streaming export providers (`IExportProvider`, `ExportSchema<T>`, CSV/JSON/XML/XLSX) on top of the
  existing `TabularExport` static methods — SDK-013.
- Optional `ILogger`/correlation-ID support and deadline-vs-cancellation distinction in `IntegrationTransport`;
  independent token-acquisition timeout and rotation (`Invalidate()`) in `ClientCredentialsTokenSource` —
  SDK-010, SDK-011.
- `CertificateCredentialProvider`/`RedactedCredential<T>` for cached, rotatable certificate loading — SDK-006.
- `Page<T>`/`ContinuationToken` resumable-pagination adapter for ADP worker listing — SDK-007, ADP-005.
- ADP: credential rotation (`AdpRotatingTokenSource`), `AdpConnector` read-only diagnostics, `AdpApiException`
  structured error classification (now correlation-ID-aware on both success and failure), `AdpChangeDetector`,
  worker identity/assignment mapping, streaming CSV/JSON/XML/XLSX worker export, typed organization/manager/
  time-off/benefits/notification DTOs, and an entitlement-state tracker — ADP-003, ADP-006 through ADP-013,
  ADP-019, ADP-020, ADP-021.
- SAP SuccessFactors: `EnumerateEmploymentAsync`/`EnumerateJobsAsync`, exception classification, manager-chain
  resolution, delta-sync checkpoint mechanics, metadata caching, `PerPerson` mapping, `AddKorasSuccessFactors` DI
  extension — SAP-005, SAP-008 through SAP-011, SAP-013 through SAP-016.
- Qualtrics: `AddKorasQualtrics` DI extension, `QualtricsApiException` and read-only diagnostics, reusable
  resumable paging, a concurrency limiter, a contacts/mailing-lists client (unverified against a live tenant),
  data mappers, and an offline sample — QLT-001, QLT-004 through QLT-006, QLT-009, QLT-012 through QLT-016.

### Fixed
- `ClientCertificateTransport.LoadPem` now re-imports the loaded certificate with a persisted key container, since
  `X509Certificate2.CreateFromPemFile`'s ephemeral key cannot be used for a Windows SChannel mTLS client-certificate
  handshake ("The credentials supplied to the package were not recognized").

### Known gaps
- No 8-package split yet (`Koras.Connect.Abstractions`/`Core`/`Authentication`/`Export`/`Observability` remain
  inside the single `Koras.Connect` assembly) — SDK-001.
- Package signing, API-compatibility baselines and a documentation website remain not started — SDK-015, SDK-017,
  SDK-020; these are process/infrastructure items, not library code.
- Every feature above is tracked as `partial` or `implemented`-but-`unverified` in its own tracker file: nothing
  in this release has been validated against a live tenant, sandbox, or production credential set unless that
  tracker's status history says otherwise.

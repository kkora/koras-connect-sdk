# Koras Connect SDK

Track requirements and completion in the [feature tracker](docs/features/README.md): one file per feature, grouped by SDK, ADP, SAP SuccessFactors and Qualtrics.

Independent .NET 10 integration libraries for application hosts, the future Koras Connect CLI and Koras Integration Cloud. This preview is independently written; no reference repository was consulted.

Packages: `Koras.Connect`, `Koras.Connect.Adp`, `Koras.Connect.Sap.SuccessFactors`, and `Koras.Connect.Qualtrics`, version `0.1.0-preview.2`. Packages are built for validation, not published.

```sh
dotnet restore Koras.Connect.slnx
dotnet build Koras.Connect.slnx -c Release
dotnet test Koras.Connect.slnx -c Release --no-build
dotnet pack Koras.Connect.slnx -c Release -o artifacts
python eng/validate_packages.py
python eng/generate_sbom.py
python eng/release_checks.py
```

## Host usage

```csharp
using Koras.Connect.Adp;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Transport;
using Microsoft.Extensions.DependencyInjection;

using var certificate = ClientCertificateTransport.LoadPkcs12(certificatePath, certificatePassword);
var services = new ServiceCollection();
services.AddKorasAdp(
    new ClientCredentialsOptions(new Uri("https://accounts.adp.com/auth/oauth/v2/token"), clientId, clientSecret),
    new AdpWorkersOptions(new Uri("https://api.adp.com")), certificate);
using var provider = services.BuildServiceProvider();
var workers = provider.GetRequiredService<AdpWorkersClient>();
await foreach (var worker in workers.EnumerateTypedWorkersAsync(cancellationToken))
{
    // Map selected fields and deliver to your own downstream service; never log employee records.
}
```

Load credential variables from the host's secret store. Keep the certificate alive until provider disposal. For direct construction use `ClientCertificateTransport.Create` for both OAuth and API connections. Custom HTTP handlers must disable redirects. Configure timeouts with `TransportOptions` and pass cancellation to every call.

For additional operations construct `AdpOperationsClient` with an `IntegrationTransport`, API root and an explicit operation allowlist. `GetMetadataAsync` exposes verified metadata endpoints. `SubmitEventAsync` dispatches vendor JSON after basic structural checks; validate customer-specific required fields and codes against metadata before submission. Writes are not automatically retried. A timeout after a write has an unknown outcome: reconcile with ADP before considering a repeat.

Polling: `ReadNotificationAsync` returns body plus `adp-msg-msgid`. Re-read affected workers, deduplicate by delivery identity in a durable inbox, complete downstream processing, then explicitly call `AcknowledgeNotificationAsync`. Empty queues can yield a null body/204. `AdpWebhookAuthentication` is a host helper; see the security notes before exposing a receiver.

Exports: `TabularExport.WriteJsonAsync`, `WriteCsvAsync`, `WriteXml`, `WriteExcel` write caller-owned streams. CSV protects common formula prefixes; XLSX uses inline strings, never formulas. Supply selected columns and explicit mappings (`Func<Worker,T>` is sufficient); streams must be discarded if export fails. `Reconciliation.DetectChangesAsync` and `BatchAsync` enable stateless downstream handoff with host-owned state.

See [coverage and entitlements](docs/features/ADP-FEATURE-MATRIX.md), [architecture/security](docs/architecture/SDK-ARCHITECTURE.md), and [execution evidence](docs/planning/VERIFICATION.md). Live ADP requests have not been verified; the test suite uses synthetic payloads. Preview APIs can change before a stable release. Licensing/distribution review remains required before public publishing.

## SAP SuccessFactors and Qualtrics

```csharp
using Koras.Connect.Sap.SuccessFactors;
using Koras.Connect.Qualtrics;

// Both HTTP handlers must disable redirects. The host owns HTTP/token-source lifetimes.
var sap = new SuccessFactorsClient(new IntegrationTransport(sapHttp, authorizedSapTokens),
    new SuccessFactorsOptions(new Uri("https://your-api-server/odata/v2/")));
await foreach (var person in sap.EnumeratePersonsAsync(cancellationToken: cancellationToken))
{
    // Apply an explicit downstream mapping; never log employee records.
}
var qualtrics = QualtricsClient.Create(qualtricsHttp, qualtricsApiTokenSource,
    new QualtricsOptions(new Uri("https://your-datacenter.qualtrics.com/API/v3/")));
var progressId = await qualtrics.StartResponseExportAsync(surveyId, cancellationToken: cancellationToken);
var ready = await qualtrics.WaitForExportAsync(surveyId, progressId, cancellationToken: cancellationToken);
await qualtrics.DownloadExportAsync(surveyId, ready.FileId!, destinationStream, cancellationToken);
```

SAP token sources must be authorized for the actual tenant. `SamlBearerTokenSource` can exchange host/IdP-generated assertions; generation/signing stays in the host. OData date values retain their vendor representation; use explicit `asOfDate` or effective-date ranges where supported. Upsert requires explicit writesEnabled and every returned row must be checked for failure, even at HTTP200.

Qualtrics uses X-API-TOKEN. Retain the returned progress ID so polling can resume without restarting exports. Downloads are raw ZIP/uncompressed bytes; the SDK does not extract archives or persist jobs. Dispose partial outputs on cancellation/failure.

See the [G4–G6 specification](docs/planning/G4-G6-SPECIFICATION.md), [SAP matrix](docs/features/SAP-FEATURE-MATRIX.md), [Qualtrics matrix](docs/features/QUALTRICS-FEATURE-MATRIX.md) and [release runbook](docs/planning/RELEASE-RUNBOOK.md). Connectors are mock-tested and have not been live-certified. Release automation does not waive existing ADP acceptance gaps.

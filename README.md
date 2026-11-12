# Koras Connect SDK

Independent .NET 10 integration libraries for application hosts, the future Koras Connect CLI and Koras Integration Cloud. This preview is independently written; no reference repository was consulted.

Packages: `Koras.Connect` and `Koras.Connect.Adp`, version `0.1.0-preview.1`. Packages are built for validation, not published.

```sh
dotnet restore Koras.Connect.slnx
dotnet build Koras.Connect.slnx -c Release
dotnet test Koras.Connect.slnx -c Release --no-build
dotnet pack src/Koras.Connect/Koras.Connect.csproj -c Release -o artifacts
dotnet pack src/Koras.Connect.Adp/Koras.Connect.Adp.csproj -c Release -o artifacts
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

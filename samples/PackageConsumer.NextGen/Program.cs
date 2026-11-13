using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Koras.Connect.Exceptions;
using Koras.Connect.Export;
using Koras.Connect.Transport;

// Proves the split packages are independently consumable without the Koras.Connect compatibility facade:
// one contract/implementation exercised from each of Abstractions, Core, Authentication and Export.

var descriptor = new ConnectorDescriptor("Example", "Product", "v1", ["read"], new Dictionary<string, EntitlementEvidence>());
if (descriptor.Provider != "Example") throw new InvalidOperationException("Koras.Connect.Abstractions contract failed.");

var root = EndpointSafety.ValidateRoot(new Uri("https://example.test/api"));
if (root.AbsoluteUri != "https://example.test/api/") throw new InvalidOperationException("Koras.Connect.Core transport contract failed.");

try { throw new ContractException("synthetic", new ErrorDetails("synthetic_code")); }
catch (IntegrationException ex) when (ex.Details.Code == "synthetic_code") { /* Koras.Connect.Core exception hierarchy resolved correctly. */ }

using var tokenSource = new ClientCredentialsTokenSource(new HttpClient(),
    new ClientCredentialsOptions(new Uri("https://example.test/oauth/token"), "client-id", "client-secret"));
if (tokenSource is not IAccessTokenSource) throw new InvalidOperationException("Koras.Connect.Authentication contract failed.");

using var csvOutput = new MemoryStream();
var schema = new ExportSchema<string>([new ExportColumn<string>("Value", v => v)]);
await new CsvExportProvider().WriteAsync(csvOutput, AsAsync(["a", "b"]), schema);
if (csvOutput.Length == 0) throw new InvalidOperationException("Koras.Connect.Export contract failed.");

Console.WriteLine("Next-generation package consumer smoke passed using only Abstractions/Core/Authentication/Export directly, no facade.");

static async IAsyncEnumerable<string> AsAsync(IEnumerable<string> values)
{
    foreach (var value in values) { yield return value; await Task.Yield(); }
}

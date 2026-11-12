using System.Text.Json;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Export;

var worker = JsonSerializer.Deserialize<Worker>("""{"associateOID":"synthetic","person":{"legalName":{"givenName":"Example"}},"workAssignments":[{"jobTitle":"Tester"}],"futureField":true}""")!;
if (worker.Person?.LegalName?.GivenName != "Example" || worker.AdditionalData?["futureField"].GetBoolean() != true)
    throw new InvalidOperationException("Packaged model contract failed.");
using var output = new MemoryStream();
TabularExport.WriteExcel(output, ["Id", "Title"], new IReadOnlyList<string?>[] { new[] { worker.AssociateOid, worker.WorkAssignments[0].JobTitle } });
if (output.Length == 0) throw new InvalidOperationException("Packaged export failed.");
Console.WriteLine("NuGet consumer smoke passed using synthetic data without ADP access.");

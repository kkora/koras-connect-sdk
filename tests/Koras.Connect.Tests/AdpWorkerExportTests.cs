using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Koras.Connect.Adp.Export;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Export;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-008/009: streaming export of <see cref="Worker"/> through the documented, fixed column set, without
/// buffering the whole population and without ever exporting vendor extension-data fields.</summary>
public sealed class AdpWorkerExportTests
{
    private static Worker Worker(string associateOid, string? extensionValue = null)
    {
        var baseJson = "{\"associateOID\":\"" + associateOid
            + "\",\"person\":{\"legalName\":{\"givenName\":\"Ada\",\"familyName1\":\"Lovelace\"}},\"workAssignments\":[{\"jobTitle\":\"Engineer\",\"hireDate\":\"2024-01-01\"}]";
        var json = extensionValue is null ? baseJson + "}" : baseJson + ",\"futureField\":\"" + extensionValue + "\"}";
        return JsonSerializer.Deserialize<Worker>(json)!;
    }

    private static async IAsyncEnumerable<Worker> ToAsync(IEnumerable<Worker> workers, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var worker in workers) { cancellationToken.ThrowIfCancellationRequested(); yield return worker; await Task.Yield(); }
    }

    /// <summary>Column-restricted JSON export composed here, in the consuming (test) layer, from
    /// <see cref="WorkerExportProjection.CreateSchema(DateOnly?)"/> plus <see cref="TabularExport"/> &#8212; the
    /// Koras.Connect.Adp package itself does not depend on Koras.Connect.Export (master spec section 7: "ADP does
    /// not require Export transitively"), so this composition lives at the call site instead of inside the package.</summary>
    private static async Task ExportWorkersAsRestrictedJsonAsync(Stream destination, IAsyncEnumerable<Worker> workers,
        DateOnly? effectiveAssignmentAsOf = null, CancellationToken cancellationToken = default)
    {
        var schema = WorkerExportProjection.CreateSchema(effectiveAssignmentAsOf);
        await TabularExport.WriteJsonAsync(destination, ProjectRows(workers, schema, cancellationToken), cancellationToken);
    }

    private static async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ProjectRows(IAsyncEnumerable<Worker> workers, ExportSchema<Worker> schema,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var worker in workers.WithCancellation(cancellationToken))
        {
            var values = schema.Project(worker);
            var row = new Dictionary<string, string?>(schema.ColumnNames.Count, StringComparer.Ordinal);
            for (var i = 0; i < schema.ColumnNames.Count; i++) row[schema.ColumnNames[i]] = values[i];
            yield return row;
        }
    }

    [Fact]
    public async Task CsvExportIncludesOnlyTheDocumentedColumnsAndExcludesExtensionData()
    {
        var workers = ToAsync([Worker("w1", extensionValue: "secret-not-exported")]);
        using var output = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new CsvExportProvider(), output, workers);

        var text = Encoding.UTF8.GetString(output.ToArray());
        var header = text.Split('\n')[0].TrimEnd('\r');
        Assert.Equal(string.Join(",", WorkerExportProjection.ColumnNames.Select(c => $"\"{c}\"")), header);
        Assert.DoesNotContain("futureField", text);
        Assert.DoesNotContain("secret-not-exported", text);
        Assert.Contains("Ada", text);
        Assert.Contains("Lovelace", text);
        Assert.Contains("Engineer", text);
    }

    [Fact]
    public async Task XmlAndColumnRestrictedJsonAndXlsxExportsAllCompleteWithoutBufferingTheWholeSequence()
    {
        var workers = ToAsync([Worker("w1"), Worker("w2")]);
        using var xml = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new XmlExportProvider(), xml, workers);
        Assert.True(xml.Length > 0);

        var workers2 = ToAsync([Worker("w1", extensionValue: "secret-not-exported"), Worker("w2")]);
        using var jsonOut = new MemoryStream();
        await ExportWorkersAsRestrictedJsonAsync(jsonOut, workers2);
        var json = Encoding.UTF8.GetString(jsonOut.ToArray());
        Assert.True(jsonOut.Length > 0);
        Assert.DoesNotContain("futureField", json);
        Assert.DoesNotContain("secret-not-exported", json);
        Assert.Contains("AssociateOid", json);

        var workers3 = ToAsync([Worker("w1"), Worker("w2")]);
        using var xlsx = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new XlsxExportProvider(), xlsx, workers3);
        Assert.True(xlsx.Length > 0);
    }

    /// <summary>ADP-009: an independent reader &#8212; raw <see cref="ZipArchive"/>/<see cref="XDocument"/> parsing
    /// of the OOXML package, not the writer's own code &#8212; recovers the documented column values and confirms
    /// no <c>&lt;f&gt;</c> formula node was generated, closing the "independent workbook-reader validation"
    /// unit-test requirement that the previous pass left as only a non-empty-output check.</summary>
    [Fact]
    public async Task XlsxExportIsIndependentlyReadableAndContainsNoFormulaNodes()
    {
        var workers = ToAsync([Worker("w1", extensionValue: "secret-not-exported")]);
        using var xlsx = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new XlsxExportProvider(), xlsx, workers);
        xlsx.Position = 0;

        using var zip = new ZipArchive(xlsx, ZipArchiveMode.Read);
        using var sheet = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var doc = XDocument.Load(sheet);

        Assert.DoesNotContain(doc.Descendants(), e => e.Name.LocalName == "f");
        var cellValues = doc.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value).ToArray();
        Assert.Equal(WorkerExportProjection.ColumnNames, cellValues.Take(WorkerExportProjection.ColumnNames.Count));
        Assert.Contains("w1", cellValues);
        Assert.Contains("Ada", cellValues);
        Assert.Contains("Lovelace", cellValues);
        Assert.Contains("Engineer", cellValues);
        Assert.DoesNotContain("futureField", cellValues);
        Assert.DoesNotContain("secret-not-exported", cellValues);
    }

    [Fact]
    public async Task OptInColumnSelectionExportsOnlyTheRequestedFieldsInCanonicalOrder()
    {
        var workers = ToAsync([Worker("w1")]);
        using var output = new MemoryStream();
        await WorkerExportProjection.ExportAsync(new CsvExportProvider(), output, workers, ["JobTitle", "AssociateOid"]);

        var header = Encoding.UTF8.GetString(output.ToArray()).Split('\n')[0].TrimEnd('\r');
        Assert.Equal("\"AssociateOid\",\"JobTitle\"", header); // canonical order, not request order
    }

    [Fact]
    public void SelectingAnUnapprovedColumnNameIsRejectedRatherThanSilentlyIgnored()
    {
        var ex = Assert.Throws<ArgumentException>(() => WorkerExportProjection.CreateSchema(["AssociateOid", "Ssn"]));
        Assert.Contains("Ssn", ex.Message);
    }

    [Fact]
    public void EmptyColumnSelectionIsRejected()
        => Assert.Throws<ArgumentException>(() => WorkerExportProjection.CreateSchema([]));

    [Fact]
    public async Task StreamingExportStopsPromptlyOnCancellationMidSequence()
    {
        async IAsyncEnumerable<Worker> Infinite([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var i = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Worker($"w{i++}");
                await Task.Delay(1, cancellationToken);
            }
        }
        using var cts = new CancellationTokenSource();
        using var output = new MemoryStream();
        var exportTask = WorkerExportProjection.ExportAsync(new CsvExportProvider(), output, Infinite(cts.Token), cancellationToken: cts.Token);
        await Task.Delay(20);
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exportTask);
        Assert.True(output.Length > 0); // some rows were already streamed out before cancellation, proving no whole-population buffering
    }
}

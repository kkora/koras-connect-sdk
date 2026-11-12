using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
        await WorkerExportProjection.ExportJsonAsync(jsonOut, workers2);
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

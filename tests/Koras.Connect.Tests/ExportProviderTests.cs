using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Koras.Connect.Export;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-013: closes the unit-test requirements not already covered by <c>CoreTests.ExportsEscapeDataAndLeaveStreamOpen</c>
/// (which only exercises one formula prefix and does not test <see cref="ExportSchema{T}"/> projection itself).
/// Covers null/date/culture/Unicode projection, every formula-injection prefix, a real newline/quote round-trip,
/// width-mismatch rejection, Excel column-count bounds, and empty record sets. "Canceled stream" is already
/// covered by SDK-010's <c>CancellationAndTimeoutTests.CancelingMidExportLeavesWhateverWasAlreadyStreamedInTheDestination...</c>.</summary>
public sealed class ExportProviderTests
{
    private sealed record Row(string? Name, DateOnly? Born, decimal? Amount);

    private static async IAsyncEnumerable<T> AsAsync<T>(IEnumerable<T> items)
    { foreach (var item in items) { yield return item; await Task.Yield(); } }

    [Fact]
    public async Task ProjectionHandlesNullDateCultureAndUnicodeValues()
    {
        var schema = new ExportSchema<Row>(
        [
            new("Name", r => r.Name),
            new("Born", r => r.Born, Format: "yyyy-MM-dd"),
            new("Amount", r => r.Amount, Format: "N2")
        ], new CultureInfo("de-DE"));

        using var output = new MemoryStream();
        await new CsvExportProvider().WriteAsync(output, AsAsync([
            new Row("Alix Müller — 日本語", new DateOnly(2026, 1, 5), 1234.5m),
            new Row(null, null, null)
        ]), schema);

        var text = Encoding.UTF8.GetString(output.ToArray());
        Assert.Contains("Alix Müller — 日本語", text);
        Assert.Contains("2026-01-05", text);
        Assert.Contains("1.234,50", text); // de-DE group/decimal separators prove Culture was honored
        var lines = text.Split('\n');
        Assert.Equal("\"\",\"\",\"\"", lines[2].TrimEnd('\r')); // the all-null row falls back to NullText ("")
    }

    [Fact]
    public void ExportSchemaRejectsAnEmptyColumnList()
        => Assert.Throws<ArgumentException>(() => new ExportSchema<Row>([]));

    [Theory]
    [InlineData("=cmd")]
    [InlineData("+1")]
    [InlineData("-1")]
    [InlineData("@SUM(A1)")]
    [InlineData("\tTab-led")]
    [InlineData("\rCr-led")]
    [InlineData("\nLf-led")]
    public async Task EveryFormulaInjectionPrefixIsNeutralizedWithALeadingApostrophe(string hostileValue)
    {
        using var output = new MemoryStream();
        await TabularExport.WriteCsvAsync(output, ["Value"], AsAsync<IReadOnlyList<string?>>([[hostileValue]]));
        var text = Encoding.UTF8.GetString(output.ToArray());
        // Csv() prepends the apostrophe to the whole original value unmodified (it only peeks at a trimmed copy
        // to decide whether to prepend it), so the escaped field is exactly "'" + the original hostile value.
        Assert.Contains("\"'" + hostileValue, text);
    }

    [Fact]
    public async Task EmbeddedNewlinesAndQuotesRoundTripCorrectlyWithinASingleQuotedCsvField()
    {
        using var output = new MemoryStream();
        await TabularExport.WriteCsvAsync(output, ["Value"], AsAsync<IReadOnlyList<string?>>([["line one\nline \"two\""]]));
        var text = Encoding.UTF8.GetString(output.ToArray());
        // One field, properly quoted, with the inner newline preserved literally and the inner quotes doubled -
        // a naive split on '\n' would incorrectly see this as two lines if escaping were broken.
        Assert.Contains("\"line one\nline \"\"two\"\"\"", text);
    }

    [Fact]
    public async Task EmptyRecordSetsStillProduceAValidHeaderOnlyDocumentForEveryFormat()
    {
        using var csv = new MemoryStream();
        await TabularExport.WriteCsvAsync(csv, ["Name"], AsAsync<IReadOnlyList<string?>>([]));
        Assert.Equal("\"Name\"", Encoding.UTF8.GetString(csv.ToArray()).TrimEnd('\r', '\n'));

        using var xml = new MemoryStream();
        await TabularExport.WriteXmlAsync(xml, ["Name"], AsAsync<IReadOnlyList<string?>>([]));
        xml.Position = 0;
        Assert.Empty(XDocument.Load(xml).Root!.Elements());

        using var excel = new MemoryStream();
        await TabularExport.WriteExcelAsync(excel, ["Name"], AsAsync<IReadOnlyList<string?>>([]));
        excel.Position = 0;
        using var zip = new ZipArchive(excel, ZipArchiveMode.Read);
        using var sheet = zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        Assert.Single(XDocument.Load(sheet).Descendants(), e => e.Name.LocalName == "row"); // header row only
    }

    [Fact]
    public async Task AMismatchedRowWidthIsRejectedRatherThanSilentlyMisaligningColumns()
    {
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            TabularExport.WriteCsvAsync(output, ["A", "B"], AsAsync<IReadOnlyList<string?>>([["only-one"]])));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16385)]
    public void ExcelColumnCountOutOfBoundsIsRejectedBeforeAnyRowIsWritten(int columnCount)
    {
        using var output = new MemoryStream();
        var columns = Enumerable.Range(0, columnCount).Select(i => $"C{i}").ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => TabularExport.WriteExcel(output, columns, []));
    }

    // Excel's 1,048,576-row ceiling is checked per-row in TabularExport.WriteExcel/WriteExcelAsync; driving a real
    // test past that boundary would mean writing over a million rows, which is impractical for a fast unit test.
    // The column-count bound above exercises the same guard clause shape at a reachable scale; the row-count bound
    // is verified by code inspection only (src/Koras.Connect.Export/TabularExport.cs: "if (++count > 1048576)").
}

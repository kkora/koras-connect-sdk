using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
namespace Koras.Connect.Export;

/// <summary>Stream-oriented exports with caller-selected fields; no implicit filesystem or persistence.</summary>
public static class TabularExport
{
    public static Task WriteJsonAsync<T>(Stream output, IEnumerable<T> records, CancellationToken cancellationToken = default)
        => JsonSerializer.SerializeAsync(output, records, cancellationToken: cancellationToken);
    public static async Task WriteCsvAsync(Stream output, IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string?>> rows, CancellationToken cancellationToken = default)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
        await writer.WriteLineAsync(string.Join(",", columns.Select(Csv)).AsMemory(), cancellationToken);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested(); ValidateWidth(columns, row);
            await writer.WriteLineAsync(string.Join(",", row.Select(Csv)).AsMemory(), cancellationToken);
        }
        await writer.FlushAsync(cancellationToken);
    }
    public static void WriteXml(Stream output, IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string?>> rows, CancellationToken cancellationToken = default)
    {
        using var writer = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = false });
        writer.WriteStartElement("records");
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested(); ValidateWidth(columns, row); writer.WriteStartElement("record");
            for (var i = 0; i < columns.Count; i++) { writer.WriteStartElement("field"); writer.WriteAttributeString("name", columns[i]); writer.WriteString(row[i]); writer.WriteEndElement(); }
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }
    /// <summary>Minimal Office Open XML workbook using inline string cells, never formulas. Leaves output open.</summary>
    public static void WriteExcel(Stream output, IReadOnlyList<string> columns, IEnumerable<IReadOnlyList<string?>> rows, CancellationToken cancellationToken = default)
    {
        if (columns.Count is < 1 or > 16384) throw new ArgumentOutOfRangeException(nameof(columns));
        using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        Entry(zip, "[Content_Types].xml", """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
        Entry(zip, "_rels/.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
        Entry(zip, "xl/workbook.xml", """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Records" sheetId="1" r:id="rId1"/></sheets></workbook>""");
        Entry(zip, "xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
        using var stream = zip.CreateEntry("xl/worksheets/sheet1.xml").Open();
        using var x = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = new UTF8Encoding(false) });
        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        x.WriteStartElement("worksheet", ns); x.WriteStartElement("sheetData", ns);
        void Row(IEnumerable<string?> values)
        {
            x.WriteStartElement("row", ns);
            foreach (var value in values)
            {
                if (value?.Length > 32767) throw new ArgumentException("Excel cell text exceeds 32767 characters.");
                x.WriteStartElement("c", ns); x.WriteAttributeString("t", "inlineStr"); x.WriteStartElement("is", ns);
                x.WriteStartElement("t", ns); x.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve"); x.WriteString(value); x.WriteEndElement(); x.WriteEndElement(); x.WriteEndElement();
            }
            x.WriteEndElement();
        }
        Row(columns); var count = 1;
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested(); ValidateWidth(columns, row);
            if (++count > 1048576) throw new ArgumentException("Excel row limit exceeded."); Row(row);
        }
        x.WriteEndElement(); x.WriteEndElement();
    }
    private static void Entry(ZipArchive zip, string path, string content)
    { using var w = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); w.Write(content); }
    private static void ValidateWidth(IReadOnlyList<string> columns, IReadOnlyList<string?> row)
    { if (columns.Count != row.Count) throw new ArgumentException("Row width must match columns."); }
    private static string Csv(string? value)
    {
        value ??= "";
        // Neutralize spreadsheet formula injection, including leading whitespace/control characters.
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@".Contains(trimmed[0]) || value.StartsWith('\t') || value.StartsWith('\r') || value.StartsWith('\n')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}

namespace Koras.Connect.Export;

public sealed class CsvExportProvider : IExportProvider
{
    public string Format => "csv";
    public Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records, ExportSchema<T> schema, CancellationToken cancellationToken = default)
        => TabularExport.WriteCsvAsync(destination, schema.ColumnNames, Project(records, schema, cancellationToken), cancellationToken);
    private static async IAsyncEnumerable<IReadOnlyList<string?>> Project<T>(IAsyncEnumerable<T> records, ExportSchema<T> schema,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    { await foreach (var record in records.WithCancellation(cancellationToken)) yield return schema.Project(record); }
}

public sealed class XmlExportProvider : IExportProvider
{
    public string Format => "xml";
    public Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records, ExportSchema<T> schema, CancellationToken cancellationToken = default)
        => TabularExport.WriteXmlAsync(destination, schema.ColumnNames, Project(records, schema, cancellationToken), cancellationToken);
    private static async IAsyncEnumerable<IReadOnlyList<string?>> Project<T>(IAsyncEnumerable<T> records, ExportSchema<T> schema,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    { await foreach (var record in records.WithCancellation(cancellationToken)) yield return schema.Project(record); }
}

public sealed class XlsxExportProvider : IExportProvider
{
    public string Format => "xlsx";
    public Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records, ExportSchema<T> schema, CancellationToken cancellationToken = default)
        => TabularExport.WriteExcelAsync(destination, schema.ColumnNames, Project(records, schema, cancellationToken), cancellationToken);
    private static async IAsyncEnumerable<IReadOnlyList<string?>> Project<T>(IAsyncEnumerable<T> records, ExportSchema<T> schema,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    { await foreach (var record in records.WithCancellation(cancellationToken)) yield return schema.Project(record); }
}

public sealed class JsonExportProvider : IExportProvider
{
    public string Format => "json";
    public Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records, ExportSchema<T> schema, CancellationToken cancellationToken = default)
        => TabularExport.WriteJsonAsync(destination, records, cancellationToken);
}

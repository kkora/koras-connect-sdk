using System.Globalization;

namespace Koras.Connect.Export;

/// <summary>A single ordered, named projection from a source record to an exportable text value.</summary>
public sealed record ExportColumn<T>(string Name, Func<T, object?> Select, string NullText = "", string? Format = null);

/// <summary>Ordered column projection and formatting policy shared by every <see cref="IExportProvider"/>.</summary>
public sealed class ExportSchema<T>
{
    public IReadOnlyList<ExportColumn<T>> Columns { get; }
    public CultureInfo Culture { get; }

    public ExportSchema(IReadOnlyList<ExportColumn<T>> columns, CultureInfo? culture = null)
    {
        if (columns is null || columns.Count == 0) throw new ArgumentException("At least one column is required.", nameof(columns));
        Columns = columns;
        Culture = culture ?? CultureInfo.InvariantCulture;
    }

    public IReadOnlyList<string> ColumnNames => Columns.Select(c => c.Name).ToArray();

    public IReadOnlyList<string?> Project(T record)
    {
        var row = new string?[Columns.Count];
        for (var i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            var value = column.Select(record);
            row[i] = value switch
            {
                null => column.NullText,
                IFormattable formattable => formattable.ToString(column.Format, Culture),
                _ => Convert.ToString(value, Culture)
            };
        }
        return row;
    }
}

/// <summary>A format-specific, schema-driven writer that streams records without buffering the whole population.</summary>
public interface IExportProvider
{
    string Format { get; }
    Task WriteAsync<T>(Stream destination, IAsyncEnumerable<T> records, ExportSchema<T> schema, CancellationToken cancellationToken = default);
}

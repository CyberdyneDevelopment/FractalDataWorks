using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Data.RowSources.Delimited.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>
/// Writes records as an Excel workbook (.xlsx): a bold frozen header row, numbers as numbers, dates as dates, and a further worksheet
/// after each <see cref="XlsxRowWriterOptions.RowsPerSheet"/> rows.
/// </summary>
/// <remarks>
/// <para>
/// Written as the rows arrive, straight into the zip, so memory holds one row whatever the data set's size. The workbook is
/// finished when the writer is disposed: that writes the parts that tie the worksheets together, so a workbook that stops partway and
/// is not disposed is not a valid file, which is the point.
/// </para>
/// <para>
/// What Excel cannot hold is changed visibly, never dropped: text over 32,767 characters is cut, a character XML cannot carry
/// becomes U+FFFD, an integer of more than fifteen digits is written as text (Excel would round it), a date before 1900-03-01 is
/// written as ISO text, and NaN or infinity as text.
/// </para>
/// </remarks>
public sealed class XlsxRowWriter : IRowWriter
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const int MaxText = 32_767;
    private const long MaxExactInteger = 999_999_999_999_999;
    private static readonly DateTime Epoch = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime FirstDay = new(1900, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private readonly ZipArchive _archive;
    private readonly List<string> _columns;
    private readonly string _sheetName;
    private readonly int _rowsPerSheet;
    private readonly List<string> _sheets = [];
    private XmlWriter? _xml;
    private int _rowCount;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="XlsxRowWriter"/> class.</summary>
    /// <param name="target">Where the workbook goes; written synchronously and left open.</param>
    /// <param name="options">The columns and worksheet limits.</param>
    public XlsxRowWriter(Stream target, XlsxRowWriterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        var opts = options ?? new XlsxRowWriterOptions();
        if (opts.Columns.Count == 0)
        {
            throw new ArgumentException("XlsxRowWriterOptions.Columns must contain at least one column name.", nameof(options));
        }

        if (opts.Columns.Count > XlsxRowWriterOptions.MaxColumns)
        {
            throw new ArgumentException(
                $"An Excel worksheet holds at most {XlsxRowWriterOptions.MaxColumns} columns and {opts.Columns.Count} were given.", nameof(options));
        }

        if (opts.RowsPerSheet is < 2 or > XlsxRowWriterOptions.MaxRowsPerSheet)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), opts.RowsPerSheet, "XlsxRowWriterOptions.RowsPerSheet must be at least 2 (a header and a row) and at most 1048576, an Excel worksheet's row limit.");
        }

        _columns = [.. opts.Columns];
        _sheetName = opts.SheetName;
        _rowsPerSheet = opts.RowsPerSheet;
        _archive = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: true);
    }

    /// <inheritdoc />
    public void Write(IReadOnlyDictionary<string, object?> row)
    {
        ArgumentNullException.ThrowIfNull(row);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_xml is null || _rowCount >= _rowsPerSheet)
        {
            FinishSheet();
            StartSheet();
        }

        _rowCount++;
        _xml!.WriteStartElement("row", Main);
        _xml.WriteAttributeString("r", _rowCount.ToString(CultureInfo.InvariantCulture));
        for (var index = 0; index < _columns.Count; index++)
        {
            Cell(XlsxParts.ColumnName(index) + _rowCount.ToString(CultureInfo.InvariantCulture), row.TryGetValue(_columns[index], out var value) ? value : null, _columns[index]);
        }

        _xml.WriteEndElement();
    }

    /// <inheritdoc />
    public void WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
        {
            Write(row);
        }
    }

    /// <inheritdoc />
    public ValueTask WriteAll(IEnumerable<IReadOnlyDictionary<string, object?>> rows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(row);
        }

        return default;
    }

    /// <inheritdoc />
    public void Write(DataRecord record) => Write(record.ToDictionary());

    /// <inheritdoc />
    public void Write(IEnumerable<DataRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        foreach (var record in records)
        {
            Write(record);
        }
    }

    /// <inheritdoc />
    public async ValueTask Write(IAsyncEnumerable<DataRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        await foreach (var record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            Write(record);
        }
    }

    /// <summary>Does nothing: the workbook is not a file until the writer is disposed, and its bytes reach the target as the zip is written.</summary>
    public void Flush()
    {
    }

    /// <summary>Finishes the workbook: closes the last worksheet and writes the parts that make it a file Excel opens.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        FinishSheet();

        // A data set with no rows is still a workbook: one worksheet holding the header row.
        if (_sheets.Count == 0)
        {
            StartSheet();
            FinishSheet();
        }

        XlsxParts.Write(_archive, _sheets);
        _archive.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }

    private void StartSheet()
    {
        _sheets.Add(XlsxParts.SheetName(_sheetName, _sheets.Count + 1));
        var entry = _archive.CreateEntry($"xl/worksheets/sheet{_sheets.Count}.xml", CompressionLevel.Fastest);
        _xml = XmlWriter.Create(entry.Open(), new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = true });
        _xml.WriteStartDocument(true);
        _xml.WriteStartElement("worksheet", Main);
        _xml.WriteStartElement("sheetViews", Main);
        _xml.WriteStartElement("sheetView", Main);
        _xml.WriteAttributeString("workbookViewId", "0");
        _xml.WriteStartElement("pane", Main);
        _xml.WriteAttributeString("ySplit", "1");
        _xml.WriteAttributeString("topLeftCell", "A2");
        _xml.WriteAttributeString("activePane", "bottomLeft");
        _xml.WriteAttributeString("state", "frozen");
        _xml.WriteEndElement();
        _xml.WriteEndElement();
        _xml.WriteEndElement();
        _xml.WriteStartElement("sheetData", Main);

        _rowCount = 1;
        _xml.WriteStartElement("row", Main);
        _xml.WriteAttributeString("r", "1");
        for (var index = 0; index < _columns.Count; index++)
        {
            Text(XlsxParts.ColumnName(index) + "1", _columns[index], 1);
        }

        _xml.WriteEndElement();
    }

    private void FinishSheet()
    {
        if (_xml is null)
        {
            return;
        }

        _xml.WriteEndElement();
        _xml.WriteEndElement();
        _xml.WriteEndDocument();
        _xml.Dispose();
        _xml = null;
    }

    private void Cell(string reference, object? value, string column)
    {
        if (RecordValueText.IsAbsent(value))
        {
            return;
        }

        if (value is string text)
        {
            Text(reference, text, 0);
        }
        else if (value is bool flag)
        {
            Plain(reference, "b", flag ? "1" : "0", 0);
        }
        else if (IsMoment(value!))
        {
            Moment(reference, value!);
        }
        else if (IsNumber(value!))
        {
            Number(reference, value!);
        }
        else
        {
            Text(reference, RecordValueText.Format(value!, column), 0);
        }
    }

    private static bool IsMoment(object value) => value is DateTime or DateTimeOffset or DateOnly or TimeOnly;

    // Why by type code: the numeric types are one contiguous run (SByte to Decimal), which stands for every one of them. An enum
    // reports its underlying type's code, so it is excluded: its name, not its number, is what a person reads.
    private static bool IsNumber(object value)
        => value is not Enum && Type.GetTypeCode(value.GetType()) is >= TypeCode.SByte and <= TypeCode.Decimal;

    private void Moment(string reference, object value)
    {
        var (when, style) = value switch
        {
            DateTime moment => (moment, 2),
            DateTimeOffset moment => (moment.UtcDateTime, 2),
            DateOnly day => (day.ToDateTime(TimeOnly.MinValue), 3),
            _ => (Epoch.Add(((TimeOnly)value).ToTimeSpan()), 4),
        };

        // Why text before 1900-03-01: Excel's calendar has no 1900-02-29 and no day before 1900-01-01, so a serial there is wrong or negative.
        if (style != 4 && when < FirstDay)
        {
            Text(reference, IsoText(value), 0);
            return;
        }

        Plain(reference, null, (when - Epoch).TotalDays.ToString("R", CultureInfo.InvariantCulture), style);
    }

    private static string IsoText(object value) => value switch
    {
        DateTime moment => moment.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
        DateTimeOffset moment => moment.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture),
        DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => ((TimeOnly)value).ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
    };

    private void Number(string reference, object value)
    {
        var text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
        var exact = value is not (long or ulong) || Math.Abs(Convert.ToDecimal(value, CultureInfo.InvariantCulture)) <= MaxExactInteger;
        var finite = value is not (double or float) || double.IsFinite(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        if (exact && finite)
        {
            Plain(reference, null, text, 0);
        }
        else
        {
            Text(reference, text, 0);
        }
    }

    private void Plain(string reference, string? type, string text, int style)
    {
        _xml!.WriteStartElement("c", Main);
        _xml.WriteAttributeString("r", reference);
        if (style != 0)
        {
            _xml.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
        }

        if (type is not null)
        {
            _xml.WriteAttributeString("t", type);
        }

        _xml.WriteElementString("v", Main, text);
        _xml.WriteEndElement();
    }

    private void Text(string reference, string text, int style)
    {
        _xml!.WriteStartElement("c", Main);
        _xml.WriteAttributeString("r", reference);
        if (style != 0)
        {
            _xml.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
        }

        _xml.WriteAttributeString("t", "inlineStr");
        _xml.WriteStartElement("is", Main);
        _xml.WriteStartElement("t", Main);
        _xml.WriteAttributeString("xml", "space", null, "preserve");
        _xml.WriteString(Clean(text));
        _xml.WriteEndElement();
        _xml.WriteEndElement();
        _xml.WriteEndElement();
    }

    /// <summary>The text Excel can hold: cut at 32,767 characters, each character XML cannot carry replaced by U+FFFD.</summary>
    /// <param name="text">The value as text.</param>
    public static string Clean(string text)
    {
        var cut = text.Length > MaxText ? Truncated(text) : text;
        var builder = new StringBuilder(cut.Length);
        for (var index = 0; index < cut.Length; index++)
        {
            if (char.IsHighSurrogate(cut[index]) && index + 1 < cut.Length && char.IsLowSurrogate(cut[index + 1]))
            {
                builder.Append(cut[index]).Append(cut[index + 1]);
                index++;
            }
            else
            {
                builder.Append(XmlConvert.IsXmlChar(cut[index]) ? cut[index] : '\uFFFD');
            }
        }

        return builder.ToString();
    }

    // Cut at the limit, and one character earlier when that would split a surrogate pair.
    private static string Truncated(string text)
        => char.IsHighSurrogate(text[MaxText - 1]) ? text[..(MaxText - 1)] : text[..MaxText];
}

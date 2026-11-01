using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>One worksheet of a workbook, read forward a row at a time straight from the zip.</summary>
internal sealed class XlsxSheetCursor : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly XmlReader _sheet;
    private readonly List<string> _shared;
    private readonly List<IXlsxValueKind> _styles;
    private readonly bool _date1904;

    private XlsxSheetCursor(ZipArchive archive, XmlReader sheet, List<string> shared, List<IXlsxValueKind> styles, bool date1904)
    {
        _archive = archive;
        _sheet = sheet;
        _shared = shared;
        _styles = styles;
        _date1904 = date1904;
    }

    /// <summary>Opens the worksheet. The workbook must be readable as a zip, so a stream that cannot seek is read into memory.</summary>
    /// <param name="content">The workbook's bytes; left open.</param>
    /// <param name="sheetName">The worksheet to read, or null for a workbook with exactly one.</param>
    /// <exception cref="InvalidDataException">The content is not a workbook, or the worksheet cannot be told.</exception>
    internal static XlsxSheetCursor Open(Stream content, string? sheetName)
    {
        var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
        try
        {
            var workbook = XlsxWorkbook.Load(archive, sheetName);
            return new XlsxSheetCursor(
                archive, XlsxReaderParts.Open(archive, workbook.SheetPath), XlsxSharedStrings.Load(archive), XlsxStyles.Load(archive), workbook.Date1904);
        }
        catch (Exception)
        {
            archive.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Reads the next row that holds any value into the array, by column; a column the row has no cell for is null. A row
    /// of no cells, or only empty ones, is skipped: Excel keeps formatted blank rows at the end of a sheet.
    /// </summary>
    /// <param name="values">The array to fill; cells beyond its length are read and left out.</param>
    /// <returns>False when the worksheet has no further row.</returns>
    internal bool ReadRow(object?[] values)
    {
        while (_sheet.Read())
        {
            if (XlsxReaderParts.IsElement(_sheet, "row") && FillRow(values))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sheet.Dispose();
        _archive.Dispose();
    }

    private bool FillRow(object?[] values)
    {
        Array.Clear(values);
        if (_sheet.IsEmptyElement)
        {
            return false;
        }

        var any = false;
        var next = 0;
        using var row = _sheet.ReadSubtree();
        while (!row.EOF)
        {
            if (XlsxReaderParts.IsElement(row, "c"))
            {
                any |= ReadCell(row, values, ref next);
            }

            row.Read();
        }

        return any;
    }

    private bool ReadCell(XmlReader row, object?[] values, ref int next)
    {
        var column = XlsxReaderParts.Column(row.GetAttribute("r"), next);
        next = column + 1;
        var type = row.GetAttribute("t");
        var kind = KindOf(row.GetAttribute("s"));
        var text = row.IsEmptyElement ? null : ReadText(row);
        var value = XlsxCellValues.Convert(type, text, kind, _date1904, _shared);
        if (column < values.Length)
        {
            values[column] = value;
        }

        return value is not null;
    }

    // A cell without an s attribute has style 0, the schema's own default.
    private IXlsxValueKind KindOf(string? style)
    {
        if (_styles.Count == 0)
        {
            return XlsxStyles.KindNumber;
        }

        if (style is null)
        {
            return _styles[0];
        }

        return int.TryParse(style, NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < _styles.Count
            ? _styles[index]
            : throw new InvalidDataException($"A cell has style '{style}', which the workbook does not define.");
    }

    // The cell's own text: its v element, or the runs of its inline string. A formula's text (f) is not a value.
    private static string? ReadText(XmlReader cell)
    {
        string? text = null;
        using var sub = cell.ReadSubtree();
        while (!sub.EOF)
        {
            if (XlsxReaderParts.IsElement(sub, "v"))
            {
                text = sub.ReadElementContentAsString();
            }
            else if (XlsxReaderParts.IsElement(sub, "is"))
            {
                text = XlsxReaderParts.ReadText(sub);
                sub.Read();
            }
            else
            {
                sub.Read();
            }
        }

        return text;
    }
}

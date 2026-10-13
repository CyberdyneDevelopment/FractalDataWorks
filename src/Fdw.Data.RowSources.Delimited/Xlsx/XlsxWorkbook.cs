using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>What the workbook part says about the worksheet to read: where it is, and which calendar its dates use.</summary>
internal sealed class XlsxWorkbook
{
    private XlsxWorkbook(string sheetPath, bool date1904)
    {
        SheetPath = sheetPath;
        Date1904 = date1904;
    }

    /// <summary>Gets the worksheet's path in the zip.</summary>
    internal string SheetPath { get; }

    /// <summary>Gets a value indicating whether dates count from 1904 (a workbook made on an old Mac) and not from 1900.</summary>
    internal bool Date1904 { get; }

    /// <summary>Resolves the worksheet to read.</summary>
    /// <param name="archive">The workbook's zip.</param>
    /// <param name="sheetName">The worksheet's name, or null for a workbook with exactly one.</param>
    /// <exception cref="InvalidDataException">The workbook is malformed, or the worksheet cannot be told.</exception>
    internal static XlsxWorkbook Load(ZipArchive archive, string? sheetName)
    {
        var date1904 = false;
        var sheets = new List<KeyValuePair<string, string>>();
        using (var xml = XlsxReaderParts.Open(archive, "xl/workbook.xml"))
        {
            while (xml.Read())
            {
                if (XlsxReaderParts.IsElement(xml, "workbookPr"))
                {
                    date1904 = IsTrue(xml.GetAttribute("date1904"));
                }
                else if (XlsxReaderParts.IsElement(xml, "sheet"))
                {
                    sheets.Add(new KeyValuePair<string, string>(Required(xml, "name"), Required(xml, "id", XlsxReaderParts.Relationships)));
                }
            }
        }

        return new XlsxWorkbook(PathOf(archive, Choose(sheets, sheetName).Value), date1904);
    }

    private static string Required(XmlReader xml, string name, string? ns = null)
        => (ns is null ? xml.GetAttribute(name) : xml.GetAttribute(name, ns))
            ?? throw new InvalidDataException($"A worksheet entry of the workbook has no '{name}'.");

    private static bool IsTrue(string? value)
        => string.Equals(value, "1", StringComparison.Ordinal) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static KeyValuePair<string, string> Choose(List<KeyValuePair<string, string>> sheets, string? sheetName)
    {
        if (sheets.Count == 0)
        {
            throw new InvalidDataException("The workbook declares no worksheet.");
        }

        var names = string.Join(", ", sheets.Select(s => $"'{s.Key}'"));
        if (sheetName is null)
        {
            return sheets.Count == 1
                ? sheets[0]
                : throw new InvalidDataException(
                    $"The workbook has {sheets.Count.ToString(CultureInfo.InvariantCulture)} worksheets ({names}); name the one to read in SheetName.");
        }

        foreach (var sheet in sheets)
        {
            if (string.Equals(sheet.Key, sheetName, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }

        throw new InvalidDataException($"The workbook has no worksheet named '{sheetName}'. It has: {names}.");
    }

    private static string PathOf(ZipArchive archive, string relationshipId)
    {
        using var xml = XlsxReaderParts.Open(archive, "xl/_rels/workbook.xml.rels");
        while (xml.Read())
        {
            if (XlsxReaderParts.IsElement(xml, "Relationship")
                && string.Equals(xml.GetAttribute("Id"), relationshipId, StringComparison.Ordinal))
            {
                var target = Required(xml, "Target");
                return target.StartsWith('/') ? target[1..] : "xl/" + target;
            }
        }

        throw new InvalidDataException($"The workbook's relationships do not name the worksheet part '{relationshipId}'.");
    }
}

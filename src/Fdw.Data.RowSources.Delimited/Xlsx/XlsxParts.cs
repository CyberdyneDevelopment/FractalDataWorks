using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using Fdw.Data.RowSources.Delimited.Abstractions;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The fixed parts of an .xlsx workbook and the names its worksheets and columns take.</summary>
internal static class XlsxParts
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string Header = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

    /// <summary>The letters of a column: 0 is A, 25 is Z, 26 is AA.</summary>
    /// <param name="index">The zero-based column number.</param>
    internal static string ColumnName(int index)
    {
        var name = new StringBuilder();
        for (var rest = index + 1; rest > 0; rest = (rest - 1) / 26)
        {
            name.Insert(0, (char)('A' + ((rest - 1) % 26)));
        }

        return name.ToString();
    }

    /// <summary>The name of the worksheet at a position: the base name with the characters Excel forbids replaced, cut to 31, numbered after the first.</summary>
    /// <param name="baseName">The first worksheet's name.</param>
    /// <param name="position">The one-based position of the worksheet.</param>
    internal static string SheetName(string baseName, int position)
    {
        var suffix = position == 1 ? string.Empty : $" ({position.ToString(CultureInfo.InvariantCulture)})";
        var cleaned = new string(baseName.Select(c => XlsxRowWriterOptions.IsForbiddenInSheetName(c) ? '_' : c).ToArray());
        var room = Math.Max(1, XlsxRowWriterOptions.MaxSheetNameLength - suffix.Length);
        return (cleaned.Length > room ? cleaned[..room] : cleaned) + suffix;
    }

    /// <summary>Writes the parts that tie the worksheets into a workbook.</summary>
    /// <param name="archive">The workbook's zip, after its worksheets.</param>
    /// <param name="sheetNames">The worksheets' names, in order; worksheet n is <c>xl/worksheets/sheet{n}.xml</c>.</param>
    internal static void Write(ZipArchive archive, IReadOnlyList<string> sheetNames)
    {
        Entry(archive, "[Content_Types].xml", ContentTypes(sheetNames.Count));
        Entry(archive, "_rels/.rels", RootRelationships());
        Entry(archive, "xl/workbook.xml", Workbook(sheetNames));
        Entry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships(sheetNames.Count));
        Entry(archive, "xl/styles.xml", Styles());
    }

    private static void Entry(ZipArchive archive, string path, string xml)
    {
        using var stream = archive.CreateEntry(path, CompressionLevel.Fastest).Open();
        var bytes = new UTF8Encoding(false).GetBytes(xml);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string ContentTypes(int sheets)
    {
        var overrides = new StringBuilder();
        for (var n = 1; n <= sheets; n++)
        {
            overrides.Append(CultureInfo.InvariantCulture, $"<Override PartName=\"/xl/worksheets/sheet{n}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        }

        return Header + "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">"
            + "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>"
            + "<Default Extension=\"xml\" ContentType=\"application/xml\"/>"
            + "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>"
            + "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>"
            + overrides + "</Types>";
    }

    private static string RootRelationships()
        => Header + $"<Relationships xmlns=\"{PackageRelationships}\">"
            + $"<Relationship Id=\"rId1\" Type=\"{Relationships}/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";

    private static string Workbook(IReadOnlyList<string> sheetNames)
    {
        var sheets = new StringBuilder();
        for (var n = 1; n <= sheetNames.Count; n++)
        {
            sheets.Append(CultureInfo.InvariantCulture, $"<sheet name=\"{SecurityElement.Escape(sheetNames[n - 1])}\" sheetId=\"{n}\" r:id=\"rId{n}\"/>");
        }

        return Header + $"<workbook xmlns=\"{Main}\" xmlns:r=\"{Relationships}\"><sheets>{sheets}</sheets></workbook>";
    }

    private static string WorkbookRelationships(int sheets)
    {
        var rels = new StringBuilder();
        for (var n = 1; n <= sheets; n++)
        {
            rels.Append(CultureInfo.InvariantCulture, $"<Relationship Id=\"rId{n}\" Type=\"{Relationships}/worksheet\" Target=\"worksheets/sheet{n}.xml\"/>");
        }

        rels.Append(CultureInfo.InvariantCulture, $"<Relationship Id=\"rId{sheets + 1}\" Type=\"{Relationships}/styles\" Target=\"styles.xml\"/>");
        return Header + $"<Relationships xmlns=\"{PackageRelationships}\">{rels}</Relationships>";
    }

    // Cell styles: 0 plain, 1 bold header, 2 date and time, 3 date, 4 time.
    private static string Styles()
        => Header + $"<styleSheet xmlns=\"{Main}\">"
            + "<numFmts count=\"3\"><numFmt numFmtId=\"164\" formatCode=\"yyyy\\-mm\\-dd\\ hh:mm:ss\"/><numFmt numFmtId=\"165\" formatCode=\"yyyy\\-mm\\-dd\"/><numFmt numFmtId=\"166\" formatCode=\"hh:mm:ss\"/></numFmts>"
            + "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>"
            + "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>"
            + "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>"
            + "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"
            + "<cellXfs count=\"5\">"
            + "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>"
            + "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>"
            + "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>"
            + "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>"
            + "<xf numFmtId=\"166\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>"
            + "</cellXfs>"
            + "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";
}

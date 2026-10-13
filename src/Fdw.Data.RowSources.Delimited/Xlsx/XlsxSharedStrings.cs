using System.Collections.Generic;
using System.IO.Compression;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The workbook's shared string table: the text a cell of type s points at by position.</summary>
internal static class XlsxSharedStrings
{
    private const string Part = "xl/sharedStrings.xml";

    /// <summary>Reads the table; a workbook with no string part (one that writes its text inline) has none.</summary>
    /// <param name="archive">The workbook's zip.</param>
    internal static List<string> Load(ZipArchive archive)
    {
        var strings = new List<string>();
        if (archive.GetEntry(Part) is null)
        {
            return strings;
        }

        using var xml = XlsxReaderParts.Open(archive, Part);
        while (xml.Read())
        {
            if (XlsxReaderParts.IsElement(xml, "si"))
            {
                strings.Add(XlsxReaderParts.ReadText(xml));
            }
        }

        return strings;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>
/// What each cell style of the workbook holds. A cell's s attribute is a position in the style table, and the style's
/// number format is the only thing that says a number is a date.
/// </summary>
internal static class XlsxStyles
{
    private const string Part = "xl/styles.xml";

    /// <summary>
    /// Reads the kind of every cell style, by style position. A workbook with no styles part has no number formats, so
    /// every number in it is a number: the table is empty.
    /// </summary>
    /// <param name="archive">The workbook's zip.</param>
    internal static List<XlsxValueKind> Load(ZipArchive archive)
    {
        var kinds = new List<XlsxValueKind>();
        if (archive.GetEntry(Part) is null)
        {
            return kinds;
        }

        var custom = new Dictionary<int, string>();
        var inStyles = false;
        using var xml = XlsxReaderParts.Open(archive, Part);
        while (xml.Read())
        {
            if (XlsxReaderParts.IsElement(xml, "numFmt"))
            {
                custom[IdAttribute(xml)] = xml.GetAttribute("formatCode") ?? throw new InvalidDataException("A number format of the workbook has no formatCode.");
            }
            else if (XlsxReaderParts.IsElement(xml, "cellXfs"))
            {
                inStyles = !xml.IsEmptyElement;
            }
            else if (XlsxReaderParts.IsEnd(xml, "cellXfs"))
            {
                inStyles = false;
            }
            else if (inStyles && XlsxReaderParts.IsElement(xml, "xf"))
            {
                kinds.Add(KindOf(IdAttribute(xml), custom));
            }
        }

        return kinds;
    }

    // The schema's own default for a style that names no number format is 0, General.
    private static int IdAttribute(XmlReader xml)
    {
        var text = xml.GetAttribute("numFmtId");
        if (text is null)
        {
            return 0;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"'{text}' is not a number format id.");
    }

    private static XlsxValueKind KindOf(int formatId, Dictionary<int, string> custom)
    {
        if (custom.TryGetValue(formatId, out var code))
        {
            return KindOfCode(code);
        }

        return formatId switch
        {
            14 or 15 or 16 or 17 or 22 or (>= 27 and <= 36) or (>= 50 and <= 58) => XlsxValueKind.DateTime,
            18 or 19 or 20 or 21 or 45 or 46 or 47 => XlsxValueKind.Time,
            _ => XlsxValueKind.Number,
        };
    }

    // A format with a year or a day is a date; one with only hours or seconds is a time. A lone m is a month (it is a
    // minute only beside an h or an s, which already make it a time). Only the first section is read: it is the one a
    // positive number takes.
    private static XlsxValueKind KindOfCode(string code)
    {
        var date = false;
        var time = false;
        foreach (var c in Visible(code).ToLowerInvariant())
        {
            date |= c is 'y' or 'd';
            time |= c is 'h' or 's';
        }

        if (date)
        {
            return XlsxValueKind.DateTime;
        }

        return time ? XlsxValueKind.Time : XlsxValueKind.Number;
    }

    private static string Visible(string code)
    {
        var end = code.IndexOf(';');
        var section = end < 0 ? code : code[..end];
        var visible = new StringBuilder(section.Length);
        var index = 0;
        while (index < section.Length)
        {
            index += Consume(section, index, visible);
        }

        return visible.ToString();
    }

    // Quoted text, escaped characters and padding are not format tokens; [h] and [mm] are, [Red] and [$-409] are not.
    private static int Consume(string text, int index, StringBuilder visible)
    {
        var c = text[index];
        if (c == '"')
        {
            return Through(text, index, '"');
        }

        if (c == '[')
        {
            return Bracket(text, index, visible);
        }

        if (c is '\\' or '_' or '*')
        {
            return 2;
        }

        visible.Append(c);
        return 1;
    }

    private static int Through(string text, int index, char close)
    {
        var end = text.IndexOf(close, index + 1);
        return end < 0 ? text.Length - index : end - index + 1;
    }

    private static int Bracket(string text, int index, StringBuilder visible)
    {
        var length = Through(text, index, ']');
        var inner = text.Substring(index + 1, Math.Max(0, length - 2));
        if (inner.Length > 0 && inner.All(letter => char.ToLowerInvariant(letter) is 'h' or 'm' or 's'))
        {
            visible.Append(inner[0]);
        }

        return length;
    }
}

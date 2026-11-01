using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The parts of an .xlsx workbook the reader opens, and the XML reading steps they share.</summary>
internal static class XlsxReaderParts
{
    /// <summary>The namespace of the relationship attributes (r:id).</summary>
    internal const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    /// <summary>Opens a part of the workbook as an XML reader that refuses a DTD and resolves nothing external.</summary>
    /// <param name="archive">The workbook's zip.</param>
    /// <param name="path">The part's path in the zip.</param>
    internal static XmlReader Open(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new InvalidDataException($"The workbook has no part '{path}'.");
        return XmlReader.Create(entry.Open(), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, CloseInput = true });
    }

    /// <summary>Gets whether the reader is on the start of an element with the given local name.</summary>
    /// <param name="xml">The reader.</param>
    /// <param name="name">The local name.</param>
    internal static bool IsElement(XmlReader xml, string name)
        => xml.NodeType == XmlNodeType.Element && string.Equals(xml.LocalName, name, StringComparison.Ordinal);

    /// <summary>Gets whether the reader is on the end of an element with the given local name.</summary>
    /// <param name="xml">The reader.</param>
    /// <param name="name">The local name.</param>
    internal static bool IsEnd(XmlReader xml, string name)
        => xml.NodeType == XmlNodeType.EndElement && string.Equals(xml.LocalName, name, StringComparison.Ordinal);

    /// <summary>
    /// Reads the text of a rich-text element (a shared string's si, an inline string's is): every t run, in order, with
    /// the phonetic runs (rPh) left out. The reader ends on the element's end tag.
    /// </summary>
    /// <param name="element">The reader, on the start of the element.</param>
    internal static string ReadText(XmlReader element)
    {
        if (element.IsEmptyElement)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        using var sub = element.ReadSubtree();
        while (!sub.EOF)
        {
            if (IsElement(sub, "rPh"))
            {
                sub.Skip();
            }
            else if (IsElement(sub, "t"))
            {
                text.Append(sub.ReadElementContentAsString());
            }
            else
            {
                sub.Read();
            }
        }

        return text.ToString();
    }

    /// <summary>Gets the zero-based column of a cell reference such as B7 or AA12.</summary>
    /// <param name="reference">The cell's r attribute, or null when the cell has none.</param>
    /// <param name="next">The column after the previous cell, which a cell without a reference takes.</param>
    internal static int Column(string? reference, int next)
    {
        if (reference is null)
        {
            return next;
        }

        var column = 0;
        var letters = 0;
        while (letters < reference.Length && reference[letters] is >= 'A' and <= 'Z')
        {
            column = (column * 26) + (reference[letters] - 'A' + 1);
            letters++;
        }

        if (letters is 0 or > 3 || column > 16384)
        {
            throw new InvalidDataException($"'{reference}' is not a cell reference of a worksheet.");
        }

        return column - 1;
    }
}

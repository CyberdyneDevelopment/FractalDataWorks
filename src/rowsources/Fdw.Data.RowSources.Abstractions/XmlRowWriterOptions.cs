using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Xml.Abstractions;

/// <summary>
/// Options for XML row writing. The write-side mirror of <see cref="XmlRowSourceOptions"/>;
/// every knob maps 1:1 to a <see cref="System.Xml.XmlWriterSettings"/> setting plus the row/root
/// element names that <see cref="XmlStreamRowSource"/> reads.
/// </summary>
public sealed class XmlRowWriterOptions : RowWriterOptions
{
    /// <summary>
    /// Gets or sets the root element name that wraps all rows.
    /// </summary>
    public required string RootElementName { get; set; }

    /// <summary>
    /// Gets or sets the element name for each row.
    /// Matches <see cref="XmlRowSourceOptions.RowElementName"/> on the read side.
    /// </summary>
    public required string RowElementName { get; set; }

    /// <summary>
    /// Gets or sets whether the output XML is indented (<c>XmlWriterSettings.Indent</c>).
    /// </summary>
    public bool Indent { get; set; }

    /// <summary>
    /// Gets or sets whether to omit the XML declaration
    /// (<c>XmlWriterSettings.OmitXmlDeclaration</c>).
    /// </summary>
    public required bool OmitXmlDeclaration { get; set; }
}

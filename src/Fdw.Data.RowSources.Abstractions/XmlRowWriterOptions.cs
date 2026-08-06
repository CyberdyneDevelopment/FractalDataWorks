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
    /// Gets or sets the root element name that wraps all rows. Default is "Rows".
    /// </summary>
    public string RootElementName { get; set; } = "Rows";

    /// <summary>
    /// Gets or sets the element name for each row. Default is "Row".
    /// Matches <see cref="XmlRowSourceOptions.RowElementName"/> on the read side.
    /// </summary>
    public string RowElementName { get; set; } = "Row";

    /// <summary>
    /// Gets or sets whether the output XML is indented (<c>XmlWriterSettings.Indent</c>).
    /// Default is false.
    /// </summary>
    public bool Indent { get; set; }

    /// <summary>
    /// Gets or sets whether to omit the XML declaration
    /// (<c>XmlWriterSettings.OmitXmlDeclaration</c>). Default is true.
    /// </summary>
    public bool OmitXmlDeclaration { get; set; } = true;
}

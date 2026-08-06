using System.Xml;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Xml.Abstractions;

/// <summary>
/// Options for XML row source processing with security settings.
/// </summary>
public sealed class XmlRowSourceOptions : RowSourceOptions
{
    /// <summary>
    /// Gets or sets the maximum characters from entities (prevents billion laughs attack).
    /// Default is 10 million characters.
    /// </summary>
    public long MaxCharactersFromEntities { get; set; } = 10_000_000;

    /// <summary>
    /// Gets or sets the maximum nesting depth.
    /// Default is 32.
    /// </summary>
    public int MaxDepth { get; set; } = 32;

    /// <summary>
    /// Gets or sets the DTD processing mode.
    /// Default is Prohibit (prevents XXE attacks).
    /// </summary>
    public DtdProcessing DtdProcessing { get; set; } = DtdProcessing.Prohibit;

    /// <summary>
    /// Gets or sets the XPath to the row elements.
    /// Example: "/Envelope/Body/Response/Items/Item"
    /// </summary>
    public string? RowElementPath { get; set; }

    /// <summary>
    /// Gets or sets the simple row element name (if not using XPath).
    /// Example: "Item"
    /// </summary>
    public string? RowElementName { get; set; }

    /// <summary>
    /// Gets or sets the namespace URI for row elements (optional).
    /// </summary>
    public string? NamespaceUri { get; set; }

    /// <summary>
    /// Gets or sets additional namespace prefixes for XPath evaluation.
    /// Key is prefix, value is namespace URI.
    /// </summary>
    public System.Collections.Generic.IDictionary<string, string>? NamespacePrefixes { get; set; }

    /// <summary>
    /// Gets or sets whether to treat element content as field values.
    /// If false, attributes are used. Default is true.
    /// </summary>
    public bool UseElementContent { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include attributes as fields in addition to elements.
    /// Default is true.
    /// </summary>
    public bool IncludeAttributes { get; set; } = true;

    /// <summary>
    /// Creates XmlReaderSettings with security protections applied.
    /// </summary>
    /// <returns>Secure XmlReaderSettings.</returns>
    public XmlReaderSettings CreateSecureSettings()
    {
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing,
            MaxCharactersFromEntities = MaxCharactersFromEntities,
            XmlResolver = null, // Prevent external entity resolution
            IgnoreWhitespace = true,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true
        };
    }
}

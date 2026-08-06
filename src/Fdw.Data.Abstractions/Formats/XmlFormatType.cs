using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Formats;

/// <summary>
/// Represents XML data format.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FormatTypes), "Xml", RestrictToCurrentCompilation = true)]
public sealed class XmlFormatType : FormatTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlFormatType"/> class.
    /// </summary>
    public XmlFormatType()
        : base(
            id: 3,
            name: "Xml",
            displayName: "XML",
            description: "Extensible Markup Language data format",
            mimeType: "application/xml",
            isBinary: false,
            supportsStreaming: true,
            canonicalFileExtension: ".xml")
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>A text node: its text.</summary>
[TypeOption(typeof(XPathNodeKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextXPathNodeKind : XPathNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextXPathNodeKind"/> class.</summary>
    public TextXPathNodeKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object node) => node is XText;

    /// <inheritdoc />
    public override string Text(object node) => ((XText)node).Value;
}

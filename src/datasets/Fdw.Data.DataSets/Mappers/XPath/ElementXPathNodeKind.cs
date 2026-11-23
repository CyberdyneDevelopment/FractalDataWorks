using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>An element: its text content.</summary>
[TypeOption(typeof(XPathNodeKinds), "Element")]
[ExcludeFromCodeCoverage]
public sealed class ElementXPathNodeKind : XPathNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ElementXPathNodeKind"/> class.</summary>
    public ElementXPathNodeKind()
        : base("Element")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object node) => node is XElement;

    /// <inheritdoc />
    public override string Text(object node) => ((XElement)node).Value;
}

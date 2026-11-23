using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>An attribute: its value.</summary>
[TypeOption(typeof(XPathNodeKinds), "Attribute")]
[ExcludeFromCodeCoverage]
public sealed class AttributeXPathNodeKind : XPathNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="AttributeXPathNodeKind"/> class.</summary>
    public AttributeXPathNodeKind()
        : base("Attribute")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object node) => node is XAttribute;

    /// <inheritdoc />
    public override string Text(object node) => ((XAttribute)node).Value;
}

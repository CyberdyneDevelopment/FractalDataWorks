using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>Any other selected node, written as its own text form.</summary>
[TypeOption(typeof(XPathNodeKinds), "Other")]
[ExcludeFromCodeCoverage]
public sealed class OtherXPathNodeKind : XPathNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="OtherXPathNodeKind"/> class.</summary>
    public OtherXPathNodeKind()
        : base("Other")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object node) => node is not (XElement or XAttribute or XText);

    /// <inheritdoc />
    public override string Text(object node) => node.ToString()!;
}

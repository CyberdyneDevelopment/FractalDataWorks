using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>An XML element, used as it is.</summary>
[TypeOption(typeof(XPathPayloadKinds), "Element")]
[ExcludeFromCodeCoverage]
public sealed class ElementXPathPayloadKind : XPathPayloadKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ElementXPathPayloadKind"/> class.</summary>
    public ElementXPathPayloadKind()
        : base("Element")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object payload) => payload is XElement;

    /// <inheritdoc />
    public override XElement Read(object payload) => (XElement)payload;
}

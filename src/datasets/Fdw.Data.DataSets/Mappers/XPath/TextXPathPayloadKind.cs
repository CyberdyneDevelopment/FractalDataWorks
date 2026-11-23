using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>Text, parsed as XML.</summary>
[TypeOption(typeof(XPathPayloadKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextXPathPayloadKind : XPathPayloadKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextXPathPayloadKind"/> class.</summary>
    public TextXPathPayloadKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object payload) => payload is string;

    /// <inheritdoc />
    public override XElement Read(object payload) => XElement.Parse((string)payload);
}

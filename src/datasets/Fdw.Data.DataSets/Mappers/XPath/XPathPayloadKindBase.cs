using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections;

namespace Fdw.Data.DataSets;

/// <summary>The base of every option of <see cref="XPathPayloadKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class XPathPayloadKindBase : TypeOptionBase<XPathPayloadKindBase>, IXPathPayloadKind
{
    /// <summary>Initializes a new instance of the <see cref="XPathPayloadKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected XPathPayloadKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object payload);

    /// <inheritdoc />
    public abstract XElement Read(object payload);
}

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Fdw.Collections;

namespace Fdw.Data.DataSets;

/// <summary>The base of every option of <see cref="XPathNodeKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class XPathNodeKindBase : TypeOptionBase<XPathNodeKindBase>, IXPathNodeKind
{
    /// <summary>Initializes a new instance of the <see cref="XPathNodeKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected XPathNodeKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object node);

    /// <inheritdoc />
    public abstract string Text(object node);
}

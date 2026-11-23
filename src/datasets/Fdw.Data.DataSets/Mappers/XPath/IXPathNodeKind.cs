using System.Xml.Linq;
using Fdw.Collections;

namespace Fdw.Data.DataSets;

/// <summary>The kinds of node an XPath expression can select, each with the text it stands for.</summary>
public interface IXPathNodeKind : ITypeOption<int, XPathNodeKindBase>
{
    /// <summary>Tells whether a selected node is of this kind.</summary>
    /// <param name="node">The node, which is not null.</param>
    /// <returns>True when this kind holds the node.</returns>
    bool Holds(object node);

    /// <summary>Gets the text a selected node of this kind stands for.</summary>
    /// <param name="node">A node this kind <see cref="Holds"/>.</param>
    /// <returns>The text.</returns>
    string Text(object node);
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>The kinds of node an XPath expression can select, each with the text it stands for.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(XPathNodeKindBase), typeof(IXPathNodeKind), typeof(XPathNodeKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class XPathNodeKinds : TypeCollectionBase<XPathNodeKindBase, IXPathNodeKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IXPathNodeKind OfValue(object value)
    {
        foreach (var option in All())
        {
            if (option.Holds(value))
            {
                return option;
            }
        }

        return NotFound;
    }
}

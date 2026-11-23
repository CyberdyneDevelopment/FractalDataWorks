using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets;

/// <summary>The kinds of payload the XPath mapper reads: an XML element, or text to parse.</summary>
/// <remarks>A value no option holds answers the NotFound sentinel; the caller refuses it.</remarks>
[TypeCollection(typeof(XPathPayloadKindBase), typeof(IXPathPayloadKind), typeof(XPathPayloadKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class XPathPayloadKinds : TypeCollectionBase<XPathPayloadKindBase, IXPathPayloadKind>
{
    /// <summary>Finds the option that holds a value.</summary>
    /// <param name="value">The value, which is not null.</param>
    /// <returns>The option, or <c>NotFound</c> when none holds the value.</returns>
    public static IXPathPayloadKind OfValue(object value)
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

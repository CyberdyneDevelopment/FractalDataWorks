using System.Xml.Linq;
using Fdw.Collections;

namespace Fdw.Data.DataSets;

/// <summary>The kinds of payload the XPath mapper reads: an XML element, or text to parse.</summary>
public interface IXPathPayloadKind : ITypeOption<int, XPathPayloadKindBase>
{
    /// <summary>Tells whether a payload is of this kind.</summary>
    /// <param name="payload">The payload, which is not null.</param>
    /// <returns>True when this kind holds the payload.</returns>
    bool Holds(object payload);

    /// <summary>Reads a payload of this kind as an element.</summary>
    /// <param name="payload">A payload this kind <see cref="Holds"/>.</param>
    /// <returns>The element.</returns>
    XElement Read(object payload);
}

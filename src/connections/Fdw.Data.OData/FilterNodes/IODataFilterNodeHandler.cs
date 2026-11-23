using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data.OData.FilterNodes;

/// <summary>Writes the OData <c>$filter</c> text of one kind of filter node; the collection is keyed by the node kind's name (<c>FilterNodeKinds</c>).</summary>
public interface IODataFilterNodeHandler : ITypeOption<int, IODataFilterNodeHandler>
{
    /// <summary>Writes the node.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <returns>The OData text; empty when the node writes nothing.</returns>
    string Build(IFilterNode node);
}

using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.RowQuery.FilterNodes;

/// <summary>Finds the record query handler of a filter node.</summary>
internal static class RowQueryNodes
{
    /// <summary>The handler for the node's kind, or <c>RowQueryNodeHandlers.NotFound</c> for a node that is neither a condition nor a group.</summary>
    /// <param name="node">The node.</param>
    /// <returns>The handler.</returns>
    internal static IRowQueryNodeHandler HandlerFor(IFilterNode node)
    {
        var kind = FilterNodeKinds.Of(node);
        return ReferenceEquals(kind, FilterNodeKinds.NotFound) ? RowQueryNodeHandlers.NotFound : RowQueryNodeHandlers.ByName(kind.Name);
    }
}

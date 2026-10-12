using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// What a translator can express in the command it produces.
/// </summary>
/// <remarks>
/// A filter, an ordering and a page are logical requests. Where each is APPLIED depends on the
/// target: SQL puts them in WHERE, ORDER BY and OFFSET/FETCH; a delimited file expresses none of
/// them and the rows have to be pulled and pruned here; OData expresses $filter but not
/// necessarily every operator in it.
///
/// The translator answers because it is the only thing that knows the native command's vocabulary.
/// Whatever it declines is the connector's job, applied over the record stream before the rows
/// leave. A translator that implements nothing here is treated as expressing nothing, which is the
/// safe reading — the alternative is claiming a filter was applied when it was not.
/// </remarks>
public interface IQueryCapability
{
    /// <summary>Whether the whole filter can be expressed natively.</summary>
    /// <remarks>
    /// The whole tree, not part of it. Partial pushdown would need the remainder expressed as a
    /// second filter, and no caller can currently split one.
    /// </remarks>
    bool CanExpressFilter(IFilterExpression filter);

    /// <summary>Whether the ordering can be expressed natively.</summary>
    bool CanExpressOrdering(IOrderingExpression ordering);

    /// <summary>Whether the page can be expressed natively.</summary>
    /// <remarks>
    /// Paging is the one that is wrong most quietly: a source that ignores Skip returns the first
    /// page forever, and every page looks like it worked.
    /// </remarks>
    bool CanExpressPaging(IPagingExpression paging);
}

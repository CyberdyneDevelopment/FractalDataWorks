using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>The filter of a command narrowed to a scope (FDW-5069): the scope ANDed with the command's own filter.</summary>
internal static class ScopedFilter
{
    /// <summary>ANDs <paramref name="scope"/> with <paramref name="own"/>.</summary>
    /// <param name="scope">The scope the rows must satisfy.</param>
    /// <param name="own">The command's own filter, if any.</param>
    /// <returns>The scope alone when the command has no filter, else both ANDed.</returns>
    public static IFilterExpression And(IFilterExpression scope, IFilterExpression? own) =>
        own?.Root is { } ownRoot && scope.Root is { } scopeRoot
            ? new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = [scopeRoot, ownRoot] } }
            : own?.Root is null ? scope : own;
}

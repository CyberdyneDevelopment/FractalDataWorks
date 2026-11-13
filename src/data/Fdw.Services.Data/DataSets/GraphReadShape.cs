using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Web.RestEndpoints.Query;

namespace Fdw.Services.Data;

/// <summary>
/// Cuts the rows a graph produced to the read that asked for them (FDW-5291): the data set's own filter ANDed with the caller's, the caller's
/// ordering, then the caller's page.
/// </summary>
/// <remarks>
/// The graph runs whole and its rows are held, so the shaping is done here on the held rows, in that order: a page is a page of the filtered and
/// ordered set, never of the rows as they arrived. A read that states no ordering, no page and no filter gets every row.
/// </remarks>
internal static class GraphReadShape
{
    internal static List<Dictionary<string, object?>> Apply(
        IEnumerable<IDictionary<string, object?>> rows, IFilterExpression? filter, IDataCommand command)
    {
        var named = rows.Select(row => new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase));
        var kept = filter?.Root is { } root ? named.Where(row => InMemoryQuery.Matches(row, root)) : named;
        var ordered = (command as IQueryCommand)?.Ordering is { } ordering ? InMemoryQuery.Sort(kept, ordering) : kept;
        if ((command as IQueryCommand)?.Paging is not { } paging)
            return [.. ordered];

        var skipped = ordered.Skip(paging.Skip);
        return [.. paging.Take is { } take ? skipped.Take(take) : skipped];
    }
}

using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// A query split into what the store runs and what is applied to the rows after they are read (FDW-1045).
/// </summary>
/// <remarks>
/// A calculated field has no value in the store, so a filter or sort that names one cannot be pushed down. The part that
/// can be is still pushed (<see cref="Command"/>); the rest runs here (<see cref="InMemoryFilter"/>,
/// <see cref="InMemoryOrdering"/>), over at most <see cref="RowCap"/> rows, and then the page is cut. A query that names no
/// calculated field is the plain command with the page in the store, exactly as before.
/// </remarks>
public sealed class QueryPlan
{
    /// <summary>Gets the command the store runs: the pushable part of the filter, and either the ordering and the page or, when the rest runs in memory, every row up to one past the cap.</summary>
    public required QueryCommand<Dictionary<string, object?>> Command { get; init; }

    /// <summary>Gets the part of the filter that names a calculated field and runs after the read, or null when none does.</summary>
    public IFilterNode? InMemoryFilter { get; init; }

    /// <summary>Gets the ordering that names a calculated field and runs after the read, or null when none does.</summary>
    public IOrderingExpression? InMemoryOrdering { get; init; }

    /// <summary>Gets the rows to skip when the page is cut in memory.</summary>
    public int Skip { get; init; }

    /// <summary>Gets the page size when the page is cut in memory.</summary>
    public int Take { get; init; }

    /// <summary>Gets the most rows the store may be asked to hand over when the rest runs in memory.</summary>
    public int RowCap { get; init; }

    /// <summary>Gets whether any part of the query runs after the read.</summary>
    public bool RunsInMemory => InMemoryFilter is not null || InMemoryOrdering is not null;
}

using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Interface for query commands that support filtering, projection, ordering, paging, and aggregation.
/// Provides strongly-typed access to query properties without reflection.
/// </summary>
/// <remarks>
/// Implemented by QueryCommand to enable translators to access
/// query properties (Filter, Projection, etc.) without using reflection.
/// </remarks>
public interface IQueryCommand : IDataCommand
{
    /// <summary>
    /// Gets the filter expression (WHERE clause).
    /// </summary>
    IFilterExpression? Filter { get; }

    /// <summary>
    /// Gets the projection expression (SELECT clause).
    /// </summary>
    IProjectionExpression? Projection { get; }

    /// <summary>
    /// Gets the ordering expression (ORDER BY clause).
    /// </summary>
    IOrderingExpression? Ordering { get; }

    /// <summary>
    /// Gets the paging expression (SKIP/TAKE).
    /// </summary>
    IPagingExpression? Paging { get; }

    /// <summary>
    /// Gets the aggregation expression (GROUP BY).
    /// </summary>
    IAggregationExpression? Aggregation { get; }

    /// <summary>
    /// Gets the join expressions (JOIN clauses).
    /// </summary>
    IReadOnlyList<IJoinExpression> Joins { get; }
}

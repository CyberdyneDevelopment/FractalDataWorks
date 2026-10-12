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

    /// <summary>
    /// Gets whether a query that names no columns also returns the container's Metadata columns (who
    /// created or changed a row and when, its tenant). Off by default: a read returns the detail (FDW-988).
    /// </summary>
    bool IncludeMetadata { get; }

    /// <summary>
    /// Gets whether a query that names no columns also returns the container's Descriptive columns.
    /// Off by default (FDW-988).
    /// </summary>
    bool IncludeDescriptions { get; }

    /// <summary>
    /// Gets whether a query that names no columns also returns the container's DbKey columns (RowId and
    /// the <c>*RowId</c> columns). Only server-side reads set it -- a configuration read that joins or
    /// re-versions a row needs its physical key; no response ever shows one (FDW-988).
    /// </summary>
    bool IncludeKeys { get; }
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Commands.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Logging;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Query command for retrieving data (SELECT operation).
/// Returns an enumerable collection of typed results - no casting needed!
/// </summary>
/// <typeparam name="T">The type of entity to query.</typeparam>
/// <remarks>
/// <para>
/// This command represents a universal SELECT query that works across all data sources.
/// Translators convert it to:
/// <list type="bullet">
/// <item>SQL: SELECT statement with WHERE, ORDER BY, etc.</item>
/// <item>REST: GET request with OData $filter, $orderby, etc.</item>
/// <item>File: Read and filter records</item>
/// <item>GraphQL: Query with where clause</item>
/// </list>
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = DataQuery.From&lt;Customer&gt;("CRM", "sales", "Customers")
///     .Where(c => c.IsActive).Equal(true)
///     .Build();
/// var result = await gateway.Execute&lt;IEnumerable&lt;Customer&gt;&gt;(call, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataCommands), "Query", RestrictToCurrentCompilation = true)]
public sealed class QueryCommand<T> : DataCommandBase<IEnumerable<T>>, IQueryCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCommand{T}"/> class.
    /// </summary>
    public QueryCommand()
        : base("Query")
    {
        QueryCommandLog.CommandCreated(NullLogger<QueryCommand<T>>.Instance, typeof(T).Name);
    }

    /// <summary>
    /// Gets or sets the filter expression (WHERE clause).
    /// </summary>
    /// <value>The filter to apply, or null to select all records.</value>
    public IFilterExpression? Filter { get; init; }

    /// <summary>
    /// Gets or sets the projection expression (SELECT clause).
    /// </summary>
    /// <value>The fields to project, or null to select all fields.</value>
    public IProjectionExpression? Projection { get; init; }

    /// <summary>
    /// Gets or sets the ordering expression (ORDER BY clause).
    /// </summary>
    /// <value>The ordering to apply, or null for no specific ordering.</value>
    public IOrderingExpression? Ordering { get; init; }

    /// <summary>
    /// Gets or sets the paging expression (SKIP/TAKE).
    /// </summary>
    /// <value>The paging parameters, or null for no paging.</value>
    public IPagingExpression? Paging { get; init; }

    /// <summary>
    /// Gets or sets the aggregation expression (GROUP BY).
    /// </summary>
    /// <value>The aggregation parameters, or null for no aggregation.</value>
    public IAggregationExpression? Aggregation { get; init; }

    /// <summary>
    /// Gets or sets the join expressions (JOIN clauses).
    /// </summary>
    /// <value>The joins to apply, or empty for no joins.</value>
    public IReadOnlyList<IJoinExpression> Joins { get; init; } = [];
}

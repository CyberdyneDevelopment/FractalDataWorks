using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Optimizes federated query execution.
/// </summary>
public sealed class QueryOptimizer : IQueryOptimizer
{
    /// <summary>
    /// Determines the optimal execution order for data sources.
    /// </summary>
    /// <param name="sources">The data sources to optimize.</param>
    /// <returns>The sources in optimal execution order.</returns>
    /// <remarks>
    /// Current heuristic: Execute sources with filters first (smaller result sets).
    /// Future: Use cardinality estimation for more sophisticated ordering.
    /// </remarks>
    public IReadOnlyList<IDataSource> OptimizeSourceOrder(IReadOnlyList<IDataSource> sources)
    {
        if (sources == null || sources.Count <= 1)
        {
            return sources ?? [];
        }

        // Simple heuristic: sources with filters should execute first
        // This reduces the size of the join input
        return sources
            .OrderByDescending(s => s.Filter != null ? 1 : 0)
            .ToList();
    }

    /// <summary>
    /// Pushes filter predicates down to individual data sources where possible.
    /// </summary>
    /// <param name="sources">The data sources.</param>
    /// <param name="globalFilter">The global filter expression.</param>
    /// <returns>Sources with optimized filters.</returns>
    /// <remarks>
    /// Current implementation: No predicate pushdown yet.
    /// Future: Analyze globalFilter and push predicates that reference only one source
    /// down to that source's Filter property.
    /// </remarks>
    public IReadOnlyList<IDataSource> PushDownPredicates(
        IReadOnlyList<IDataSource> sources,
        IFilterExpression? globalFilter)
    {
        // Phase 11 enhancement: Implement predicate pushdown analysis
        // For now, return sources unchanged
        return sources;
    }

    /// <summary>
    /// Estimates the cardinality (number of rows) for a data source.
    /// </summary>
    /// <param name="source">The data source to estimate.</param>
    /// <returns>Estimated row count, or null if unknown.</returns>
    /// <remarks>
    /// Current implementation: No estimation (returns null).
    /// Future: Query metadata, statistics, or sampling to estimate cardinality.
    /// </remarks>
    public long? EstimateCardinality(IDataSource source)
    {
        // Phase 11 enhancement: Implement cardinality estimation
        // Could query metadata catalogs, use statistics, or perform sampling
        return null;
    }

    /// <summary>
    /// Selects the best join algorithm for the given data sources.
    /// </summary>
    /// <param name="leftCardinality">Estimated rows in left source.</param>
    /// <param name="rightCardinality">Estimated rows in right source.</param>
    /// <returns>The recommended join algorithm name ("Hash" or "NestedLoop").</returns>
    /// <remarks>
    /// Hash join is O(n+m) and preferred for larger datasets.
    /// Nested loop is O(n*m) but simpler and works for small datasets.
    /// </remarks>
    public string SelectJoinAlgorithm(long? leftCardinality, long? rightCardinality)
    {
        // If we don't have cardinality estimates, default to hash join
        if (!leftCardinality.HasValue || !rightCardinality.HasValue)
        {
            return "Hash";
        }

        // If both sources are very small (< 100 rows), nested loop is fine
        if (leftCardinality.Value < 100 && rightCardinality.Value < 100)
        {
            return "NestedLoop";
        }

        // Otherwise, use hash join
        return "Hash";
    }
}

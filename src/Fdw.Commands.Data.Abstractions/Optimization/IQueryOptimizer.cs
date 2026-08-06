using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Optimizes federated query execution.
/// </summary>
public interface IQueryOptimizer
{
    /// <summary>
    /// Determines the optimal execution order for data sources.
    /// </summary>
    /// <param name="sources">The data sources to optimize.</param>
    /// <returns>The sources in optimal execution order.</returns>
    IReadOnlyList<IDataSource> OptimizeSourceOrder(IReadOnlyList<IDataSource> sources);

    /// <summary>
    /// Pushes filter predicates down to individual data sources where possible.
    /// </summary>
    /// <param name="sources">The data sources.</param>
    /// <param name="globalFilter">The global filter expression.</param>
    /// <returns>Sources with optimized filters.</returns>
    IReadOnlyList<IDataSource> PushDownPredicates(
        IReadOnlyList<IDataSource> sources,
        IFilterExpression? globalFilter);

    /// <summary>
    /// Estimates the cardinality (number of rows) for a data source.
    /// </summary>
    /// <param name="source">The data source to estimate.</param>
    /// <returns>Estimated row count, or null if unknown.</returns>
    long? EstimateCardinality(IDataSource source);

    /// <summary>
    /// Selects the best join algorithm for the given data sources.
    /// </summary>
    /// <param name="leftCardinality">Estimated rows in left source.</param>
    /// <param name="rightCardinality">Estimated rows in right source.</param>
    /// <returns>The recommended join algorithm name ("Hash" or "NestedLoop").</returns>
    string SelectJoinAlgorithm(long? leftCardinality, long? rightCardinality);
}

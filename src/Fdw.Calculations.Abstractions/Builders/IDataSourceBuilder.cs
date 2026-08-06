using System;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring data sources.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface IDataSourceBuilder<TOutput>
{
    /// <summary>
    /// Sets the connection name for the data source.
    /// </summary>
    /// <param name="connectionName">The name of the connection.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceBuilder<TOutput> WithConnection(string connectionName);

    /// <summary>
    /// Sets the container/table name for the data source.
    /// </summary>
    /// <param name="containerName">The name of the container or table.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceBuilder<TOutput> FromContainer(string containerName);

    /// <summary>
    /// Adds a filter to the data source query.
    /// </summary>
    /// <param name="filterExpression">The filter expression.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceBuilder<TOutput> Where(string filterExpression);

    /// <summary>
    /// Joins another data source.
    /// </summary>
    /// <param name="configure">Action to configure the join.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceBuilder<TOutput> Join(Action<IDataSourceJoinBuilder<TOutput>> configure);
}

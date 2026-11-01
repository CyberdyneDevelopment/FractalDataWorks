using Fdw.Data.Abstractions;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring joins between data sources.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface IDataSourceJoinBuilder<TOutput>
{
    /// <summary>
    /// Sets the type of join (Inner, Left, Right, Full, Cross).
    /// </summary>
    /// <param name="joinType">The type of join.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceJoinBuilder<TOutput> WithJoinType(IJoinType joinType);

    /// <summary>
    /// Sets the connection name for the right-side data source.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceJoinBuilder<TOutput> WithConnection(string connectionName);

    /// <summary>
    /// Sets the container/table name for the right-side data source.
    /// </summary>
    /// <param name="containerName">The container name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceJoinBuilder<TOutput> FromContainer(string containerName);

    /// <summary>
    /// Sets the join condition (field equality).
    /// </summary>
    /// <param name="leftField">The field name from the left source.</param>
    /// <param name="rightField">The field name from the right source.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IDataSourceJoinBuilder<TOutput> On(string leftField, string rightField);
}

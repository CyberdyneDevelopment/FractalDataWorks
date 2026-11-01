namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring aggregation operations.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface IAggregationBuilder<TOutput>
{
    /// <summary>
    /// Sets the aggregation type by name (Sum, Average, Count, etc.).
    /// </summary>
    /// <param name="aggregationTypeName">The name of the aggregation type to perform.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IAggregationBuilder<TOutput> WithAggregationType(string aggregationTypeName);

    /// <summary>
    /// Sets the field/property name to aggregate.
    /// </summary>
    /// <param name="fieldName">The name of the field to aggregate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IAggregationBuilder<TOutput> OnField(string fieldName);

    /// <summary>
    /// Sets the field/property names to group by before aggregating.
    /// </summary>
    /// <param name="fields">The field names to group by.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IAggregationBuilder<TOutput> GroupBy(params string[] fields);

    /// <summary>
    /// Sets a filter expression to apply before aggregation.
    /// </summary>
    /// <param name="filterExpression">The filter expression.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IAggregationBuilder<TOutput> WithFilter(string filterExpression);
}

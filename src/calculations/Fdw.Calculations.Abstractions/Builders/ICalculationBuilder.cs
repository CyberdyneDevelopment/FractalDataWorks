using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for constructing calculations.
/// Provides a fluent API for building complex calculations with various operations.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface ICalculationBuilder<TOutput>
{
    /// <summary>
    /// Sets the name of the calculation.
    /// </summary>
    /// <param name="name">The calculation name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithName(string name);

    /// <summary>
    /// Sets the description of the calculation.
    /// </summary>
    /// <param name="description">The calculation description.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithDescription(string description);

    /// <summary>
    /// Configures aggregation operations for this calculation.
    /// </summary>
    /// <param name="configure">Action to configure aggregation settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithAggregation(Action<IAggregationBuilder<TOutput>> configure);

    /// <summary>
    /// Configures period comparison operations for this calculation.
    /// </summary>
    /// <param name="configure">Action to configure period comparison settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithPeriodComparison(Action<IPeriodComparisonBuilder<TOutput>> configure);

    /// <summary>
    /// Configures business rule evaluation for this calculation.
    /// </summary>
    /// <param name="configure">Action to configure business rule settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithBusinessRule(Action<IBusinessRuleBuilder<TOutput>> configure);

    /// <summary>
    /// Configures time-series operations for this calculation.
    /// </summary>
    /// <param name="configure">Action to configure time-series settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> WithTimeSeries(Action<ITimeSeriesBuilder<TOutput>> configure);

    /// <summary>
    /// Configures the data source for this calculation.
    /// </summary>
    /// <param name="configure">Action to configure data source settings.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ICalculationBuilder<TOutput> FromDataSource(Action<IDataSourceBuilder<TOutput>> configure);

    /// <summary>
    /// Builds the calculation instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the built calculation or validation errors.</returns>
    Task<IGenericResult<ICalculation<object, TOutput>>> Build(CancellationToken cancellationToken = default);
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration.Abstractions;

namespace Fdw.Calculations.Aggregations;

/// <summary>
/// Configuration for aggregation calculations.
/// Defines what data source to aggregate and how to perform the aggregation.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Configuration DTO with init-only properties")]
public sealed class AggregationConfiguration : ConfigurationBase<AggregationConfiguration>
{
    /// <summary>
    /// Gets or sets the name of the aggregation type to use.
    /// </summary>
    public string? AggregationTypeName { get; init; }

    /// <summary>
    /// Gets or sets the name of the field/property to aggregate.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets or sets the connection name for the data source.
    /// </summary>
    public string? ConnectionName { get; init; }

    /// <summary>
    /// Gets or sets the container/table name for the data source.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <summary>
    /// Gets or sets the metric name for this aggregation (used for identification/reporting).
    /// </summary>
    public string? MetricName { get; init; }

    /// <summary>
    /// Gets or sets the optional field names to group by before aggregating.
    /// </summary>
    public string[]? GroupByFields { get; init; }

    /// <summary>
    /// Gets or sets an optional filter expression to apply before aggregation.
    /// </summary>
    public string? FilterExpression { get; init; }

    /// <inheritdoc/>
    public override string SectionName => "Aggregation";

    /// <summary>
    /// Gets the service type (domain) for this configuration.
    /// </summary>
    public override string ServiceType => "Calculation";
}

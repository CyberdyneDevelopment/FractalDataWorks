using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// One aggregate of an Aggregate transformation.
/// Rows live in <c>transform.AggregateTransformationConfigurationAggregation</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AggregateTransformationAggregationConfiguration : IGenericConfiguration
{
    /// <inheritdoc/>
    /// <remarks>The table has no Id of its own; a row is identified by its owner and position.</remarks>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the input field aggregated.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the function: an AggregationFunctions option name.</summary>
    public string AggregateFunction { get; set; } = string.Empty;

    /// <summary>Gets or sets the field the aggregate is written to.</summary>
    public string OutputField { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate's position.</summary>
    public int ExecutionOrder { get; set; }
}

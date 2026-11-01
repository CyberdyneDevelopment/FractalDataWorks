using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Data.Transformations.Configuration;

/// <summary>
/// The Aggregate implementation of a Transformation: groups its one input and computes aggregates over each group; it needs its complete input.
/// Ports: input -> output. Rows live in <c>transform.AggregateTransformationConfiguration</c>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class AggregateTransformationConfiguration : ITransformationImplementationConfiguration
{
    /// <summary>The implementation name a Aggregate transformation's domain row carries.</summary>
    public const string ImplementationName = "Aggregate";

    /// <inheritdoc/>
    /// <remarks>Stamped by the provider from the domain row; the table has no Id of its own.</remarks>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public bool IsSystemProvided { get; set; }


    /// <summary>Gets or sets the fields the input is grouped by, in Ordinal order.</summary>
    /// <remarks>One row each in <c>transform.AggregateTransformationConfigurationGroupByField</c>.</remarks>
    [NotMapped]
    public IList<AggregateTransformationGroupByFieldConfiguration> GroupByFields { get; set; } = new List<AggregateTransformationGroupByFieldConfiguration>();

    /// <summary>Gets or sets the aggregates computed over each group.</summary>
    /// <remarks>One row each in <c>transform.AggregateTransformationConfigurationAggregation</c>.</remarks>
    [NotMapped]
    public IList<AggregateTransformationAggregationConfiguration> Aggregations { get; set; } = new List<AggregateTransformationAggregationConfiguration>();
}

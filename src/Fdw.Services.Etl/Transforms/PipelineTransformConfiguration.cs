using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Database-backed configuration for a pipeline transform step.
/// Child of EtlPipelineConfiguration (pipe.Pipeline table).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Pipeline")]
public sealed partial class PipelineTransformConfiguration : IGenericConfiguration
{

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/binding.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the logical FK to the parent EtlPipeline (column pipe.PipelineOperation.EtlPipelineId).
    /// </summary>
    /// <remarks>
    /// Why: set by the configuration cascade-save to the parent EtlPipeline body's logical Id; the
    /// configuration save translator resolves the physical EtlPipelineRowId via FK subquery on insert.
    /// </remarks>
    public Guid EtlPipelineId { get; set; }

    /// <inheritdoc/>
    public string SectionName { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ServiceType { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets or sets the operation type (Map, Filter, Calculate, Lookup, Aggregate).
    /// Maps to the pipe.PipelineOperation.OperationType column (FDW-389 rename); resolved at runtime
    /// against the TransformTypes registry.
    /// </summary>
    [ValuesFrom(typeof(TransformTypes))]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order within the pipeline.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Gets or sets whether this transform is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the filter expression (for Filter transforms).
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the field mappings (for Map transforms).
    /// </summary>
    public IList<PipelineTransformFieldMappingConfiguration> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the group-by fields (for Aggregate transforms).
    /// </summary>
    public IList<PipelineTransformGroupByFieldConfiguration> GroupByFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the aggregations to apply (for Aggregate transforms).
    /// </summary>
    public IList<PipelineTransformAggregationConfiguration> Aggregations { get; set; } = [];

    /// <summary>
    /// Gets or sets the calculations to apply (for Calculate transforms).
    /// </summary>
    public IList<PipelineTransformCalculationConfiguration> Calculations { get; set; } = [];

    /// <summary>
    /// Gets or sets the lookups to apply (for Lookup transforms) — one row per brought-across column.
    /// </summary>
    public IList<PipelineTransformLookupConfiguration> Lookups { get; set; } = [];
}

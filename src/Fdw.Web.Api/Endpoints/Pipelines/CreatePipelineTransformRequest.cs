using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body describing a single transform (operation) step on a create/update-pipeline request.
/// Maps onto <c>PipelineTransformConfiguration</c> (pipe.PipelineOperation), with <see cref="FieldMappings"/>,
/// <see cref="Aggregation"/>, <see cref="Lookup"/>, and <see cref="Calculation"/> persisting as its typed
/// cascade-child rows.
/// </summary>
/// <remarks>
/// Why: implements <see cref="ITransformOperationSpec"/> (FDW-556) so it flows directly into
/// <c>TransformTypeBase.MapSpecToConfiguration</c> — the per-option dispatch mechanism — without an
/// app-owned adapter type or a switch on <see cref="OperationType"/>.
/// </remarks>
public class CreatePipelineTransformRequest : ITransformOperationSpec
{
    /// <summary>
    /// Gets or sets the transform step name (display/identity within the pipeline).
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation/transform type (Map, Filter, Calculate, Lookup, Aggregate), resolved
    /// against <c>TransformTypes</c>.
    /// </summary>
    /// <remarks>
    /// Why: no silent "Map" default (FDW-556 Part 6.4) — the caller must name the operation explicitly;
    /// an absent/unresolvable value fails loud via <c>EtlLog.UnknownTransformType</c> at dispatch time
    /// rather than silently behaving as a field-mapping transform.
    /// </remarks>
    [Required]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order of this transform within the pipeline.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Gets or sets the field mappings for this transform (used by Map transforms).
    /// </summary>
    public IList<CreatePipelineFieldMappingRequest> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the aggregation parameters (used by Aggregate transforms).
    /// </summary>
    public AggregationRequest? Aggregation { get; set; }

    /// <summary>
    /// Gets or sets the lookup parameters (used by Lookup transforms).
    /// </summary>
    public LookupRequest? Lookup { get; set; }

    /// <summary>
    /// Gets or sets the calculation parameters (used by Calculate transforms).
    /// </summary>
    public CalculationRequest? Calculation { get; set; }

    /// <summary>
    /// Gets or sets the filter expression (used by Filter transforms).
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <inheritdoc />
    IReadOnlyList<IFieldMapping> ITransformOperationSpec.FieldMappings => [.. FieldMappings];

    /// <inheritdoc />
    IReadOnlyList<string> ITransformOperationSpec.GroupByFields => Aggregation?.GroupByFields ?? [];

    /// <inheritdoc />
    IReadOnlyList<IAggregationSpec> ITransformOperationSpec.Aggregations => Aggregation?.Aggregations ?? [];

    /// <inheritdoc />
    ILookupSpec? ITransformOperationSpec.Lookup => Lookup;

    /// <inheritdoc />
    IReadOnlyList<ICalculationSpec> ITransformOperationSpec.ComputedColumns => Calculation?.ComputedColumns ?? [];
}

using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Neutral, read-only surface for a single transform (operation) step, implemented by the HTTP
/// request DTO so the domain mapping mechanism (<c>ITransformOperationMapper</c> / per-option
/// <c>TransformTypeBase.MapSpecToConfiguration</c>) never depends on a specific web-layer type.
/// </summary>
public interface ITransformOperationSpec
{
    /// <summary>Gets the transform step name.</summary>
    string Name { get; }

    /// <summary>Gets the operation/transform type name (resolved against <c>TransformTypes</c>).</summary>
    string OperationType { get; }

    /// <summary>Gets the execution order of this transform within the pipeline.</summary>
    int ExecutionOrder { get; }

    /// <summary>Gets the field mappings (for Map transforms).</summary>
    IReadOnlyList<IFieldMapping> FieldMappings { get; }

    /// <summary>Gets the group-by field names (for Aggregate transforms).</summary>
    IReadOnlyList<string> GroupByFields { get; }

    /// <summary>Gets the aggregations to apply (for Aggregate transforms).</summary>
    IReadOnlyList<IAggregationSpec> Aggregations { get; }

    /// <summary>Gets the lookup configuration (for Lookup transforms).</summary>
    ILookupSpec? Lookup { get; }

    /// <summary>Gets the computed columns (for Calculate transforms).</summary>
    IReadOnlyList<ICalculationSpec> ComputedColumns { get; }

    /// <summary>Gets the filter expression (for Filter transforms).</summary>
    string? FilterExpression { get; }
}

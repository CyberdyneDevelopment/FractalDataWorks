using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas.Projection;

/// <summary>
/// Round-trip authoring state produced by <see cref="TransformConfigPayloadSerializer.FromConfigPayload"/>.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Mappings"/> (non-empty), <see cref="FilterExpression"/>,
/// <see cref="Aggregation"/>, <see cref="Calculation"/>, or <see cref="Lookup"/> is populated,
/// matching the OperationType the payload was parsed for — the others are left at their defaults.
/// </remarks>
public sealed class TransformAuthoringState
{
    /// <summary>
    /// Gets or sets the field mappings (populated for a <c>Map</c> operation type).
    /// </summary>
    public IReadOnlyList<PipelineFieldMappingClientRequest> Mappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the filter expression (populated for a <c>Filter</c> operation type).
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the aggregation parameters (populated for an <c>Aggregate</c> operation type).
    /// </summary>
    public AggregationClientRequest? Aggregation { get; set; }

    /// <summary>
    /// Gets or sets the calculation parameters (populated for a <c>Calculate</c> operation type).
    /// </summary>
    public CalculationClientRequest? Calculation { get; set; }

    /// <summary>
    /// Gets or sets the lookup parameters (populated for a <c>Lookup</c> operation type).
    /// </summary>
    public LookupClientRequest? Lookup { get; set; }
}

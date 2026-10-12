using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Editor-state payload for an aggregation definition during in-place workbench composition.
/// </summary>
public sealed class DataSetAggregationEditorPayload
{
    /// <summary>Gets or sets the unique identifier for this aggregation.</summary>
    public Guid AggregationId { get; set; }

    /// <summary>Gets or sets the output field name produced by the aggregation.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregation function applied to <see cref="SourceField"/>.</summary>
    public string AggregationFunction { get; set; } = "Sum";

    /// <summary>Gets or sets the source field that the aggregation function is applied to.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the fields used to group rows before aggregating.</summary>
    public IReadOnlyList<string> GroupByFields { get; set; } = [];

    /// <summary>Gets or sets an optional description of what the aggregation computes.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this aggregation can be removed from the working set.</summary>
    public bool CanRemove { get; set; }
}

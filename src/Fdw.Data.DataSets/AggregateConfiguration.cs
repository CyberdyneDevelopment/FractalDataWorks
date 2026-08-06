using System;
using System.Collections.Generic;

namespace Fdw.Data.DataSets;

/// <summary>
/// Designer-time model for an Aggregate node in the CalculatedDesigner graph.
/// Carries the group-by fields and the list of aggregate measures needed to
/// configure the node at graph-authoring time.
/// </summary>
/// <remarks>
/// Why: replaces the free-text <c>node.Configuration["AggregateSpec"]</c> pattern
/// with a strongly-typed POCO that serialises to/from JSON without custom converters.
/// The <see cref="Measures"/> list maps directly to B2's DataSetAggregateDefinition
/// persistence model: each measure carries a source field, aggregate function, and
/// output column name.
/// </remarks>
public sealed class AggregateConfiguration
{
    /// <summary>Gets or sets the designer-node identifier (matches the graph node Id).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the task-node identifier in the pipeline graph.</summary>
    public Guid TaskNodeId { get; set; }

    /// <summary>
    /// Gets or sets the field names to group by when computing aggregates.
    /// Must not be empty — compile validation rejects an Aggregate node with no group-by fields.
    /// </summary>
    public IList<string> GroupByFields { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the aggregate measures to compute.
    /// Must not be empty — compile validation rejects an Aggregate node with no measures.
    /// </summary>
    public IList<AggregateMeasure> Measures { get; set; } = new List<AggregateMeasure>();
}

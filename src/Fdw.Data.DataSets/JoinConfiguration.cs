using System;

namespace Fdw.Data.DataSets;

/// <summary>
/// Designer-time model for a Join node in the CalculatedDesigner graph.
/// Carries only the fields needed to configure the node at graph-authoring time;
/// audit and version-on-write columns live on the persisted
/// <see cref="DataSetJoinConfiguration"/> record, not here.
/// </summary>
/// <remarks>
/// Why: replaces the free-text <c>node.Configuration["LeftSourceName"]</c> pattern.
/// The structured POCO can be serialised via System.Text.Json or Newtonsoft.Json
/// without custom converters, enabling round-trip persistence through the pipeline
/// task Configuration dictionary.
/// </remarks>
public sealed class JoinConfiguration
{
    /// <summary>Gets or sets the designer-node identifier (matches the graph node Id).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the task-node identifier in the pipeline graph.</summary>
    public Guid TaskNodeId { get; set; }

    /// <summary>Gets or sets the name of the left source in the join.</summary>
    /// <remarks>References a DataSetSource.SourceName within the same DataSet.</remarks>
    public string LeftSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical field name in the left source to join on.</summary>
    public string LeftFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the right source in the join.</summary>
    /// <remarks>References a DataSetSource.SourceName within the same DataSet.</remarks>
    public string RightSourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the logical field name in the right source to join on.</summary>
    public string RightFieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of join operation.</summary>
    /// <value>One of: "Inner", "Left", "Right", "Full", or "Cross".</value>
    public string JoinType { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the join purpose and semantics.</summary>
    public string? Description { get; set; }
}

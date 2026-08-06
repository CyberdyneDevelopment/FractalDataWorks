using System;
using Fdw.Data;

namespace Fdw.Services.Etl.Projects.Lineage;

/// <summary>
/// Internal query record for reading <c>pipe.OrchestrationNode</c> rows for the lineage graph.
/// Replaces the v1 records: <c>ProjectLineageRecord</c>, <c>StageLineageRecord</c>, <c>StepLineageRecord</c>.
/// A single query on the recursive table returns all hierarchy levels; the
/// <see cref="NodeTypeId"/> discriminator is used to route each row to the correct
/// lineage node type.
/// </summary>
[GenerateMapper]
public partial class OrchestrationNodeLineageRecord
{
    /// <summary>Gets or sets the node logical identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the node type discriminator. Matches OrchestrationNodeTypes.</summary>
    public int NodeTypeId { get; set; }

    /// <summary>Gets or sets the logical identifier of the parent node. NULL for root nodes.</summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the node name (unique within sibling scope).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the ordinal position among siblings.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this node is enabled for execution.</summary>
    public bool IsEnabled { get; set; }
}

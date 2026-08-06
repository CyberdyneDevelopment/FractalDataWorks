using System;
using Fdw.Data;

namespace Fdw.Services.Etl.Projects.Lineage;

/// <summary>
/// Internal query record for reading <c>pipe.OrchestrationNodePipeline</c> rows for the lineage graph.
/// Represents the Node → Pipeline containment edge.
/// Replaces <c>StepPipelineLineageRecord</c> (v1 <c>pipe.StepPipeline</c>).
/// </summary>
[GenerateMapper]
public partial class OrchestrationNodePipelineLineageRecord
{
    /// <summary>Gets or sets the membership record identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical identifier of the parent node.</summary>
    public Guid NodeId { get; set; }

    /// <summary>Gets or sets the logical identifier of the contained pipeline.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the pipeline name (resolved from the membership name field).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display ordinal of this pipeline within the node.</summary>
    public int Ordinal { get; set; }
}

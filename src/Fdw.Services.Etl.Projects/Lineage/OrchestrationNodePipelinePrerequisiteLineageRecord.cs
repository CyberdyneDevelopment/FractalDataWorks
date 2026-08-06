using System;
using Fdw.Data;

namespace Fdw.Services.Etl.Projects.Lineage;

/// <summary>
/// Internal query record for reading <c>pipe.OrchestrationNodePipelinePrerequisite</c> rows for the lineage graph.
/// Represents the Pipeline → DependsOn → Pipeline edge within a leaf node.
/// Replaces <c>StepPipelinePrerequisiteLineageRecord</c> (v1 <c>pipe.StepPipelinePrerequisite</c>).
/// </summary>
[GenerateMapper]
public partial class OrchestrationNodePipelinePrerequisiteLineageRecord
{
    /// <summary>Gets or sets the prerequisite record identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the logical identifier of the pipeline that has the dependency.</summary>
    public Guid PipelineId { get; set; }

    /// <summary>Gets or sets the logical identifier of the pipeline that must run first.</summary>
    public Guid PrerequisitePipelineId { get; set; }

    /// <summary>Gets or sets the node logical identifier that owns this prerequisite.</summary>
    public Guid NodeId { get; set; }
}

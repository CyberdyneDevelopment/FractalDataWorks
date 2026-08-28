using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.OrchestrationNode</c> table.
/// Represents a single node in the recursive orchestration hierarchy
/// (Project → Stage → Step → SubStep → …).
/// The <see cref="NodeTypeId"/> discriminator identifies the semantic role of the node;
/// <c>ParentRowId</c> forms the self-FK that creates the recursive tree.
/// </summary>
/// <remarks>
/// <para>
/// Policy fields (StepFailurePolicy, StageFailurePolicy, MaxParallelPipelines,
/// RequireApprovalToRun, AllowResume, AllowCrossTenant) inherit downward through the tree.
/// NULL means "inherit from parent's effective value (or server default at root)."
/// A child may make a policy stricter but never more permissive than its parent's effective value.
/// </para>
/// <para>
/// // Why: ParentTableName is intentionally absent on the ManagedConfiguration attribute.
/// Self-FK parent linkage is expressed as data (the ParentRowId column), not as a structural
/// cascade. The cascade writer uses ParentTableName to determine join-table relationships;
/// for a self-referencing table the single-table INSERT path is the correct one.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Orchestration", ServiceType = "Node")]
public sealed partial class OrchestrationNodeConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this node.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the node type discriminator. References <see cref="TypeCollections.OrchestrationNodeTypes"/>.</summary>
    public int NodeTypeId { get; set; }


    /// <summary>
    /// Gets or sets the logical Id of the parent node (denormalized from ParentRowId).
    /// NULL for root nodes.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>Gets or sets the name of this node (unique within sibling scope).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "OrchestrationNodes";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "Node";

    /// <summary>Gets the service option type discriminator. Not applicable for generic nodes.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the optional description of this node.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the ordinal position among siblings (ascending execution order).</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether this node is enabled for execution.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the tenant identifier this node belongs to.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group identifier for RBAC.</summary>
    public Guid? VisibilityGroupId { get; set; }

    // ========================================
    // Policy fields (nullable — NULL = inherit)
    // ========================================

    /// <summary>
    /// Gets or sets what to do when a Step fails within a Stage.
    /// Values: "HaltStage" (stricter) or "ContinueStage". NULL = inherit.
    /// </summary>
    public string? StepFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets what to do when a Stage fails within a Project.
    /// Values: "HaltProject" (stricter) or "ContinueProject". NULL = inherit.
    /// </summary>
    public string? StageFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pipelines that may run in parallel within a Step.
    /// Lower value is stricter. NULL = inherit.
    /// </summary>
    public int? MaxParallelPipelines { get; set; }

    /// <summary>
    /// Gets or sets whether execution requires explicit approval before starting.
    /// true is stricter. NULL = inherit.
    /// </summary>
    public bool? RequireApprovalToRun { get; set; }

    /// <summary>
    /// Gets or sets whether a failed execution can be resumed from the last checkpoint.
    /// false is stricter. NULL = inherit.
    /// </summary>
    public bool? AllowResume { get; set; }

    /// <summary>
    /// Gets or sets whether pipelines from different tenants may be composed in this node.
    /// false is stricter. NULL = inherit.
    /// </summary>
    public bool? AllowCrossTenant { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy identifier for stage wrapping.
    /// References <c>settings.ResiliencyPolicy.Id</c>. NULL = inherit from parent (or server default).
    /// </summary>
    public Guid? ResiliencyPolicyId { get; set; }

    // ========================================
    // Navigation — populated by provider, not by source generator
    // ========================================

    /// <summary>Gets or sets the direct child nodes ordered by Ordinal.</summary>
    public IList<OrchestrationNodeConfiguration> Children { get; set; } = [];

    /// <summary>Gets or sets the pipeline memberships for this node (only meaningful when NodeTypeId = Step).</summary>
    public IList<OrchestrationNodePipelineMembershipConfiguration> PipelineMemberships { get; set; } = [];

    /// <summary>Gets or sets the pipeline prerequisites for this node (only meaningful when NodeTypeId = Step).</summary>
    public IList<OrchestrationNodePipelinePrerequisiteConfiguration> PipelinePrerequisites { get; set; } = [];
}

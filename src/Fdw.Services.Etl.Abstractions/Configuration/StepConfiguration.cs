using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.StageStep</c> table.
/// Represents an ordered unit within a Stage. A Step contains one or more Pipelines
/// that may run in parallel (bounded by MaxParallelPipelines) with an optional DAG
/// of prerequisites between those pipelines.
/// </summary>
/// <remarks>
/// Policy fields on Step can only be equal to or stricter than the parent Stage's effective policy.
/// NULL means "inherit from parent Stage's effective value."
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Project",
    ServiceType = "Step")]
public sealed partial class StepConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this step.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of this step.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "Projects";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "Step";

    /// <summary>Gets the service option type discriminator. Not applicable for steps.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the FK to the parent Stage logical identifier.
    /// Follows the {ParentTableName}ConfigurationId naming convention.
    /// </summary>
    public Guid ProjectStageConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this step within the parent stage.
    /// Steps in the same stage execute according to their prerequisite DAG and MaxParallelPipelines.
    /// </summary>
    public int Ordinal { get; set; }

    // ========================================
    // Policy fields (7 nullable — NULL = inherit from parent Stage effective)
    // ========================================

    /// <summary>
    /// Gets or sets what to do when a Pipeline fails within this Step.
    /// Values: "HaltStage" (stricter) or "ContinueStage". NULL = inherit.
    /// </summary>
    public string? StepFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets what to do when this Step's Stage fails.
    /// Values: "HaltProject" (stricter) or "ContinueProject". NULL = inherit.
    /// </summary>
    public string? StageFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of pipelines that may run in parallel within this step.
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
    /// Gets or sets whether pipelines from different tenants may be composed in this step.
    /// false is stricter. NULL = inherit.
    /// </summary>
    public bool? AllowCrossTenant { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy identifier for stage wrapping.
    /// References <c>settings.ResiliencyPolicy.Id</c>. NULL = inherit from parent Stage.
    /// </summary>
    public Guid? ResiliencyPolicyId { get; set; }

    /// <summary>Gets or sets the pipeline memberships belonging to this step.</summary>
    // Why: IList<T> required by IOptions binding — IReadOnlyList<T> would break deserialization.
    public IList<StepPipelineMembershipConfiguration> Pipelines { get; set; } = new List<StepPipelineMembershipConfiguration>();

    /// <summary>Gets or sets the prerequisite relationships between pipelines in this step.</summary>
    // Why: IList<T> required by IOptions binding — IReadOnlyList<T> would break deserialization.
    public IList<StepPipelinePrerequisiteConfiguration> Prerequisites { get; set; } = new List<StepPipelinePrerequisiteConfiguration>();
}

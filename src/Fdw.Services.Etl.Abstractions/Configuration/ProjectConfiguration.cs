using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.Project</c> table.
/// Represents the top-level orchestration unit: a Project contains ordered Stages,
/// each Stage contains ordered Steps, each Step contains one or more Pipelines.
/// </summary>
/// <remarks>
/// Policy fields (7 nullable columns) inherit downward through Stage → Step.
/// A child can make a policy stricter but never more permissive than its parent's effective value.
/// NULL means "inherit from parent effective value (or server default at root)."
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Project")]
public sealed partial class ProjectConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this project.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the unique name of this project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "Projects";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "Project";

    /// <summary>Gets the service option type discriminator. Not applicable for projects.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the optional description of this project.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this project is enabled for execution.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets the tenant identifier this project belongs to.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the visibility group identifier for RBAC.</summary>
    public Guid? VisibilityGroupId { get; set; }

    // ========================================
    // Policy fields (7 nullable — NULL = inherit from server defaults)
    // ========================================

    /// <summary>
    /// Gets or sets what to do when a Step fails within a Stage.
    /// Values: "HaltStage" (stricter) or "ContinueStage". NULL = inherit.
    /// </summary>
    public string? StepFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets what to do when a Stage fails within this Project.
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
    /// Gets or sets whether pipelines from different tenants may be composed in this project.
    /// false is stricter. NULL = inherit.
    /// </summary>
    public bool? AllowCrossTenant { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy identifier for stage wrapping.
    /// References <c>settings.ResiliencyPolicy.Id</c>. NULL = inherit from parent (or server default).
    /// </summary>
    public Guid? ResiliencyPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy name for UI and configuration-time lookup.
    /// Resolved to <see cref="ResiliencyPolicyId"/> at execution time. NULL = inherit.
    /// </summary>
    public string? ResiliencyPolicyName { get; set; }

    /// <summary>Gets or sets the ordered stages belonging to this project.</summary>
    // Why: IList<T> required by IOptions binding — IReadOnlyList<T> would break deserialization.
    public IList<StageConfiguration> Stages { get; set; } = new List<StageConfiguration>();
}

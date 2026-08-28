using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.ProjectStage</c> table.
/// Represents an ordered phase within a Project. Stage N+1 waits for ALL Steps of Stage N to complete.
/// </summary>
/// <remarks>
/// Policy fields on Stage can only be equal to or stricter than the Project's effective policy.
/// NULL means "inherit from parent Project's effective value."
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Project",
    ServiceType = "Stage")]
public sealed partial class StageConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this stage.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of this stage.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "Projects";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "Stage";

    /// <summary>Gets the service option type discriminator. Not applicable for stages.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the FK to the parent Project logical identifier.
    /// Follows the {ParentTableName}ConfigurationId naming convention.
    /// </summary>
    public Guid ProjectConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of this stage within the parent project.
    /// Stages execute in ascending ordinal order; Stage N+1 starts only after Stage N completes.
    /// </summary>
    public int Ordinal { get; set; }

    // ========================================
    // Policy fields (7 nullable — NULL = inherit from parent Project effective)
    // ========================================

    /// <summary>
    /// Gets or sets what to do when a Step fails within this Stage.
    /// Values: "HaltStage" (stricter) or "ContinueStage". NULL = inherit.
    /// </summary>
    public string? StepFailurePolicy { get; set; }

    /// <summary>
    /// Gets or sets what to do when this Stage fails within its Project.
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
    /// Gets or sets whether pipelines from different tenants may be composed in this stage.
    /// false is stricter. NULL = inherit.
    /// </summary>
    public bool? AllowCrossTenant { get; set; }

    /// <summary>
    /// Gets or sets the resiliency policy identifier for stage wrapping.
    /// References <c>settings.ResiliencyPolicy.Id</c>. NULL = inherit from parent Project.
    /// </summary>
    public Guid? ResiliencyPolicyId { get; set; }

    /// <summary>Gets or sets the ordered steps belonging to this stage.</summary>
    public IList<StepConfiguration> Steps { get; set; } = new List<StepConfiguration>();
}

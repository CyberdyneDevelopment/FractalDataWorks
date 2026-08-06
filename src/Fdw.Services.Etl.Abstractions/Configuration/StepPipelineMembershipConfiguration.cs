using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.StepPipeline</c> table.
/// Represents the membership of a Pipeline within a Step. No policy columns —
/// policy is owned at the Step level and applied to all member pipelines uniformly.
/// </summary>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Project",
    ServiceType = "StepPipeline")]
public sealed partial class StepPipelineMembershipConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this step-pipeline membership.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of this membership record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "Projects";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "StepPipeline";

    /// <summary>Gets the service option type discriminator. Not applicable here.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the FK to the parent Step (StageStep) logical identifier.
    /// Follows the {ParentTableName}ConfigurationId naming convention.
    /// </summary>
    public Guid StageStepConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the logical identifier of the Pipeline being assigned to this step.
    /// References <c>pipe.Pipeline.Id</c>.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the display ordinal of this pipeline within the step.
    /// Ordinal is informational — execution order is governed by the prerequisite DAG.
    /// </summary>
    public int Ordinal { get; set; }
}

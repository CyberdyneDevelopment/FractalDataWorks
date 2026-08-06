using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.StepPipelinePrerequisite</c> table.
/// Records a directed prerequisite edge: a Pipeline within a Step cannot start
/// until its prerequisite Pipeline within the same Step has completed successfully.
/// </summary>
/// <remarks>
/// Prerequisites are validated at save time to ensure:
/// (1) Both pipelines belong to the same Step.
/// (2) The prerequisite graph is acyclic (topological sort).
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Project",
    ServiceType = "StepPipelinePrerequisite")]
public sealed partial class StepPipelinePrerequisiteConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this prerequisite record.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of this prerequisite record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "Projects";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "StepPipelinePrerequisite";

    /// <summary>Gets the service option type discriminator. Not applicable here.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the FK to the parent Step (StageStep) logical identifier.
    /// Follows the {ParentTableName}ConfigurationId naming convention.
    /// </summary>
    public Guid StageStepConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the logical identifier of the Pipeline that has the prerequisite requirement.
    /// This pipeline cannot start until <see cref="PrerequisitePipelineId"/> completes.
    /// References <c>pipe.Pipeline.Id</c>.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the logical identifier of the Pipeline that must complete first.
    /// References <c>pipe.Pipeline.Id</c>.
    /// </summary>
    public Guid PrerequisitePipelineId { get; set; }
}

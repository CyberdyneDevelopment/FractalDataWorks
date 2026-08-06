using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;

namespace Fdw.Services.Etl.Projects.Abstractions.Configuration;

/// <summary>
/// ManagedConfiguration for the <c>pipe.OrchestrationNodePipeline</c> table.
/// Represents the membership of a Pipeline within an OrchestrationNode (Step-level node).
/// No policy columns — policy is owned at the parent node level and applied to all member pipelines uniformly.
/// </summary>
/// <remarks>
/// Replaces <c>StepPipelineMembershipConfiguration</c> (v1 pipe.StepPipeline table).
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration( ServiceCategory = "Orchestration",
    ServiceType = "NodePipeline")]
public sealed partial class OrchestrationNodePipelineMembershipConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this node-pipeline membership.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of this membership record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name for IOptions binding.</summary>
    public string SectionName => "OrchestrationNodes";

    /// <summary>Gets the service type discriminator.</summary>
    public string ServiceType => "NodePipeline";

    /// <summary>Gets the service option type discriminator. Not applicable here.</summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the FK to the parent OrchestrationNode logical identifier.
    /// Follows the {ParentTableName}ConfigurationId naming convention.
    /// </summary>
    public Guid OrchestrationNodeConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the logical identifier of the Pipeline being assigned to this node.
    /// References <c>pipe.Pipeline.Id</c>.
    /// </summary>
    public Guid PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the display ordinal of this pipeline within the node.
    /// Ordinal is informational — execution order is governed by the prerequisite DAG.
    /// </summary>
    public int Ordinal { get; set; }
}

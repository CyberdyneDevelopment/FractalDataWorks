using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a node's pipeline prerequisites, the <c>PipelinePrerequisites</c>
/// child of <see cref="OrchestrationNodeImplementationConfiguration"/>. Targets pipe.OrchestrationNodePipelinePrerequisite.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OrchestrationNodePipelinePrerequisite")]
public sealed class OrchestrationNodePipelinePrerequisiteConfigurationCommand : ConfigurationCommandBase<OrchestrationNodePipelinePrerequisiteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodePipelinePrerequisiteConfigurationCommand"/> class.</summary>
    public OrchestrationNodePipelinePrerequisiteConfigurationCommand() : base("OrchestrationNodePipelinePrerequisite") { }
}

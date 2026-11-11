using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for a node's pipeline memberships, the <c>PipelineMemberships</c>
/// child of <see cref="OrchestrationNodeImplementationConfiguration"/>. Targets pipe.OrchestrationNodePipeline.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OrchestrationNodePipeline")]
public sealed class OrchestrationNodePipelineMembershipConfigurationCommand : ConfigurationCommandBase<OrchestrationNodePipelineMembershipConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodePipelineMembershipConfigurationCommand"/> class.</summary>
    public OrchestrationNodePipelineMembershipConfigurationCommand() : base("OrchestrationNodePipeline") { }
}

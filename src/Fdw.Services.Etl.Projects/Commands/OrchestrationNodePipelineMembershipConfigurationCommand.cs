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
    /// <inheritdoc/>
    public OrchestrationNodePipelineMembershipConfigurationCommand() : base("OrchestrationNodePipeline") { }
}

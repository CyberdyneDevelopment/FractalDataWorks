using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for orchestration node pipeline memberships.
/// Targets the pipe.OrchestrationNodePipeline table.
/// </summary>
/// <remarks>
/// The container name and the C# type name differ here, which is exactly why the cascade reads it
/// from this declaration rather than deriving one: without this TypeOption the load and save of
/// <c>OrchestrationNodeConfiguration.PipelineMemberships</c> both fail loud with NoChildCommandForType.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "OrchestrationNodePipeline")]
public sealed class OrchestrationNodePipelineMembershipConfigurationCommand
    : ConfigurationCommandBase<OrchestrationNodePipelineMembershipConfiguration>
{
    /// <summary>Initializes the command targeting the OrchestrationNodePipeline table.</summary>
    public OrchestrationNodePipelineMembershipConfigurationCommand() : base("OrchestrationNodePipeline") { }
}

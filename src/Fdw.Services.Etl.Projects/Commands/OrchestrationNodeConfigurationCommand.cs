using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the OrchestrationNode configuration domain.
/// Targets the pipe.OrchestrationNode table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OrchestrationNode")]
public sealed class OrchestrationNodeConfigurationCommand : ConfigurationCommandBase<OrchestrationNodeConfiguration>
{
    /// <inheritdoc/>
    public OrchestrationNodeConfigurationCommand() : base("OrchestrationNode") { }
}

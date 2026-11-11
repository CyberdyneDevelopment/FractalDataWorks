using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the OrchestrationNode configuration domain.
/// Targets the pipe.OrchestrationNode table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "OrchestrationNode")]
public sealed class OrchestrationNodeConfigurationCommand : ConfigurationCommandBase<OrchestrationNodeImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="OrchestrationNodeConfigurationCommand"/> class.</summary>
    public OrchestrationNodeConfigurationCommand() : base("OrchestrationNode") { }
}

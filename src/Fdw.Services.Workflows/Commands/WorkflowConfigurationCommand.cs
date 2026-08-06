using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workflows.Commands;

/// <summary>ConfigurationCommands TypeOption for the Workflow configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Workflow")]
public sealed class WorkflowConfigurationCommand : ConfigurationCommandBase<WorkflowConfiguration>
{
    /// <inheritdoc/>
    public WorkflowConfigurationCommand() : base("Workflow") { }
}

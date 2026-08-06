using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the Project configuration domain.
/// Targets the pipe.Project table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "Project")]
public sealed class ProjectConfigurationCommand : ConfigurationCommandBase<ProjectConfiguration>
{
    /// <inheritdoc/>
    public ProjectConfigurationCommand() : base("Project") { }
}

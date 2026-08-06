using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the Stage configuration domain.
/// Targets the pipe.ProjectStage table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "Stage")]
public sealed class StageConfigurationCommand : ConfigurationCommandBase<StageConfiguration>
{
    /// <inheritdoc/>
    public StageConfigurationCommand() : base("ProjectStage") { }
}

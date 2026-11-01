using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>
/// ConfigurationCommands TypeOption for the Step configuration domain.
/// Targets the pipe.StageStep table.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "Step")]
public sealed class StepConfigurationCommand : ConfigurationCommandBase<StepConfiguration>
{
    /// <inheritdoc/>
    public StepConfigurationCommand() : base("StageStep") { }
}

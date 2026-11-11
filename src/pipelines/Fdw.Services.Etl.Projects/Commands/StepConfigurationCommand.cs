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
    /// <summary>Initializes a new instance of the <see cref="StepConfigurationCommand"/> class.</summary>
    public StepConfigurationCommand() : base("StageStep") { }
}

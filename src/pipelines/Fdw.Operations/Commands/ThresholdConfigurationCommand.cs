using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the Threshold configuration records (FDW-3936).</summary>
[TypeOption(typeof(ConfigurationCommands), "Threshold")]
public sealed class ThresholdConfigurationCommand : ConfigurationCommandBase<ThresholdImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdConfigurationCommand"/> class.</summary>
    public ThresholdConfigurationCommand() : base("Threshold") { }
}

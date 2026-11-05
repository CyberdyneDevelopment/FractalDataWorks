using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the ThresholdParameter configuration records (FDW-3935).</summary>
[TypeOption(typeof(ConfigurationCommands), "ThresholdParameter")]
public sealed class ThresholdParameterConfigurationCommand : ConfigurationCommandBase<ThresholdParameterConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdParameterConfigurationCommand"/> class.</summary>
    public ThresholdParameterConfigurationCommand() : base("ThresholdParameter") { }
}

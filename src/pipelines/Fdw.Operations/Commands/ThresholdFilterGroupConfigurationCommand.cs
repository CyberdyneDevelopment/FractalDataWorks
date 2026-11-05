using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the ThresholdFilterGroup configuration records (FDW-3936).</summary>
[TypeOption(typeof(ConfigurationCommands), "ThresholdFilterGroup")]
public sealed class ThresholdFilterGroupConfigurationCommand : ConfigurationCommandBase<ThresholdFilterGroupConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdFilterGroupConfigurationCommand"/> class.</summary>
    public ThresholdFilterGroupConfigurationCommand() : base("ThresholdFilterGroup") { }
}

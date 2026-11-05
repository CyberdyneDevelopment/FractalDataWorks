using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the ThresholdFilterCondition configuration records (FDW-3936).</summary>
[TypeOption(typeof(ConfigurationCommands), "ThresholdFilterCondition")]
public sealed class ThresholdFilterConditionConfigurationCommand : ConfigurationCommandBase<ThresholdFilterConditionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdFilterConditionConfigurationCommand"/> class.</summary>
    public ThresholdFilterConditionConfigurationCommand() : base("ThresholdFilterCondition") { }
}

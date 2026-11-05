using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the ThresholdRule configuration records (FDW-3936).</summary>
[TypeOption(typeof(ConfigurationCommands), "ThresholdRule")]
public sealed class ThresholdRuleConfigurationCommand : ConfigurationCommandBase<ThresholdRuleConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRuleConfigurationCommand"/> class.</summary>
    public ThresholdRuleConfigurationCommand() : base("ThresholdRule") { }
}

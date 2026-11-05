using Fdw.Collections.Attributes;
using Fdw.Operations.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Operations.Commands;

/// <summary>Names the table of the ThresholdRuleNotification configuration records (FDW-3936).</summary>
[TypeOption(typeof(ConfigurationCommands), "ThresholdRuleNotification")]
public sealed class ThresholdRuleNotificationConfigurationCommand : ConfigurationCommandBase<ThresholdRuleNotificationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdRuleNotificationConfigurationCommand"/> class.</summary>
    public ThresholdRuleNotificationConfigurationCommand() : base("ThresholdRuleNotification") { }
}

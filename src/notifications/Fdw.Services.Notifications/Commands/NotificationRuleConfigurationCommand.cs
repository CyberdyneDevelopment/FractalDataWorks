using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Commands;

/// <summary>ConfigurationCommands TypeOption for the NotificationRule configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "NotificationRule")]
public sealed class NotificationRuleConfigurationCommand : ConfigurationCommandBase<NotificationRuleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationRuleConfigurationCommand"/> class.</summary>
    public NotificationRuleConfigurationCommand() : base("NotificationRule") { }
}

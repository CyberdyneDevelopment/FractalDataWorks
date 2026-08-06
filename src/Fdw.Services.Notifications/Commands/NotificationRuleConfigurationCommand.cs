using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Notifications.Configuration;

namespace Fdw.Services.Notifications.Commands;

/// <summary>ConfigurationCommands TypeOption for the NotificationRule configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "NotificationRule")]
public sealed class NotificationRuleConfigurationCommand : ConfigurationCommandBase<NotificationRuleConfiguration>
{
    /// <inheritdoc/>
    public NotificationRuleConfigurationCommand() : base("NotificationRule") { }
}

using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Notifications.System.Commands;

/// <summary>ConfigurationCommands TypeOption for the SystemNotification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "SystemNotificationConfiguration")]
public sealed class SystemNotificationConfigurationCommand : ConfigurationCommandBase<SystemNotificationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SystemNotificationConfigurationCommand"/> class.</summary>
    public SystemNotificationConfigurationCommand() : base("SystemNotificationConfiguration") { }
}

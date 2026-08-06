using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Notifications.Commands;

/// <summary>ConfigurationCommands TypeOption for the Notification configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Notification")]
public sealed class NotificationConfigurationCommand : ConfigurationCommandBase<NotificationConfiguration>
{
    /// <inheritdoc/>
    public NotificationConfigurationCommand() : base("Notification") { }
}
